using UnityEngine;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// Centralized input configuration for the dialogue system.
    /// Uses reflection to detect and support both input systems without compile-time dependencies.
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
        
        [Header("Navigation Keys")]
        [Tooltip("Key to cancel/close dialogue selection")]
        public KeyCode cancelKey = KeyCode.Escape;
        
        [Header("Input Settings")]
        [Tooltip("Whether interaction keys should also work for dialogue continuation")]
        public bool useInteractionKeysForContinuation = true;
        
        [Tooltip("Whether to show all available keys in prompts")]
        public bool showAllKeysInPrompts = true;
        
        [Header("Debug Settings")]
        [Tooltip("Enable verbose logging for input debugging")]
        public bool enableDebugLogging;
        
        // Input consumption tracking
        private HashSet<KeyCode> _consumedKeysThisFrame = new HashSet<KeyCode>();
        private int _lastConsumedFrame = -1;
        
        /// <summary>
        /// Check if the new Input System is available using safe type checking.
        /// </summary>
        private static bool IsNewInputSystemAvailable()
        {
            try
            {
                // Use reflection to safely check if Input System is available
                var keyboardType = System.Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                if (keyboardType == null) return false;
                
                var currentProperty = keyboardType.GetProperty("current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (currentProperty == null) return false;
                
                var keyboard = currentProperty.GetValue(null);
                return keyboard != null;
            }
            catch
            {
                return false;
            }
        }
        
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
        /// Universal key press detection that works with both input systems.
        /// </summary>
        /// <param name="keyCode">The key to check</param>
        /// <returns>True if the key was pressed this frame</returns>
        private bool IsKeyPressed(KeyCode keyCode)
        {
            // Try new Input System first if available
            if (IsNewInputSystemAvailable())
            {
                bool keyPressed = IsKeyPressedNewInputSystem(keyCode);
                if (keyPressed && enableDebugLogging)
                {
                    Debug.Log($"[DialogueInputSettings] Key {keyCode} pressed via NEW Input System");
                }
                return keyPressed;
            }
            else
            {
                // Use legacy Input Manager as fallback
                bool keyPressed = Input.GetKeyDown(keyCode);
                if (keyPressed && enableDebugLogging)
                {
                    Debug.Log($"[DialogueInputSettings] Key {keyCode} pressed via LEGACY Input Manager");
                }
                return keyPressed;
            }
        }
        
        
        /// <summary>
        /// Check key press using new Input System via reflection (safe approach).
        /// </summary>
        /// <param name="keyCode">The key to check</param>
        /// <returns>True if the key was pressed this frame</returns>
        private bool IsKeyPressedNewInputSystem(KeyCode keyCode)
        {
            try
            {
                // Get keyboard instance via reflection
                var keyboardType = System.Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                if (keyboardType == null) return false;
                
                var currentProperty = keyboardType.GetProperty("current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (currentProperty == null) return false;
                
                var keyboard = currentProperty.GetValue(null);
                if (keyboard == null) return false;
                
                // Get the appropriate key property name
                string keyPropertyName = GetKeyPropertyName(keyCode);
                if (string.IsNullOrEmpty(keyPropertyName)) return false;
                
                // Get the key control
                var keyProperty = keyboardType.GetProperty(keyPropertyName);
                if (keyProperty == null) return false;
                
                var keyControl = keyProperty.GetValue(keyboard);
                if (keyControl == null) return false;
                
                // Check wasPressedThisFrame
                var wasPressedProperty = keyControl.GetType().GetProperty("wasPressedThisFrame");
                if (wasPressedProperty == null) return false;
                
                return (bool)wasPressedProperty.GetValue(keyControl);
            }
            catch (System.Exception)
            {
                // Silently fall back to legacy input if new Input System fails
                return false;
            }
        }
        
        /// <summary>
        /// Get the Input System property name for a given KeyCode.
        /// </summary>
        private string GetKeyPropertyName(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.T => "tKey",
                KeyCode.E => "eKey",
                KeyCode.F => "fKey",
                KeyCode.Q => "qKey",
                KeyCode.R => "rKey",
                KeyCode.Space => "spaceKey",
                KeyCode.Return => "enterKey",
                KeyCode.Escape => "escapeKey",
                KeyCode.Tab => "tabKey",
                KeyCode.LeftShift => "leftShiftKey",
                KeyCode.RightShift => "rightShiftKey",
                KeyCode.LeftControl => "leftCtrlKey",
                KeyCode.RightControl => "rightCtrlKey",
                KeyCode.LeftAlt => "leftAltKey",
                KeyCode.RightAlt => "rightAltKey",
                KeyCode.UpArrow => "upArrowKey",
                KeyCode.DownArrow => "downArrowKey",
                KeyCode.LeftArrow => "leftArrowKey",
                KeyCode.RightArrow => "rightArrowKey",
                _ => null // Unsupported key, will fall back to legacy input
            };
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
            
            KeyCode primaryKey = primary != KeyCode.None ? primary : primaryInteractionKey;
            KeyCode secondaryKey = secondary != KeyCode.None ? secondary : secondaryInteractionKey;
            
            bool primaryPressed = primaryKey != KeyCode.None && IsKeyPressed(primaryKey);
            bool secondaryPressed = secondaryKey != KeyCode.None && IsKeyPressed(secondaryKey);
            
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
                
            KeyCode primaryKey = primary != KeyCode.None ? primary : primaryInteractionKey;
            KeyCode secondaryKey = secondary != KeyCode.None ? secondary : secondaryInteractionKey;
            
            // Check if ANY of the keys are not consumed (not requiring both to be available)
            bool primaryAvailable = primaryKey == KeyCode.None || !_consumedKeysThisFrame.Contains(primaryKey);
            bool secondaryAvailable = secondaryKey == KeyCode.None || !_consumedKeysThisFrame.Contains(secondaryKey);
            
            return primaryAvailable || secondaryAvailable;
        }
        
        /// <summary>
        /// Consume the interaction keys so they can't be used by other systems this frame.
        /// </summary>
        /// <param name="primary">Primary interaction key to consume</param>
        /// <param name="secondary">Secondary interaction key to consume</param>
        public void ConsumeInteractionKey(KeyCode primary = KeyCode.None, KeyCode secondary = KeyCode.None)
        {
            EnsureFrameReset();
            
            KeyCode primaryKey = primary != KeyCode.None ? primary : primaryInteractionKey;
            KeyCode secondaryKey = secondary != KeyCode.None ? secondary : secondaryInteractionKey;
            
            // Only consume keys that were actually pressed this frame
            if (primaryKey != KeyCode.None && IsKeyPressed(primaryKey))
            {
                _consumedKeysThisFrame.Add(primaryKey);
            }
            if (secondaryKey != KeyCode.None && IsKeyPressed(secondaryKey))
            {
                _consumedKeysThisFrame.Add(secondaryKey);
            }
        }
        
        /// <summary>
        /// Check if any of the continuation keys are pressed this frame.
        /// </summary>
        /// <returns>True if any continuation key was pressed</returns>
        public bool IsContinueKeyPressed()
        {
            bool primaryPressed = primaryContinueKey != KeyCode.None && IsKeyPressed(primaryContinueKey);
            bool secondaryPressed = secondaryContinueKey != KeyCode.None && IsKeyPressed(secondaryContinueKey);
            
            return primaryPressed || secondaryPressed;
        }
        
        /// <summary>
        /// Check if the cancel/escape key was pressed this frame.
        /// </summary>
        /// <returns>True if cancel key was pressed</returns>
        public bool IsCancelKeyPressed()
        {
            return cancelKey != KeyCode.None && IsKeyPressed(cancelKey);
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
        /// Check if a specific key was pressed this frame (public version for external use).
        /// </summary>
        /// <param name="keyCode">The key to check</param>
        /// <returns>True if the key was pressed this frame</returns>
        public bool IsSpecificKeyPressed(KeyCode keyCode)
        {
            return keyCode != KeyCode.None && IsKeyPressed(keyCode);
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
            var keys = new List<string>();
            
            if (primaryContinueKey != KeyCode.None)
                keys.Add(GetKeyDisplayName(primaryContinueKey));
            if (secondaryContinueKey != KeyCode.None)
                keys.Add(GetKeyDisplayName(secondaryContinueKey));
            
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
        /// Get or create a DialogueInputSettings instance with default values.
        /// This ensures the input system works even without explicit asset creation.
        /// </summary>
        public static DialogueInputSettings GetOrCreateDefaultSettings()
        {
            // Try to find existing input settings in Resources first
            var settings = Resources.Load<DialogueInputSettings>("DialogueInputSettings");
            
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<DialogueInputSettings>();
                settings.ResetToDefaults();
            }
            
            return settings;
        }
        
        /// <summary>
        /// Debug method to test input detection (Editor only - for troubleshooting)
        /// </summary>
        [ContextMenu("Test Input Detection")]
        public void TestInputDetection()
        {
            Debug.Log($"DialogueInputSettings - Primary: {primaryInteractionKey}, Secondary: {secondaryInteractionKey}, " +
                     $"New Input System: {IsNewInputSystemAvailable()}, Key Available: {IsInteractionKeyAvailable()}");
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
            cancelKey = KeyCode.Escape;
            useInteractionKeysForContinuation = true;
            showAllKeysInPrompts = true;
            enableDebugLogging = false; // Disable debug by default for production
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}