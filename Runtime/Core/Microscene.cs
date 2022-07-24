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
        Precondition,
        Action
    }

    [Serializable]
    public struct MicrosceneNode // collection of preconditions or actions that are executed in parallel
    {
        [NonSerialized]
        public MicrosceneNodeState nodeState;
        public MicrosceneNodeType  nodeType;
        public int[]               myNodeStack; // index into Preconditions or Actions depending on nodeType
                                                // Usually just 1 element, but can be multiple if they are inside a stack node
        public int[]               actionConnections; // index into m_Nodes
        public int[]               branchConnections;
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
        [SerializeField] MicrosceneNode[] m_Nodes;

        [SerializeReference] MicroPrecondition[] m_MicroPreconditions;
        [SerializeReference] MicroAction[]       m_MicroActions;

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
                executingActions  = new List<int>  (1 + m_MicroActions.Length);
                executingBranches = new List<int[]>(1 + m_Nodes.Length);
            }

            customContextData = customData;

            if(IsExecutingAnyNode)
            {
                for (int i = 0; i < m_Nodes.Length; i++)
                {
                    m_Nodes[i].nodeState = MicrosceneNodeState.None;
                }
            }

            executingActions .Clear();
            executingBranches.Clear();

            executingActions .AddRange(m_RootActions);
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
                    ref var branchNode = ref m_Nodes[branch[i]];
                    Assert.AreEqual(MicrosceneNodeType.Precondition, branchNode.nodeType);

                    if (branchNode.nodeState == MicrosceneNodeState.Finished | branchNode.nodeState == MicrosceneNodeState.None)
                    {
                        foreach (var conditionIndex in branchNode.myNodeStack)
                        {
                            var cond = m_MicroPreconditions[conditionIndex];
                            cond.Start(ctx);
                        }

                        branchNode.nodeState = MicrosceneNodeState.Executing;
                    }
                    else
                    {
                        bool taken = true;
                        foreach (var conditionIndex in branchNode.myNodeStack)
                        {
                            var cond = m_MicroPreconditions[conditionIndex];
                            taken   &= cond.Update(ctx);
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

                ref var node = ref m_Nodes[nodeIndex];
                if (node.nodeState == MicrosceneNodeState.None | node.nodeState == MicrosceneNodeState.Finished)
                {
#if UNITY_ASSERTIONS
                    if (node.nodeState == MicrosceneNodeState.Finished)
                        Debug.LogWarning("Microscene is executing node that was already finished", this);
#endif 

                    Assert.AreEqual(MicrosceneNodeType.Action, node.nodeType);

                    foreach (byte connectionIndex in node.myNodeStack)
                    {
                        var action = m_MicroActions[connectionIndex];
                        action.Reset(ctx);
                    }

                    node.nodeState = MicrosceneNodeState.Executing;
                }
                else
                {
                    bool finished = true;

                    foreach (byte connectionIndex in node.myNodeStack)
                    {
                        var action = m_MicroActions[connectionIndex];
                        finished &= action.UpdateExecute(ctx);
                    }

                    if (finished)
                    {
                        AdvanceNode(ref j, ref node, executingActions);
                    }
                }
            }
        }

        private void AdvanceNode<T>(ref int j, ref MicrosceneNode node, List<T> nodes)
        {
            node.nodeState = MicrosceneNodeState.Finished;
            nodes.RemoveAt(j--);

            if (node.actionConnections != null && node.actionConnections.Length > 0)
                executingActions.AddRange(node.actionConnections);

            if (node.branchConnections != null && node.branchConnections.Length > 0)
                executingBranches.Add(node.branchConnections);
        }

#if UNITY_EDITOR
        internal MicrosceneNode[] AllNodes_Editor { get => m_Nodes; set => m_Nodes = value; }
        internal int[] RootActions_Editor { get => m_RootActions; set => m_RootActions = value; }
        internal int[] RootBranch_Editor { get => m_RootBranches; set => m_RootBranches = value; }

        internal MicroPrecondition[] PreconditionsArray_Editor
        {
            get => m_MicroPreconditions;
            set => m_MicroPreconditions = value;
        }

        internal MicroAction[] MicroactionsArray_Editor
        {
            get => m_MicroActions;
            set => m_MicroActions = value;
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

            if (m_MicroActions is null || m_MicroPreconditions is null)
                return;

            foreach (var node in m_MicroActions)
                node.OnValidate();

            foreach (var node in m_MicroPreconditions)
                node.OnValidate();
        }

        private void OnDrawGizmos()
        {
            if (m_MicroPreconditions is null)
                return;

            foreach (var node in m_MicroPreconditions)
                node.DrawSceneGizmo(false, this);
        }

        private void OnDrawGizmosSelected()
        {
            if (m_MicroPreconditions is null)
                return;

            foreach (var node in m_MicroPreconditions)
                node.DrawSceneGizmo(true, this);
        }
#endif
    }
}
