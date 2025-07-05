using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// A dialogue node that branches based on variable conditions.
    /// </summary>
    public class ConditionalNode : DialogueConnectionNode
    {
        [Header("Conditions")]
        [SerializeField] private List<VariableCondition> conditions = new List<VariableCondition>();
        
        [Header("Logic")]
        [SerializeField] private ConditionLogicType logicType = ConditionLogicType.And;
        
        [Header("Debug")]
        [SerializeField] private bool logEvaluation = false;
        
        public ConditionLogicType LogicType
        {
            get => logicType;
            set => logicType = value;
        }
        
        public bool LogEvaluation
        {
            get => logEvaluation;
            set => logEvaluation = value;
        }
        
        public override string GetDisplayTitle()
        {
            return "Condition";
        }
        
        public override string GetDisplayText()
        {
            if (conditions.Count == 0)
                return "No conditions";
                
            if (conditions.Count == 1)
                return $"If {conditions[0].variableName}";
                
            string logic = logicType == ConditionLogicType.And ? "AND" : "OR";
            return $"If {conditions.Count} conditions ({logic})";
        }
        
        public override void Execute(DialogueRunner runner)
        {
            var variableManager = DialogueVariableManager.Instance;
            if (variableManager == null)
            {
                Debug.LogError("DialogueVariableManager not found in scene!");
                return;
            }
            
            bool conditionResult = EvaluateConditions(variableManager);
            
            if (logEvaluation)
            {
                Debug.Log($"[ConditionalNode] Condition result: {conditionResult}");
            }
            
            // Get the appropriate connection based on the result
            string nextNodeId = conditionResult ? GetTrueConnectionId() : GetFalseConnectionId();
            
            if (!string.IsNullOrEmpty(nextNodeId))
            {
                var nextNode = runner.FindNodeById(nextNodeId);
                if (nextNode != null)
                {
                    // Continue to the appropriate branch
                    runner.ContinueToNode(nextNode);
                }
                else
                {
                    Debug.LogWarning($"Next node not found: {nextNodeId}");
                    runner.EndDialogue();
                }
            }
            else
            {
                // No connection for this branch - end dialogue
                runner.EndDialogue();
            }
        }
        
        private bool EvaluateConditions(DialogueVariableManager variableManager)
        {
            if (conditions.Count == 0)
            {
                if (logEvaluation)
                {
                    Debug.Log("[ConditionalNode] No conditions - defaulting to true");
                }
                return true;
            }
            
            bool result = logicType == ConditionLogicType.And;
            
            foreach (var condition in conditions)
            {
                bool conditionResult = EvaluateCondition(condition, variableManager);
                
                if (logEvaluation)
                {
                    Debug.Log($"[ConditionalNode] Condition '{condition.variableName}' {condition.comparisonType} '{condition.expectedValue}' = {conditionResult}");
                }
                
                if (logicType == ConditionLogicType.And)
                {
                    result = result && conditionResult;
                    // Early exit for AND logic
                    if (!result) break;
                }
                else // OR logic
                {
                    result = result || conditionResult;
                    // Early exit for OR logic
                    if (result) break;
                }
            }
            
            return result;
        }
        
        private bool EvaluateCondition(VariableCondition condition, DialogueVariableManager variableManager)
        {
            if (string.IsNullOrEmpty(condition.variableName))
            {
                Debug.LogWarning("Condition has empty variable name");
                return false;
            }
            
            // Get the current variable value
            var currentValue = variableManager.GetVariable(condition.variableName);
            
            // Handle variable doesn't exist case
            if (currentValue == null)
            {
                switch (condition.comparisonType)
                {
                    case ComparisonType.Exists:
                        return false;
                    case ComparisonType.NotExists:
                        return true;
                    default:
                        // For other comparisons, treat missing variables as having default values
                        currentValue = GetDefaultValueForType(condition.valueType);
                        break;
                }
            }
            
            // Convert expected value to the appropriate type
            object expectedValue = ParseValue(condition.expectedValue, condition.valueType);
            
            // Perform comparison
            return CompareValues(currentValue, expectedValue, condition.comparisonType);
        }
        
        private object GetDefaultValueForType(VariableValueType valueType)
        {
            switch (valueType)
            {
                case VariableValueType.String: return "";
                case VariableValueType.Integer: return 0;
                case VariableValueType.Float: return 0f;
                case VariableValueType.Boolean: return false;
                default: return null;
            }
        }
        
        private object ParseValue(string value, VariableValueType valueType)
        {
            switch (valueType)
            {
                case VariableValueType.String:
                    return value ?? "";
                    
                case VariableValueType.Integer:
                    if (int.TryParse(value, out int intResult))
                        return intResult;
                    return 0;
                    
                case VariableValueType.Float:
                    if (float.TryParse(value, out float floatResult))
                        return floatResult;
                    return 0f;
                    
                case VariableValueType.Boolean:
                    if (bool.TryParse(value, out bool boolResult))
                        return boolResult;
                    return false;
                    
                default:
                    return value;
            }
        }
        
        private bool CompareValues(object currentValue, object expectedValue, ComparisonType comparisonType)
        {
            try
            {
                switch (comparisonType)
                {
                    case ComparisonType.Exists:
                        return currentValue != null;
                        
                    case ComparisonType.NotExists:
                        return currentValue == null;
                        
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
                        
                    case ComparisonType.Contains:
                        return currentValue?.ToString().Contains(expectedValue?.ToString() ?? "") ?? false;
                        
                    case ComparisonType.NotContains:
                        return !(currentValue?.ToString().Contains(expectedValue?.ToString() ?? "") ?? false);
                        
                    default:
                        Debug.LogWarning($"Unknown comparison type: {comparisonType}");
                        return false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error comparing values: {ex.Message}");
                return false;
            }
        }
        
        private bool AreValuesEqual(object value1, object value2)
        {
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;
            
            // Try direct equality first
            if (value1.Equals(value2)) return true;
            
            // Try converting to the same type for comparison
            try
            {
                if (value1 is System.IComparable comparable1 && value2 is System.IComparable)
                {
                    var converted2 = System.Convert.ChangeType(value2, value1.GetType());
                    return comparable1.CompareTo(converted2) == 0;
                }
            }
            catch
            {
                // Fall back to string comparison
                return value1.ToString() == value2.ToString();
            }
            
            return false;
        }
        
        private int CompareNumericValues(object value1, object value2)
        {
            try
            {
                double num1 = System.Convert.ToDouble(value1);
                double num2 = System.Convert.ToDouble(value2);
                return num1.CompareTo(num2);
            }
            catch
            {
                // If not numeric, compare as strings
                string str1 = value1?.ToString() ?? "";
                string str2 = value2?.ToString() ?? "";
                return string.Compare(str1, str2, System.StringComparison.Ordinal);
            }
        }
        
        public override int GetMaxConnections()
        {
            return 2; // True and False branches
        }
        
        public override string[] GetConnectionLabels()
        {
            return new string[] { "True", "False" };
        }
        
        /// <summary>
        /// Get the node ID for the true branch.
        /// </summary>
        /// <returns>The node ID for true condition, or null if no connection.</returns>
        public string GetTrueConnectionId()
        {
            return GetConnectionAtIndex(0);
        }
        
        /// <summary>
        /// Get the node ID for the false branch.
        /// </summary>
        /// <returns>The node ID for false condition, or null if no connection.</returns>
        public string GetFalseConnectionId()
        {
            return GetConnectionAtIndex(1);
        }
        
        /// <summary>
        /// Add a new condition.
        /// </summary>
        /// <param name="variableName">Name of the variable to check</param>
        /// <param name="comparisonType">Type of comparison</param>
        /// <param name="expectedValue">Expected value</param>
        /// <param name="valueType">Type of the value</param>
        public void AddCondition(string variableName, ComparisonType comparisonType, string expectedValue, VariableValueType valueType)
        {
            conditions.Add(new VariableCondition
            {
                variableName = variableName,
                comparisonType = comparisonType,
                expectedValue = expectedValue,
                valueType = valueType
            });
        }
        
        /// <summary>
        /// Clear all conditions.
        /// </summary>
        public void ClearConditions()
        {
            conditions.Clear();
        }
        
        /// <summary>
        /// Get all conditions (read-only).
        /// </summary>
        /// <returns>List of conditions</returns>
        public System.Collections.Generic.IReadOnlyList<VariableCondition> GetConditions()
        {
            return conditions.AsReadOnly();
        }
    }
    
    /// <summary>
    /// Represents a condition to check against a variable.
    /// </summary>
    [System.Serializable]
    public class VariableCondition
    {
        [Header("Variable")]
        public string variableName = "";
        
        [Header("Comparison")]
        public ComparisonType comparisonType = ComparisonType.Equals;
        public VariableValueType valueType = VariableValueType.String;
        [TextArea(1, 2)]
        public string expectedValue = "";
        
        [Header("Description (Optional)")]
        [TextArea(1, 3)]
        public string description = "";
    }
    
    /// <summary>
    /// Types of comparisons that can be performed.
    /// </summary>
    public enum ComparisonType
    {
        Equals,                 // ==
        NotEquals,              // !=
        GreaterThan,           // >
        GreaterThanOrEqual,    // >=
        LessThan,              // <
        LessThanOrEqual,       // <=
        Contains,              // string contains
        NotContains,           // string does not contain
        Exists,                // variable exists
        NotExists              // variable does not exist
    }
    
    /// <summary>
    /// Logic type for combining multiple conditions.
    /// </summary>
    public enum ConditionLogicType
    {
        And,    // All conditions must be true
        Or      // At least one condition must be true
    }
}