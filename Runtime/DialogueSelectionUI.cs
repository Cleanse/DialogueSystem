using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// UI controller for selecting between multiple dialogue options from an NPC.
    /// </summary>
    public class DialogueSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private TextMeshProUGUI npcNameText;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private Transform dialogueOptionsContainer;
        [SerializeField] private Button dialogueOptionButtonPrefab;
        [SerializeField] private Button cancelButton;
        
        [Header("Character Portrait")]
        [SerializeField] private GameObject portraitContainer;
        [SerializeField] private UnityEngine.UI.Image npcPortrait;
        [SerializeField] private Sprite defaultPortraitSprite;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private bool useAnimations = true;
        
        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip selectionOpenSound;
        [SerializeField] private AudioClip selectionCloseSound;
        [SerializeField] private AudioClip optionHoverSound;
        [SerializeField] private AudioClip optionSelectSound;
        
        private DialogueUI _dialogueUI;
        private List<DialogueOption> _currentOptions = new List<DialogueOption>();
        private CanvasGroup _canvasGroup;
        
        void Awake()
        {
            // Get or find DialogueUI
            _dialogueUI = FindFirstObjectByType<DialogueUI>();
            if (_dialogueUI == null)
            {
                Debug.LogError("DialogueSelectionUI: No DialogueUI found in scene!");
            }
            
            // Get canvas group for animations
            _canvasGroup = selectionPanel?.GetComponent<CanvasGroup>();
            if (_canvasGroup == null && selectionPanel != null)
            {
                _canvasGroup = selectionPanel.AddComponent<CanvasGroup>();
            }
            
            // Setup cancel button
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
            
            // Hide selection UI initially
            SetSelectionUIActive(false);
            
            // Set default instruction text
            if (instructionText != null)
            {
                instructionText.text = "Choose a conversation topic:";
            }
        }
        
        #region Public Methods
        
        /// <summary>
        /// Show the dialogue selection menu with the given options.
        /// </summary>
        /// <param name="npcName">Name of the NPC</param>
        /// <param name="dialogueOptions">Available dialogue options</param>
        public void ShowDialogueSelection(string npcName, List<DialogueOption> dialogueOptions)
        {
            ShowDialogueSelection(npcName, dialogueOptions, null, "Default");
        }
        
        /// <summary>
        /// Show the dialogue selection menu with the given options and character portrait.
        /// </summary>
        /// <param name="npcName">Name of the NPC</param>
        /// <param name="dialogueOptions">Available dialogue options</param>
        /// <param name="npcCharacter">Character asset for the NPC (optional)</param>
        /// <param name="portraitName">Portrait name to display (optional)</param>
        public void ShowDialogueSelection(string npcName, List<DialogueOption> dialogueOptions, Character npcCharacter, string portraitName = "Default")
        {
            if (dialogueOptions == null || dialogueOptions.Count == 0)
            {
                Debug.LogWarning("No dialogue options provided");
                return;
            }
            
            // If only one option, start it directly
            if (dialogueOptions.Count == 1)
            {
                StartDialogue(dialogueOptions[0]);
                return;
            }
            
            _currentOptions = new List<DialogueOption>(dialogueOptions);
            
            // Set NPC name (prioritize character name over provided string)
            if (npcNameText != null)
            {
                string effectiveName = npcCharacter != null ? npcCharacter.CharacterName : npcName;
                npcNameText.text = effectiveName;
            }
            
            // Set NPC portrait
            UpdateNPCPortrait(npcCharacter, portraitName);
            
            // Create option buttons
            CreateOptionButtons();
            
            // Show UI
            SetSelectionUIActive(true);
            PlaySound(selectionOpenSound);
        }
        
        /// <summary>
        /// Hide the dialogue selection menu.
        /// </summary>
        public void HideDialogueSelection()
        {
            SetSelectionUIActive(false);
            ClearOptionButtons();
            PlaySound(selectionCloseSound);
        }
        
        #endregion
        
        #region UI Management
        
        void SetSelectionUIActive(bool active)
        {
            if (selectionPanel == null) return;
            
            if (useAnimations && _canvasGroup != null)
            {
                if (active)
                {
                    selectionPanel.SetActive(true);
                    StartCoroutine(FadeIn());
                }
                else
                {
                    StartCoroutine(FadeOut());
                }
            }
            else
            {
                selectionPanel.SetActive(active);
            }
        }
        
        System.Collections.IEnumerator FadeIn()
        {
            if (_canvasGroup == null) yield break;
            
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            
            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
                yield return null;
            }
            
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
        }
        
        System.Collections.IEnumerator FadeOut()
        {
            if (_canvasGroup == null)
            {
                selectionPanel.SetActive(false);
                yield break;
            }
            
            _canvasGroup.interactable = false;
            
            float elapsedTime = 0f;
            float startAlpha = _canvasGroup.alpha;
            
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeInDuration);
                yield return null;
            }
            
            _canvasGroup.alpha = 0f;
            selectionPanel.SetActive(false);
        }
        
        /// <summary>
        /// Update the NPC portrait display with the given character and portrait name.
        /// </summary>
        /// <param name="npcCharacter">The character to display, or null to hide/use default.</param>
        /// <param name="portraitName">The portrait name to display.</param>
        void UpdateNPCPortrait(Character npcCharacter, string portraitName)
        {
            if (npcPortrait != null)
            {
                Sprite portraitSprite = null;
                
                // Get portrait from character if available
                if (npcCharacter != null)
                {
                    portraitSprite = npcCharacter.GetPortraitForPortrait(portraitName);
                }
                
                // Use provided sprite, fallback to default, or hide if none available
                Sprite spriteToUse = portraitSprite ?? defaultPortraitSprite;
                
                if (spriteToUse != null)
                {
                    npcPortrait.sprite = spriteToUse;
                    npcPortrait.enabled = true;
                    
                    if (portraitContainer != null)
                    {
                        portraitContainer.SetActive(true);
                    }
                }
                else
                {
                    npcPortrait.enabled = false;
                    
                    if (portraitContainer != null)
                    {
                        portraitContainer.SetActive(false);
                    }
                }
            }
        }
        
        #endregion
        
        #region Option Button Management
        
        void CreateOptionButtons()
        {
            ClearOptionButtons();
            
            for (int i = 0; i < _currentOptions.Count; i++)
            {
                CreateOptionButton(_currentOptions[i], i);
            }
        }
        
        void CreateOptionButton(DialogueOption option, int optionIndex)
        {
            if (dialogueOptionButtonPrefab == null || dialogueOptionsContainer == null)
            {
                Debug.LogError("Dialogue option button prefab or container is not assigned!");
                return;
            }
            
            Button optionButton = Instantiate(dialogueOptionButtonPrefab, dialogueOptionsContainer);
            
            // Set button text
            TextMeshProUGUI buttonText = optionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = option.displayName;
            }
            
            // Add hover sound effect
            var eventTrigger = optionButton.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = optionButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            
            var pointerEnterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnterEntry.callback.AddListener((data) => { PlaySound(optionHoverSound); });
            eventTrigger.triggers.Add(pointerEnterEntry);
            
            // Add click listener
            optionButton.onClick.AddListener(() => OnOptionSelected(optionIndex));
            
            // Check if option should be enabled
            if (!IsOptionAvailable(option))
            {
                optionButton.interactable = false;
                if (buttonText != null)
                {
                    buttonText.color = Color.gray;
                    if (!string.IsNullOrEmpty(option.unavailableReason))
                    {
                        buttonText.text += $" ({option.unavailableReason})";
                    }
                }
            }
        }
        
        void ClearOptionButtons()
        {
            if (dialogueOptionsContainer != null)
            {
                foreach (Transform child in dialogueOptionsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        bool IsOptionAvailable(DialogueOption option)
        {
            // Check if dialogue exists
            if (option.dialogueGraph == null)
                return false;
            
            // Check variable conditions if any
            if (option.requirementConditions != null && option.requirementConditions.Count > 0)
            {
                var variableManager = DialogueVariableManager.Instance;
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
        
        #endregion
        
        #region Event Handlers
        
        void OnOptionSelected(int optionIndex)
        {
            if (optionIndex >= 0 && optionIndex < _currentOptions.Count)
            {
                PlaySound(optionSelectSound);
                var selectedOption = _currentOptions[optionIndex];
                HideDialogueSelection();
                StartDialogue(selectedOption);
            }
        }
        
        void OnCancelClicked()
        {
            HideDialogueSelection();
        }
        
        void StartDialogue(DialogueOption option)
        {
            if (option.dialogueGraph != null && _dialogueUI != null)
            {
                _dialogueUI.StartDialogue(option.dialogueGraph);
            }
            else
            {
                Debug.LogError($"Cannot start dialogue: {(option.dialogueGraph == null ? "graph is null" : "DialogueUI not found")}");
            }
        }
        
        #endregion
        
        #region Audio
        
        void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        #endregion
        
        #region Input Handling
        
        void Update()
        {
            // Allow escape key to close selection menu
            if (selectionPanel != null && selectionPanel.activeInHierarchy && Input.GetKeyDown(KeyCode.Escape))
            {
                OnCancelClicked();
            }
        }
        
        #endregion
    }
}