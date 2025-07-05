using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// A function node for the dialogue graph view.
    /// </summary>
    public class FunctionNode : DialogueConnectionNode
    {
        [SerializeField] private List<string> m_Functions = new List<string>() { "givegold" };
        
        public List<string> Functions
        {
            get => m_Functions;
            set => m_Functions = value;
        }

        public override string GetDisplayTitle()
        {
            return "Function";
        }

        public override string GetDisplayText()
        {
            return $"";
        }

        public override void Execute(DialogueRunner runner)
        {
            runner.RunFunction(this);
        }
        
        public override int GetMaxConnections()
        {
            return 1; // Function nodes can only connect to one next node
        }
        
        /// <summary>
        /// Get the next node ID.
        /// </summary>
        /// <returns>The next node ID, or null if no connection.</returns>
        public string GetNextNodeId()
        {
            return m_Connections.FirstOrDefault();
        }
    }
}