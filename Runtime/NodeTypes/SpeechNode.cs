using System.Linq;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// A speech node for the dialogue graph view.
    /// </summary>
    public class SpeechNode : DialogueConnectionNode
    {
        [Header("Speaker Settings")]
        [SerializeField]
        public string speakerName = "Speaker";
        
        [SerializeField]
        public Character speakerCharacter;
        
        [SerializeField]
        [Portrait]
        public string portraitName = "Default";
        
        [Header("Dialogue Content")]
        [SerializeField]
        [TextArea(3, 6)]
        public string dialogueText = "Enter dialogue text here...";
        
        public override string GetDisplayTitle()
        {
            return "Speech";
        }
        
        public override string GetDisplayText()
        {
            return $"{GetEffectiveSpeakerName()}: {dialogueText}";
        }
        
        public override void Execute(DialogueRunner runner)
        {
            runner.DisplaySpeech(this);
        }
        
        public override int GetMaxConnections()
        {
            return 1; // Speech nodes can only connect to one next node
        }
        
        /// <summary>
        /// Get the next node ID.
        /// </summary>
        /// <returns>The next node ID, or null if no connection.</returns>
        public string GetNextNodeId()
        {
            return m_Connections.FirstOrDefault();
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
        /// Check if this speech node has a valid character assigned.
        /// </summary>
        /// <returns>True if a character is assigned, false otherwise.</returns>
        public bool HasCharacter()
        {
            return speakerCharacter != null;
        }
    }
}