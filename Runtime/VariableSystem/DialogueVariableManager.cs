using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// Manages dialogue variables and state for the dialogue system.
    /// Supports persistent storage and various data types.
    /// </summary>
    public class DialogueVariableManager : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool showVariablesInInspector = true;
        
        [Header("Persistence Settings")]
        [SerializeField] private bool autosaveVariables = true;
        [SerializeField] private string saveFileName = "dialogue_variables.json";
        
        // Core variable storage
        private Dictionary<string, DialogueVariable> variables = new Dictionary<string, DialogueVariable>();
        
        // Events for variable changes
        public static event Action<string, object, object> OnVariableChanged; // variableName, oldValue, newValue
        public static event Action<string, object> OnVariableSet; // variableName, value
        
        // Singleton pattern for easy access
        public static DialogueVariableManager Instance { get; private set; }
        
        // Inspector display (for debugging)
        [SerializeField, Space(10)] 
        private List<VariableDisplay> debugVariables = new List<VariableDisplay>();
        
        void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Load saved variables
                if (autosaveVariables)
                {
                    LoadVariables();
                }
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && autosaveVariables)
            {
                SaveVariables();
            }
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && autosaveVariables)
            {
                SaveVariables();
            }
        }
        
        void OnDestroy()
        {
            if (Instance == this && autosaveVariables)
            {
                SaveVariables();
            }
        }
        
        #region Variable Management
        
        /// <summary>
        /// Set a variable value. Creates the variable if it doesn't exist.
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <param name="value">Variable value</param>
        public void SetVariable(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Variable name cannot be null or empty");
                return;
            }
            
            object oldValue = null;
            bool hadOldValue = variables.ContainsKey(name);
            
            if (hadOldValue)
            {
                oldValue = variables[name].Value;
            }
            
            // Create or update variable
            var variable = new DialogueVariable(name, value);
            variables[name] = variable;
            
            // Log change
            if (enableDebugLogging)
            {
                if (hadOldValue)
                {
                    Debug.Log($"[DialogueVar] Changed '{name}': {oldValue} → {value}");
                }
                else
                {
                    Debug.Log($"[DialogueVar] Set '{name}': {value}");
                }
            }
            
            // Update inspector display
            UpdateDebugDisplay();
            
            // Fire events
            OnVariableSet?.Invoke(name, value);
            if (hadOldValue)
            {
                OnVariableChanged?.Invoke(name, oldValue, value);
            }
        }
        
        /// <summary>
        /// Get a variable value with type conversion.
        /// </summary>
        /// <typeparam name="T">Expected type</typeparam>
        /// <param name="name">Variable name</param>
        /// <param name="defaultValue">Default value if variable doesn't exist</param>
        /// <returns>Variable value or default</returns>
        public T GetVariable<T>(string name, T defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(name) || !variables.ContainsKey(name))
            {
                return defaultValue;
            }
            
            try
            {
                var value = variables[name].Value;
                
                // Handle null values
                if (value == null)
                {
                    return defaultValue;
                }
                
                // Direct type match
                if (value is T directMatch)
                {
                    return directMatch;
                }
                
                // Type conversion
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to convert variable '{name}' to type {typeof(T).Name}: {ex.Message}");
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Get variable value as object.
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <returns>Variable value or null</returns>
        public object GetVariable(string name)
        {
            if (string.IsNullOrEmpty(name) || !variables.ContainsKey(name))
            {
                return null;
            }
            
            return variables[name].Value;
        }
        
        /// <summary>
        /// Check if a variable exists.
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <returns>True if variable exists</returns>
        public bool HasVariable(string name)
        {
            return !string.IsNullOrEmpty(name) && variables.ContainsKey(name);
        }
        
        /// <summary>
        /// Remove a variable.
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <returns>True if variable was removed</returns>
        public bool RemoveVariable(string name)
        {
            if (string.IsNullOrEmpty(name) || !variables.ContainsKey(name))
            {
                return false;
            }
            
            variables.Remove(name);
            UpdateDebugDisplay();
            
            if (enableDebugLogging)
            {
                Debug.Log($"[DialogueVar] Removed '{name}'");
            }
            
            return true;
        }
        
        /// <summary>
        /// Clear all variables.
        /// </summary>
        public void ClearAllVariables()
        {
            variables.Clear();
            UpdateDebugDisplay();
            
            if (enableDebugLogging)
            {
                Debug.Log("[DialogueVar] Cleared all variables");
            }
        }
        
        /// <summary>
        /// Get all variable names.
        /// </summary>
        /// <returns>Array of variable names</returns>
        public string[] GetAllVariableNames()
        {
            var names = new string[variables.Count];
            variables.Keys.CopyTo(names, 0);
            return names;
        }
        
        /// <summary>
        /// Get total number of variables.
        /// </summary>
        /// <returns>Variable count</returns>
        public int GetVariableCount()
        {
            return variables.Count;
        }
        
        #endregion
        
        #region Convenience Methods
        
        /// <summary>
        /// Increment a numeric variable.
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <param name="amount">Amount to increment (default: 1)</param>
        public void IncrementVariable(string name, float amount = 1f)
        {
            var currentValue = GetVariable<float>(name, 0f);
            SetVariable(name, currentValue + amount);
        }
        
        /// <summary>
        /// Toggle a boolean variable.
        /// </summary>
        /// <param name="name">Variable name</param>
        public void ToggleVariable(string name)
        {
            var currentValue = GetVariable<bool>(name, false);
            SetVariable(name, !currentValue);
        }
        
        /// <summary>
        /// Append text to a string variable.
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <param name="text">Text to append</param>
        public void AppendToVariable(string name, string text)
        {
            var currentValue = GetVariable<string>(name, "");
            SetVariable(name, currentValue + text);
        }
        
        /// <summary>
        /// Set variable to maximum of current value and new value.
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <param name="value">Value to compare</param>
        public void SetVariableMax(string name, float value)
        {
            var currentValue = GetVariable<float>(name, float.MinValue);
            SetVariable(name, Mathf.Max(currentValue, value));
        }
        
        /// <summary>
        /// Set variable to minimum of current value and new value.
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <param name="value">Value to compare</param>
        public void SetVariableMin(string name, float value)
        {
            var currentValue = GetVariable<float>(name, float.MaxValue);
            SetVariable(name, Mathf.Min(currentValue, value));
        }
        
        #endregion
        
        #region Persistence
        
        /// <summary>
        /// Save variables to persistent storage.
        /// </summary>
        public void SaveVariables()
        {
            try
            {
                var saveData = new DialogueVariableSaveData();
                foreach (var kvp in variables)
                {
                    saveData.variables.Add(kvp.Value);
                }
                
                string json = JsonUtility.ToJson(saveData, true);
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);
                System.IO.File.WriteAllText(filePath, json);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[DialogueVar] Saved {variables.Count} variables to {filePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save dialogue variables: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load variables from persistent storage.
        /// </summary>
        public void LoadVariables()
        {
            try
            {
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);
                
                if (!System.IO.File.Exists(filePath))
                {
                    if (enableDebugLogging)
                    {
                        Debug.Log("[DialogueVar] No save file found, starting with empty variables");
                    }
                    return;
                }
                
                string json = System.IO.File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<DialogueVariableSaveData>(json);
                
                variables.Clear();
                foreach (var variable in saveData.variables)
                {
                    variables[variable.name] = variable;
                }
                
                UpdateDebugDisplay();
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[DialogueVar] Loaded {variables.Count} variables from {filePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load dialogue variables: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Debug Display
        
        void UpdateDebugDisplay()
        {
            if (!showVariablesInInspector)
                return;
                
            debugVariables.Clear();
            foreach (var kvp in variables)
            {
                debugVariables.Add(new VariableDisplay
                {
                    name = kvp.Key,
                    value = kvp.Value.Value?.ToString() ?? "null",
                    type = kvp.Value.GetValueType().Name
                });
            }
        }
        
        [Serializable]
        public class VariableDisplay
        {
            public string name;
            public string value;
            public string type;
        }
        
        #endregion
        
        #region Context Menu (Debug)
        
        [ContextMenu("Print All Variables")]
        void PrintAllVariables()
        {
            Debug.Log($"=== Dialogue Variables ({variables.Count}) ===");
            foreach (var kvp in variables)
            {
                Debug.Log($"{kvp.Key} = {kvp.Value.Value} ({kvp.Value.GetValueType().Name})");
            }
        }
        
        [ContextMenu("Clear All Variables")]
        void DebugClearVariables()
        {
            ClearAllVariables();
        }
        
        [ContextMenu("Add Test Variables")]
        void AddTestVariables()
        {
            SetVariable("player_name", "Hero");
            SetVariable("player_level", 5);
            SetVariable("has_sword", true);
            SetVariable("gold", 150.5f);
            SetVariable("location", "Forest");
        }
        
        #endregion
    }
}