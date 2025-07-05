using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// A choice node for the dialogue graph view.
    /// </summary>
    public class ChoiceNode : DialogueConnectionNode
    {
        [Header("Speaker Settings")]
        [SerializeField]
        public string speakerName = "Speaker";
        
        [SerializeField]
        public Character speakerCharacter;
        
        [SerializeField]
        [Portrait]
        public string portraitName = "Default";
        
        [Header("Choice Content")]
        [SerializeField]
        [TextArea(2, 4)]
        public string promptText = "What do you want to say?";
        
        [SerializeField]
        public List<string> choiceTexts = new List<string> { "Choice 1", "Choice 2" };
        
        public override string GetDisplayTitle()
        {
            return "Choice";
        }
        
        public override string GetDisplayText()
        {
            return $"{GetEffectiveSpeakerName()}: {promptText}";
        }
        
        public override void Execute(DialogueRunner runner)
        {
            runner.DisplayChoice(this);
        }
        
        public override int GetMaxConnections()
        {
            return choiceTexts.Count; // One connection per choice
        }
        
        public override string[] GetConnectionLabels()
        {
            return choiceTexts.ToArray();
        }
        
        /// <summary>
        /// Get the node ID for a specific choice.
        /// </summary>
        /// <param name="choiceIndex">The index of the choice.</param>
        /// <returns>The node ID for the choice, or null if no connection.</returns>
        public string GetChoiceNodeId(int choiceIndex)
        {
            if (choiceIndex >= 0 && choiceIndex < m_Connections.Count)
            {
                return m_Connections[choiceIndex];
            }
            return null;
        }
        
        /// <summary>
        /// Ensure the connections list matches the number of choices.
        /// </summary>
        public void ValidateChoiceConnections()
        {
            while (m_Connections.Count < choiceTexts.Count)
            {
                m_Connections.Add(null);
            }
            
            while (m_Connections.Count > choiceTexts.Count)
            {
                m_Connections.RemoveAt(m_Connections.Count - 1);
            }
        }
        
        /// <summary>
        /// Get the effective speaker name, prioritizing character over string.
        /// </summary>
        /// <returns>The speaker name to use for display.</returns>
        public string GetEffectiveSpeakerName()
        {
            return speakerCharacter != null ? speakerCharacter.CharacterName : speakerName;
        }
        
        /// <summary>
        /// Get the portrait sprite for the current speaker and portrait.
        /// </summary>
        /// <returns>The portrait sprite, or null if no character or portrait is set.</returns>
        public Sprite GetSpeakerPortrait()
        {
            return speakerCharacter?.GetPortraitForPortrait(portraitName);
        }
        
        /// <summary>
        /// Get the current portrait name, with fallback to "Default".
        /// </summary>
        /// <returns>The portrait name to use.</returns>
        public string GetEffectivePortrait()
        {
            return string.IsNullOrEmpty(portraitName) ? "Default" : portraitName;
        }
        
        /// <summary>
        /// Check if this choice node has a valid character assigned.
        /// </summary>
        /// <returns>True if a character is assigned, false otherwise.</returns>
        public bool HasCharacter()
        {
            return speakerCharacter != null;
        }
    }
}