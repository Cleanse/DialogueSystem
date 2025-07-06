using UnityEngine;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// Centralized input configuration for the dialogue system.
    /// Supports dual keybinds for each interaction type.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueInputSettings", menuName = "Dialogue System/Input Settings")]
    public class DialogueInputSettings : ScriptableObject
    {
        [Header("Default Interaction Keys")]
        [Tooltip("Primary key for triggering dialogue interactions")]
        public KeyCode primaryInteractionKey = KeyCode.T;
        
        [Tooltip("Secondary key for triggering dialogue interactions (optional)")]
        public KeyCode secondaryInteractionKey = KeyCode.E;
        
        [Header("Dialogue Continuation Keys")]
        [Tooltip("Primary key for continuing dialogue (always available)")]
        public KeyCode primaryContinueKey = KeyCode.Space;
        
        [Tooltip("Secondary key for continuing dialogue (always available)")]
        public KeyCode secondaryContinueKey = KeyCode.Return;
        
        [Header("Input Settings")]
        [Tooltip("Whether interaction keys should also work for dialogue continuation")]
        public bool useInteractionKeysForContinuation = true;
        
        [Tooltip("Whether to show all available keys in prompts")]
        public bool showAllKeysInPrompts = true;
        
        // Input consumption tracking to prevent key conflicts between systems
        private HashSet<KeyCode> _consumedKeysThisFrame = new HashSet<KeyCode>();
        private int _lastConsumedFrame = -1;
        
        /// <summary>
        /// Reset consumed keys tracking each frame.
        /// </summary>
        private void EnsureFrameReset()
        {
            int currentFrame = Time.frameCount;
            if (_lastConsumedFrame != currentFrame)
            {
                _consumedKeysThisFrame.Clear();
                _lastConsumedFrame = currentFrame;
            }
        }
        
        /// <summary>
        /// Check if any of the interaction keys are pressed this frame.
        /// </summary>
        /// <param name="primary">Primary interaction key to check</param>
        /// <param name="secondary">Secondary interaction key to check</param>
        /// <returns>True if any interaction key was pressed</returns>
        public bool IsInteractionKeyPressed(KeyCode primary = KeyCode.None, KeyCode secondary = KeyCode.None)
        {
            EnsureFrameReset();
            
            // Use provided keys or fall back to defaults
            KeyCode primaryKey = primary != KeyCode.None ? primary : primaryInteractionKey;
            KeyCode secondaryKey = secondary != KeyCode.None ? secondary : secondaryInteractionKey;
            
            bool primaryPressed = primaryKey != KeyCode.None && Input.GetKeyDown(primaryKey);
            bool secondaryPressed = secondaryKey != KeyCode.None && Input.GetKeyDown(secondaryKey);
            
            return primaryPressed || secondaryPressed;
        }
        
        /// <summary>
        /// Check if interaction keys are available (pressed and not consumed this frame).
        /// </summary>
        /// <param name="primary">Primary interaction key to check</param>
        /// <param name="secondary">Secondary interaction key to check</param>
        /// <returns>True if interaction key is pressed and available</returns>
        public bool IsInteractionKeyAvailable(KeyCode primary = KeyCode.None, KeyCode secondary = KeyCode.None)
        {
            EnsureFrameReset();
            
            if (!IsInteractionKeyPressed(primary, secondary))
                return false;
                
            // Use provided keys or fall back to defaults
            KeyCode primaryKey = primary != KeyCode.None ? primary : primaryInteractionKey;
            KeyCode secondaryKey = secondary != KeyCode.None ? secondary : secondaryInteractionKey;
            
            // Check if either key is consumed
            bool primaryConsumed = primaryKey != KeyCode.None && _consumedKeysThisFrame.Contains(primaryKey);
            bool secondaryConsumed = secondaryKey != KeyCode.None && _consumedKeysThisFrame.Contains(secondaryKey);
            
            return !primaryConsumed && !secondaryConsumed;
        }
        
        /// <summary>
        /// Consume the interaction keys so they can't be used by other systems this frame.
        /// </summary>
        /// <param name="primary">Primary interaction key to consume</param>
        /// <param name="secondary">Secondary interaction key to consume</param>
        public void ConsumeInteractionKey(KeyCode primary = KeyCode.None, KeyCode secondary = KeyCode.None)
        {
            EnsureFrameReset();
            
            // Use provided keys or fall back to defaults
            KeyCode primaryKey = primary != KeyCode.None ? primary : primaryInteractionKey;
            KeyCode secondaryKey = secondary != KeyCode.None ? secondary : secondaryInteractionKey;
            
            // Only consume keys that are actually pressed
            if (primaryKey != KeyCode.None && Input.GetKeyDown(primaryKey))
                _consumedKeysThisFrame.Add(primaryKey);
            if (secondaryKey != KeyCode.None && Input.GetKeyDown(secondaryKey))
                _consumedKeysThisFrame.Add(secondaryKey);
        }
        
        /// <summary>
        /// Check if any of the continuation keys are pressed this frame.
        /// </summary>
        /// <returns>True if any continuation key was pressed</returns>
        public bool IsContinueKeyPressed()
        {
            bool primaryPressed = primaryContinueKey != KeyCode.None && Input.GetKeyDown(primaryContinueKey);
            bool secondaryPressed = secondaryContinueKey != KeyCode.None && Input.GetKeyDown(secondaryContinueKey);
            
            return primaryPressed || secondaryPressed;
        }
        
        /// <summary>
        /// Check if any dialogue advancement key is pressed (continue keys + interaction keys if enabled).
        /// </summary>
        /// <param name="interactionPrimary">Primary interaction key from DialogueTrigger</param>
        /// <param name="interactionSecondary">Secondary interaction key from DialogueTrigger</param>
        /// <returns>True if any advancement key was pressed</returns>
        public bool IsDialogueAdvancementKeyPressed(KeyCode interactionPrimary = KeyCode.None, KeyCode interactionSecondary = KeyCode.None)
        {
            bool continuePressed = IsContinueKeyPressed();
            bool interactionPressed = useInteractionKeysForContinuation && 
                                    IsInteractionKeyPressed(interactionPrimary, interactionSecondary);
            
            return continuePressed || interactionPressed;
        }
        
        /// <summary>
        /// Get a formatted string of interaction keys for UI display.
        /// </summary>
        /// <param name="primary">Primary interaction key</param>
        /// <param name="secondary">Secondary interaction key</param>
        /// <returns>Formatted key string (e.g., "T or E")</returns>
        public string GetInteractionKeyText(KeyCode primary = KeyCode.None, KeyCode secondary = KeyCode.None)
        {
            KeyCode primaryKey = primary != KeyCode.None ? primary : primaryInteractionKey;
            KeyCode secondaryKey = secondary != KeyCode.None ? secondary : secondaryInteractionKey;
            
            if (primaryKey == KeyCode.None && secondaryKey == KeyCode.None)
                return "Interaction";
            
            if (primaryKey != KeyCode.None && secondaryKey != KeyCode.None && showAllKeysInPrompts)
                return $"{GetKeyDisplayName(primaryKey)} or {GetKeyDisplayName(secondaryKey)}";
            
            if (primaryKey != KeyCode.None)
                return GetKeyDisplayName(primaryKey);
            
            return GetKeyDisplayName(secondaryKey);
        }
        
        /// <summary>
        /// Get a formatted string of all dialogue advancement keys for UI display.
        /// </summary>
        /// <param name="interactionPrimary">Primary interaction key from DialogueTrigger</param>
        /// <param name="interactionSecondary">Secondary interaction key from DialogueTrigger</param>
        /// <returns>Formatted key string (e.g., "Space, Enter, T, or E")</returns>
        public string GetAdvancementKeyText(KeyCode interactionPrimary = KeyCode.None, KeyCode interactionSecondary = KeyCode.None)
        {
            var keys = new System.Collections.Generic.List<string>();
            
            // Add continue keys
            if (primaryContinueKey != KeyCode.None)
                keys.Add(GetKeyDisplayName(primaryContinueKey));
            if (secondaryContinueKey != KeyCode.None)
                keys.Add(GetKeyDisplayName(secondaryContinueKey));
            
            // Add interaction keys if enabled
            if (useInteractionKeysForContinuation)
            {
                KeyCode primaryKey = interactionPrimary != KeyCode.None ? interactionPrimary : primaryInteractionKey;
                KeyCode secondaryKey = interactionSecondary != KeyCode.None ? interactionSecondary : secondaryInteractionKey;
                
                if (primaryKey != KeyCode.None && !keys.Contains(GetKeyDisplayName(primaryKey)))
                    keys.Add(GetKeyDisplayName(primaryKey));
                if (secondaryKey != KeyCode.None && !keys.Contains(GetKeyDisplayName(secondaryKey)))
                    keys.Add(GetKeyDisplayName(secondaryKey));
            }
            
            if (keys.Count == 0)
                return "Continue";
            
            if (keys.Count == 1)
                return keys[0];
            
            if (keys.Count == 2)
                return $"{keys[0]} or {keys[1]}";
            
            // For 3+ keys, use comma separation with "or" before the last one
            var result = string.Join(", ", keys.GetRange(0, keys.Count - 1));
            result += $", or {keys[keys.Count - 1]}";
            return result;
        }
        
        /// <summary>
        /// Convert KeyCode to user-friendly display name.
        /// </summary>
        /// <param name="keyCode">The KeyCode to convert</param>
        /// <returns>User-friendly key name</returns>
        private string GetKeyDisplayName(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.Return => "Enter",
                KeyCode.Space => "Space",
                KeyCode.LeftShift => "Shift",
                KeyCode.RightShift => "Shift",
                KeyCode.LeftControl => "Ctrl",
                KeyCode.RightControl => "Ctrl",
                KeyCode.LeftAlt => "Alt",
                KeyCode.RightAlt => "Alt",
                KeyCode.Escape => "Esc",
                KeyCode.Backspace => "Backspace",
                KeyCode.Delete => "Del",
                KeyCode.Tab => "Tab",
                KeyCode.CapsLock => "Caps Lock",
                KeyCode.UpArrow => "↑",
                KeyCode.DownArrow => "↓",
                KeyCode.LeftArrow => "←",
                KeyCode.RightArrow => "→",
                _ => keyCode.ToString()
            };
        }
        
        /// <summary>
        /// Create a default input settings asset.
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            primaryInteractionKey = KeyCode.T;
            secondaryInteractionKey = KeyCode.E;
            primaryContinueKey = KeyCode.Space;
            secondaryContinueKey = KeyCode.Return;
            useInteractionKeysForContinuation = true;
            showAllKeysInPrompts = true;
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}