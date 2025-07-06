#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace DialogueSystem
{
    /// <summary>
    /// Custom inspector for the dialogue graph asset.
    /// </summary>
    [CustomEditor(typeof(DialogueGraphAsset))]
    public class DialogueGraphAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var graph = (DialogueGraphAsset)target;
            
            EditorGUILayout.LabelField("Dialogue Graph", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Add null checks for all operations
            var nodeList = graph.nodes?.Where(n => n != null).ToList();
            int nodeCount = nodeList?.Count ?? 0;
            
            EditorGUILayout.LabelField($"Nodes: {nodeCount}");
            
            string startNodeDisplay = "None";
            if (!string.IsNullOrEmpty(graph.startNodeId))
            {
                var startNode = graph.FindNodeById(graph.startNodeId);
                if (startNode != null)
                {
                    startNodeDisplay = startNode.name;
                }
                else
                {
                    startNodeDisplay = $"Missing ({graph.startNodeId})";
                }
            }
            EditorGUILayout.LabelField($"Start Node: {startNodeDisplay}");
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Open in Graph Editor"))
            {
                DialogueGraphWindow.Init();
                Selection.activeObject = graph;
            }
            
            EditorGUILayout.Space();
            
            // Start Node Selection - only show if we have valid nodes
            if (nodeList != null && nodeList.Count > 0)
            {
                EditorGUILayout.LabelField("Start Node Selection", EditorStyles.boldLabel);
                
                var nodeNames = nodeList.Select((n, i) => $"{i}: {n.GetDisplayTitle()} - {n.name}").ToArray();
                var currentIndex = nodeList.FindIndex(n => n.nodeId == graph.startNodeId);
                
                // Handle case where start node is not found
                if (currentIndex < 0)
                    currentIndex = 0;
                
                var newIndex = EditorGUILayout.Popup("Start Node", currentIndex, nodeNames);
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < nodeList.Count)
                {
                    graph.SetStartNode(nodeList[newIndex].nodeId);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No nodes found. Use the Graph Editor to add nodes.", MessageType.Info);
            }
            
            // Debug section
            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Graph"))
            {
                ValidateGraph(graph);
            }
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(graph);
            }
        }
        
        private void ValidateGraph(DialogueGraphAsset graph)
        {
            var issues = new System.Text.StringBuilder();
            var nodeList = graph.nodes?.Where(n => n != null).ToList();
            
            if (nodeList == null || nodeList.Count == 0)
            {
                issues.AppendLine("• No nodes in graph");
            }
            else
            {
                // Check for missing start node
                if (string.IsNullOrEmpty(graph.startNodeId))
                {
                    issues.AppendLine("• No start node set");
                }
                else if (graph.FindNodeById(graph.startNodeId) == null)
                {
                    issues.AppendLine("• Start node reference is broken");
                }
                
                // Check for broken connections
                foreach (var node in nodeList.OfType<DialogueConnectionNode>())
                {
                    foreach (var connectionId in node.connections)
                    {
                        if (!string.IsNullOrEmpty(connectionId) && graph.FindNodeById(connectionId) == null)
                        {
                            issues.AppendLine($"• Node '{node.name}' has broken connection to '{connectionId}'");
                        }
                    }
                }
            }
            
            if (issues.Length > 0)
            {
                EditorUtility.DisplayDialog("Graph Validation", 
                    "Issues found:\n\n" + issues.ToString(), "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Graph Validation", 
                    "No issues found! Graph is valid.", "OK");
            }
        }
    }
}
#endif