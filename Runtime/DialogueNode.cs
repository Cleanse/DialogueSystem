using System;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// Base class for all dialogue view nodes.
    /// </summary>
    public abstract class DialogueNode : ScriptableObject
    {
        [SerializeField]
        protected string m_NodeId;
        
        [SerializeField]
        protected Vector2 m_Position;
        
        /// <summary>
        /// Unique identifier for this node.
        /// </summary>
        public string nodeId
        {
            get
            {
                if (string.IsNullOrEmpty(m_NodeId))
                {
                    m_NodeId = Guid.NewGuid().ToString();
                }
                return m_NodeId;
            }
        }
        
        /// <summary>
        /// Position of this node in the graph view.
        /// </summary>
        public Vector2 position
        {
            get => m_Position;
            set => m_Position = value;
        }
        
        protected virtual void OnEnable()
        {
            if (string.IsNullOrEmpty(m_NodeId))
            {
                m_NodeId = Guid.NewGuid().ToString();
            }
        }
        
        /// <summary>
        /// Get the display title for this node.
        /// </summary>
        /// <returns>The display title.</returns>
        public abstract string GetDisplayTitle();
        
        /// <summary>
        /// Get the display text for this node.
        /// </summary>
        /// <returns>The display text.</returns>
        public abstract string GetDisplayText();
        
        /// <summary>
        /// Execute this node's logic.
        /// </summary>
        /// <param name="runner">The dialogue runner.</param>
        public abstract void Execute(DialogueRunner runner);
    }
}