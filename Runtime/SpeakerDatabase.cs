using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DialogueSystem
{
    /// <summary>
    /// Centralized database for managing all characters in the dialogue system.
    /// Provides easy lookup and management of speaker characters.
    /// </summary>
    [CreateAssetMenu(fileName = "Speaker Database", menuName = "Dialogue System/Speaker Database")]
    public class SpeakerDatabase : ScriptableObject
    {
        [Header("Characters")]
        [SerializeField] 
        private List<Character> characters = new List<Character>();
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Fallback character to use when a requested character is not found")]
        private Character fallbackCharacter;
        
        /// <summary>
        /// All characters in the database.
        /// </summary>
        public IReadOnlyList<Character> Characters => characters.AsReadOnly();
        
        /// <summary>
        /// The fallback character used when a requested character is not found.
        /// </summary>
        public Character FallbackCharacter => fallbackCharacter;
        
        /// <summary>
        /// Get a character by name.
        /// </summary>
        /// <param name="characterName">The name of the character to find.</param>
        /// <returns>The character if found, fallback character if not found, or null if no fallback is set.</returns>
        public Character GetCharacter(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return fallbackCharacter;
                
            var character = characters.FirstOrDefault(c => c != null && 
                c.CharacterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase));
                
            return character ?? fallbackCharacter;
        }
        
        /// <summary>
        /// Get a character by index.
        /// </summary>
        /// <param name="index">The index of the character.</param>
        /// <returns>The character at the specified index, or null if index is out of range.</returns>
        public Character GetCharacter(int index)
        {
            if (index >= 0 && index < characters.Count)
                return characters[index];
                
            return null;
        }
        
        /// <summary>
        /// Check if a character with the given name exists in the database.
        /// </summary>
        /// <param name="characterName">The name to check for.</param>
        /// <returns>True if the character exists, false otherwise.</returns>
        public bool HasCharacter(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return false;
                
            return characters.Any(c => c != null && 
                c.CharacterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Get all character names in the database.
        /// </summary>
        /// <returns>Array of character names.</returns>
        public string[] GetCharacterNames()
        {
            return characters.Where(c => c != null)
                           .Select(c => c.CharacterName)
                           .ToArray();
        }
        
        /// <summary>
        /// Get a character's portrait for a specific portrait name.
        /// </summary>
        /// <param name="characterName">The name of the character.</param>
        /// <param name="portraitName">The portrait name.</param>
        /// <returns>The portrait sprite, or null if character/portrait not found.</returns>
        public Sprite GetCharacterPortrait(string characterName, string portraitName = "Default")
        {
            var character = GetCharacter(characterName);
            return character?.GetPortraitForPortrait(portraitName);
        }
        
        /// <summary>
        /// Add a character to the database.
        /// </summary>
        /// <param name="character">The character to add.</param>
        /// <returns>True if added successfully, false if character is null or already exists.</returns>
        public bool AddCharacter(Character character)
        {
            if (character == null || HasCharacter(character.CharacterName))
                return false;
                
            characters.Add(character);
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
            
            return true;
        }
        
        /// <summary>
        /// Remove a character from the database.
        /// </summary>
        /// <param name="characterName">The name of the character to remove.</param>
        /// <returns>True if removed successfully, false if not found.</returns>
        public bool RemoveCharacter(string characterName)
        {
            var character = characters.FirstOrDefault(c => c != null && 
                c.CharacterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase));
                
            if (character != null)
            {
                characters.Remove(character);
                
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Remove a character from the database by reference.
        /// </summary>
        /// <param name="character">The character to remove.</param>
        /// <returns>True if removed successfully, false if not found.</returns>
        public bool RemoveCharacter(Character character)
        {
            if (characters.Remove(character))
            {
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get the index of a character in the database.
        /// </summary>
        /// <param name="character">The character to find the index of.</param>
        /// <returns>The index of the character, or -1 if not found.</returns>
        public int GetCharacterIndex(Character character)
        {
            return characters.IndexOf(character);
        }
        
        /// <summary>
        /// Get the index of a character by name.
        /// </summary>
        /// <param name="characterName">The name of the character to find the index of.</param>
        /// <returns>The index of the character, or -1 if not found.</returns>
        public int GetCharacterIndex(string characterName)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] != null && 
                    characters[i].CharacterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            
            return -1;
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Validate the database in the editor.
        /// </summary>
        void OnValidate()
        {
            // Skip validation during play mode to avoid unnecessary warnings
            if (Application.isPlaying)
                return;
                
            // Remove null characters
            characters.RemoveAll(c => c == null);
            
            // Use a more robust validation that handles Unity's serialization timing
            EditorApplication.delayCall += ValidateCharacterNames;
        }
        
        void ValidateCharacterNames()
        {
            if (this == null || characters == null) return;
            
            // Check for duplicate names, but ignore default/empty names to avoid false positives
            var validCharacters = characters.Where(c => c != null && 
                                                      !string.IsNullOrEmpty(c.CharacterName) && 
                                                      c.CharacterName != "Character Name" && 
                                                      c.CharacterName != "New Character")
                                          .ToList();
            
            var duplicateGroups = validCharacters.GroupBy(c => c.CharacterName)
                                               .Where(g => g.Count() > 1);
            
            foreach (var group in duplicateGroups)
            {
                var characterList = string.Join(", ", group.Select(c => c.name));
                Debug.LogWarning($"Multiple characters with name '{group.Key}' found in Speaker Database " +
                               $"(Assets: {characterList}). This may cause unexpected behavior.", this);
            }
        }
        
        /// <summary>
        /// Create a default speaker database with common portraits.
        /// </summary>
        [ContextMenu("Create Default Database")]
        void CreateDefaultDatabase()
        {
            // This could be expanded to create default characters
            Debug.Log("Speaker Database created. Add characters manually or through scripts.", this);
        }
        #endif
    }
}