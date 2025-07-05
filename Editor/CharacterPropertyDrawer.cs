using UnityEngine;
using UnityEditor;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Custom property drawer for Character fields in the inspector.
    /// Provides enhanced UI for character selection and portrait dropdown.
    /// </summary>
    [CustomPropertyDrawer(typeof(Character))]
    public class CharacterPropertyDrawer : PropertyDrawer
    {
        private const float LineHeight = 18f;
        private const float Spacing = 2f;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Base height for the character reference field
            return LineHeight;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Draw the character object field
            EditorGUI.PropertyField(position, property, label);
            
            EditorGUI.EndProperty();
        }
    }
    
    /// <summary>
    /// Custom property drawer for portrait string fields when used in dialogue nodes.
    /// Provides a dropdown of available portraits from the assigned character.
    /// </summary>
    [CustomPropertyDrawer(typeof(PortraitAttribute))]
    public class PortraitPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Get the character property from the same object
            var target = property.serializedObject.targetObject;
            Character character = null;
            
            // Try to find the character field
            if (target is SpeechNode speechNode)
            {
                character = speechNode.speakerCharacter;
            }
            else if (target is ChoiceNode choiceNode)
            {
                character = choiceNode.speakerCharacter;
            }
            
            if (character != null)
            {
                // Get available portraits
                string[] portraits = character.GetPortraitNames();
                
                // Find current selection index
                int currentIndex = System.Array.IndexOf(portraits, property.stringValue);
                if (currentIndex == -1) currentIndex = 0; // Default to first option
                
                // Draw dropdown
                int newIndex = EditorGUI.Popup(position, label.text, currentIndex, portraits);
                
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < portraits.Length)
                {
                    property.stringValue = portraits[newIndex];
                }
            }
            else
            {
                // No character assigned, show regular string field
                EditorGUI.PropertyField(position, property, label);
            }
            
            EditorGUI.EndProperty();
        }
    }
    
}