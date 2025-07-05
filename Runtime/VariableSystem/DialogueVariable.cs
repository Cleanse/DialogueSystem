using System;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// Represents a single dialogue variable with type information and serialization support.
    /// </summary>
    [System.Serializable]
    public class DialogueVariable
    {
        [SerializeField] public string name;
        [SerializeField] private string serializedValue;
        [SerializeField] private string typeName;
        
        // Runtime value cache
        private object _cachedValue;
        private Type _cachedType;
        private bool _isCacheValid = false;
        
        public DialogueVariable()
        {
            // Default constructor for serialization
        }
        
        public DialogueVariable(string name, object value)
        {
            this.name = name;
            SetValue(value);
        }
        
        /// <summary>
        /// Get the current value of this variable.
        /// </summary>
        public object Value
        {
            get
            {
                if (!_isCacheValid)
                {
                    DeserializeValue();
                }
                return _cachedValue;
            }
        }
        
        /// <summary>
        /// Set the value of this variable.
        /// </summary>
        /// <param name="value">New value</param>
        public void SetValue(object value)
        {
            _cachedValue = value;
            _cachedType = value?.GetType();
            _isCacheValid = true;
            
            SerializeValue();
        }
        
        /// <summary>
        /// Get the type of the stored value.
        /// </summary>
        /// <returns>Type of the value</returns>
        public Type GetValueType()
        {
            if (!_isCacheValid)
            {
                DeserializeValue();
            }
            return _cachedType ?? typeof(object);
        }
        
        /// <summary>
        /// Get the value as a specific type.
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="defaultValue">Default value if conversion fails</param>
        /// <returns>Converted value or default</returns>
        public T GetValue<T>(T defaultValue = default(T))
        {
            try
            {
                var value = Value;
                
                if (value == null)
                    return defaultValue;
                
                if (value is T directMatch)
                    return directMatch;
                
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Check if the value is of a specific type.
        /// </summary>
        /// <typeparam name="T">Type to check</typeparam>
        /// <returns>True if value is of type T</returns>
        public bool IsType<T>()
        {
            return GetValueType() == typeof(T);
        }
        
        /// <summary>
        /// Serialize the current value to string for persistence.
        /// </summary>
        private void SerializeValue()
        {
            try
            {
                if (_cachedValue == null)
                {
                    serializedValue = "";
                    typeName = "";
                    return;
                }
                
                _cachedType = _cachedValue.GetType();
                typeName = _cachedType.AssemblyQualifiedName;
                
                // Handle common types efficiently
                if (_cachedType == typeof(string))
                {
                    serializedValue = (string)_cachedValue;
                }
                else if (_cachedType == typeof(int))
                {
                    serializedValue = ((int)_cachedValue).ToString();
                }
                else if (_cachedType == typeof(float))
                {
                    serializedValue = ((float)_cachedValue).ToString("R"); // Round-trip format
                }
                else if (_cachedType == typeof(bool))
                {
                    serializedValue = ((bool)_cachedValue).ToString();
                }
                else if (_cachedType == typeof(double))
                {
                    serializedValue = ((double)_cachedValue).ToString("R");
                }
                else
                {
                    // Use JsonUtility for complex types
                    var wrapper = new SerializationWrapper { value = _cachedValue };
                    serializedValue = JsonUtility.ToJson(wrapper);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to serialize dialogue variable '{name}': {ex.Message}");
                serializedValue = "";
                typeName = "";
            }
        }
        
        /// <summary>
        /// Deserialize the string value back to object.
        /// </summary>
        private void DeserializeValue()
        {
            try
            {
                if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(serializedValue))
                {
                    _cachedValue = null;
                    _cachedType = null;
                    _isCacheValid = true;
                    return;
                }
                
                _cachedType = Type.GetType(typeName);
                if (_cachedType == null)
                {
                    Debug.LogError($"Could not resolve type '{typeName}' for variable '{name}'");
                    _cachedValue = null;
                    _isCacheValid = true;
                    return;
                }
                
                // Handle common types efficiently
                if (_cachedType == typeof(string))
                {
                    _cachedValue = serializedValue;
                }
                else if (_cachedType == typeof(int))
                {
                    _cachedValue = int.Parse(serializedValue);
                }
                else if (_cachedType == typeof(float))
                {
                    _cachedValue = float.Parse(serializedValue);
                }
                else if (_cachedType == typeof(bool))
                {
                    _cachedValue = bool.Parse(serializedValue);
                }
                else if (_cachedType == typeof(double))
                {
                    _cachedValue = double.Parse(serializedValue);
                }
                else
                {
                    // Use JsonUtility for complex types
                    var wrapper = JsonUtility.FromJson<SerializationWrapper>(serializedValue);
                    _cachedValue = wrapper.value;
                }
                
                _isCacheValid = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize dialogue variable '{name}': {ex.Message}");
                _cachedValue = null;
                _cachedType = null;
                _isCacheValid = true;
            }
        }
        
        /// <summary>
        /// Convert variable to string representation.
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            var value = Value;
            var type = GetValueType().Name;
            return $"{name} = {value ?? "null"} ({type})";
        }
        
        /// <summary>
        /// Wrapper class for complex type serialization.
        /// </summary>
        [Serializable]
        private class SerializationWrapper
        {
            public object value;
        }
    }
    
    /// <summary>
    /// Save data structure for dialogue variables.
    /// </summary>
    [Serializable]
    public class DialogueVariableSaveData
    {
        public System.Collections.Generic.List<DialogueVariable> variables = 
            new System.Collections.Generic.List<DialogueVariable>();
    }
}