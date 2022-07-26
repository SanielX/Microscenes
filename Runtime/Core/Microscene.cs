using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Microscenes
{

    public enum MicrosceneNodeState : short
    {
        None,
        Executing,
        Finished
    }

    public enum MicrosceneNodeType : short
    {
        Precondition = 1,
        Action = 2,
        Hybrid = Precondition | Action
    }

    [Serializable]
    internal struct MicrosceneNodeData // collection of preconditions or actions that are executed in parallel
    {
        [NonSerialized]
        public MicrosceneNodeState nodeState;
        public MicrosceneNodeType  nodeType;
        public int[]               myNodeStack; // index into Preconditions or Actions depending on nodeType
                                                // Usually just 1 element, but can be multiple if they are inside a stack node
        public int[]               actionConnections; // index into m_Nodes
        public int[]               branchConnections;
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class MicrosceneNodeTypeAttribute : System.Attribute
    {
        public MicrosceneNodeTypeAttribute(MicrosceneNodeType nodeType)
        {
            if (nodeType == 0)
                throw new System.ArgumentException("Node type can not be 0");

            NodeTypeCapabilities = nodeType;
        }

        public MicrosceneNodeType NodeTypeCapabilities { get; }
    }

    [DisallowMultipleComponent]
    public class Microscene : MonoBehaviour
    {
        List<int>   executingActions;
        List<int[]> executingBranches;

#if UNITY_EDITOR
        [SerializeField] string m_MetadataJson; // This contains data about node positions, sticky notes, etc
#endif 

        [SerializeField] int[]            m_RootActions;  // Connections that go from entry node
        [SerializeField] int[]            m_RootBranches; // indexing into m_Nodes array
        [SerializeField] MicrosceneNodeData[] m_NodeData;

        [SerializeReference] MicrosceneNode[] m_Nodes;

        /// <summary>
        /// Context allows to unlock special nodes, available only when microscene is run by another system 
        /// </summary>
        public Type context
        {
            get
            {
                if (TryGetComponent<IMicrosceneContextProvider>(out var provider))
                    return provider.MicrosceneContext;

                return null;
            }
        }

        public bool IsExecutingAnyNode
        {
            get => executingActions.Count > 0 || executingBranches.Count > 0;
        }

        void Start()
        {
#if UNITY_ASSERTIONS
            if (m_RootActions is null || m_RootBranches is null || m_Nodes is null)
            {
                Debug.LogError("Microscene was incorrectly serialized", this);
                return;
            }
#endif 
            if (context is null)
                StartExecutingMicroscene(null);
        }

        object customContextData;

        /// <summary>
        /// Starts or restarts execution of a graph
        /// </summary>
        public void StartExecutingMicroscene(object customData)
        {
#if UNITY_ASSERTIONS
            if (customData != null && context is null)
                Debug.LogWarning("Microscene has no context but is invoked with non null custom data", this);
#endif 
            if (executingActions is null)
            {
                executingActions  = new List<int>(1 + m_Nodes.Length);
                executingBranches = new List<int[]>(1 + m_Nodes.Length);
            }

            customContextData = customData;

            if (IsExecutingAnyNode)
            {
                for (int i = 0; i < m_NodeData.Length; i++)
                {
                    m_NodeData[i].nodeState = MicrosceneNodeState.None;
                }
            }

            executingActions .Clear();
            executingBranches.Clear();

            executingActions.AddRange(m_RootActions);
            executingBranches.Add(m_RootBranches);

            enabled = true;
        }

        void LateUpdate()
        {
            var ctx = new MicrosceneContext(caller: this, customData: customContextData);

            for (int j = 0; j < executingBranches.Count; j++)
            {
                int[] branch = executingBranches[j];

                // Branches are taken in packs
                // This is basically an OR statement, first branch to be met is winning
                // After this, all other preconditions are ignored and all connected nodes of a winning branch are added to the list

                for (int i = 0; i < branch.Length; i++)
                {
                    ref var branchNode = ref m_NodeData[branch[i]];
                    Assert.AreEqual(MicrosceneNodeType.Precondition, branchNode.nodeType);

                    if (branchNode.nodeState == MicrosceneNodeState.Finished | branchNode.nodeState == MicrosceneNodeState.None)
                    {
                        foreach (var conditionIndex in branchNode.myNodeStack)
                        {
                            var cond = m_Nodes[conditionIndex];
                            cond.ResetState();
                        }

                        branchNode.nodeState = MicrosceneNodeState.Executing;
                    }
                    else
                    {
                        bool taken = true;
                        foreach (var conditionIndex in branchNode.myNodeStack)
                        {
                            var cond = m_Nodes[conditionIndex];
                            cond.Update(ctx);

                            taken &= cond.State == MicrosceneNodeState.Finished;
                        }

                        if (taken)
                        {
                            AdvanceNode(ref j, ref branchNode, executingBranches);
                            break;
                        }
                    }
                }
            }

            // Unlike branches, all action nodes are just executed in parallel without waiting for each other
            for (int j = 0; j < executingActions.Count; j++)
            {
                int nodeIndex = executingActions[j];

                ref var node = ref m_NodeData[nodeIndex];
                if (node.nodeState == MicrosceneNodeState.None | node.nodeState == MicrosceneNodeState.Finished)
                {
#if UNITY_ASSERTIONS
                    if (node.nodeState == MicrosceneNodeState.Finished)
                        Debug.LogWarning("Microscene is executing node that was already finished", this);
#endif 

                    Assert.AreEqual(MicrosceneNodeType.Action, node.nodeType);

                    foreach (byte connectionIndex in node.myNodeStack)
                    {
                        var action = m_Nodes[connectionIndex];
                        action.ResetState();
                    }

                    node.nodeState = MicrosceneNodeState.Executing;
                }
                else
                {
                    bool finished = true;

                    foreach (byte connectionIndex in node.myNodeStack)
                    {
                        var action = m_Nodes[connectionIndex];
                        action.Update(ctx);

                        finished &= action.State == MicrosceneNodeState.Finished;
                    }

                    if (finished)
                    {
                        AdvanceNode(ref j, ref node, executingActions);
                    }
                }
            }
        }

        private void AdvanceNode<T>(ref int j, ref MicrosceneNodeData node, List<T> nodes)
        {
            node.nodeState = MicrosceneNodeState.Finished;
            nodes.RemoveAt(j--);

            if (node.actionConnections != null && node.actionConnections.Length > 0)
                executingActions.AddRange(node.actionConnections);

            if (node.branchConnections != null && node.branchConnections.Length > 0)
                executingBranches.Add(node.branchConnections);
        }
        
#if UNITY_EDITOR

        internal MicrosceneNodeData[] AllNodes_Editor { get => m_NodeData ??= new MicrosceneNodeData[0]; set => m_NodeData = value; }
        internal int[] RootActions_Editor { get => m_RootActions ??= new int[0]; set => m_RootActions = value; }
        internal int[] RootBranch_Editor { get => m_RootBranches ??= new int[0]; set => m_RootBranches = value; }

        internal MicrosceneNode[] Nodes_Editor
        {
            get => m_Nodes ??= new MicrosceneNode[0];
            set => m_Nodes = value;
        }

        internal string MetadataJson_Editor
        {
            get => m_MetadataJson;
            set => m_MetadataJson = value;
        }

        private void OnValidate()
        {
            if (!UnityEditor.EditorApplication.isPlaying && context != null)
                enabled = false;

            if (m_Nodes is null)
                return;

            foreach (var node in m_Nodes)
                node?.OnValidate();
        }

        private void OnDrawGizmos()
        {
            if (m_Nodes is null)
                return;

            foreach (var node in m_Nodes)
                node?.OnDrawSceneGizmo(false, this);
        }

        private void OnDrawGizmosSelected()
        {
            if (m_Nodes is null)
                return;

            foreach (var node in m_Nodes)
                node?.OnDrawSceneGizmo(true, this);
        }
#endif
    }
}
