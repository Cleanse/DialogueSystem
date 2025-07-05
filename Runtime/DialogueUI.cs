using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace DialogueSystem
{
    /// <summary>
    /// UI controller for the dialogue system.
    /// Handles displaying speech, choices, and managing the dialogue flow.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Button continueButton;
        
        [Header("Character Portrait")]
        [SerializeField] private GameObject portraitContainer;
        [SerializeField] private UnityEngine.UI.Image speakerPortrait;
        [SerializeField] private Sprite defaultPortraitSprite;
        
        [Header("Choice UI")]
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private Button choiceButtonPrefab;
        
        [Header("Animation Settings")]
        [SerializeField] private float typewriterSpeed = 0.05f;
        [SerializeField] private bool useTypewriterEffect = true;
        
        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip dialogueStartSound;
        [SerializeField] private AudioClip dialogueEndSound;
        [SerializeField] private AudioClip choiceSelectSound;
        
        private DialogueRunner _dialogueRunner;
        private Coroutine _typewriterCoroutine;
        private bool _isTyping;
        private string _fullText = "";
        
        void Awake()
        {
            // Get or create DialogueRunner
            _dialogueRunner = FindFirstObjectByType<DialogueRunner>();
            if (_dialogueRunner == null)
            {
                GameObject runnerObj = new GameObject("DialogueRunner");
                _dialogueRunner = runnerObj.AddComponent<DialogueRunner>();
            }
            
            // Subscribe to dialogue events
            _dialogueRunner.OnDialogueStarted += OnDialogueStarted;
            _dialogueRunner.OnDialogueEnded += OnDialogueEnded;
            _dialogueRunner.OnSpeechDisplayed += OnSpeechDisplayed;
            _dialogueRunner.OnChoiceDisplayed += OnChoiceDisplayed;
            _dialogueRunner.OnFunctionRan += OnFunctionRan;
            
            // Setup continue button
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
            
            // Hide UI initially
            SetDialogueUIActive(false);
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (_dialogueRunner != null)
            {
                _dialogueRunner.OnDialogueStarted -= OnDialogueStarted;
                _dialogueRunner.OnDialogueEnded -= OnDialogueEnded;
                _dialogueRunner.OnSpeechDisplayed -= OnSpeechDisplayed;
                _dialogueRunner.OnChoiceDisplayed -= OnChoiceDisplayed;
                _dialogueRunner.OnFunctionRan -= OnFunctionRan;
            }
        }
        
        #region Public Methods
        
        /// <summary>
        /// Start a dialogue with the given graph asset.
        /// </summary>
        /// <param name="dialogueGraph">The dialogue graph to start.</param>
        public void StartDialogue(DialogueGraphAsset dialogueGraph)
        {
            if (dialogueGraph == null)
            {
                Debug.LogWarning("Cannot start dialogue: graph is null");
                return;
            }
            
            _dialogueRunner.StartDialogue(dialogueGraph);
        }
        
        /// <summary>
        /// End the current dialogue.
        /// </summary>
        public void EndDialogue()
        {
            _dialogueRunner.EndDialogue();
        }
        
        #endregion
        
        #region Event Handlers
        
        void OnDialogueStarted()
        {
            SetDialogueUIActive(true);
            PlaySound(dialogueStartSound);
        }
        
        void OnDialogueEnded()
        {
            SetDialogueUIActive(false);
            ClearChoices();
            PlaySound(dialogueEndSound);
        }
        
        void OnSpeechDisplayed(SpeechNode speechNode)
        {
            ShowSpeechUI();
            
            // Set speaker name
            if (speakerNameText != null)
            {
                speakerNameText.text = speechNode.GetEffectiveSpeakerName();
            }
            
            // Set character portrait
            UpdateCharacterPortrait(speechNode.GetSpeakerPortrait());
            
            // Display dialogue text with optional typewriter effect
            if (useTypewriterEffect && dialogueText != null)
            {
                StartTypewriter(speechNode.dialogueText);
            }
            else if (dialogueText != null)
            {
                dialogueText.text = speechNode.dialogueText;
                SetContinueButtonActive(true);
            }
        }
        
        void OnChoiceDisplayed(ChoiceNode choiceNode)
        {
            ShowChoiceUI();
            
            // Set speaker name and prompt
            if (speakerNameText != null)
            {
                speakerNameText.text = choiceNode.GetEffectiveSpeakerName();
            }
            
            // Set character portrait
            UpdateCharacterPortrait(choiceNode.GetSpeakerPortrait());
            
            if (dialogueText != null)
            {
                dialogueText.text = choiceNode.promptText;
            }
            else
            {
                dialogueText.text = "...";
            }
            
            // Create choice buttons
            CreateChoiceButtons(choiceNode);
        }
        
        void OnFunctionRan(FunctionNode functionNode)
        {
            Debug.Log($"Function node executed with functions: {string.Join(", ", functionNode.Functions)}");
            
            // Here you would implement your function calls
            // For example:
            foreach (string functionName in functionNode.Functions)
            {
                ExecuteFunction(functionName);
            }
        }
        
        #endregion
        
        #region UI Management
        
        void SetDialogueUIActive(bool active)
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(active);
            }
        }
        
        void ShowSpeechUI()
        {
            if (choicePanel != null)
                choicePanel.SetActive(false);
                
            SetContinueButtonActive(false); // Will be enabled after text display
        }
        
        void ShowChoiceUI()
        {
            SetContinueButtonActive(false);
            
            if (choicePanel != null)
                choicePanel.SetActive(true);
        }
        
        void SetContinueButtonActive(bool active)
        {
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(active);
            }
        }
        
        /// <summary>
        /// Update the character portrait display with the given sprite.
        /// </summary>
        /// <param name="portraitSprite">The sprite to display, or null to hide/use default.</param>
        void UpdateCharacterPortrait(Sprite portraitSprite)
        {
            if (speakerPortrait != null)
            {
                // Use provided sprite, fallback to default, or hide if none available
                Sprite spriteToUse = portraitSprite ?? defaultPortraitSprite;
                
                if (spriteToUse != null)
                {
                    speakerPortrait.sprite = spriteToUse;
                    speakerPortrait.enabled = true;
                    
                    if (portraitContainer != null)
                    {
                        portraitContainer.SetActive(true);
                    }
                }
                else
                {
                    speakerPortrait.enabled = false;
                    
                    if (portraitContainer != null)
                    {
                        portraitContainer.SetActive(false);
                    }
                }
            }
        }
        
        #endregion
        
        #region Typewriter Effect
        
        void StartTypewriter(string text)
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
            
            _fullText = text;
            _typewriterCoroutine = StartCoroutine(TypewriterEffect());
        }
        
        IEnumerator TypewriterEffect()
        {
            _isTyping = true;
            dialogueText.text = "";
            
            for (int i = 0; i <= _fullText.Length; i++)
            {
                dialogueText.text = _fullText.Substring(0, i);
                yield return new WaitForSeconds(typewriterSpeed);
            }
            
            _isTyping = false;
            SetContinueButtonActive(true);
        }
        
        void SkipTypewriter()
        {
            if (_isTyping && _typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                dialogueText.text = _fullText;
                _isTyping = false;
                SetContinueButtonActive(true);
            }
        }
        
        #endregion
        
        #region Choice Handling
        
        void CreateChoiceButtons(ChoiceNode choiceNode)
        {
            ClearChoices();
            
            for (int i = 0; i < choiceNode.choiceTexts.Count; i++)
            {
                CreateChoiceButton(choiceNode.choiceTexts[i], i);
            }
        }
        
        void CreateChoiceButton(string choiceText, int choiceIndex)
        {
            if (choiceButtonPrefab == null || choiceContainer == null)
            {
                Debug.LogError("Choice button prefab or container is not assigned!");
                return;
            }
            
            Button choiceButton = Instantiate(choiceButtonPrefab, choiceContainer);
            
            // Set button text
            TextMeshProUGUI buttonText = choiceButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choiceText;
            }
            
            // Add click listener
            choiceButton.onClick.AddListener(() => OnChoiceSelected(choiceIndex));
        }
        
        void ClearChoices()
        {
            if (choiceContainer != null)
            {
                foreach (Transform child in choiceContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        void OnChoiceSelected(int choiceIndex)
        {
            PlaySound(choiceSelectSound);
            ClearChoices();
            
            if (choicePanel != null)
                choicePanel.SetActive(false);
                
            _dialogueRunner.SelectChoice(choiceIndex);
        }
        
        #endregion
        
        #region Button Handlers
        
        void OnContinueClicked()
        {
            if (_isTyping)
            {
                SkipTypewriter();
            }
            else
            {
                _dialogueRunner.Continue();
            }
        }
        
        #endregion
        
        #region Function Execution
        
        /// <summary>
        /// Execute a function by name. Override this method to implement your game-specific functions.
        /// </summary>
        /// <param name="functionName">The name of the function to execute.</param>
        protected virtual void ExecuteFunction(string functionName)
        {
            switch (functionName.ToLower())
            {
                case "givegold":
                    GiveGold();
                    break;
                case "additem":
                    AddItem();
                    break;
                case "playssound":
                    PlayDialogueSound();
                    break;
                default:
                    Debug.LogWarning($"Unknown function: {functionName}");
                    break;
            }
        }
        
        // Example function implementations, need to create full system soonTM
        void GiveGold()
        {
            Debug.Log("NYI. Player received gold!");
            // Implement gold-giving logic here
        }
        
        void AddItem()
        {
            Debug.Log("NYI. Item added to inventory!");
            // Implement item-adding logic here
        }
        
        void PlayDialogueSound()
        {
            Debug.Log("NYI. Playing dialogue sound effect!");
            // Implement sound playing logic here
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
        
        #region Input Handling (Optional)
        
        void Update()
        {
            // Can use space or enter to continue dialogue
            if (_dialogueRunner.IsRunning && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                if (continueButton != null && continueButton.gameObject.activeInHierarchy)
                {
                    OnContinueClicked();
                }
            }
        }
        
        #endregion
    }
}