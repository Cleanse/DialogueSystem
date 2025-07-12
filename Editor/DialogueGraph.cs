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
                AddCharacterValidationIndicator(node, speechNode);
            }
            else if (dialogueNode is ChoiceNode choiceNode)
            {
                node.AddToClassList("choice-node");
                var inspector = GenerateChoiceInspector(serializedObject);
                node.extensionContainer.Add(inspector);
                node.topContainer.parent.Insert(0, GenerateDescription("Choice"));
                choiceNode.ValidateChoiceConnections();
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
            var inputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(string));
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
                    var outputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(string));
                    outputPort.portName = labels[i];
                    outputPort.userData = i;
                    node.outputContainer.Add(outputPort);
                }
            }

            node.RefreshExpandedState();
            node.RefreshPorts();
            node.SetPosition(new Rect(dialogueNode.position, Vector2.zero));

            // FIXED: Register callbacks first, then schedule binding
            RegisterNodeCallbacks(node, dialogueNode, serializedObject);
            
            // FIXED: Schedule binding after panel attachment
            node.schedule.Execute(() =>
            {
                if (node.panel != null && dialogueNode != null)
                {
                    try
                    {
                        node.Bind(serializedObject);
                    }
                    catch (System.Exception ex)
                    {
                        if (!ex.Message.Contains("DPI setting"))
                        {
                            Debug.LogWarning($"Failed to bind node {dialogueNode.name}: {ex.Message}");
                        }
                    }
                }
            }).ExecuteLater(10);

            return node;
        }
        
        void RegisterNodeCallbacks(Node node, DialogueNode dialogueNode, SerializedObject serializedObject)
        {
            // Add selection handling for inspector panel
            node.RegisterCallback((MouseDownEvent evt) =>
            {
                if (evt.button == 0 && dialogueNode != null)
                {
                    Selection.activeObject = dialogueNode;
                    SelectNodeInGraphView(node);
                }
            });

            // Schedule callback registration after panel attachment
            node.schedule.Execute(() =>
            {
                if (node.panel != null)
                {
                    RegisterTextFieldSelectionEvents(node);
                    RegisterPortSelectionEvents(node);
                }
            }).ExecuteLater(50);

            node.schedule.Execute(() =>
            {
                if (node.panel != null && dialogueNode != null)
                {
                    try
                    {
                        serializedObject.Update();
                    }
                    catch (System.Exception ex)
                    {
                        if (!ex.Message.Contains("DPI setting"))
                        {
                            Debug.LogWarning($"Error updating node {dialogueNode.name}: {ex.Message}");
                        }
                    }
                }
            }).Every(500);
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
        
        

        void SelectNodeInGraphView(Node node)
        {
            ClearSelection();
            AddToSelection(node);
            
            var dialogueNode = node.userData as DialogueNode;
            if (dialogueNode != null)
            {
                Selection.activeObject = dialogueNode;
            }
        }

        void RegisterTextFieldSelectionEvents(Node node)
        {
            if (node.panel == null) return;
            
            var textFields = node.Query<TextField>().ToList();
            var propertyFields = node.Query<PropertyField>().ToList();
            
            foreach (var propertyField in propertyFields.ToList())
            {
                var nestedTextFields = propertyField.Query<TextField>().ToList();
                textFields.AddRange(nestedTextFields);
                
                var nestedPropertyFields = propertyField.Query<PropertyField>().ToList();
                propertyFields.AddRange(nestedPropertyFields);
            }
            
            foreach (var textField in textFields)
            {
                textField.RegisterCallback((FocusInEvent _) => SelectNodeAndUpdateInspector(node));
                textField.RegisterCallback((MouseDownEvent evt) =>
                {
                    if (evt.button == 0) SelectNodeAndUpdateInspector(node);
                });
            }
            
            foreach (var propertyField in propertyFields)
            {
                propertyField.RegisterCallback((FocusInEvent _) => SelectNodeAndUpdateInspector(node));
                propertyField.RegisterCallback((MouseDownEvent evt) =>
                {
                    if (evt.button == 0) SelectNodeAndUpdateInspector(node);
                });
                
                propertyField.schedule.Execute(() =>
                {
                    if (propertyField.panel != null)
                    {
                        RegisterNestedElementEvents(propertyField, node);
                    }
                }).ExecuteLater(100);
            }
        }

        void RegisterNestedElementEvents(VisualElement parentElement, Node node)
        {
            if (parentElement.panel == null) return;
            
            parentElement.schedule.Execute(() =>
            {
                if (parentElement.panel != null)
                {
                    var nestedFields = parentElement.Query<TextField>().ToList();
                    foreach (var field in nestedFields)
                    {
                        if (field.userData == null)
                        {
                            field.userData = "events_registered";
                            field.RegisterCallback((FocusInEvent _) => SelectNodeAndUpdateInspector(node));
                            field.RegisterCallback((MouseDownEvent evt) =>
                            {
                                if (evt.button == 0) SelectNodeAndUpdateInspector(node);
                            });
                        }
                    }
                }
            }).ExecuteLater(50);
        }

        void RegisterPortSelectionEvents(Node node)
        {
            if (node.panel == null) return;
            
            var dialoguePorts = node.Query<Port>().ToList();
            
            foreach (var port in dialoguePorts)
            {
                port.RegisterCallback((MouseDownEvent evt) =>
                {
                    if (evt.button == 0) SelectNodeAndUpdateInspector(node);
                });
                
                port.RegisterCallback((PointerDownEvent _) => SelectNodeAndUpdateInspector(node));
            }
        }

        void SelectNodeAndUpdateInspector(Node node)
        {
            var dialogueNode = node.userData as DialogueNode;
            if (dialogueNode != null)
            {
                SelectNodeInGraphView(node);
                
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
            
            var databases = Resources.LoadAll<SpeakerDatabase>("Dialogue/CharacterDatabases");
            var characterNames = new List<string> { "None" };
            var characterLookup = new Dictionary<string, Character>();
            
            foreach (var database in databases)
            {
                if (database?.Characters != null)
                {
                    foreach (var character in database.Characters)
                    {
                        if (character != null && !characterLookup.ContainsKey(character.CharacterName))
                        {
                            characterNames.Add(character.CharacterName);
                            characterLookup[character.CharacterName] = character;
                        }
                    }
                }
            }
            
            var characterProperty = serializedObject.FindProperty("speakerCharacter");
            var currentCharacter = characterProperty.objectReferenceValue as Character;
            
            var dropdown = new DropdownField();
            dropdown.choices = characterNames;
            
            int currentIndex = 0;
            if (currentCharacter != null)
            {
                int foundIndex = characterNames.IndexOf(currentCharacter.CharacterName);
                if (foundIndex >= 0) currentIndex = foundIndex;
            }
            dropdown.index = currentIndex;
            
            dropdown.RegisterValueChangedCallback((ChangeEvent<string> evt) =>
            {
                container.schedule.Execute(() =>
                {
                    if (evt.newValue == "None")
                        characterProperty.objectReferenceValue = null;
                    else if (characterLookup.ContainsKey(evt.newValue))
                        characterProperty.objectReferenceValue = characterLookup[evt.newValue];
                    
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(node);
                    RefreshNodeDisplays();
                    
                    var graphNode = nodes.FirstOrDefault(n => (DialogueNode)n.userData == node);
                    if (graphNode?.panel != null)
                    {
                        graphNode.schedule.Execute(() =>
                        {
                            if (graphNode.panel != null)
                                RefreshNodeInspector(graphNode, node, serializedObject);
                        }).ExecuteLater(10);
                    }
                }).ExecuteLater(1);
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
            if (node is SpeechNode speechNode) character = speechNode.speakerCharacter;
            else if (node is ChoiceNode choiceNode) character = choiceNode.speakerCharacter;
            
            var portraitProperty = serializedObject.FindProperty("portraitName");
            
            if (character != null)
            {
                var dropdown = new DropdownField();
                var portraits = character.GetPortraitNames().ToList();
                dropdown.choices = portraits;
                
                var currentValue = string.IsNullOrEmpty(portraitProperty.stringValue) ? "Default" : portraitProperty.stringValue;
                int currentIndex = Math.Max(0, portraits.IndexOf(currentValue));
                if (currentIndex == -1)
                {
                    currentIndex = 0;
                    portraitProperty.stringValue = portraits[0];
                }
                dropdown.index = currentIndex;
                
                dropdown.RegisterValueChangedCallback((ChangeEvent<string> evt) =>
                {
                    container.schedule.Execute(() =>
                    {
                        portraitProperty.stringValue = evt.newValue;
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(node);
                    }).ExecuteLater(1);
                });
                
                dropdown.AddToClassList("unity-base-field__input");
                container.Add(dropdown);
            }
            else
            {
                var textField = new TextField();
                textField.bindingPath = "portraitName";
                textField.value = portraitProperty.stringValue;
                textField.AddToClassList("unity-base-field__input");
                container.Add(textField);
            }
            
            return container;
        }
        
        void RefreshNodeInspector(Node graphNode, DialogueNode node, SerializedObject serializedObject)
        {
            graphNode.extensionContainer.Clear();
            VisualElement newInspector = null;
            
            if (node is SpeechNode) newInspector = GenerateSpeechInspector(serializedObject);
            else if (node is ChoiceNode) newInspector = GenerateChoiceInspector(serializedObject);
            
            if (newInspector != null)
            {
                graphNode.extensionContainer.Add(newInspector);
                graphNode.schedule.Execute(() =>
                {
                    if (graphNode.panel != null) RegisterTextFieldSelectionEvents(graphNode);
                }).ExecuteLater(50);
            }
        }

        VisualElement GenerateSpeechInspector(SerializedObject obj)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-container");

            var databases = Resources.LoadAll<SpeakerDatabase>("Dialogue/CharacterDatabases");
            var hasCharacterDatabases = databases.Length > 0 && databases.Any(db => db?.Characters?.Count > 0);
            
            if (hasCharacterDatabases)
                container.AddToClassList("character-database-active");

            var speechNode = obj.targetObject as SpeechNode;

            var characterDropdown = CreateCharacterDropdown(obj, speechNode);
            container.Add(characterDropdown);

            var speakerField = new PropertyField(obj.FindProperty("speakerName")) { label = "Speaker" };
            speakerField.AddToClassList("speaker-name-field");
            container.Add(speakerField);

            var portraitDropdown = CreatePortraitDropdown(obj, speechNode);
            container.Add(portraitDropdown);

            var textField = new PropertyField(obj.FindProperty("dialogueText")) { label = "Text" };
            container.Add(textField);

            return container;
        }

        VisualElement GenerateChoiceInspector(SerializedObject obj)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-container");

            var databases = Resources.LoadAll<SpeakerDatabase>("Dialogue/CharacterDatabases");
            var hasCharacterDatabases = databases.Length > 0 && databases.Any(db => db?.Characters?.Count > 0);
            
            if (hasCharacterDatabases)
                container.AddToClassList("character-database-active");

            var choiceNode = obj.targetObject as ChoiceNode;

            var characterDropdown = CreateCharacterDropdown(obj, choiceNode);
            container.Add(characterDropdown);

            var speakerField = new PropertyField(obj.FindProperty("speakerName")) { label = "Speaker" };
            speakerField.AddToClassList("speaker-name-field");
            container.Add(speakerField);

            var portraitDropdown = CreatePortraitDropdown(obj, choiceNode);
            container.Add(portraitDropdown);

            var promptField = new PropertyField(obj.FindProperty("promptText")) { label = "Prompt" };
            container.Add(promptField);

            var choicesField = new PropertyField(obj.FindProperty("choiceTexts")) { label = "Choices" };
            container.Add(choicesField);

            return container;
        }

        VisualElement GenerateFunctionInspector(SerializedObject obj)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-container");

            var functionsField = new PropertyField(obj.FindProperty("m_Functions")) { label = "Functions" };
            container.Add(functionsField);
            
            return container;
        }

        VisualElement GenerateConditionalInspector(SerializedObject obj)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-container");

            var logicTypeField = new PropertyField(obj.FindProperty("logicType")) { label = "Logic" };
            container.Add(logicTypeField);

            var conditionsField = new PropertyField(obj.FindProperty("conditions")) { label = "Conditions" };
            container.Add(conditionsField);
            
            return container;
        }

        VisualElement GenerateVariableSetInspector(SerializedObject obj)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-container");

            var operationsField = new PropertyField(obj.FindProperty("operations")) { label = "Operations" };
            container.Add(operationsField);
            
            return container;
        }

        void CreateConnections(DialogueConnectionNode connectionNode)
        {
            var sourceNode = TryGetNodeByGuid(connectionNode.nodeId);
            if (sourceNode == null) return;

            var outputPorts = sourceNode.outputContainer.Children().OfType<Port>().ToList();

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
                    if (element is Edge edge) OnRemoveEdge(edge);
                    else if (element is Node node) OnRemoveNode(node);
                }
            }

            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                    OnCreateEdge(edge);
            }

            if (graphViewChange.movedElements != null)
            {
                foreach (var element in graphViewChange.movedElements)
                {
                    if (element is Node { userData: DialogueNode dialogueNode } node)
                    {
                        dialogueNode.position = node.GetPosition().position;
                        EditorUtility.SetDirty(dialogueNode);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            return graphViewChange;
        }

        void OnRemoveEdge(Edge edge)
        {
            var outputNode = edge.output.node.userData as DialogueConnectionNode;
            var inputNode = edge.input.node.userData as DialogueNode;

            if (outputNode != null && inputNode != null)
            {
                outputNode.RemoveConnection(inputNode.nodeId);
                EditorUtility.SetDirty(outputNode);
                SelectNodeAndUpdateInspector(edge.output.node);
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
                outputNode.SetConnectionAtIndex(inputNode.nodeId, outputIndex);
                EditorUtility.SetDirty(outputNode);
                SelectNodeAndUpdateInspector(edge.output.node);
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
            evt.menu.AppendAction("Create Speech Node", CreateSpeechNode, DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Create Choice Node", CreateChoiceNode, DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Create Function Node", CreateFunctionNode, DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Create Conditional Node", CreateConditionalNode, DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Create Variable Set Node", CreateVariableSetNode, DropdownMenuAction.AlwaysEnabled);

            evt.menu.AppendSeparator();

            // Character-specific node creation
            var speakerDatabase = GetDefaultSpeakerDatabase();
            if (speakerDatabase?.Characters?.Count > 0)
            {
                evt.menu.AppendAction("Create Speech Node with Character/", null, DropdownMenuAction.Status.Disabled);
                
                foreach (var character in speakerDatabase.Characters)
                {
                    if (character != null)
                    {
                        evt.menu.AppendAction($"Create Speech Node with Character/{character.CharacterName}",
                            (action) => CreateSpeechNodeWithCharacter(action, character), DropdownMenuAction.AlwaysEnabled);
                    }
                }
                
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Create Choice Node with Character/", null, DropdownMenuAction.Status.Disabled);
                
                foreach (var character in speakerDatabase.Characters)
                {
                    if (character != null)
                    {
                        evt.menu.AppendAction($"Create Choice Node with Character/{character.CharacterName}",
                            (action) => CreateChoiceNodeWithCharacter(action, character), DropdownMenuAction.AlwaysEnabled);
                    }
                }
                
                evt.menu.AppendSeparator();
            }

            var isNodeSelected = selection.Count == 1 && selection[0] is Node;
            if (isNodeSelected)
            {
                var node = (Node)selection[0];
                var dialogueNode = node.userData as DialogueNode;

                if (dialogueNode != null)
                {
                    var isStartNode = GraphAsset.startNodeId == dialogueNode.nodeId;
                    evt.menu.AppendAction("Set as Start Node", SetAsStartNode,
                        isStartNode ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                    
                    if (dialogueNode is SpeechNode || dialogueNode is ChoiceNode)
                    {
                        evt.menu.AppendSeparator();
                        
                        if (speakerDatabase?.Characters?.Count > 0)
                        {
                            evt.menu.AppendAction("Assign Character/", null, DropdownMenuAction.Status.Disabled);
                            
                            foreach (var character in speakerDatabase.Characters)
                            {
                                if (character != null)
                                {
                                    evt.menu.AppendAction($"Assign Character/{character.CharacterName}",
                                        _ => AssignCharacterToSelectedNode(character), DropdownMenuAction.AlwaysEnabled);
                                }
                            }
                            
                            evt.menu.AppendSeparator();
                            
                            bool hasCharacterAssignment = false;
                            if (dialogueNode is SpeechNode speechNode && speechNode.speakerCharacter != null)
                                hasCharacterAssignment = true;
                            else if (dialogueNode is ChoiceNode choiceNode && choiceNode.speakerCharacter != null)
                                hasCharacterAssignment = true;
                            
                            evt.menu.AppendAction("Remove Character Assignment", RemoveCharacterFromSelectedNode,
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

        // Node Creation Methods
        void CreateSpeechNode(DropdownMenuAction obj)
        {
            var position = MouseToContent(obj.eventInfo.localMousePosition);
            var speechNode = ScriptableObject.CreateInstance<SpeechNode>();
            speechNode.name = "Speech Node";
            speechNode.speakerName = "Speaker";
            speechNode.dialogueText = "Enter dialogue text...";
            speechNode.position = position;

            TryAutoAssignCharacter(speechNode);
            GraphAsset.AddNode(speechNode);
            var newNode = GenerateDialogueNode(speechNode);
            AddElement(newNode);

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

            TryAutoAssignCharacter(choiceNode);
            GraphAsset.AddNode(choiceNode);
            var newNode = GenerateDialogueNode(choiceNode);
            AddElement(newNode);

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

            Selection.activeObject = functionNode;
            SelectNodeAndUpdateInspector(newNode);
        }

        void CreateConditionalNode(DropdownMenuAction obj)
        {
            var position = MouseToContent(obj.eventInfo.localMousePosition);
            var conditionalNode = ScriptableObject.CreateInstance<ConditionalNode>();
            conditionalNode.name = "Conditional Node";
            conditionalNode.position = position;
            conditionalNode.AddCondition("variable_name", ComparisonType.Equals, "expected_value", VariableValueType.String);

            GraphAsset.AddNode(conditionalNode);
            var newNode = GenerateDialogueNode(conditionalNode);
            AddElement(newNode);

            Selection.activeObject = conditionalNode;
            SelectNodeAndUpdateInspector(newNode);
        }
        
        void CreateVariableSetNode(DropdownMenuAction obj)
        {
            var position = MouseToContent(obj.eventInfo.localMousePosition);
            var variableSetNode = ScriptableObject.CreateInstance<VariableSetNode>();
            variableSetNode.name = "Variable Set Node";
            variableSetNode.position = position;
            variableSetNode.AddOperation("variable_name", VariableOperationType.Set, "value", VariableValueType.String);

            GraphAsset.AddNode(variableSetNode);
            var newNode = GenerateDialogueNode(variableSetNode);
            AddElement(newNode);

            Selection.activeObject = variableSetNode;
            SelectNodeAndUpdateInspector(newNode);
        }

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

            Selection.activeObject = speechNode;
            SelectNodeAndUpdateInspector(newNode);
        }
        
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

            Selection.activeObject = choiceNode;
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
                selectedNode.RemoveFromClassList("no-character-warning");
                var warningIcon = selectedNode.Q<Label>("character-warning-icon");
                warningIcon?.parent?.Remove(warningIcon);
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
                
                if (dialogueNode is SpeechNode speechNodeForWarning)
                    AddCharacterValidationIndicator(selectedNode, speechNodeForWarning);
                else if (dialogueNode is ChoiceNode choiceNodeForWarning)
                    AddCharacterValidationIndicator(selectedNode, choiceNodeForWarning);
                
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
                    node.inputContainer.AddToClassList("hidden");
                else
                    node.inputContainer.RemoveFromClassList("hidden");

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
        
        public void RefreshNodeDisplays()
        {
            foreach (var node in nodes.ToList())
            {
                if (node.userData is DialogueNode dialogueNode)
                {
                    var titleLabel = node.titleContainer.Q<Label>("title-label");
                    if (titleLabel != null)
                        titleLabel.text = dialogueNode.name;

                    var serializedObject = new SerializedObject(dialogueNode);
                    
                    node.schedule.Execute(() =>
                    {
                        if (node.panel != null)
                        {
                            try
                            {
                                node.Bind(serializedObject);
                            }
                            catch (Exception ex)
                            {
                                if (!ex.Message.Contains("DPI setting"))
                                    Debug.LogWarning($"Error rebinding node {dialogueNode.name}: {ex.Message}");
                            }
                        }
                    }).ExecuteLater(10);

                    if (dialogueNode is ChoiceNode choiceNode)
                        RefreshChoiceNodePorts(node, choiceNode);
                    else if (dialogueNode is ConditionalNode conditionalNode)
                        RefreshConditionalNodePorts(node, conditionalNode);
                }
            }

            RefreshAllConnections();
        }

        void RefreshChoiceNodePorts(Node node, ChoiceNode choiceNode)
        {
            var outputPorts = node.outputContainer.Children().OfType<Port>().ToList();
            foreach (var port in outputPorts)
                node.outputContainer.Remove(port);

            var labels = choiceNode.GetConnectionLabels();
            for (int i = 0; i < labels.Length; i++)
            {
                var outputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(string));
                outputPort.portName = labels[i];
                outputPort.userData = i;
                node.outputContainer.Add(outputPort);
            }

            node.RefreshPorts();
        }
        
        void RefreshConditionalNodePorts(Node node, ConditionalNode conditionalNode)
        {
            var outputPorts = node.outputContainer.Children().OfType<Port>().ToList();
            foreach (var port in outputPorts)
                node.outputContainer.Remove(port);

            var labels = conditionalNode.GetConnectionLabels();
            for (int i = 0; i < labels.Length; i++)
            {
                var outputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(string));
                outputPort.portName = labels[i];
                outputPort.userData = i;
                node.outputContainer.Add(outputPort);
            }

            node.RefreshPorts();
        }

        void RefreshAllConnections()
        {
            var dialogueEdges = graphElements.OfType<Edge>().ToList();
            foreach (var edge in dialogueEdges)
                RemoveElement(edge);

            foreach (var dialogueNode in GraphAsset.nodes.OfType<DialogueConnectionNode>())
                CreateConnections(dialogueNode);
        }
        
        SpeakerDatabase GetDefaultSpeakerDatabase()
        {
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
            
            string[] allDatabaseGuids = AssetDatabase.FindAssets("t:SpeakerDatabase");
            if (allDatabaseGuids.Length > 0)
            {
                string databasePath = AssetDatabase.GUIDToAssetPath(allDatabaseGuids[0]);
                return AssetDatabase.LoadAssetAtPath<SpeakerDatabase>(databasePath);
            }
            
            return null;
        }
        
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
        
        void AddCharacterValidationIndicator(Node node, SpeechNode speechNode)
        {
            if (speechNode.speakerCharacter == null)
            {
                node.AddToClassList("no-character-warning");
                
                var warningIcon = new Label("⚠") 
                { 
                    name = "character-warning-icon",
                    tooltip = "No character assigned. Consider using 'Create Speech Node with Character' or assign a character manually."
                };
                warningIcon.AddToClassList("character-warning-icon");
                node.titleContainer.Add(warningIcon);
            }
        }
        
        void AddCharacterValidationIndicator(Node node, ChoiceNode choiceNode)
        {
            if (choiceNode.speakerCharacter == null)
            {
                node.AddToClassList("no-character-warning");
                
                var warningIcon = new Label("⚠") 
                { 
                    name = "character-warning-icon",
                    tooltip = "No character assigned. Consider using 'Create Choice Node with Character' or assign a character manually."
                };
                warningIcon.AddToClassList("character-warning-icon");
                node.titleContainer.Add(warningIcon);
            }
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