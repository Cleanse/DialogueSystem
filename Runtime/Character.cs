using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DialogueSystem
{
    /// <summary>
    /// Represents a character in the dialogue system with portrait support.
    /// </summary>
    [CreateAssetMenu(fileName = "New Character", menuName = "Dialogue System/Character")]
    public class Character : ScriptableObject
    {
        [Header("Character Info")]
        [SerializeField] 
        private string characterName = "Character Name";
        
        [SerializeField]
        [TextArea(2, 3)]
        private string characterDescription = "Character description...";
        
        [Header("Default Portrait")]
        [SerializeField] 
        private Sprite defaultPortrait;
        
        [Header("Portraits")]
        public List<CharacterPortrait> portraits = new List<CharacterPortrait>();
        
        [Header("Audio Settings (Optional)")]
        [SerializeField] 
        [Range(0.5f, 2.0f)]
        private float voicePitch = 1.0f;
        
        [SerializeField] 
        private AudioClip voiceClip;
        
        /// <summary>
        /// The display name of this character.
        /// </summary>
        public string CharacterName => characterName;
        
        /// <summary>
        /// Description of this character.
        /// </summary>
        public string CharacterDescription => characterDescription;
        
        /// <summary>
        /// The default portrait sprite for this character.
        /// </summary>
        public Sprite DefaultPortrait => defaultPortrait;
        
        /// <summary>
        /// Voice pitch modifier for audio playback.
        /// </summary>
        public float VoicePitch => voicePitch;
        
        /// <summary>
        /// Optional voice clip for this character.
        /// </summary>
        public AudioClip VoiceClip => voiceClip;
        
        /// <summary>
        /// Get a portrait sprite for a specific portrait name.
        /// </summary>
        /// <param name="portraitName">The name of the portrait to get.</param>
        /// <returns>The sprite for the portrait, or the default portrait if not found.</returns>
        public Sprite GetPortraitForPortrait(string portraitName)
        {
            if (string.IsNullOrEmpty(portraitName))
                return defaultPortrait;
                
            var portrait = portraits.Find(p => p.portraitName.Equals(portraitName, System.StringComparison.OrdinalIgnoreCase));
            
            if (portrait == null)
            {
                return defaultPortrait;
            }
            
            return portrait.portrait ?? defaultPortrait;
        }
        
        /// <summary>
        /// Get all available portrait names for this character.
        /// </summary>
        /// <returns>Array of portrait names.</returns>
        public string[] GetPortraitNames()
        {
            var names = new string[portraits.Count + 1];
            names[0] = "Default";
            
            for (int i = 0; i < portraits.Count; i++)
            {
                names[i + 1] = portraits[i].portraitName;
            }
            
            return names;
        }
        
        /// <summary>
        /// Check if this character has a specific portrait.
        /// </summary>
        /// <param name="portraitName">The portrait name to check.</param>
        /// <returns>True if the portrait exists, false otherwise.</returns>
        public bool HasPortrait(string portraitName)
        {
            if (string.IsNullOrEmpty(portraitName) || portraitName.Equals("Default", System.StringComparison.OrdinalIgnoreCase))
                return true;
                
            return portraits.Exists(p => p.portraitName.Equals(portraitName, System.StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Add a new portrait to this character.
        /// </summary>
        /// <param name="portraitName">The name of the portrait.</param>
        /// <param name="portrait">The portrait sprite for this portrait.</param>
        public void AddPortrait(string portraitName, Sprite portrait)
        {
            if (string.IsNullOrEmpty(portraitName))
                portraitName = "Portrait";
                
            portraits.Add(new CharacterPortrait 
            { 
                portraitName = portraitName, 
                portrait = portrait 
            });
            
            #if UNITY_EDITOR
            // Mark the asset as dirty so Unity saves the changes
            EditorUtility.SetDirty(this);
            #endif
        }
        
        /// <summary>
        /// Remove a portrait from this character.
        /// </summary>
        /// <param name="portraitName">The name of the portrait to remove.</param>
        public void RemovePortrait(string portraitName)
        {
            int removed = portraits.RemoveAll(p => p.portraitName.Equals(portraitName, System.StringComparison.OrdinalIgnoreCase));
            
            #if UNITY_EDITOR
            // Mark the asset as dirty if we removed any portraits
            if (removed > 0)
            {
                EditorUtility.SetDirty(this);
            }
            #endif
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Validate the character data in the editor.
        /// </summary>
        void OnValidate()
        {
            // Skip validation during play mode
            if (Application.isPlaying)
                return;
                
            bool hasChanges = false;
            
            // Ensure no duplicate portrait names
            for (int i = portraits.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(portraits[i].portraitName))
                {
                    portraits.RemoveAt(i);
                    hasChanges = true;
                    continue;
                }
            }
            
            // Mark the asset as dirty if any changes were made
            if (hasChanges)
            {
                EditorUtility.SetDirty(this);
            }
        }
        #endif
    }
    
    /// <summary>
    /// Represents a single portrait for a character.
    /// </summary>
    [System.Serializable]
    public class CharacterPortrait
    {
        public string portraitName = "Portrait Name";
        public Sprite portrait;
        
        // Parameterless constructor for Unity serialization
        public CharacterPortrait()
        {
        }
        
        public CharacterPortrait(string name, Sprite sprite)
        {
            portraitName = name;
            portrait = sprite;
        }
    }
}