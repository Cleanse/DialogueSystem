using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DialogueSystem
{
    /// <summary>
    /// Component that triggers dialogue when interacted with.
    /// Supports both single dialogues and multiple dialogue selection.
    /// Can be used on NPCs, objects, or trigger zones.
    /// </summary>
    public class DialogueTrigger : MonoBehaviour
    {
        [Header("NPC Settings")]
        [SerializeField] private string npcName = "NPC";
        [SerializeField] private Character npcCharacter;
        [SerializeField] private string npcPortraitName = "Default";
        [SerializeField] private bool triggerOnStart;
        [SerializeField] private bool triggerOnCollision = true;
        [SerializeField] private bool triggerOnInteraction = true;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        
        [Header("Single Dialogue (Legacy Support)")]
        [SerializeField] private DialogueGraphAsset dialogueGraph;
        
        [Header("Multiple Dialogue Options")]
        [SerializeField] private List<DialogueOption> dialogueOptions = new List<DialogueOption>();
        [SerializeField] private bool sortByPriority = true;
        [SerializeField] private bool showUnavailableOptions = false;
        
        [Header("UI Settings")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private string interactionPromptText = "Press T to talk";
        
        [Header("Fallback Dialogue")]
        [SerializeField] private DialogueGraphAsset fallbackDialogue;
        [SerializeField] private bool useFallbackWhenNoOptions = true;
        
        private DialogueSelectionUI _dialogueSelectionUI;
        private DialogueUI _dialogueUI;
        private bool _playerInRange;
        private Transform _playerTransform;
        private TMPro.TextMeshProUGUI _promptText;
        
        void Start()
        {
            // Find UI components
            _dialogueSelectionUI = FindFirstObjectByType<DialogueSelectionUI>();
            _dialogueUI = FindFirstObjectByType<DialogueUI>();
            
            if (_dialogueUI == null)
            {
                Debug.LogError("DialogueTrigger: No DialogueUI found in scene!");
            }
            
            // Find player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
            
            // Setup interaction prompt
            SetupInteractionPrompt();
            
            // Try to auto-assign character if not set
            TryAutoAssignCharacter();
            
            // Initialize dialogue options
            InitializeDialogueOptions();
            
            // Trigger dialogue on start if enabled
            if (triggerOnStart)
            {
                TriggerDialogue();
            }
        }
        
        void Update()
        {
            if (!triggerOnInteraction || _playerTransform == null)
                return;
            
            // Check distance to player
            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            bool wasInRange = _playerInRange;
            _playerInRange = distanceToPlayer <= interactionDistance;
            
            // Show/hide interaction prompt
            if (_playerInRange != wasInRange)
            {
                ShowInteractionPrompt(_playerInRange);
            }
            
            // Check for interaction input
            if (_playerInRange && Input.GetKeyDown(interactionKey))
            {
                TriggerDialogue();
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (!triggerOnCollision || !other.CompareTag("Player"))
                return;
                
            TriggerDialogue();
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!triggerOnCollision || !other.CompareTag("Player"))
                return;
                
            TriggerDialogue();
        }
        
        #region Public Methods
        
        /// <summary>
        /// Trigger the dialogue selection or start single dialogue.
        /// </summary>
        public void TriggerDialogue()
        {
            // Check if we have multiple dialogue options
            var availableOptions = GetAvailableDialogueOptions();
            
            if (availableOptions.Count == 0)
            {
                // No available options - try legacy single dialogue or fallback
                if (dialogueGraph != null)
                {
                    StartSingleDialogue(dialogueGraph);
                }
                else if (useFallbackWhenNoOptions && fallbackDialogue != null)
                {
                    StartSingleDialogue(fallbackDialogue);
                }
                else
                {
                    Debug.LogWarning($"No available dialogue options for {npcName}");
                }
                return;
            }
            
            if (availableOptions.Count == 1)
            {
                // Only one option available - start directly
                StartSingleDialogue(availableOptions[0].dialogueGraph);
            }
            else
            {
                // Multiple options - show selection menu if available
                if (_dialogueSelectionUI != null)
                {
                    ShowDialogueSelection(availableOptions);
                }
                else
                {
                    // Fallback to first available dialogue if no selection UI
                    Debug.LogWarning("Multiple dialogues available but no DialogueSelectionUI found. Starting first dialogue.");
                    StartSingleDialogue(availableOptions[0].dialogueGraph);
                }
            }
            
            ShowInteractionPrompt(false);
        }
        
        /// <summary>
        /// Set the dialogue graph for single dialogue mode (legacy support).
        /// </summary>
        /// <param name="newDialogueGraph">The new dialogue graph to use.</param>
        public void SetDialogueGraph(DialogueGraphAsset newDialogueGraph)
        {
            dialogueGraph = newDialogueGraph;
        }
        
        /// <summary>
        /// Add a new dialogue option.
        /// </summary>
        /// <param name="option">Dialogue option to add</param>
        public void AddDialogueOption(DialogueOption option)
        {
            if (option != null)
            {
                dialogueOptions.Add(option);
                InitializeDialogueOption(option);
            }
        }
        
        /// <summary>
        /// Remove a dialogue option by display name.
        /// </summary>
        /// <param name="displayName">Display name of option to remove</param>
        /// <returns>True if option was found and removed</returns>
        public bool RemoveDialogueOption(string displayName)
        {
            for (int i = dialogueOptions.Count - 1; i >= 0; i--)
            {
                if (dialogueOptions[i].displayName == displayName)
                {
                    dialogueOptions.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Get dialogue option by display name.
        /// </summary>
        /// <param name="displayName">Display name to search for</param>
        /// <returns>Dialogue option if found, null otherwise</returns>
        public DialogueOption GetDialogueOption(string displayName)
        {
            return dialogueOptions.FirstOrDefault(option => option.displayName == displayName);
        }
        
        /// <summary>
        /// Set the availability of a dialogue option.
        /// </summary>
        /// <param name="displayName">Display name of the option</param>
        /// <param name="available">Whether the option should be available</param>
        /// <param name="reason">Reason for unavailability (if applicable)</param>
        public void SetDialogueOptionAvailability(string displayName, bool available, string reason = "")
        {
            var option = GetDialogueOption(displayName);
            if (option != null)
            {
                if (!available)
                {
                    // Add a condition that always fails
                    var impossibleCondition = new VariableCondition
                    {
                        variableName = "__impossible_condition__",
                        comparisonType = ComparisonType.Equals,
                        expectedValue = "true",
                        valueType = VariableValueType.Boolean
                    };
                    option.requirementConditions.Clear();
                    option.requirementConditions.Add(impossibleCondition);
                    option.unavailableReason = reason;
                }
                else
                {
                    // Clear impossible conditions
                    option.requirementConditions.RemoveAll(c => c.variableName == "__impossible_condition__");
                }
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Try to auto-assign a character based on the NPC name and available Speaker Database.
        /// </summary>
        void TryAutoAssignCharacter()
        {
            if (npcCharacter == null && !string.IsNullOrEmpty(npcName))
            {
                var speakerDatabase = GetSpeakerDatabase();
                if (speakerDatabase != null)
                {
                    var character = speakerDatabase.GetCharacter(npcName);
                    if (character != null)
                    {
                        npcCharacter = character;
                        if (string.IsNullOrEmpty(npcPortraitName))
                        {
                            npcPortraitName = "Default";
                        }
                        Debug.Log($"Auto-assigned character '{character.CharacterName}' to DialogueTrigger '{gameObject.name}'");
                    }
                }
            }
        }
        
        /// <summary>
        /// Get the Speaker Database for character lookup.
        /// </summary>
        SpeakerDatabase GetSpeakerDatabase()
        {
            #if UNITY_EDITOR
            // Find any speaker database in the project
            string[] databaseGuids = UnityEditor.AssetDatabase.FindAssets("t:SpeakerDatabase");
            if (databaseGuids.Length > 0)
            {
                string databasePath = UnityEditor.AssetDatabase.GUIDToAssetPath(databaseGuids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<SpeakerDatabase>(databasePath);
            }
            #else
            // At runtime, try to find through Resources or other means
            var allSpeakerDatabases = Resources.FindObjectsOfTypeAll<SpeakerDatabase>();
            if (allSpeakerDatabases.Length > 0)
            {
                return allSpeakerDatabases[0];
            }
            #endif
            
            return null;
        }
        
        void SetupInteractionPrompt()
        {
            if (interactionPrompt != null)
            {
                _promptText = interactionPrompt.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (_promptText != null && !string.IsNullOrEmpty(interactionPromptText))
                {
                    _promptText.text = interactionPromptText;
                }
                interactionPrompt.SetActive(false);
            }
        }
        
        void InitializeDialogueOptions()
        {
            foreach (var option in dialogueOptions)
            {
                InitializeDialogueOption(option);
            }
        }
        
        void InitializeDialogueOption(DialogueOption option)
        {
            // Auto-generate read variable name if not set
            if (option.markAsReadAfterCompleted && string.IsNullOrEmpty(option.readVariableName))
            {
                option.readVariableName = $"{npcName}_{option.displayName}_read".Replace(" ", "_").ToLower();
            }
        }
        
        List<DialogueOption> GetAvailableDialogueOptions()
        {
            var availableOptions = new List<DialogueOption>();
            var variableManager = DialogueVariableManager.Instance;
            
            foreach (var option in dialogueOptions)
            {
                bool isAvailable = IsDialogueOptionAvailable(option, variableManager);
                
                if (isAvailable || showUnavailableOptions)
                {
                    availableOptions.Add(option);
                }
            }
            
            // Sort by priority if enabled
            if (sortByPriority)
            {
                availableOptions.Sort((a, b) => b.priority.CompareTo(a.priority));
            }
            
            return availableOptions;
        }
        
        bool IsDialogueOptionAvailable(DialogueOption option, DialogueVariableManager variableManager)
        {
            // Check if dialogue graph exists
            if (option.dialogueGraph == null)
                return false;
            
            // Check requirement conditions
            if (option.requirementConditions != null && option.requirementConditions.Count > 0)
            {
                if (variableManager == null)
                    return false;
                
                foreach (var condition in option.requirementConditions)
                {
                    if (!EvaluateCondition(condition, variableManager))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        bool EvaluateCondition(VariableCondition condition, DialogueVariableManager variableManager)
        {
            var currentValue = variableManager.GetVariable(condition.variableName);
            var expectedValue = ParseValue(condition.expectedValue, condition.valueType);
            
            switch (condition.comparisonType)
            {
                case ComparisonType.Equals:
                    return AreValuesEqual(currentValue, expectedValue);
                case ComparisonType.NotEquals:
                    return !AreValuesEqual(currentValue, expectedValue);
                case ComparisonType.GreaterThan:
                    return CompareNumericValues(currentValue, expectedValue) > 0;
                case ComparisonType.GreaterThanOrEqual:
                    return CompareNumericValues(currentValue, expectedValue) >= 0;
                case ComparisonType.LessThan:
                    return CompareNumericValues(currentValue, expectedValue) < 0;
                case ComparisonType.LessThanOrEqual:
                    return CompareNumericValues(currentValue, expectedValue) <= 0;
                case ComparisonType.Exists:
                    return variableManager.HasVariable(condition.variableName);
                case ComparisonType.NotExists:
                    return !variableManager.HasVariable(condition.variableName);
                case ComparisonType.Contains:
                    return currentValue?.ToString().Contains(expectedValue?.ToString() ?? "") ?? false;
                case ComparisonType.NotContains:
                    return !(currentValue?.ToString().Contains(expectedValue?.ToString() ?? "") ?? false);
                default:
                    return true;
            }
        }
        
        object ParseValue(string value, VariableValueType valueType)
        {
            switch (valueType)
            {
                case VariableValueType.String: return value ?? "";
                case VariableValueType.Integer: return int.TryParse(value, out int i) ? i : 0;
                case VariableValueType.Float: return float.TryParse(value, out float f) ? f : 0f;
                case VariableValueType.Boolean: return bool.TryParse(value, out bool b) ? b : false;
                default: return value;
            }
        }
        
        bool AreValuesEqual(object value1, object value2)
        {
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;
            return value1.ToString() == value2.ToString();
        }
        
        int CompareNumericValues(object value1, object value2)
        {
            try
            {
                double num1 = System.Convert.ToDouble(value1);
                double num2 = System.Convert.ToDouble(value2);
                return num1.CompareTo(num2);
            }
            catch
            {
                return string.Compare(value1?.ToString() ?? "", value2?.ToString() ?? "");
            }
        }
        
        void ShowDialogueSelection(List<DialogueOption> options)
        {
            if (_dialogueSelectionUI != null)
            {
                // Use character-aware method if character is assigned
                if (npcCharacter != null)
                {
                    _dialogueSelectionUI.ShowDialogueSelection(npcName, options, npcCharacter, npcPortraitName);
                }
                else
                {
                    _dialogueSelectionUI.ShowDialogueSelection(npcName, options);
                }
            }
            else
            {
                Debug.LogError("DialogueSelectionUI not found! Cannot show dialogue selection.");
            }
        }
        
        void StartSingleDialogue(DialogueGraphAsset dialogue)
        {
            if (_dialogueUI != null && dialogue != null)
            {
                _dialogueUI.StartDialogue(dialogue);
            }
            else
            {
                Debug.LogError("Cannot start dialogue: DialogueUI or dialogue graph is null");
            }
        }
        
        void ShowInteractionPrompt(bool show)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(show);
            }
        }
        
        #endregion
        
        #region Gizmos
        
        void OnDrawGizmosSelected()
        {
            // Draw interaction range
            if (triggerOnInteraction)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, interactionDistance);
            }
            
            // Draw dialogue count indicator
            Gizmos.color = Color.cyan;
            Vector3 textPos = transform.position + Vector3.up * 2f;
            
            #if UNITY_EDITOR
            int totalDialogues = dialogueOptions.Count + (dialogueGraph != null ? 1 : 0);
            UnityEditor.Handles.Label(textPos, $"{npcName}\n{totalDialogues} dialogue(s)");
            #endif
        }
        
        #endregion
        
        #region Context Menu (Debug)
        
        [ContextMenu("Test Dialogue")]
        void TestDialogue()
        {
            TriggerDialogue();
        }
        
        [ContextMenu("Add Sample Dialogue Options")]
        void AddSampleDialogueOptions()
        {
            dialogueOptions.Clear();
            
            dialogueOptions.Add(new DialogueOption
            {
                displayName = "General Chat",
                description = "Have a casual conversation",
                category = DialogueCategory.General,
                priority = 10
            });
            
            dialogueOptions.Add(new DialogueOption
            {
                displayName = "Quest Information",
                description = "Ask about available quests",
                category = DialogueCategory.Quest,
                priority = 20
            });
            
            dialogueOptions.Add(new DialogueOption
            {
                displayName = "Shop",
                description = "Browse items for sale",
                category = DialogueCategory.Shop,
                priority = 15
            });
            
            dialogueOptions.Add(new DialogueOption
            {
                displayName = "Goodbye",
                description = "End the conversation",
                category = DialogueCategory.Goodbye,
                priority = 0
            });
            
            Debug.Log($"Added {dialogueOptions.Count} sample dialogue options");
        }
        
        [ContextMenu("Convert Single Dialogue to Option")]
        void ConvertSingleDialogueToOption()
        {
            if (dialogueGraph != null)
            {
                var newOption = new DialogueOption
                {
                    displayName = "Talk",
                    description = "Start a conversation",
                    dialogueGraph = dialogueGraph,
                    category = DialogueCategory.General,
                    priority = 10
                };
                
                dialogueOptions.Add(newOption);
                dialogueGraph = null; // Clear the single dialogue
                
                Debug.Log("Converted single dialogue to dialogue option");
            }
            else
            {
                Debug.LogWarning("No single dialogue to convert");
            }
        }
        
        #endregion
    }
}