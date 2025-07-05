using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DialogueSystem
{
    /// <summary>
    /// Representation of a dialogue graph as a Unity asset.
    /// This asset contains a hierarchy of DialogueNode instances.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue.asset", menuName = "Dialogue System/Dialogue")]
    public class DialogueGraphAsset : ScriptableObject
    {
        [Tooltip("The nodes of this dialogue graph.")]
        [SerializeField] List<DialogueNode> m_Nodes = new List<DialogueNode>();
        
        [Tooltip("The starting node of this dialogue.")]
        [SerializeField] string m_StartNodeId = "";
        
        /// <summary>
        /// The nodes of this dialogue graph.
        /// </summary>
        public IEnumerable<DialogueNode> nodes => m_Nodes ?? new List<DialogueNode>();
        
        /// <summary>
        /// The starting node ID of this dialogue.
        /// </summary>
        public string startNodeId => m_StartNodeId ?? "";

        /// <summary>
        /// Add a node to this dialogue graph. The node will be saved as a sub-asset of this graph.
        /// </summary>
        /// <param name="node">The node to add.</param>
        public void AddNode(DialogueNode node)
        {
            if (node == null) return;
            
            // Ensure list is initialized
            if (m_Nodes == null)
                m_Nodes = new List<DialogueNode>();
            
            m_Nodes.Add(node);
            
            // Set as start node if it's the first node
            if (m_Nodes.Count == 1)
            {
                m_StartNodeId = node.nodeId;
            }
            
#if UNITY_EDITOR
            // Only perform asset operations if we're not importing and the asset is persistent
            if (EditorUtility.IsPersistent(this) && !EditorApplication.isCompiling)
            {
                try
                {
                    AssetDatabase.AddObjectToAsset(node, this);
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
                }
                catch (UnityException ex)
                {
                    // Handle the case where we're in an import worker
                    Debug.LogWarning($"Could not add node to asset during import: {ex.Message}");
                }
            }
#endif
        }
        
        /// <summary>
        /// Remove a node from this dialogue graph. The node will be destroyed.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        public void RemoveNode(DialogueNode node)
        {
            if (node == null || m_Nodes == null) return;
            
            // Remove connections to this node
            foreach (var otherNode in m_Nodes)
            {
                if (otherNode is DialogueConnectionNode connectionNode)
                {
                    connectionNode.RemoveConnection(node.nodeId);
                }
            }
            
            // Clear start node if it was removed
            if (m_StartNodeId == node.nodeId)
            {
                m_StartNodeId = m_Nodes.Count > 1 ? m_Nodes.FirstOrDefault(n => n != node)?.nodeId ?? "" : "";
            }

            m_Nodes.Remove(node);
            
#if UNITY_EDITOR
            if (EditorUtility.IsPersistent(this) && !EditorApplication.isCompiling)
            {
                try
                {
                    AssetDatabase.RemoveObjectFromAsset(node);
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
                }
                catch (UnityException ex)
                {
                    Debug.LogWarning($"Could not remove node from asset: {ex.Message}");
                }
            }
#endif
        }

        /// <summary>
        /// Set the start node of this dialogue.
        /// </summary>
        /// <param name="nodeId">The ID of the node to set as start.</param>
        public void SetStartNode(string nodeId)
        {
            if (m_Nodes != null && m_Nodes.Any(n => n != null && n.nodeId == nodeId))
            {
                m_StartNodeId = nodeId;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        /// Find a node by its ID.
        /// </summary>
        /// <param name="nodeId">The ID of the node to find.</param>
        /// <returns>The node if found, null otherwise.</returns>
        public DialogueNode FindNodeById(string nodeId)
        {
            if (m_Nodes == null || string.IsNullOrEmpty(nodeId))
                return null;

            return m_Nodes.FirstOrDefault(n => n.nodeId == nodeId);
        }

        /// <summary>
        /// Get the start node of this dialogue.
        /// </summary>
        /// <returns>The start node if found, null otherwise.</returns>
        public DialogueNode GetStartNode()
        {
            if (m_Nodes == null) return null;
            
            if (!string.IsNullOrEmpty(m_StartNodeId))
            {
                return FindNodeById(m_StartNodeId);
            }
            
            return m_Nodes.FirstOrDefault(n => n != null);
        }

        /// <summary>
        /// Whether this dialogue graph is empty.
        /// </summary>
        public bool isEmpty => m_Nodes == null || m_Nodes.Count == 0 || m_Nodes.All(n => n == null);

        void OnEnable()
        {
            // Ensure list is always initialized
            if (m_Nodes == null)
                m_Nodes = new List<DialogueNode>();
                
            if (string.IsNullOrEmpty(m_StartNodeId))
                m_StartNodeId = "";
        }
        
#if UNITY_EDITOR
        void Awake()
        {
            // Only initialize if we're not in play mode and not importing
            if (!Application.isPlaying && !EditorApplication.isCompiling)
            {
                Init();
            }
        }

        void OnValidate()
        {
            // Clean up any null references
            if (m_Nodes != null)
            {
                m_Nodes.RemoveAll(n => n == null);
            }
            
            // Only initialize if we're not in play mode and not importing
            if (!Application.isPlaying && !EditorApplication.isCompiling)
            {
                Init();
            }
        }

        void Reset()
        {
            Init();
        }
        
        void OnDestroy()
        {
            if (EditorApplication.update != null)
                EditorApplication.update -= DelayedInit;
        }

        void Init()
        {
            // Ensure lists are initialized
            if (m_Nodes == null)
                m_Nodes = new List<DialogueNode>();
                
            // Don't create initial nodes if we already have nodes or we're importing
            if (!isEmpty || EditorApplication.isCompiling)
                return;

            if (AssetDatabase.Contains(this))
            {
                DelayedInit();
            }
            else
            {
                // Use EditorApplication.delayCall instead of update for one-time execution
                EditorApplication.delayCall += DelayedInit;
            }
        }
        
        void DelayedInit()
        {
            // Remove from delayed call to prevent multiple executions
            EditorApplication.delayCall -= DelayedInit;
            
            if (!AssetDatabase.Contains(this) || EditorApplication.isCompiling)
                return;
            
            // Only create initial node if we don't have any nodes
            if (isEmpty)
            {
                // Create initial speech node
                var speechNode = CreateInstance<SpeechNode>();
                speechNode.name = "Start Node";
                if (speechNode is SpeechNode speech)
                {
                    speech.speakerName = "Speaker";
                    speech.dialogueText = "Welcome to the dialogue!";
                }
                AddNode(speechNode);
            }
        }
#endif
    }
}