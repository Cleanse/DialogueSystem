using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// A dialogue node that sets or modifies dialogue variables.
    /// </summary>
    public class VariableSetNode : DialogueConnectionNode
    {
        [Header("Variable Operations")]
        [SerializeField] private List<VariableOperation> operations = new List<VariableOperation>();

        [Header("Debug")]
        [SerializeField] private bool logOperations = true;
        
        public bool LogOperations
        {
            get => logOperations;
            set => logOperations = value;
        }

        public override string GetDisplayTitle()
        {
            return "Set Variables";
        }

        public override string GetDisplayText()
        {
            if (operations.Count == 0)
                return "No operations";

            if (operations.Count == 1)
                return $"Set {operations[0].variableName}";

            return $"Set {operations.Count} variables";
        }

        public override void Execute(DialogueRunner runner)
        {
            var variableManager = DialogueVariableManager.Instance;
            if (variableManager == null)
            {
                Debug.LogError("DialogueVariableManager not found in scene!");
                return;
            }

            foreach (var operation in operations)
            {
                ExecuteOperation(operation, variableManager);
            }

            // Variable set nodes auto-continue like function nodes
            // This will be handled by the DialogueRunner's auto-continue system
        }

        private void ExecuteOperation(VariableOperation operation, DialogueVariableManager variableManager)
        {
            if (string.IsNullOrEmpty(operation.variableName))
            {
                Debug.LogWarning("Variable operation has empty variable name");
                return;
            }

            try
            {
                switch (operation.operationType)
                {
                    case VariableOperationType.Set:
                        ExecuteSetOperation(operation, variableManager);
                        break;

                    case VariableOperationType.Add:
                        ExecuteAddOperation(operation, variableManager);
                        break;

                    case VariableOperationType.Subtract:
                        ExecuteSubtractOperation(operation, variableManager);
                        break;

                    case VariableOperationType.Multiply:
                        ExecuteMultiplyOperation(operation, variableManager);
                        break;

                    case VariableOperationType.Divide:
                        ExecuteDivideOperation(operation, variableManager);
                        break;

                    case VariableOperationType.Toggle:
                        ExecuteToggleOperation(operation, variableManager);
                        break;

                    case VariableOperationType.Append:
                        ExecuteAppendOperation(operation, variableManager);
                        break;

                    case VariableOperationType.SetMax:
                        ExecuteSetMaxOperation(operation, variableManager);
                        break;

                    case VariableOperationType.SetMin:
                        ExecuteSetMinOperation(operation, variableManager);
                        break;
                }

                if (logOperations)
                {
                    Debug.Log($"[VariableSetNode] Executed {operation.operationType} on '{operation.variableName}'");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"Failed to execute variable operation {operation.operationType} on '{operation.variableName}': {ex.Message}");
            }
        }

        private void ExecuteSetOperation(VariableOperation operation, DialogueVariableManager variableManager)
        {
            var value = ParseValue(operation.value, operation.valueType);
            variableManager.SetVariable(operation.variableName, value);
        }

        private void ExecuteAddOperation(VariableOperation operation, DialogueVariableManager variableManager)
        {
            var currentValue = variableManager.GetVariable<float>(operation.variableName, 0f);
            var addValue = ParseValue(operation.value, VariableValueType.Float);
            variableManager.SetVariable(operation.variableName, currentValue + (float)addValue);
        }

        private void ExecuteSubtractOperation(VariableOperation operation, DialogueVariableManager variableManager)
        {
            var currentValue = variableManager.GetVariable<float>(operation.variableName, 0f);
            var subtractValue = ParseValue(operation.value, VariableValueType.Float);
            variableManager.SetVariable(operation.variableName, currentValue - (float)subtractValue);
        }

        private void ExecuteMultiplyOperation(VariableOperation operation, DialogueVariableManager variableManager)
        {
            var currentValue = variableManager.GetVariable<float>(operation.variableName, 0f);
            var multiplyValue = ParseValue(operation.value, VariableValueType.Float);
            variableManager.SetVariable(operation.variableName, currentValue * (float)multiplyValue);
        }

        private void ExecuteDivideOperation(VariableOperation operation, DialogueVariableManager variableManager)
        {
            var currentValue = variableManager.GetVariable<float>(operation.variableName, 0f);
            var divideValue = ParseValue(operation.value, VariableValueType.Float);
            var divisor = (float)divideValue;

            if (Mathf.Approximately(divisor, 0f))
            {
                Debug.LogWarning($"Division by zero attempted for variable '{operation.variableName}'");
                return;
            }

            variableManager.SetVariable(operation.variableName, currentValue / divisor);
        }

        private void ExecuteToggleOperation(VariableOperation operation, DialogueVariableManager variableManager)
        {
            var currentValue = variableManager.GetVariable<bool>(operation.variableName, false);
            variableManager.SetVariable(operation.variableName, !currentValue);
        }

        private void ExecuteAppendOperation(VariableOperation operation, DialogueVariableManager variableManager)
        {
            var currentValue = variableManager.GetVariable<string>(operation.variableName, "");
            var appendValue = ParseValue(operation.value, VariableValueType.String);
            variableManager.SetVariable(operation.variableName, currentValue + (string)appendValue);
        }

        private void ExecuteSetMaxOperation(VariableOperation operation, DialogueVariableManager variableManager)
        {
            var currentValue = variableManager.GetVariable<float>(operation.variableName, float.MinValue);
            var maxValue = ParseValue(operation.value, VariableValueType.Float);
            variableManager.SetVariable(operation.variableName, Mathf.Max(currentValue, (float)maxValue));
        }

        private void ExecuteSetMinOperation(VariableOperation operation, DialogueVariableManager variableManager)
        {
            var currentValue = variableManager.GetVariable<float>(operation.variableName, float.MaxValue);
            var minValue = ParseValue(operation.value, VariableValueType.Float);
            variableManager.SetVariable(operation.variableName, Mathf.Min(currentValue, (float)minValue));
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
                    // Also accept numeric values (0 = false, anything else = true)
                    if (float.TryParse(value, out float numericResult))
                        return !Mathf.Approximately(numericResult, 0f);
                    return false;

                default:
                    return value;
            }
        }

        public override int GetMaxConnections()
        {
            return 1; // Variable set nodes continue to one next node
        }

        /// <summary>
        /// Get the next node ID.
        /// </summary>
        /// <returns>The next node ID, or null if no connection.</returns>
        public string GetNextNodeId()
        {
            return GetConnectionAtIndex(0);
        }

        /// <summary>
        /// Add a new variable operation.
        /// </summary>
        /// <param name="variableName">Name of the variable</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="value">Value for the operation</param>
        /// <param name="valueType">Type of the value</param>
        public void AddOperation(string variableName, VariableOperationType operationType, string value,
            VariableValueType valueType)
        {
            operations.Add(new VariableOperation
            {
                variableName = variableName,
                operationType = operationType,
                value = value,
                valueType = valueType
            });
        }

        /// <summary>
        /// Clear all operations.
        /// </summary>
        public void ClearOperations()
        {
            operations.Clear();
        }

        /// <summary>
        /// Get all operations (read-only).
        /// </summary>
        /// <returns>List of operations</returns>
        public IReadOnlyList<VariableOperation> GetOperations()
        {
            return operations.AsReadOnly();
        }
    }

    /// <summary>
    /// Represents a single variable operation.
    /// </summary>
    [System.Serializable]
    public class VariableOperation
    {
        [Header("Variable")] public string variableName = "";

        [Header("Operation")] public VariableOperationType operationType = VariableOperationType.Set;

        [Header("Value")] public VariableValueType valueType = VariableValueType.String;
        [TextArea(1, 3)] public string value = "";

        [Header("Options")] public bool useCurrentVariableValue = false;
        public string sourceVariableName = "";
    }

    /// <summary>
    /// Types of operations that can be performed on variables.
    /// </summary>
    public enum VariableOperationType
    {
        Set, // variable = value
        Add, // variable += value
        Subtract, // variable -= value
        Multiply, // variable *= value
        Divide, // variable /= value
        Toggle, // variable = !variable (for booleans)
        Append, // variable += value (for strings)
        SetMax, // variable = max(variable, value)
        SetMin // variable = min(variable, value)
    }

    /// <summary>
    /// Types of values that can be used in variable operations.
    /// </summary>
    public enum VariableValueType
    {
        String,
        Integer,
        Float,
        Boolean
    }
}