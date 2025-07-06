#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// Custom inspector for Character assets with enhanced portrait list management.
    /// </summary>
    [CustomEditor(typeof(Character))]
    public class CharacterEditor : Editor
    {
        private SerializedProperty characterNameProp;
        private SerializedProperty characterDescriptionProp;
        private SerializedProperty defaultPortraitProp;
        private SerializedProperty portraitsProp;
        private SerializedProperty voicePitchProp;
        private SerializedProperty voiceClipProp;
        
        private bool showPortraits = true;
        private Vector2 portraitsScrollPos;
        
        private void OnEnable()
        {
            // Cache serialized properties
            characterNameProp = serializedObject.FindProperty("characterName");
            characterDescriptionProp = serializedObject.FindProperty("characterDescription");
            defaultPortraitProp = serializedObject.FindProperty("defaultPortrait");
            portraitsProp = serializedObject.FindProperty("portraits");
            voicePitchProp = serializedObject.FindProperty("voicePitch");
            voiceClipProp = serializedObject.FindProperty("voiceClip");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var character = (Character)target;
            
            // Character Info Section
            EditorGUILayout.LabelField("Character Info", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(characterNameProp);
            EditorGUILayout.PropertyField(characterDescriptionProp);
            
            EditorGUILayout.Space(10);
            
            // Default Portrait Section
            EditorGUILayout.LabelField("Default Portrait", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(defaultPortraitProp);
            
            EditorGUILayout.Space(10);
            
            // Portraits Section
            showPortraits = EditorGUILayout.Foldout(showPortraits, "Portraits", true, EditorStyles.foldoutHeader);
            
            if (showPortraits)
            {
                EditorGUILayout.Space(5);
                
                // Add portrait button
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Portrait", GUILayout.Width(120)))
                {
                    AddNewPortrait();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // Portraits list
                if (portraitsProp.arraySize > 0)
                {
                    // Scroll view for portraits if there are many
                    if (portraitsProp.arraySize > 5)
                    {
                        portraitsScrollPos = EditorGUILayout.BeginScrollView(portraitsScrollPos, GUILayout.MaxHeight(300));
                    }
                    
                    // Track items to remove (can't modify during iteration)
                    var indicesToRemove = new List<int>();
                    
                    for (int i = 0; i < portraitsProp.arraySize; i++)
                    {
                        var portraitProp = portraitsProp.GetArrayElementAtIndex(i);
                        var portraitNameProp = portraitProp.FindPropertyRelative("portraitName");
                        var portraitSpriteProp = portraitProp.FindPropertyRelative("portrait");
                        
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        EditorGUILayout.BeginHorizontal();
                        
                        // Portrait name field
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField($"Portrait {i + 1}", EditorStyles.miniBoldLabel);
                        
                        string newName = EditorGUILayout.TextField("Name", portraitNameProp.stringValue);
                        if (newName != portraitNameProp.stringValue)
                        {
                            portraitNameProp.stringValue = newName;
                        }
                        
                        // Portrait field
                        EditorGUILayout.PropertyField(portraitSpriteProp, new GUIContent("Portrait"));
                        EditorGUILayout.EndVertical();
                        
                        // Remove button
                        EditorGUILayout.BeginVertical(GUILayout.Width(60));
                        GUILayout.Space(20);
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            indicesToRemove.Add(i);
                        }
                        EditorGUILayout.EndVertical();
                        
                        EditorGUILayout.EndHorizontal();
                        
                        // Validation warnings
                        if (string.IsNullOrEmpty(portraitNameProp.stringValue))
                        {
                            EditorGUILayout.HelpBox("Portrait name cannot be empty.", MessageType.Warning);
                        }
                        else if (HasDuplicatePortraitName(portraitNameProp.stringValue, i))
                        {
                            EditorGUILayout.HelpBox("Duplicate portrait name found.", MessageType.Warning);
                        }
                        
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(5);
                    }
                    
                    // Remove marked portraits (in reverse order to maintain indices)
                    for (int i = indicesToRemove.Count - 1; i >= 0; i--)
                    {
                        portraitsProp.DeleteArrayElementAtIndex(indicesToRemove[i]);
                    }
                    
                    if (portraitsProp.arraySize > 5)
                    {
                        EditorGUILayout.EndScrollView();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No portraits added yet. Click 'Add Portrait' to create one.", MessageType.Info);
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Audio Settings Section
            EditorGUILayout.LabelField("Audio Settings (Optional)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(voicePitchProp);
            EditorGUILayout.PropertyField(voiceClipProp);
            
            EditorGUILayout.Space(10);
            
            // Character Summary
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField($"Total Portraits: {portraitsProp.arraySize}");
            if (portraitsProp.arraySize > 0)
            {
                var portraitNames = new List<string>();
                for (int i = 0; i < portraitsProp.arraySize; i++)
                {
                    var portraitProp = portraitsProp.GetArrayElementAtIndex(i);
                    var portraitNameProp = portraitProp.FindPropertyRelative("portraitName");
                    if (!string.IsNullOrEmpty(portraitNameProp.stringValue))
                    {
                        portraitNames.Add(portraitNameProp.stringValue);
                    }
                }
                
                if (portraitNames.Count > 0)
                {
                    EditorGUILayout.LabelField("Available: " + string.Join(", ", portraitNames), EditorStyles.wordWrappedMiniLabel);
                }
            }
            
            // Apply changes
            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(target);
            }
        }
        
        private void AddNewPortrait()
        {
            portraitsProp.arraySize++;
            var newPortraitProp = portraitsProp.GetArrayElementAtIndex(portraitsProp.arraySize - 1);
            var portraitNameProp = newPortraitProp.FindPropertyRelative("portraitName");
            var portraitSpriteProp = newPortraitProp.FindPropertyRelative("portrait");
            
            // Set default values
            portraitNameProp.stringValue = $"Portrait {portraitsProp.arraySize}";
            portraitSpriteProp.objectReferenceValue = null;
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
        
        private bool HasDuplicatePortraitName(string name, int currentIndex)
        {
            if (string.IsNullOrEmpty(name)) return false;
            
            for (int i = 0; i < portraitsProp.arraySize; i++)
            {
                if (i == currentIndex) continue;
                
                var portraitProp = portraitsProp.GetArrayElementAtIndex(i);
                var portraitNameProp = portraitProp.FindPropertyRelative("portraitName");
                
                if (name.Equals(portraitNameProp.stringValue, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
#endif