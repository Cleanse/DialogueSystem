using UnityEngine;
using System;

namespace DialogueSystem
{
    /// <summary>
    /// Handles dialogue execution for the graph-based system.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        [SerializeField] 
        private DialogueGraphAsset currentDialogue;
        
        private DialogueNode _currentNode;
        private bool _isRunning;
        
        // Events
        public event Action<SpeechNode> OnSpeechDisplayed;
        public event Action<ChoiceNode> OnChoiceDisplayed;
        public event Action<FunctionNode> OnFunctionRan;
        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;
        
        public bool IsRunning => _isRunning;
        public DialogueNode CurrentNode => _currentNode;
        
        public void StartDialogue(DialogueGraphAsset dialogue)
        {
            if (dialogue == null || dialogue.isEmpty)
            {
                Debug.LogWarning("Cannot start dialogue: dialogue is null or empty");
                return;
            }
            
            currentDialogue = dialogue;
            _currentNode = dialogue.GetStartNode();
            _isRunning = true;
            
            OnDialogueStarted?.Invoke();
            
            if (_currentNode != null)
            {
                ExecuteCurrentNode();
            }
            else
            {
                Debug.LogError("No start node found in dialogue");
                EndDialogue();
            }
        }
        
        public void DisplaySpeech(SpeechNode speechNode)
        {
            OnSpeechDisplayed?.Invoke(speechNode);
        }
        
        public void DisplayChoice(ChoiceNode choiceNode)
        {
            OnChoiceDisplayed?.Invoke(choiceNode);
        }

        public void RunFunction(FunctionNode functionNode)
        {
            OnFunctionRan?.Invoke(functionNode);
            // Note: Auto-continuation is handled in ExecuteCurrentNode()
        }
        
        public void Continue()
        {
            if (!_isRunning || _currentNode == null)
                return;
            
            ContinueFromCurrentNode();
        }
        
        /// <summary>
        /// Continue to a specific node (used by conditional nodes)
        /// </summary>
        /// <param name="node">The node to continue to</param>
        public void ContinueToNode(DialogueNode node)
        {
            if (!_isRunning || node == null)
                return;
                
            _currentNode = node;
            ExecuteCurrentNode();
        }
        
        /// <summary>
        /// Find a node by ID in the current dialogue
        /// </summary>
        /// <param name="nodeId">The ID of the node to find</param>
        /// <returns>The node if found, null otherwise</returns>
        public DialogueNode FindNodeById(string nodeId)
        {
            return currentDialogue?.FindNodeById(nodeId);
        }
        
        public void SelectChoice(int choiceIndex)
        {
            if (!_isRunning || !(_currentNode is ChoiceNode choiceNode))
                return;
            
            var nextNodeId = choiceNode.GetChoiceNodeId(choiceIndex);
            if (!string.IsNullOrEmpty(nextNodeId))
            {
                var nextNode = currentDialogue.FindNodeById(nextNodeId);
                if (nextNode != null)
                {
                    _currentNode = nextNode;
                    ExecuteCurrentNode();
                }
                else
                {
                    Debug.LogWarning($"Choice target node not found: {nextNodeId}");
                    EndDialogue();
                }
            }
            else
            {
                EndDialogue();
            }
        }
        
        public void EndDialogue()
        {
            _isRunning = false;
            _currentNode = null;
            currentDialogue = null;
            
            OnDialogueEnded?.Invoke();
        }
        
        [ContextMenu("Start Dialogue")]
        public void StartCurrentDialogue()
        {
            if (currentDialogue != null)
            {
                StartDialogue(currentDialogue);
            }
        }
        
        /// <summary>
        /// Execute the current node and handle auto-progression for function nodes
        /// </summary>
        void ExecuteCurrentNode()
        {
            if (_currentNode == null || !_isRunning)
                return;
                
            _currentNode.Execute(this);
            
            // Auto-continue for certain node types
            if (_currentNode is FunctionNode || _currentNode is VariableSetNode || _currentNode is ConditionalNode)
            {
                // Note: ConditionalNode handles its own continuation logic in Execute()
                if (!(_currentNode is ConditionalNode))
                {
                    ContinueFromCurrentNode();
                }
            }
        }
        
        /// <summary>
        /// Continue from the current node to the next one
        /// </summary>
        void ContinueFromCurrentNode()
        {
            if (!_isRunning || _currentNode == null)
                return;
            
            string nextNodeId = null;
            
            // Get next node ID based on current node type
            if (_currentNode is SpeechNode speechNode)
            {
                nextNodeId = speechNode.GetNextNodeId();
            }
            else if (_currentNode is FunctionNode functionNode)
            {
                nextNodeId = functionNode.GetNextNodeId();
            }
            else if (_currentNode is VariableSetNode variableSetNode)
            {
                nextNodeId = variableSetNode.GetNextNodeId();
            }
            else
            {
                Debug.LogWarning($"Cannot continue from node type: {_currentNode.GetType().Name}");
                return;
            }
            
            // Move to next node or end dialogue
            if (!string.IsNullOrEmpty(nextNodeId))
            {
                var nextNode = currentDialogue.FindNodeById(nextNodeId);
                if (nextNode != null)
                {
                    _currentNode = nextNode;
                    ExecuteCurrentNode();
                }
                else
                {
                    Debug.LogWarning($"Next node not found: {nextNodeId}");
                    EndDialogue();
                }
            }
            else
            {
                EndDialogue();
            }
        }
    }
}