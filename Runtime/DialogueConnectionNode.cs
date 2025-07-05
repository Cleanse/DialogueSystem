using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// Base class for dialogue nodes that can connect to other nodes.
    /// </summary>
    public abstract class DialogueConnectionNode : DialogueNode
    {
        [SerializeField]
        protected List<string> m_Connections = new List<string>();
        
        /// <summary>
        /// The connections from this node to other nodes.
        /// </summary>
        public IEnumerable<string> connections => m_Connections;
        
        /// <summary>
        /// Add a connection at a specific index.
        /// </summary>
        /// <param name="nodeId">The ID of the node to connect to.</param>
        /// <param name="index">The index to add the connection at.</param>
        public virtual void SetConnectionAtIndex(string nodeId, int index)
        {
            // Ensure the connections list is large enough
            while (m_Connections.Count <= index)
            {
                m_Connections.Add(null);
            }
            
            m_Connections[index] = nodeId;
        }
        
        /// <summary>
        /// Add a connection to another node (legacy method for compatibility).
        /// </summary>
        /// <param name="nodeId">The ID of the node to connect to.</param>
        public virtual void AddConnection(string nodeId)
        {
            if (!m_Connections.Contains(nodeId))
            {
                m_Connections.Add(nodeId);
            }
        }
        
        /// <summary>
        /// Remove a connection to another node.
        /// </summary>
        /// <param name="nodeId">The ID of the node to disconnect from.</param>
        public virtual void RemoveConnection(string nodeId)
        {
            // Find and remove the connection, replacing with null to maintain indices
            for (int i = 0; i < m_Connections.Count; i++)
            {
                if (m_Connections[i] == nodeId)
                {
                    m_Connections[i] = null;
                }
            }
            
            // Clean up trailing nulls but preserve indices for existing connections
            while (m_Connections.Count > 0 && string.IsNullOrEmpty(m_Connections[m_Connections.Count - 1]))
            {
                m_Connections.RemoveAt(m_Connections.Count - 1);
            }
        }
        
        /// <summary>
        /// Get connection at specific index.
        /// </summary>
        /// <param name="index">The index to get the connection from.</param>
        /// <returns>The node ID at the index, or null if none.</returns>
        public string GetConnectionAtIndex(int index)
        {
            if (index >= 0 && index < m_Connections.Count)
            {
                return m_Connections[index];
            }
            return null;
        }
        
        /// <summary>
        /// Get the maximum number of output connections this node can have.
        /// </summary>
        /// <returns>The maximum number of connections, or -1 for unlimited.</returns>
        public abstract int GetMaxConnections();
        
        /// <summary>
        /// Get the labels for each connection port.
        /// </summary>
        /// <returns>An array of port labels.</returns>
        public virtual string[] GetConnectionLabels()
        {
            return new string[] { "Next" };
        }
        
        /// <summary>
        /// Ensure connections list matches the expected number of outputs.
        /// </summary>
        public virtual void ValidateConnections()
        {
            var maxConnections = GetMaxConnections();
            if (maxConnections > 0)
            {
                while (m_Connections.Count < maxConnections)
                {
                    m_Connections.Add(null);
                }
                
                while (m_Connections.Count > maxConnections)
                {
                    m_Connections.RemoveAt(m_Connections.Count - 1);
                }
            }
        }
    }
}