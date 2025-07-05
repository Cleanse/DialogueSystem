using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// Represents a dialogue option that can be selected by the player.
    /// </summary>
    [System.Serializable]
    public class DialogueOption
    {
        [Header("Display")]
        public string displayName = "Talk";
        [TextArea(2, 3)]
        public string description = "";
        
        [Header("Dialogue")]
        public DialogueGraphAsset dialogueGraph;
        
        [Header("Availability")]
        public List<VariableCondition> requirementConditions = new List<VariableCondition>();
        public string unavailableReason = "Not available";
        
        [Header("Categorization")]
        public DialogueCategory category = DialogueCategory.General;
        public int priority = 0;
        
        [Header("Tracking")]
        public bool markAsReadAfterCompleted = true;
        public string readVariableName = "";
        
        public DialogueOption()
        {
            // Default constructor
        }
        
        public DialogueOption(string displayName, DialogueGraphAsset dialogueGraph)
        {
            this.displayName = displayName;
            this.dialogueGraph = dialogueGraph;
        }
    }
    
    /// <summary>
    /// Categories for organizing dialogue options.
    /// </summary>
    public enum DialogueCategory
    {
        General,
        Quest,
        Shop,
        Information,
        Personal,
        Goodbye
    }
}