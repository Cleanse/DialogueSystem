#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem
{
    public class DialogueGraph : GraphView
    {
        public event Action<DialogueGraphAsset> OnGraphChanged;
        public DialogueGraphAsset GraphAsset { get; private set; }

        public DialogueGraph()
        {
            SetupZoom(0.75f, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        public void SetGraphAsset(DialogueGraphAsset asset)
        {
            if (asset == null)
                return;

            GraphAsset = asset;
            viewDataKey = asset.name;
            RefreshGraph();
        }

        void RefreshGraph()
        {
            graphViewChanged -= OnGraphViewChanged;
            ClearGraph();

            if (GraphAsset == null)
                return;

            // Create nodes for all dialogue nodes first
            foreach (var dialogueNode in GraphAsset.nodes)
            {
                if (dialogueNode != null)
                {
                    AddElement(GenerateDialogueNode(dialogueNode));
                }
            }

            // Create connections after all nodes exist
            foreach (var dialogueNode in GraphAsset.nodes.OfType<DialogueConnectionNode>())
            {
                CreateConnections(dialogueNode);
            }

            RefreshStartNode();

            // Frame all nodes
            schedule.Execute(() => FrameAll()).ExecuteLater(10);

            graphViewChanged += OnGraphViewChanged;
            OnGraphChanged?.Invoke(GraphAsset);
        }

        void ClearGraph()
        {
            DeleteElements(graphElements.ToList());
        }

        Node GenerateDialogueNode(DialogueNode dialogueNode)
        {
            var serializedObject = new SerializedObject(dialogueNode);
            var node = new Node
            {
                title = dialogueNode.name,
                viewDataKey = dialogueNode.nodeId,
                userData = dialogueNode
            };

            // Bind title to the name property for two-way sync
            var titleLabel = node.titleContainer.Q<Label>("title-label");
            if (titleLabel != null)
            {
                titleLabel.bindingPath = "m_Name";
            }

            // Add node-specific styling and inspectors
            if (dialogueNode is SpeechNode speechNode)
            {
                node.AddToClassList("speech-node");
                var inspector = GenerateSpeechInspector(serializedObject);
                node.extensionContainer.Add(inspector);
                node.topContainer.parent.Insert(0, GenerateDescription("Speech"));
                
                // Add validation indicator for character assignment
                AddCharacterValidationIndicator(node, speechNode);
            }
            else if (dialogueNode is ChoiceNode choiceNode)
            {
                node.AddToClassList("choice-node");
                var inspector = GenerateChoiceInspector(serializedObject);
                node.extensionContainer.Add(inspector);
                node.topContainer.parent.Insert(0, GenerateDescription("Choice"));

                // Ensure choice node connections are validated
                choiceNode.ValidateChoiceConnections();
                
                // Add validation indicator for character assignment
                AddCharacterValidationIndicator(node, choiceNode);
            }
            else if (dialogueNode is FunctionNode)
            {
                node.AddToClassList("function-node");
                var inspector = GenerateFunctionInspector(serializedObject);
                node.extensionContainer.Add(inspector);
                node.topContainer.parent.Insert(0, GenerateDescription("Function"));
            }
            else if (dialogueNode is ConditionalNode)
            {
                node.AddToClassList("conditional-node");
                var inspector = GenerateConditionalInspector(serializedObject);
                node.extensionContainer.Add(inspector);
                node.topContainer.parent.Insert(0, GenerateDescription("Conditional"));
            }
            else if (dialogueNode is VariableSetNode)
            {
                node.AddToClassList("variable-set-node");
                var inspector = GenerateVariableSetInspector(serializedObject);
                node.extensionContainer.Add(inspector);
                node.topContainer.parent.Insert(0, GenerateDescription("Variable Set"));
            }

            // Add input port
            var inputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi,
                typeof(string));
            inputPort.portName = "";
            node.inputContainer.Add(inputPort);

            // Hide input port if this is the start node
            var isStartNode = GraphAsset.startNodeId == dialogueNode.nodeId;
            if (isStartNode)
            {
                node.inputContainer.AddToClassList("hidden");
            }

            // Add output ports based on node type
            if (dialogueNode is DialogueConnectionNode connectionNode)
            {
                var labels = connectionNode.GetConnectionLabels();
                for (int i = 0; i < labels.Length; i++)
                {
                    var outputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output,
                        Port.Capacity.Single, typeof(string));
                    outputPort.portName = labels[i];
                    outputPort.userData = i; // Store the connection index
                    node.outputContainer.Add(outputPort);
                }
            }

            node.RefreshExpandedState();
            node.RefreshPorts();
            node.SetPosition(new Rect(dialogueNode.position, Vector2.zero));

            // Bind the serialized object to the node for automatic two-way sync
            node.Bind(serializedObject);

            // Add selection handling for inspector panel
            node.RegisterCallback((MouseDownEvent evt) =>
            {
                if (evt.button == 0) // Left click
                {
                    var nodeData = node.userData as DialogueNode;
                    if (nodeData != null)
                    {
                        // Use Unity's Selection system to communicate with inspector panel
                        Selection.activeObject = nodeData;
                        
                        // Also select the node in the graph view
                        SelectNodeInGraphView(node);
                    }
                }
            });

            // Add selection handling for text field interactions
            RegisterTextFieldSelectionEvents(node);
            
            // Add selection handling for port connection events
            RegisterPortSelectionEvents(node);

            // Schedule periodic binding refresh to ensure two-way sync (less frequent)
            node.schedule.Execute(() =>
            {
                if (dialogueNode != null)
                {
                    serializedObject.Update();
                }
            }).Every(500);

            return node;
        }

        void SelectNodeInGraphView(Node node)
        {
            // Clear current selection
            ClearSelection();
            
            // Add this node to selection
            AddToSelection(node);
            
            // Update the inspector with the selected node
            var dialogueNode = node.userData as DialogueNode;
            if (dialogueNode != null)
            {
                Selection.activeObject = dialogueNode;
            }
        }

        void RegisterTextFieldSelectionEvents(Node node)
        {
            // Find all TextField and PropertyField elements in the node (including nested ones)
            var textFields = node.Query<TextField>().ToList();
            var propertyFields = node.Query<PropertyField>().ToList();
            
            // Also search for nested elements within PropertyFields
            foreach (var propertyField in propertyFields.ToList())
            {
                var nestedTextFields = propertyField.Query<TextField>().ToList();
                textFields.AddRange(nestedTextFields);
                
                var nestedPropertyFields = propertyField.Query<PropertyField>().ToList();
                propertyFields.AddRange(nestedPropertyFields);
            }
            
            // Register focus events for TextFields
            foreach (var textField in textFields)
            {
                textField.RegisterCallback((FocusInEvent _) =>
                {
                    SelectNodeAndUpdateInspector(node);
                });
                
                textField.RegisterCallback((MouseDownEvent evt) =>
                {
                    if (evt.button == 0) // Left click
                    {
                        SelectNodeAndUpdateInspector(node);
                    }
                });
            }
            
            // Register focus events for PropertyFields
            foreach (var propertyField in propertyFields)
            {
                propertyField.RegisterCallback((FocusInEvent _) =>
                {
                    SelectNodeAndUpdateInspector(node);
                });
                
                propertyField.RegisterCallback((MouseDownEvent evt) =>
                {
                    if (evt.button == 0) // Left click
                    {
                        SelectNodeAndUpdateInspector(node);
                    }
                });
                
                // Also register for nested elements that might be added dynamically
                propertyField.RegisterCallback((AttachToPanelEvent _) =>
                {
                    RegisterNestedElementEvents(propertyField, node);
                });
            }
        }

        void RegisterNestedElementEvents(VisualElement parentElement, Node node)
        {
            // Use a delayed call to ensure nested elements are created
            parentElement.schedule.Execute(() =>
            {
                var nestedFields = parentElement.Query<TextField>().ToList();
                foreach (var field in nestedFields)
                {
                    // Check if we already registered events (to avoid duplicates)
                    if (field.userData == null)
                    {
                        field.userData = "events_registered";
                        
                        field.RegisterCallback((FocusInEvent _) =>
                        {
                            SelectNodeAndUpdateInspector(node);
                        });
                        
                        field.RegisterCallback((MouseDownEvent evt) =>
                        {
                            if (evt.button == 0)
                            {
                                SelectNodeAndUpdateInspector(node);
                            }
                        });
                    }
                }
            }).ExecuteLater(50);
        }

        void RegisterPortSelectionEvents(Node node)
        {
            // Find all ports in the node
            var dialoguePorts = node.Query<Port>().ToList();
            
            foreach (var port in dialoguePorts)
            {
                // Register mouse down events on ports for connection dragging
                port.RegisterCallback((MouseDownEvent evt) =>
                {
                    if (evt.button == 0) // Left click
                    {
                        SelectNodeAndUpdateInspector(node);
                    }
                });
                
                // Register for when connections are being made
                port.RegisterCallback((PointerDownEvent _) =>
                {
                    SelectNodeAndUpdateInspector(node);
                });
            }
        }

        void SelectNodeAndUpdateInspector(Node node)
        {
            var dialogueNode = node.userData as DialogueNode;
            if (dialogueNode != null)
            {
                // Select in graph view
                SelectNodeInGraphView(node);
                
                // Update inspector in the dialogue window if it exists
                var dialogueWindow = DialogueGraphWindow.s_Instance;
                if (dialogueWindow != null)
                {
                    dialogueWindow.m_SelectedNode = dialogueNode;
                    dialogueWindow.UpdateInspectorContent();
                }
            }
        }

        VisualElement CreateCharacterDropdown(SerializedObject serializedObject, DialogueNode node)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-field");
            
            var label = new Label("Character");
            label.AddToClassList("unity-base-field__label");
            container.Add(label);
            
            // Load all SpeakerDatabases from Resources
            var databases = Resources.LoadAll<SpeakerDatabase>("Dialogue/CharacterDatabases");
            var allCharacters = new List<Character>();
            var characterNames = new List<string> { "None" }; // Add None option
            var characterLookup = new Dictionary<string, Character>();
            
            // Collect all characters from all databases
            foreach (var database in databases)
            {
                if (database != null && database.Characters != null)
                {
                    foreach (var character in database.Characters)
                    {
                        if (character != null && !characterLookup.ContainsKey(character.CharacterName))
                        {
                            allCharacters.Add(character);
                            characterNames.Add(character.CharacterName);
                            characterLookup[character.CharacterName] = character;
                        }
                    }
                }
            }
            
            var characterProperty = serializedObject.FindProperty("speakerCharacter");
            var currentCharacter = characterProperty.objectReferenceValue as Character;
            
            // Create dropdown
            var dropdown = new DropdownField();
            dropdown.choices = characterNames;
            
            // Set current selection
            int currentIndex = 0; // Default to "None"
            if (currentCharacter != null)
            {
                int foundIndex = characterNames.IndexOf(currentCharacter.CharacterName);
                if (foundIndex >= 0)
                {
                    currentIndex = foundIndex;
                }
            }
            dropdown.index = currentIndex;
            
            // Handle value changes
            dropdown.RegisterValueChangedCallback((ChangeEvent<string> evt) =>
            {
                if (evt.newValue == "None")
                {
                    characterProperty.objectReferenceValue = null;
                }
                else if (characterLookup.ContainsKey(evt.newValue))
                {
                    characterProperty.objectReferenceValue = characterLookup[evt.newValue];
                }
                
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(node);
                
                // Refresh all node displays to update any affected nodes
                RefreshNodeDisplays();
                
                // Force inspector refresh to update portrait dropdown
                // Intentional reference comparison to find UI node containing this data object
                var graphNode = nodes.FirstOrDefault(n => (DialogueNode)n.userData == node);
                if (graphNode != null)
                {
                    // Clear and regenerate the inspector to update portrait dropdown
                    graphNode.extensionContainer.Clear();
                    VisualElement newInspector = null;
                    
                    if (node is SpeechNode speechNode)
                    {
                        newInspector = GenerateSpeechInspector(serializedObject);
                    }
                    else if (node is ChoiceNode choiceNode)
                    {
                        newInspector = GenerateChoiceInspector(serializedObject);
                    }
                    
                    if (newInspector != null)
                    {
                        graphNode.extensionContainer.Add(newInspector);
                    }
                }
            });
            
            dropdown.AddToClassList("unity-base-field__input");
            container.Add(dropdown);
            
            return container;
        }

        VisualElement CreatePortraitDropdown(SerializedObject serializedObject, DialogueNode node)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-field");
            
            var label = new Label("Portrait");
            label.AddToClassList("unity-base-field__label");
            container.Add(label);
            
            Character character = null;
            
            // Get the character reference based on node type
            if (node is SpeechNode speechNode)
            {
                character = speechNode.speakerCharacter;
            }
            else if (node is ChoiceNode choiceNode)
            {
                character = choiceNode.speakerCharacter;
            }
            
            var portraitProperty = serializedObject.FindProperty("portraitName");
            
            if (character != null)
            {
                // Create dropdown with portraits from character
                var dropdown = new DropdownField();
                var portraits = character.GetPortraitNames().ToList();
                dropdown.choices = portraits;
                
                // Set current value
                var currentValue = portraitProperty.stringValue;
                if (string.IsNullOrEmpty(currentValue))
                    currentValue = "Default";
                    
                int currentIndex = portraits.IndexOf(currentValue);
                if (currentIndex >= 0)
                {
                    dropdown.index = currentIndex;
                }
                else
                {
                    dropdown.index = 0; // Default to first option
                    portraitProperty.stringValue = portraits[0];
                }
                
                // Handle value changes
                dropdown.RegisterValueChangedCallback((ChangeEvent<string> evt) =>
                {
                    portraitProperty.stringValue = evt.newValue;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(node);
                });
                
                dropdown.AddToClassList("unity-base-field__input");
                container.Add(dropdown);
            }
            else
            {
                // No character assigned, show regular text field
                var textField = new TextField();
                textField.bindingPath = "portraitName";
                textField.value = portraitProperty.stringValue;
                textField.AddToClassList("unity-base-field__input");
                container.Add(textField);
            }
            
            return container;
        }

        VisualElement GenerateSpeechInspector(SerializedObject obj)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-container");

            // Check if character databases are populated
            var databases = Resources.LoadAll<SpeakerDatabase>("Dialogue/CharacterDatabases");
            var hasCharacterDatabases = databases.Length > 0 && databases.Any(db => db != null && db.Characters != null && db.Characters.Count > 0);
            
            if (hasCharacterDatabases)
            {
                container.AddToClassList("character-database-active");
            }

            var speechNode = obj.targetObject as SpeechNode;

            // Character dropdown from SpeakerDatabase
            var characterDropdown = CreateCharacterDropdown(obj, speechNode);
            container.Add(characterDropdown);

            var speakerField = new PropertyField(obj.FindProperty("speakerName"))
            {
                label = "Speaker"
            };
            speakerField.AddToClassList("speaker-name-field");
            container.Add(speakerField);

            // Portrait dropdown
            var portraitDropdown = CreatePortraitDropdown(obj, speechNode);
            container.Add(portraitDropdown);

            var textField = new PropertyField(obj.FindProperty("dialogueText"))
            {
                label = "Text"
            };
            container.Add(textField);

            return container;
        }

        VisualElement GenerateChoiceInspector(SerializedObject obj)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-container");

            // Check if character databases are populated
            var databases = Resources.LoadAll<SpeakerDatabase>("Dialogue/CharacterDatabases");
            var hasCharacterDatabases = databases.Length > 0 && databases.Any(db => db != null && db.Characters != null && db.Characters.Count > 0);
            
            if (hasCharacterDatabases)
            {
                container.AddToClassList("character-database-active");
            }

            var choiceNode = obj.targetObject as ChoiceNode;

            // Character dropdown from SpeakerDatabase
            var characterDropdown = CreateCharacterDropdown(obj, choiceNode);
            container.Add(characterDropdown);

            var speakerField = new PropertyField(obj.FindProperty("speakerName"))
            {
                label = "Speaker"
            };
            speakerField.AddToClassList("speaker-name-field");
            container.Add(speakerField);

            // Portrait dropdown
            var portraitDropdown = CreatePortraitDropdown(obj, choiceNode);
            container.Add(portraitDropdown);

            var promptField = new PropertyField(obj.FindProperty("promptText"))
            {
                label = "Prompt"
            };
            container.Add(promptField);

            // For choices in graph nodes, use a simpler PropertyField
            var choicesField = new PropertyField(obj.FindProperty("choiceTexts"))
            {
                label = "Choices"
            };
            container.Add(choicesField);

            return container;
        }

        VisualElement GenerateFunctionInspector(SerializedObject obj)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-container");

            var functionsField = new PropertyField(obj.FindProperty("m_Functions"))
            {
                label = "Functions"
            };
            container.Add(functionsField);
            
            return container;
        }

        VisualElement GenerateConditionalInspector(SerializedObject obj)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-container");

            var logicTypeField = new PropertyField(obj.FindProperty("logicType"))
            {
                label = "Logic"
            };
            container.Add(logicTypeField);

            var conditionsField = new PropertyField(obj.FindProperty("conditions"))
            {
                label = "Conditions"
            };
            container.Add(conditionsField);
            
            return container;
        }

        VisualElement GenerateVariableSetInspector(SerializedObject obj)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-container");

            var operationsField = new PropertyField(obj.FindProperty("operations"))
            {
                label = "Operations"
            };
            container.Add(operationsField);
            
            return container;
        }

        static VisualElement GenerateDescription(string description)
        {
            var container = new VisualElement { pickingMode = PickingMode.Ignore };
            container.AddToClassList("description-container");
            var label = new Label(description) { pickingMode = PickingMode.Ignore, name = "description-label" };
            label.AddToClassList("description-label");
            container.Add(label);
            return container;
        }

        void CreateConnections(DialogueConnectionNode connectionNode)
        {
            var sourceNode = TryGetNodeByGuid(connectionNode.nodeId);
            if (sourceNode == null) return;

            // Get all output ports from the source node
            var outputPorts = sourceNode.outputContainer.Children().OfType<Port>().ToList();

            // Create connections for each output port that has a target
            for (int i = 0; i < outputPorts.Count; i++)
            {
                var targetNodeId = connectionNode.GetConnectionAtIndex(i);
                if (string.IsNullOrEmpty(targetNodeId)) continue;

                var targetNode = TryGetNodeByGuid(targetNodeId);
                if (targetNode == null) continue;

                var outputPort = outputPorts[i];
                var inputPort = targetNode.inputContainer.Q<Port>();

                if (outputPort != null && inputPort != null)
                {
                    var edge = outputPort.ConnectTo<Edge>(inputPort);
                    AddElement(edge);
                }
            }
        }

        GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                foreach (var element in graphViewChange.elementsToRemove)
                {
                    OnRemoveElement(element);
                }
            }

            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    OnCreateEdge(edge);
                }
            }

            if (graphViewChange.movedElements != null)
            {
                foreach (var element in graphViewChange.movedElements)
                {
                    OnMoveElement(element);
                }
            }

            AssetDatabase.SaveAssets();
            return graphViewChange;
        }

        void OnRemoveElement(GraphElement element)
        {
            if (element is Edge edge)
            {
                OnRemoveEdge(edge);
            }
            else if (element is Node node)
            {
                OnRemoveNode(node);
            }
        }

        void OnRemoveEdge(Edge edge)
        {
            var outputNode = edge.output.node.userData as DialogueConnectionNode;
            var inputNode = edge.input.node.userData as DialogueNode;

            if (outputNode != null && inputNode != null)
            {
                // Remove the specific connection
                outputNode.RemoveConnection(inputNode.nodeId);
                EditorUtility.SetDirty(outputNode);
                
                // Select the source node when removing a connection
                var sourceNode = edge.output.node;
                SelectNodeAndUpdateInspector(sourceNode);
            }
        }

        void OnRemoveNode(Node node)
        {
            var dialogueNode = node.userData as DialogueNode;
            if (dialogueNode != null)
            {
                GraphAsset.RemoveNode(dialogueNode);
                EditorUtility.SetDirty(GraphAsset);
            }
        }

        void OnCreateEdge(Edge edge)
        {
            var outputNode = edge.output.node.userData as DialogueConnectionNode;
            var inputNode = edge.input.node.userData as DialogueNode;

            if (outputNode != null && inputNode != null)
            {
                var outputIndex = (int)(edge.output.userData ?? 0);

                // Set the connection at the specific index
                outputNode.SetConnectionAtIndex(inputNode.nodeId, outputIndex);
                EditorUtility.SetDirty(outputNode);
                
                // Select the source node when creating a connection
                var sourceNode = edge.output.node;
                SelectNodeAndUpdateInspector(sourceNode);
            }
        }

        static void OnMoveElement(GraphElement element)
        {
            if (element is Node { userData: DialogueNode dialogueNode } node)
            {
                dialogueNode.position = node.GetPosition().position;
                EditorUtility.SetDirty(dialogueNode);
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            foreach (var port in ports.ToList())
            {
                if (startPort == port) continue;
                if (startPort.node == port.node) continue;
                if (startPort.direction == port.direction) continue;

                compatiblePorts.Add(port);
            }

            return compatiblePorts;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (GraphAsset == null) return;

            // Standard node creation
            evt.menu.AppendAction("Create Speech Node",
                CreateSpeechNode, DropdownMenuAction.AlwaysEnabled);

            evt.menu.AppendAction("Create Choice Node",
                CreateChoiceNode, DropdownMenuAction.AlwaysEnabled);
            
            evt.menu.AppendAction("Create Function Node",
                CreateFunctionNode, DropdownMenuAction.AlwaysEnabled);

            evt.menu.AppendAction("Create Conditional Node",
                CreateConditionalNode, DropdownMenuAction.AlwaysEnabled);

            evt.menu.AppendAction("Create Variable Set Node",
                CreateVariableSetNode, DropdownMenuAction.AlwaysEnabled);

            evt.menu.AppendSeparator();
            
            // Character-specific node creation
            var speakerDatabase = GetDefaultSpeakerDatabase();
            if (speakerDatabase != null && speakerDatabase.Characters.Count > 0)
            {
                evt.menu.AppendAction("Create Speech Node with Character/", null, DropdownMenuAction.Status.Disabled);
                
                foreach (var character in speakerDatabase.Characters)
                {
                    if (character != null)
                    {
                        evt.menu.AppendAction($"Create Speech Node with Character/{character.CharacterName}",
                            (action) => CreateSpeechNodeWithCharacter(action, character), 
                            DropdownMenuAction.AlwaysEnabled);
                    }
                }
                
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Create Choice Node with Character/", null, DropdownMenuAction.Status.Disabled);
                
                foreach (var character in speakerDatabase.Characters)
                {
                    if (character != null)
                    {
                        evt.menu.AppendAction($"Create Choice Node with Character/{character.CharacterName}",
                            (action) => CreateChoiceNodeWithCharacter(action, character), 
                            DropdownMenuAction.AlwaysEnabled);
                    }
                }
                
                evt.menu.AppendSeparator();
            }

            evt.menu.AppendSeparator();

            var isNodeSelected = selection.Count == 1 && selection[0] is Node;

            if (isNodeSelected)
            {
                var node = (Node)selection[0];
                var dialogueNode = node.userData as DialogueNode;

                if (dialogueNode != null)
                {
                    var isStartNode = GraphAsset.startNodeId == dialogueNode.nodeId;
                    evt.menu.AppendAction("Set as Start Node",
                        SetAsStartNode,
                        isStartNode ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                    
                    // Add character assignment options for speech and choice nodes
                    if (dialogueNode is SpeechNode || dialogueNode is ChoiceNode)
                    {
                        evt.menu.AppendSeparator();
                        
                        var defaultSpeakerDatabase = GetDefaultSpeakerDatabase();
                        if (defaultSpeakerDatabase != null && defaultSpeakerDatabase.Characters.Count > 0)
                        {
                            evt.menu.AppendAction("Assign Character/", null, DropdownMenuAction.Status.Disabled);
                            
                            foreach (var character in defaultSpeakerDatabase.Characters)
                            {
                                if (character != null)
                                {
                                    evt.menu.AppendAction($"Assign Character/{character.CharacterName}",
                                        _ => AssignCharacterToSelectedNode(character), 
                                        DropdownMenuAction.AlwaysEnabled);
                                }
                            }
                            
                            evt.menu.AppendSeparator();
                            
                            // Check if current node has character assignment for remove option
                            bool hasCharacterAssignment = false;
                            if (dialogueNode is SpeechNode speechNode && speechNode.speakerCharacter != null)
                            {
                                hasCharacterAssignment = true;
                            }
                            else if (dialogueNode is ChoiceNode choiceNode && choiceNode.speakerCharacter != null)
                            {
                                hasCharacterAssignment = true;
                            }
                            
                            evt.menu.AppendAction("Remove Character Assignment",
                                RemoveCharacterFromSelectedNode,
                                hasCharacterAssignment ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                        }
                        else
                        {
                            evt.menu.AppendAction("No Speaker Database Found", null, DropdownMenuAction.Status.Disabled);
                        }
                    }
                }
            }
        }

        void CreateSpeechNode(DropdownMenuAction obj)
        {
            var position = MouseToContent(obj.eventInfo.localMousePosition);
            var speechNode = ScriptableObject.CreateInstance<SpeechNode>();
            speechNode.name = "Speech Node";
            speechNode.speakerName = "Speaker";
            speechNode.dialogueText = "Enter dialogue text...";
            speechNode.position = position;

            // Try to auto-assign character from speaker database
            TryAutoAssignCharacter(speechNode);

            GraphAsset.AddNode(speechNode);
            var newNode = GenerateDialogueNode(speechNode);
            AddElement(newNode);

            // Auto-select the new node for inspector
            Selection.activeObject = speechNode;
            SelectNodeAndUpdateInspector(newNode);
        }

        void CreateChoiceNode(DropdownMenuAction obj)
        {
            var position = MouseToContent(obj.eventInfo.localMousePosition);
            var choiceNode = ScriptableObject.CreateInstance<ChoiceNode>();
            choiceNode.name = "Choice Node";
            choiceNode.speakerName = "Speaker";
            choiceNode.promptText = "What do you want to say?";
            choiceNode.choiceTexts = new List<string> { "Choice 1", "Choice 2" };
            choiceNode.position = position;
            choiceNode.ValidateChoiceConnections();

            // Try to auto-assign character from speaker database
            TryAutoAssignCharacter(choiceNode);

            GraphAsset.AddNode(choiceNode);
            var newNode = GenerateDialogueNode(choiceNode);
            AddElement(newNode);

            // Auto-select the new node for inspector
            Selection.activeObject = choiceNode;
            SelectNodeAndUpdateInspector(newNode);
        }
        
        void CreateFunctionNode(DropdownMenuAction obj)
        {
            var position = MouseToContent(obj.eventInfo.localMousePosition);
            var functionNode = ScriptableObject.CreateInstance<FunctionNode>();
            functionNode.name = "Function Node";
            functionNode.Functions = new List<string> { "YourFunctionHere" };
            functionNode.position = position;

            GraphAsset.AddNode(functionNode);
            var newNode = GenerateDialogueNode(functionNode);
            AddElement(newNode);

            // Auto-select the new node for inspector
            Selection.activeObject = functionNode;
            SelectNodeAndUpdateInspector(newNode);
        }

        void CreateConditionalNode(DropdownMenuAction obj)
        {
            var position = MouseToContent(obj.eventInfo.localMousePosition);
            var conditionalNode = ScriptableObject.CreateInstance<ConditionalNode>();
            conditionalNode.name = "Conditional Node";
            conditionalNode.position = position;

            // Add a default condition
            conditionalNode.AddCondition("variable_name", ComparisonType.Equals, "expected_value", VariableValueType.String);

            GraphAsset.AddNode(conditionalNode);
            var newNode = GenerateDialogueNode(conditionalNode);
            AddElement(newNode);

            // Auto-select the new node for inspector
            Selection.activeObject = conditionalNode;
            SelectNodeAndUpdateInspector(newNode);
        }

        void CreateVariableSetNode(DropdownMenuAction obj)
        {
            var position = MouseToContent(obj.eventInfo.localMousePosition);
            var variableSetNode = ScriptableObject.CreateInstance<VariableSetNode>();
            variableSetNode.name = "Variable Set Node";
            variableSetNode.position = position;

            // Add a default operation
            variableSetNode.AddOperation("variable_name", VariableOperationType.Set, "value", VariableValueType.String);

            GraphAsset.AddNode(variableSetNode);
            var newNode = GenerateDialogueNode(variableSetNode);
            AddElement(newNode);

            // Auto-select the new node for inspector
            Selection.activeObject = variableSetNode;
            SelectNodeAndUpdateInspector(newNode);
        }

        void SetAsStartNode(DropdownMenuAction action)
        {
            var node = (Node)selection[0];
            var dialogueNode = node.userData as DialogueNode;

            if (dialogueNode != null)
            {
                GraphAsset.SetStartNode(dialogueNode.nodeId);
                RefreshStartNode();
            }
        }
        
        void AssignCharacterToSelectedNode(Character character)
        {
            if (selection.Count != 1 || !(selection[0] is Node selectedNode))
                return;
                
            var dialogueNode = selectedNode.userData as DialogueNode;
            bool hasChanges = false;
            
            if (dialogueNode is SpeechNode speechNode)
            {
                speechNode.speakerCharacter = character;
                speechNode.speakerName = character.CharacterName;
                speechNode.portraitName = "Default";
                hasChanges = true;
            }
            else if (dialogueNode is ChoiceNode choiceNode)
            {
                choiceNode.speakerCharacter = character;
                choiceNode.speakerName = character.CharacterName;
                choiceNode.portraitName = "Default";
                hasChanges = true;
            }
            
            if (hasChanges)
            {
                EditorUtility.SetDirty(dialogueNode);
                
                // Remove warning indicator and refresh node display
                selectedNode.RemoveFromClassList("no-character-warning");
                var warningIcon = selectedNode.Q<Label>("character-warning-icon");
                if (warningIcon != null)
                {
                    selectedNode.titleContainer.Remove(warningIcon);
                }
                
                RefreshNodeDisplays();
            }
        }
        
        void RemoveCharacterFromSelectedNode(DropdownMenuAction action)
        {
            if (selection.Count != 1 || !(selection[0] is Node selectedNode))
                return;
                
            var dialogueNode = selectedNode.userData as DialogueNode;
            bool hasChanges = false;
            
            if (dialogueNode is SpeechNode speechNode && speechNode.speakerCharacter != null)
            {
                speechNode.speakerCharacter = null;
                speechNode.portraitName = "Default";
                hasChanges = true;
            }
            else if (dialogueNode is ChoiceNode choiceNode && choiceNode.speakerCharacter != null)
            {
                choiceNode.speakerCharacter = null;
                choiceNode.portraitName = "Default";
                hasChanges = true;
            }
            
            if (hasChanges)
            {
                EditorUtility.SetDirty(dialogueNode);
                
                // Add warning indicator back
                if (dialogueNode is SpeechNode speechNodeForWarning)
                {
                    AddCharacterValidationIndicator(selectedNode, speechNodeForWarning);
                }
                else if (dialogueNode is ChoiceNode choiceNodeForWarning)
                {
                    AddCharacterValidationIndicator(selectedNode, choiceNodeForWarning);
                }
                
                RefreshNodeDisplays();
            }
        }

        void RefreshStartNode()
        {
            foreach (var node in nodes.ToList().Where(n => n.userData is DialogueNode))
            {
                var dialogueNode = (DialogueNode)node.userData;
                var isStartNode = GraphAsset.startNodeId == dialogueNode.nodeId;
                node.EnableInClassList("start-node", isStartNode);

                if (isStartNode)
                {
                    node.inputContainer.AddToClassList("hidden");
                }
                else
                {
                    node.inputContainer.RemoveFromClassList("hidden");
                }

                var descriptionLabel = node.contentContainer.Q<Label>("description-label");
                if (descriptionLabel != null)
                {
                    string nodeType = GetNodeTypeDisplayName(dialogueNode);
                    descriptionLabel.text = isStartNode ? $"Start {nodeType}" : nodeType;
                }
            }
        }

        string GetNodeTypeDisplayName(DialogueNode dialogueNode)
        {
            return dialogueNode switch
            {
                SpeechNode => "Speech",
                ChoiceNode => "Choice",
                FunctionNode => "Function",
                ConditionalNode => "Conditional",
                VariableSetNode => "Variable Set",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Refresh the visual display of nodes (called when inspector changes node data)
        /// </summary>
        public void RefreshNodeDisplays()
        {
            foreach (var node in nodes.ToList())
            {
                if (node.userData is DialogueNode dialogueNode)
                {
                    // Update the title
                    var titleLabel = node.titleContainer.Q<Label>("title-label");
                    if (titleLabel != null)
                    {
                        titleLabel.text = dialogueNode.name;
                    }

                    // Force the node to rebind its serialized object
                    var serializedObject = new SerializedObject(dialogueNode);
                    node.Bind(serializedObject);

                    // If this is a choice node, we might need to regenerate ports
                    if (dialogueNode is ChoiceNode choiceNode)
                    {
                        RefreshChoiceNodePorts(node, choiceNode);
                    }
                    // If this is a conditional node, we might need to regenerate ports
                    else if (dialogueNode is ConditionalNode conditionalNode)
                    {
                        RefreshConditionalNodePorts(node, conditionalNode);
                    }
                }
            }

            // Refresh connections to ensure they're still valid
            RefreshAllConnections();
        }

        void RefreshChoiceNodePorts(Node node, ChoiceNode choiceNode)
        {
            // Remove existing output ports
            var outputPorts = node.outputContainer.Children().OfType<Port>().ToList();
            foreach (var port in outputPorts)
            {
                node.outputContainer.Remove(port);
            }

            // Add new output ports based on current choice texts
            var labels = choiceNode.GetConnectionLabels();
            for (int i = 0; i < labels.Length; i++)
            {
                var outputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single,
                    typeof(string));
                outputPort.portName = labels[i];
                outputPort.userData = i;
                node.outputContainer.Add(outputPort);
            }

            node.RefreshPorts();
        }

        void RefreshConditionalNodePorts(Node node, ConditionalNode conditionalNode)
        {
            // Remove existing output ports
            var outputPorts = node.outputContainer.Children().OfType<Port>().ToList();
            foreach (var port in outputPorts)
            {
                node.outputContainer.Remove(port);
            }

            // Add new output ports based on conditional node labels
            var labels = conditionalNode.GetConnectionLabels();
            for (int i = 0; i < labels.Length; i++)
            {
                var outputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single,
                    typeof(string));
                outputPort.portName = labels[i];
                outputPort.userData = i;
                node.outputContainer.Add(outputPort);
            }

            node.RefreshPorts();
        }

        void RefreshAllConnections()
        {
            // Remove all existing edges
            var dialogueEdges = graphElements.OfType<Edge>().ToList();
            foreach (var edge in dialogueEdges)
            {
                RemoveElement(edge);
            }

            // Recreate all connections
            foreach (var dialogueNode in GraphAsset.nodes.OfType<DialogueConnectionNode>())
            {
                CreateConnections(dialogueNode);
            }
        }

        /// <summary>
        /// Get the default speaker database for character integration.
        /// </summary>
        SpeakerDatabase GetDefaultSpeakerDatabase()
        {
            // First, try to find a speaker database in the same folder as the current dialogue
            if (GraphAsset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(GraphAsset);
                string directory = System.IO.Path.GetDirectoryName(assetPath);
                string[] databaseGuids = AssetDatabase.FindAssets("t:SpeakerDatabase", new[] { directory });
                
                if (databaseGuids.Length > 0)
                {
                    string databasePath = AssetDatabase.GUIDToAssetPath(databaseGuids[0]);
                    return AssetDatabase.LoadAssetAtPath<SpeakerDatabase>(databasePath);
                }
            }
            
            // Fallback: find any speaker database in the project
            string[] allDatabaseGuids = AssetDatabase.FindAssets("t:SpeakerDatabase");
            if (allDatabaseGuids.Length > 0)
            {
                string databasePath = AssetDatabase.GUIDToAssetPath(allDatabaseGuids[0]);
                return AssetDatabase.LoadAssetAtPath<SpeakerDatabase>(databasePath);
            }
            
            return null;
        }
        
        /// <summary>
        /// Try to auto-assign a character to a speech node based on speaker name.
        /// </summary>
        void TryAutoAssignCharacter(SpeechNode speechNode)
        {
            var speakerDatabase = GetDefaultSpeakerDatabase();
            if (speakerDatabase != null && !string.IsNullOrEmpty(speechNode.speakerName))
            {
                var character = speakerDatabase.GetCharacter(speechNode.speakerName);
                if (character != null)
                {
                    speechNode.speakerCharacter = character;
                    speechNode.portraitName = "Default";
                }
            }
        }
        
        /// <summary>
        /// Try to auto-assign a character to a choice node based on speaker name.
        /// </summary>
        void TryAutoAssignCharacter(ChoiceNode choiceNode)
        {
            var speakerDatabase = GetDefaultSpeakerDatabase();
            if (speakerDatabase != null && !string.IsNullOrEmpty(choiceNode.speakerName))
            {
                var character = speakerDatabase.GetCharacter(choiceNode.speakerName);
                if (character != null)
                {
                    choiceNode.speakerCharacter = character;
                    choiceNode.portraitName = "Default";
                }
            }
        }
        
        /// <summary>
        /// Add character validation indicator to speech nodes.
        /// </summary>
        void AddCharacterValidationIndicator(Node node, SpeechNode speechNode)
        {
            if (speechNode.speakerCharacter == null)
            {
                node.AddToClassList("no-character-warning");
                
                // Add warning icon to the node
                var warningIcon = new Label("⚠") 
                { 
                    name = "character-warning-icon",
                    tooltip = "No character assigned. Consider using 'Create Speech Node with Character' or assign a character manually."
                };
                warningIcon.AddToClassList("character-warning-icon");
                node.titleContainer.Add(warningIcon);
            }
        }
        
        /// <summary>
        /// Add character validation indicator to choice nodes.
        /// </summary>
        void AddCharacterValidationIndicator(Node node, ChoiceNode choiceNode)
        {
            if (choiceNode.speakerCharacter == null)
            {
                node.AddToClassList("no-character-warning");
                
                // Add warning icon to the node
                var warningIcon = new Label("⚠") 
                { 
                    name = "character-warning-icon",
                    tooltip = "No character assigned. Consider using 'Create Choice Node with Character' or assign a character manually."
                };
                warningIcon.AddToClassList("character-warning-icon");
                node.titleContainer.Add(warningIcon);
            }
        }
        
        /// <summary>
        /// Create a speech node with a pre-assigned character.
        /// </summary>
        void CreateSpeechNodeWithCharacter(DropdownMenuAction obj, Character character)
        {
            var position = MouseToContent(obj.eventInfo.localMousePosition);
            var speechNode = ScriptableObject.CreateInstance<SpeechNode>();
            speechNode.name = $"Speech Node - {character.CharacterName}";
            speechNode.speakerName = character.CharacterName;
            speechNode.speakerCharacter = character;
            speechNode.portraitName = "Default";
            speechNode.dialogueText = "Enter dialogue text...";
            speechNode.position = position;

            GraphAsset.AddNode(speechNode);
            var newNode = GenerateDialogueNode(speechNode);
            AddElement(newNode);

            // Auto-select the new node for inspector
            Selection.activeObject = speechNode;
            SelectNodeAndUpdateInspector(newNode);
        }
        
        /// <summary>
        /// Create a choice node with a pre-assigned character.
        /// </summary>
        void CreateChoiceNodeWithCharacter(DropdownMenuAction obj, Character character)
        {
            var position = MouseToContent(obj.eventInfo.localMousePosition);
            var choiceNode = ScriptableObject.CreateInstance<ChoiceNode>();
            choiceNode.name = $"Choice Node - {character.CharacterName}";
            choiceNode.speakerName = character.CharacterName;
            choiceNode.speakerCharacter = character;
            choiceNode.portraitName = "Default";
            choiceNode.promptText = "What do you want to say?";
            choiceNode.choiceTexts = new List<string> { "Choice 1", "Choice 2" };
            choiceNode.position = position;
            choiceNode.ValidateChoiceConnections();

            GraphAsset.AddNode(choiceNode);
            var newNode = GenerateDialogueNode(choiceNode);
            AddElement(newNode);

            // Auto-select the new node for inspector
            Selection.activeObject = choiceNode;
            SelectNodeAndUpdateInspector(newNode);
        }

        Vector2 MouseToContent(Vector2 position)
        {
            position.x = (position.x - contentViewContainer.worldBound.x) / scale;
            position.y = (position.y - contentViewContainer.worldBound.y) / scale;
            return position;
        }

        Node TryGetNodeByGuid(string guid)
        {
            return nodes.ToList().FirstOrDefault(n => n.viewDataKey == guid);
        }
    }
}
#endif