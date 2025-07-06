using UnityEngine;
using UnityEditor;

namespace DialogueSystem
{
    /// <summary>
    /// Custom editor for DialogueTrigger to provide better portrait selection.
    /// </summary>
    [CustomEditor(typeof(DialogueTrigger))]
    public class DialogueTriggerEditor : Editor
    {
        private SerializedProperty npcCharacterProp;
        private SerializedProperty selectedPortraitIndexProp;
        private SerializedProperty triggerOnStartProp;
        private SerializedProperty triggerOnCollisionProp;
        private SerializedProperty triggerOnInteractionProp;
        private SerializedProperty dialogueGraphProp;
        private SerializedProperty dialogueOptionsProp;
        private SerializedProperty sortByPriorityProp;
        private SerializedProperty showUnavailableOptionsProp;
        private SerializedProperty interactionPromptProp;
        private SerializedProperty interactionDistanceProp;
        private SerializedProperty customInteractionPromptTextProp;
        private SerializedProperty fallbackDialogueProp;
        private SerializedProperty useFallbackWhenNoOptionsProp;
        private SerializedProperty inputSettingsProp;

        void OnEnable()
        {
            // Find all the serialized properties
            npcCharacterProp = serializedObject.FindProperty("npcCharacter");
            selectedPortraitIndexProp = serializedObject.FindProperty("selectedPortraitIndex");
            triggerOnStartProp = serializedObject.FindProperty("triggerOnStart");
            triggerOnCollisionProp = serializedObject.FindProperty("triggerOnCollision");
            triggerOnInteractionProp = serializedObject.FindProperty("triggerOnInteraction");
            dialogueGraphProp = serializedObject.FindProperty("dialogueGraph");
            dialogueOptionsProp = serializedObject.FindProperty("dialogueOptions");
            sortByPriorityProp = serializedObject.FindProperty("sortByPriority");
            showUnavailableOptionsProp = serializedObject.FindProperty("showUnavailableOptions");
            interactionPromptProp = serializedObject.FindProperty("interactionPrompt");
            interactionDistanceProp = serializedObject.FindProperty("interactionDistance");
            customInteractionPromptTextProp = serializedObject.FindProperty("customInteractionPromptText");
            fallbackDialogueProp = serializedObject.FindProperty("fallbackDialogue");
            useFallbackWhenNoOptionsProp = serializedObject.FindProperty("useFallbackWhenNoOptions");
            inputSettingsProp = serializedObject.FindProperty("inputSettings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // NPC Settings Header
            EditorGUILayout.LabelField("NPC Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(npcCharacterProp);

            // Portrait Selection with Dropdown
            DrawPortraitSelection();

            EditorGUILayout.PropertyField(triggerOnStartProp);
            EditorGUILayout.PropertyField(triggerOnCollisionProp);
            EditorGUILayout.PropertyField(triggerOnInteractionProp);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Interaction keys are now configured globally in DialogueInputSettings assets.", MessageType.Info);

            EditorGUILayout.Space();

            // Single Dialogue Header
            EditorGUILayout.LabelField("Single Dialogue", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(dialogueGraphProp);

            EditorGUILayout.Space();

            // Multiple Dialogue Options Header
            EditorGUILayout.LabelField("Multiple Dialogue Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(dialogueOptionsProp);
            EditorGUILayout.PropertyField(sortByPriorityProp);
            EditorGUILayout.PropertyField(showUnavailableOptionsProp);

            EditorGUILayout.Space();

            // UI Settings Header
            EditorGUILayout.LabelField("UI Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(interactionPromptProp);
            EditorGUILayout.PropertyField(interactionDistanceProp);
            EditorGUILayout.PropertyField(customInteractionPromptTextProp);

            EditorGUILayout.Space();

            // Fallback Dialogue Header
            EditorGUILayout.LabelField("Fallback Dialogue", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fallbackDialogueProp);
            EditorGUILayout.PropertyField(useFallbackWhenNoOptionsProp);

            EditorGUILayout.Space();

            // Input Settings Header
            EditorGUILayout.LabelField("Input Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(inputSettingsProp);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawPortraitSelection()
        {
            Character npcCharacter = npcCharacterProp.objectReferenceValue as Character;
            
            if (npcCharacter != null)
            {
                string[] portraitNames = npcCharacter.GetPortraitNames();
                
                if (portraitNames.Length > 0)
                {
                    // Clamp the current index to valid range
                    int currentIndex = Mathf.Clamp(selectedPortraitIndexProp.intValue, 0, portraitNames.Length - 1);
                    
                    // Show dropdown with portrait names
                    string label = "Selected Portrait";
                    int newIndex = EditorGUILayout.Popup(label, currentIndex, portraitNames);
                    
                    // Update if changed
                    if (newIndex != selectedPortraitIndexProp.intValue)
                    {
                        selectedPortraitIndexProp.intValue = newIndex;
                    }
                    
                    // Show preview of selected portrait if available
                    if (currentIndex < portraitNames.Length)
                    {
                        Sprite selectedPortrait = npcCharacter.GetPortraitForPortrait(portraitNames[currentIndex]);
                        if (selectedPortrait != null)
                        {
                            EditorGUILayout.Space(5);
                            GUILayout.Label("Portrait Preview:", EditorStyles.miniLabel);
                            
                            // Create a small preview
                            Rect previewRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
                            GUI.DrawTexture(previewRect, selectedPortrait.texture, ScaleMode.ScaleToFit);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No portraits available on this Character. Add portraits to the Character asset.", MessageType.Info);
                    selectedPortraitIndexProp.intValue = 0;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a Character asset to select portraits.", MessageType.Warning);
                selectedPortraitIndexProp.intValue = 0;
            }
        }
    }
}