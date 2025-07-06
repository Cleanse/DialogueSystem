#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem
{
    /// <summary>
    /// Editor window for creating and editing DialogueGraphAsset objects with built-in inspector.
    /// </summary>
    public class DialogueGraphWindow : EditorWindow
    {
        public static DialogueGraphWindow s_Instance;
        public DialogueNode m_SelectedNode;
        
        VisualElement m_GraphViewPane;
        VisualElement m_InspectorPane;
        ScrollView m_InspectorScrollView;
        TwoPaneSplitView m_SplitView;
        DialogueGraphAsset m_LastGraphAsset;
        
        const float INSPECTOR_MIN_WIDTH = 300f;
        const float INSPECTOR_DEFAULT_WIDTH = 400f;
        
        [MenuItem("Window/Dialogue System/Graph Window")]
        public static void Init()
        {
            if (s_Instance == null)
            {
                s_Instance = GetWindow<DialogueGraphWindow>();
                s_Instance.titleContent = new GUIContent("Dialogue Graph");
                s_Instance.minSize = new Vector2(800, 600);
            }
        }
        
        void CreateGUI()
        {
            var root = rootVisualElement;
            
            // Load stylesheet - try package path first, then Assets path for development
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Packages/com.insomniaguildgames.dialoguesystem/Editor/DialogueEditorStyles.uss");
            
            // Fallback to Assets path for development builds
            if (styleSheet == null)
            {
                styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/DialogueSystem/Editor/DialogueEditorStyles.uss");
            }
            
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
            
            CreateSplitView(root);
            
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }
        
        void OnDestroy()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }
        
        void CreateSplitView(VisualElement root)
        {
            // Calculate initial inspector width based on current window size
            var currentWindowWidth = position.width;
            var inspectorWidth = Mathf.Clamp(INSPECTOR_DEFAULT_WIDTH, INSPECTOR_MIN_WIDTH, 
                currentWindowWidth * 0.4f);
            
            // Create split view with proper orientation and initial sizes
            m_SplitView = new TwoPaneSplitView(
                1,
                inspectorWidth,
                TwoPaneSplitViewOrientation.Horizontal
            );
            
            m_SplitView.StretchToParentSize();
            root.Add(m_SplitView);
            
            // Create panes
            CreateGraphPane();
            CreateInspectorPane();
            
            SetupSplitViewStyling();
        }
        
        void SetupSplitViewStyling()
        {
            EditorApplication.delayCall += () =>
            {
                if (s_Instance == null || s_Instance.m_SplitView == null || s_Instance.m_GraphViewPane == null)
                    return;
                    
                var dragger = s_Instance.m_SplitView.Q("unity-dragline-anchor");
                if (dragger != null)
                {
                    dragger.style.width = 4;

                    if (s_Instance?.m_GraphViewPane != null && s_Instance.m_GraphViewPane.worldBound.width > 0)
                    {
                        var leftOffset = s_Instance.m_GraphViewPane.worldBound.width;
                        dragger.style.left = leftOffset;
                    }
                    
                    dragger.style.backgroundColor = new Color(0.3f, 0.8f, 0.9f, 0.6f);

                    // Add hover effect using Unity 6 callback syntax
                    dragger.RegisterCallback<MouseEnterEvent>((MouseEnterEvent evt) =>
                    {
                        dragger.style.backgroundColor = new Color(0.3f, 0.8f, 0.9f, 0.9f);
                    });

                    dragger.RegisterCallback<MouseLeaveEvent>((MouseLeaveEvent evt) =>
                    {
                        dragger.style.backgroundColor = new Color(0.3f, 0.8f, 0.9f, 0.6f);
                    });
                }
            };
        }
        
        void CreateGraphPane()
        {
            m_GraphViewPane = new VisualElement();
            m_GraphViewPane.AddToClassList("graph-view-pane");
            m_GraphViewPane.style.flexGrow = 1;
            m_GraphViewPane.style.minWidth = 300; // Ensure graph view has minimum width
            
            var graphView = new DialogueGraph();
            graphView.StretchToParentSize();
            
            m_GraphViewPane.Add(graphView);
            
            var toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");
            
            var titleLabel = new Label("Dialogue Graph Editor");
            titleLabel.AddToClassList("toolbar-title");
            toolbar.Add(titleLabel);
            
            var validateButton = new Button(OnValidateClicked)
            {
                text = "Validate Graph"
            };
            validateButton.AddToClassList("toolbar-button");
            toolbar.Add(validateButton);
            
            m_GraphViewPane.Add(toolbar);
            
            graphView.OnGraphChanged += (graph) =>
            {
                m_LastGraphAsset = graph;
            };
            
            m_GraphViewPane.SetEnabled(false);
            
            // Load last graph
            if (m_LastGraphAsset)
            {
                graphView.SetGraphAsset(m_LastGraphAsset);
                m_GraphViewPane.SetEnabled(true);
            }
            
            m_SplitView.Add(m_GraphViewPane);
        }
        
        void CreateInspectorPane()
        {
            m_InspectorPane = new VisualElement();
            m_InspectorPane.AddToClassList("inspector-pane");
            
            // Set explicit size constraints
            m_InspectorPane.style.minWidth = INSPECTOR_MIN_WIDTH;
            m_InspectorPane.style.width = INSPECTOR_DEFAULT_WIDTH;
            
            // Ensure the inspector pane doesn't shrink
            m_InspectorPane.style.flexShrink = 0;
            
            // Inspector header
            var inspectorHeader = new VisualElement();
            inspectorHeader.AddToClassList("inspector-header");
            
            var headerLabel = new Label("Inspector");
            headerLabel.AddToClassList("inspector-header-label");
            inspectorHeader.Add(headerLabel);
            
            m_InspectorPane.Add(inspectorHeader);
            
            // Scrollable inspector content
            m_InspectorScrollView = new ScrollView();
            m_InspectorScrollView.AddToClassList("inspector-scroll-view");
            m_InspectorScrollView.style.flexGrow = 1;
            m_InspectorPane.Add(m_InspectorScrollView);
            
            // Show default content
            ShowDefaultInspector();
            
            m_SplitView.Add(m_InspectorPane);
        }
        
        public void UpdateInspectorContent()
        {
            // Add null check for inspector scroll view
            if (m_InspectorScrollView == null)
                return;
                
            m_InspectorScrollView.Clear();
            
            if (m_SelectedNode == null)
            {
                ShowDefaultInspector();
                return;
            }
            
            // Create inspector content based on node type
            if (m_SelectedNode is SpeechNode speechNode)
            {
                ShowSpeechNodeInspector(speechNode);
            }
            else if (m_SelectedNode is ChoiceNode choiceNode)
            {
                ShowChoiceNodeInspector(choiceNode);
            }
            else if (m_SelectedNode is FunctionNode functionNode)
            {
                ShowFunctionNodeInspector(functionNode);
            }
            else if (m_SelectedNode is ConditionalNode conditionalNode)
            {
                ShowConditionalNodeInspector(conditionalNode);
            }
            else if (m_SelectedNode is VariableSetNode variableSetNode)
            {
                ShowVariableSetNodeInspector(variableSetNode);
            }
        }
        
        void ShowDefaultInspector()
        {
            // Add null check for inspector scroll view
            if (m_InspectorScrollView == null)
                return;
                
            var container = new VisualElement();
            container.AddToClassList("inspector-content");
            
            if (m_LastGraphAsset != null)
            {
                var graphLabel = new Label("Dialogue Graph");
                graphLabel.AddToClassList("inspector-section-header");
                container.Add(graphLabel);
                
                var nodeCount = m_LastGraphAsset.nodes?.Count() ?? 0;
                var nodeCountLabel = new Label($"Total Nodes: {nodeCount}");
                nodeCountLabel.AddToClassList("inspector-info-label");
                container.Add(nodeCountLabel);
                
                var startNodeLabel = new Label($"Start Node: {GetStartNodeName()}");
                startNodeLabel.AddToClassList("inspector-info-label");
                container.Add(startNodeLabel);
            }
            else
            {
                var helpLabel = new Label("No dialogue graph selected.\nSelect a graph to begin editing.");
                helpLabel.AddToClassList("inspector-help-label");
                container.Add(helpLabel);
            }
            
            m_InspectorScrollView.Add(container);
        }
        
        void ShowSpeechNodeInspector(SpeechNode speechNode)
        {
            if (m_InspectorScrollView == null)
                return;
                
            var container = new VisualElement();
            container.AddToClassList("inspector-content");
            
            // Check if character databases are populated
            var databases = Resources.LoadAll<SpeakerDatabase>("Dialogue/CharacterDatabases");
            var hasCharacterDatabases = databases.Length > 0 && databases.Any(db => db != null && db.Characters != null && db.Characters.Count > 0);
            
            if (hasCharacterDatabases)
            {
                container.AddToClassList("character-database-active");
            }
            
            var serializedObject = new SerializedObject(speechNode);
            
            // Header
            var header = new Label("Speech Node");
            header.AddToClassList("inspector-section-header");
            container.Add(header);
            
            // Node name with App UI TextField
            var nameField = new UnityEngine.UIElements.TextField();
            nameField.label = "Node Name";
            nameField.value = speechNode.name;
            nameField.RegisterCallback<ChangeEvent<string>>((ChangeEvent<string> evt) =>
            {
                speechNode.name = evt.newValue;
                EditorUtility.SetDirty(speechNode);
                MarkGraphDirty();
                RefreshGraphView();
            });
            nameField.AddToClassList("inspector-field");
            container.Add(nameField);
            
            // Speaker Character dropdown from SpeakerDatabase
            var speakerCharacterDropdown = CreateCharacterDropdown(serializedObject, speechNode);
            container.Add(speakerCharacterDropdown);
            
            // Speaker name with binding - using standard TextField
            var speakerField = new TextField("Speaker Name");
            speakerField.bindingPath = "speakerName";
            speakerField.AddToClassList("inspector-field");
            speakerField.AddToClassList("speaker-name-field");
            container.Add(speakerField);
            
            // Portrait dropdown - create custom implementation
            var portraitContainer = CreatePortraitDropdown(serializedObject, speechNode);
            container.Add(portraitContainer);
            
            // Dialogue text with PropertyField for better binding
            var textField = new PropertyField(serializedObject.FindProperty("dialogueText"), "Dialogue Text");
            textField.style.minHeight = 100;
            textField.style.maxHeight = 300;
            textField.AddToClassList("inspector-field");
            textField.AddToClassList("inspector-text-area");
            container.Add(textField);
            
            // Node info section
            AddNodeInfoSection(container, speechNode);
            
            // Bind to enable two-way synchronization
            container.Bind(serializedObject);
            
            // Schedule updates for dynamic content
            container.schedule.Execute(() =>
            {
                if (speechNode != null)
                {
                    serializedObject.Update();
                }
            }).Every(1000);
            
            m_InspectorScrollView.Add(container);
        }
        
        void ShowChoiceNodeInspector(ChoiceNode choiceNode)
        {
            if (m_InspectorScrollView == null)
                return;
                
            var container = new VisualElement();
            container.AddToClassList("inspector-content");
            
            // Check if character databases are populated
            var databases = Resources.LoadAll<SpeakerDatabase>("Dialogue/CharacterDatabases");
            var hasCharacterDatabases = databases.Length > 0 && databases.Any(db => db != null && db.Characters != null && db.Characters.Count > 0);
            
            if (hasCharacterDatabases)
            {
                container.AddToClassList("character-database-active");
            }
            
            var serializedObject = new SerializedObject(choiceNode);
            
            // Header
            var header = new Label("Choice Node");
            header.AddToClassList("inspector-section-header");
            container.Add(header);
            
            // Node name with App UI TextField
            var nameField = new UnityEngine.UIElements.TextField();
            nameField.label = "Node Name";
            nameField.value = choiceNode.name;
            nameField.RegisterCallback<ChangeEvent<string>>((ChangeEvent<string> evt) =>
            {
                choiceNode.name = evt.newValue;
                EditorUtility.SetDirty(choiceNode);
                MarkGraphDirty();
                RefreshGraphView();
            });
            nameField.AddToClassList("inspector-field");
            container.Add(nameField);
            
            // Speaker Character dropdown from SpeakerDatabase
            var speakerCharacterDropdown = CreateCharacterDropdown(serializedObject, choiceNode);
            container.Add(speakerCharacterDropdown);
            
            // Speaker name with binding - using standard TextField
            var speakerField = new TextField("Speaker Name");
            speakerField.bindingPath = "speakerName";
            speakerField.AddToClassList("inspector-field");
            speakerField.AddToClassList("speaker-name-field");
            container.Add(speakerField);
            
            // Portrait dropdown - create custom implementation
            var portraitContainer = CreatePortraitDropdown(serializedObject, choiceNode);
            container.Add(portraitContainer);
            
            // Prompt text with PropertyField for better binding
            var promptField = new PropertyField(serializedObject.FindProperty("promptText"), "Prompt Text");
            promptField.style.minHeight = 80;
            promptField.style.maxHeight = 200;
            promptField.AddToClassList("inspector-field");
            promptField.AddToClassList("inspector-text-area");
            container.Add(promptField);
            
            // Choices section with PropertyField for automatic list handling
            var choicesLabel = new Label("Choices");
            choicesLabel.AddToClassList("inspector-section-header");
            container.Add(choicesLabel);
            
            var choicesField = new PropertyField(serializedObject.FindProperty("choiceTexts"));
            choicesField.style.minHeight = 100;
            choicesField.style.maxHeight = 200;
            choicesField.AddToClassList("inspector-field");
            
            // Handle list changes to validate connections and refresh graph
            choicesField.RegisterCallback<SerializedPropertyChangeEvent>((SerializedPropertyChangeEvent evt) =>
            {
                if (evt.changedProperty.propertyPath.StartsWith("choiceTexts"))
                {
                    choiceNode.ValidateChoiceConnections();
                    EditorUtility.SetDirty(choiceNode);
                    MarkGraphDirty();
                    RefreshGraphView();
                }
            });
            
            container.Add(choicesField);
            
            // Node info section
            AddNodeInfoSection(container, choiceNode);
            
            // Bind for automatic synchronization of bound fields
            container.Bind(serializedObject);
            
            // Schedule updates for dynamic content
            container.schedule.Execute(() =>
            {
                if (choiceNode != null)
                {
                    serializedObject.Update();
                }
            }).Every(1000);
            
            m_InspectorScrollView.Add(container);
        }
        
        void ShowFunctionNodeInspector(FunctionNode functionNode)
        {
            if (m_InspectorScrollView == null)
                return;
                
            var container = new VisualElement();
            container.AddToClassList("inspector-content");
            
            var serializedObject = new SerializedObject(functionNode);
            
            // Header
            var header = new Label("Function Node");
            header.AddToClassList("inspector-section-header");
            container.Add(header);
            
            // Node name with App UI TextField
            var nameField = new UnityEngine.UIElements.TextField();
            nameField.label = "Node Name";
            nameField.value = functionNode.name;
            nameField.RegisterCallback<ChangeEvent<string>>((ChangeEvent<string> evt) =>
            {
                functionNode.name = evt.newValue;
                EditorUtility.SetDirty(functionNode);
                MarkGraphDirty();
                RefreshGraphView();
            });
            nameField.AddToClassList("inspector-field");
            container.Add(nameField);
            
            // Functions section with PropertyField for automatic list handling
            var functionsLabel = new Label("Functions");
            functionsLabel.AddToClassList("inspector-section-header");
            container.Add(functionsLabel);
            
            // Functions section with PropertyField for automatic list handling
            var functionsField = new PropertyField(serializedObject.FindProperty("m_Functions"));
            functionsField.style.minHeight = 100;
            functionsField.style.maxHeight = 200;
            functionsField.AddToClassList("inspector-field");
            
            // Handle list changes to validate connections and refresh graph
            functionsField.RegisterCallback<SerializedPropertyChangeEvent>((SerializedPropertyChangeEvent evt) =>
            {
                if (evt.changedProperty.propertyPath.StartsWith("m_Functions"))
                {
                    EditorUtility.SetDirty(functionNode);
                    MarkGraphDirty();
                    RefreshGraphView();
                }
            });
            
            container.Add(functionsField);
            
            // Node info section
            AddNodeInfoSection(container, functionNode);
            
            // Bind for automatic synchronization of bound fields
            container.Bind(serializedObject);
            
            // Schedule updates for dynamic content
            container.schedule.Execute(() =>
            {
                if (functionNode != null)
                {
                    serializedObject.Update();
                }
            }).Every(1000);
            
            m_InspectorScrollView.Add(container);
        }
        
        void ShowConditionalNodeInspector(ConditionalNode conditionalNode)
        {
            if (m_InspectorScrollView == null)
                return;
                
            var container = new VisualElement();
            container.AddToClassList("inspector-content");
            
            var serializedObject = new SerializedObject(conditionalNode);
            
            // Header
            var header = new Label("Conditional Node");
            header.AddToClassList("inspector-section-header");
            container.Add(header);
            
            // Node name with TextField
            var nameField = new UnityEngine.UIElements.TextField();
            nameField.label = "Node Name";
            nameField.value = conditionalNode.name;
            nameField.RegisterCallback<ChangeEvent<string>>((ChangeEvent<string> evt) =>
            {
                conditionalNode.name = evt.newValue;
                EditorUtility.SetDirty(conditionalNode);
                MarkGraphDirty();
                RefreshGraphView();
            });
            nameField.AddToClassList("inspector-field");
            container.Add(nameField);
            
            // Logic Type Field
            var logicTypeField = new PropertyField(serializedObject.FindProperty("logicType"), "Logic Type");
            logicTypeField.AddToClassList("inspector-field");
            container.Add(logicTypeField);
            
            // Conditions section
            var conditionsLabel = new Label("Conditions");
            conditionsLabel.AddToClassList("inspector-section-header");
            container.Add(conditionsLabel);
            
            var conditionsField = new PropertyField(serializedObject.FindProperty("conditions"));
            conditionsField.style.minHeight = 120;
            conditionsField.style.maxHeight = 250;
            conditionsField.AddToClassList("inspector-field");
            
            // Handle condition changes
            conditionsField.RegisterCallback<SerializedPropertyChangeEvent>((SerializedPropertyChangeEvent evt) =>
            {
                if (evt.changedProperty.propertyPath.StartsWith("conditions"))
                {
                    EditorUtility.SetDirty(conditionalNode);
                    MarkGraphDirty();
                    RefreshGraphView();
                }
            });
            
            container.Add(conditionsField);
            
            // Debug options
            var debugLabel = new Label("Debug Options");
            debugLabel.AddToClassList("inspector-section-header");
            container.Add(debugLabel);
            
            var logEvaluationField = new PropertyField(serializedObject.FindProperty("logEvaluation"), "Log Evaluation");
            logEvaluationField.AddToClassList("inspector-field");
            container.Add(logEvaluationField);
            
            // Node info section
            AddNodeInfoSection(container, conditionalNode);
            
            // Bind for automatic synchronization
            container.Bind(serializedObject);
            
            // Schedule updates for dynamic content
            container.schedule.Execute(() =>
            {
                if (conditionalNode != null)
                {
                    serializedObject.Update();
                }
            }).Every(1000);
            
            m_InspectorScrollView.Add(container);
        }
        
        void ShowVariableSetNodeInspector(VariableSetNode variableSetNode)
        {
            if (m_InspectorScrollView == null)
                return;
                
            var container = new VisualElement();
            container.AddToClassList("inspector-content");
            
            var serializedObject = new SerializedObject(variableSetNode);
            
            // Header
            var header = new Label("Variable Set Node");
            header.AddToClassList("inspector-section-header");
            container.Add(header);
            
            // Node name with TextField
            var nameField = new UnityEngine.UIElements.TextField();
            nameField.label = "Node Name";
            nameField.value = variableSetNode.name;
            nameField.RegisterCallback<ChangeEvent<string>>((ChangeEvent<string> evt) =>
            {
                variableSetNode.name = evt.newValue;
                EditorUtility.SetDirty(variableSetNode);
                MarkGraphDirty();
                RefreshGraphView();
            });
            nameField.AddToClassList("inspector-field");
            container.Add(nameField);
            
            // Operations section
            var operationsLabel = new Label("Variable Operations");
            operationsLabel.AddToClassList("inspector-section-header");
            container.Add(operationsLabel);
            
            var operationsField = new PropertyField(serializedObject.FindProperty("operations"));
            operationsField.style.minHeight = 120;
            operationsField.style.maxHeight = 250;
            operationsField.AddToClassList("inspector-field");
            
            // Handle operation changes
            operationsField.RegisterCallback<SerializedPropertyChangeEvent>((SerializedPropertyChangeEvent evt) =>
            {
                if (evt.changedProperty.propertyPath.StartsWith("operations"))
                {
                    EditorUtility.SetDirty(variableSetNode);
                    MarkGraphDirty();
                    RefreshGraphView();
                }
            });
            
            container.Add(operationsField);
            
            // Debug options
            var debugLabel = new Label("Debug Options");
            debugLabel.AddToClassList("inspector-section-header");
            container.Add(debugLabel);
            
            var logOperationsField = new PropertyField(serializedObject.FindProperty("logOperations"), "Log Operations");
            logOperationsField.AddToClassList("inspector-field");
            container.Add(logOperationsField);
            
            // Node info section
            AddNodeInfoSection(container, variableSetNode);
            
            // Bind for automatic synchronization
            container.Bind(serializedObject);
            
            // Schedule updates for dynamic content
            container.schedule.Execute(() =>
            {
                if (variableSetNode != null)
                {
                    serializedObject.Update();
                }
            }).Every(1000);
            
            m_InspectorScrollView.Add(container);
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
            
            var portraitNameProperty = serializedObject.FindProperty("portraitName");
            
            if (character != null)
            {
                // Create dropdown with portraits from character
                var dropdown = new DropdownField();
                var portraits = character.GetPortraitNames().ToList();
                dropdown.choices = portraits;
                
                // Set current value
                var currentValue = portraitNameProperty.stringValue;
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
                    portraitNameProperty.stringValue = portraits[0];
                }
                
                // Handle value changes
                dropdown.RegisterValueChangedCallback((ChangeEvent<string> evt) =>
                {
                    portraitNameProperty.stringValue = evt.newValue;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(node);
                    MarkGraphDirty();
                });
                
                dropdown.AddToClassList("unity-base-field__input");
                container.Add(dropdown);
                
                // Monitor character changes to update dropdown
                var characterProperty = serializedObject.FindProperty("speakerCharacter");
                container.schedule.Execute(() =>
                {
                    if (characterProperty.objectReferenceValue != character)
                    {
                        // Character changed, rebuild the dropdown
                        UpdateInspectorContent();
                    }
                }).Every(500); // Check every 500ms
            }
            else
            {
                // No character assigned, show regular text field
                var textField = new TextField();
                textField.bindingPath = "portraitName";
                textField.value = portraitNameProperty.stringValue;
                textField.AddToClassList("unity-base-field__input");
                container.Add(textField);
            }
            
            return container;
        }
        
        VisualElement CreateCharacterDropdown(SerializedObject serializedObject, DialogueNode node)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-field");
            
            var label = new Label("Speaker Character");
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
                MarkGraphDirty();
                
                // Refresh inspector to update portrait dropdown
                EditorApplication.delayCall += () =>
                {
                    UpdateInspectorContent();
                };
            });
            
            dropdown.AddToClassList("unity-base-field__input");
            container.Add(dropdown);
            
            return container;
        }
        
        void AddNodeInfoSection(VisualElement container, DialogueNode node)
        {
            var infoLabel = new Label("Node Information");
            infoLabel.AddToClassList("inspector-section-header");
            container.Add(infoLabel);
            
            var nodeIdLabel = new Label($"ID: {node.nodeId}");
            nodeIdLabel.AddToClassList("inspector-info-label");
            nodeIdLabel.AddToClassList("inspector-node-id");
            container.Add(nodeIdLabel);
            
            var positionLabel = new Label($"Position: ({node.position.x:F0}, {node.position.y:F0})");
            positionLabel.AddToClassList("inspector-info-label");
            container.Add(positionLabel);
            
            if (node is DialogueConnectionNode connectionNode)
            {
                var connectionCount = connectionNode.connections.Count(c => !string.IsNullOrEmpty(c));
                var connectionsLabel = new Label($"Connections: {connectionCount}");
                connectionsLabel.AddToClassList("inspector-info-label");
                container.Add(connectionsLabel);
            }
        }
        
        string GetStartNodeName()
        {
            if (m_LastGraphAsset == null || string.IsNullOrEmpty(m_LastGraphAsset.startNodeId))
                return "None";
                
            var startNode = m_LastGraphAsset.FindNodeById(m_LastGraphAsset.startNodeId);
            return startNode?.name ?? "Missing";
        }
        
        void RefreshGraphView()
        {
            var graphView = m_GraphViewPane?.Q<DialogueGraph>();
            graphView?.RefreshNodeDisplays();
        }
        
        void MarkGraphDirty()
        {
            if (m_LastGraphAsset != null)
            {
                EditorUtility.SetDirty(m_LastGraphAsset);
            }
        }
        
        void OnValidateClicked()
        {
            var graphView = m_GraphViewPane?.Q<DialogueGraph>();
            var graph = graphView?.GraphAsset;
            if (graph)
                ValidateGraph(graph);
        }
        
        void ValidateGraph(DialogueGraphAsset graph)
        {
            var issues = new System.Text.StringBuilder();
            var nodeList = graph.nodes?.Where(n => n != null).ToList();
            
            if (nodeList == null || nodeList.Count == 0)
            {
                issues.AppendLine("• No nodes in graph");
            }
            else
            {
                // Check for missing start node
                if (string.IsNullOrEmpty(graph.startNodeId))
                {
                    issues.AppendLine("• No start node set");
                }
                else if (graph.FindNodeById(graph.startNodeId) == null)
                {
                    issues.AppendLine("• Start node reference is broken");
                }
                
                // Check for broken connections
                foreach (var node in nodeList.OfType<DialogueConnectionNode>())
                {
                    foreach (var connectionId in node.connections)
                    {
                        if (!string.IsNullOrEmpty(connectionId) && graph.FindNodeById(connectionId) == null)
                        {
                            issues.AppendLine($"• Node '{node.name}' has broken connection to '{connectionId}'");
                        }
                    }
                }
                
                // Check for conditional nodes without variable manager
                var conditionalNodes = nodeList.OfType<ConditionalNode>().ToList();
                if (conditionalNodes.Count > 0 && !Application.isPlaying)
                {
                    issues.AppendLine("• Conditional nodes require DialogueVariableManager in scene (warning)");
                }
                
                // Check for variable set nodes without variable manager
                var variableSetNodes = nodeList.OfType<VariableSetNode>().ToList();
                if (variableSetNodes.Count > 0 && !Application.isPlaying)
                {
                    issues.AppendLine("• Variable set nodes require DialogueVariableManager in scene (warning)");
                }
            }
            
            if (issues.Length > 0)
            {
                EditorUtility.DisplayDialog("Graph Validation", 
                    "Issues found:\n\n" + issues, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Graph Validation", 
                    "No issues found! Graph is valid.", "OK");
            }
        }
        
        void OnSelectionChanged()
        {
            if (Selection.activeObject is DialogueGraphAsset graph)
            {
                m_LastGraphAsset = graph;
                var graphView = m_GraphViewPane?.Q<DialogueGraph>();
                if (graphView != null)
                {
                    graphView.SetGraphAsset(graph);
                    m_GraphViewPane.SetEnabled(true);
                }
        
                // When graph asset is selected, clear node selection and show graph info
                m_SelectedNode = null;
                UpdateInspectorContent();
            }
            else if (Selection.activeObject is DialogueNode node)
            {
                // Only update selected node if the node belongs to our current graph
                if (m_LastGraphAsset != null && m_LastGraphAsset.nodes.Contains(node))
                {
                    m_SelectedNode = node;
                    UpdateInspectorContent();
                }
            }
            else if (Selection.activeObject == null)
            {
                // Selection was cleared - keep current graph but clear node selection
                if (m_SelectedNode != null)
                {
                    m_SelectedNode = null;
                    UpdateInspectorContent();
                }
            }
            // For any other selection types, don't change anything
        }
        
        [OnOpenAsset(1, OnOpenAssetAttributeMode.Execute)]
        static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as DialogueGraphAsset;
            if (asset != null)
            {
                Init();
                
                // Wait for the window to be fully initialized before setting the graph
                s_Instance.m_LastGraphAsset = asset;
                
                // Use EditorApplication.delayCall to ensure GUI is created
                EditorApplication.delayCall += () =>
                {
                    if (s_Instance != null)
                    {
                        var graphView = s_Instance.m_GraphViewPane?.Q<DialogueGraph>();
                        if (graphView != null)
                        {
                            graphView.SetGraphAsset(asset);
                            s_Instance.m_GraphViewPane.SetEnabled(true);
                            graphView.FrameAll();
                        }
                    }
                };
                
                return true;
            }
            
            return false;
        }
    }
}
#endif