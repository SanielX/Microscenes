using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Microscenes
{
    public enum MicrosceneNodeState : short
    {
        None,
        Executing,
        Finished,
        
        Crashed,
    }

    [System.Serializable]
    struct MicrosceneNodeEntryConnections
    {   
        [FormerlySerializedAs("connections")] [SerializeReference]
        public MicrosceneNodeEntry[] connectionsArray;
        
        public MicrosceneNodeEntry this[int index] => connectionsArray[index];
        
        public int Length => connectionsArray.Length;
        
        public static implicit operator MicrosceneNodeEntryConnections(MicrosceneNodeEntry[] con) => new()
        {
            connectionsArray = con
        };
    }
     
    // Microscene node can either be one or a stack of multiple nodes
    // Which are connected with each other
    [System.Serializable]
    internal class MicrosceneNodeEntry
    {
        [System.NonSerialized] public int reachedCount;
        
        public int NodeID; // Used for metadata
        public int InputPortsConnectionCount;
        
        [SerializeReference] public MicrosceneStackBehaviour stackBehaviour;
        
        // At least 1 node, can be multiple, in graph represented as stack node
        // Stack node define an AND statement inside a graph
        // To complete, all nodes inside a stack need to finish their execution
        [SerializeField] public MicrosceneNode[]                 nodeStack    = Array.Empty<MicrosceneNode>();
        [SerializeField] public MicrosceneNodeEntryConnections[] connections  = Array.Empty<MicrosceneNodeEntryConnections>();
    }

    public enum MicrosceneGraphState
    {
        NotStarted,
        Executing,
        Finishing,
        Finished,
    }

    [DisallowMultipleComponent]
    public sealed class Microscene : MonoBehaviour
    {   
        MicrosceneGraphState      graphState;
        List<MicrosceneNodeEntry> executingStacks     = new(16);
        List<MicrosceneNodeEntry> executingQuitStacks = new(16);
        object customContextData;
 
        /// <summary>
        /// Should be used if you want to serialize microscene state
        /// </summary>
        public bool IsSkipped
        {
            get => m_Skip;
            set => m_Skip = value;
        }

        internal enum NodeReportResult
        {
            None,
            Updating,
            Succeded,
            Crashed,
        }

        //https://docs.unity3d.com/2021.3/Documentation/Manual/script-Serialization.html
        // States you can have these but actually it only works on top-most fields like this one.
        // If any of inner fields of struct/class are marked as editor only then it breaks
#if UNITY_EDITOR

        internal struct NodeReport
        {
            public NodeReportResult result;
            public Exception        exception;
        }
        
        internal Dictionary<MicrosceneNode, NodeReport> _nodeReports = new(); 
        
        // Gonna leave graph version in case serialization changes much (shouldn't happen tho?)
        [SerializeField] int    m_SerializedVersion;
        [SerializeField] string m_Notes;
        
        [SerializeField] internal string m_MetadataJson;              // This contains data about node positions, sticky notes, etc
        [SerializeReference] internal MicrosceneNodeEntry[] m_AllEntries = Array.Empty<MicrosceneNodeEntry>(); // This is needed to load graph back properly
                                                                          // since root only contains connections to nodes it needs
                                                                          // therefore if we restore graph just by looking at nodes root is connected to,
                                                                          // we'll loose any information about unconnected nodes
#endif
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        internal void Report(in MicrosceneContext context, NodeReportResult report, Exception exception = null)
        {
#if UNITY_EDITOR
            _nodeReports[context.CurrentNode] = new()
            {
                result        = report, 
                exception     = exception,
            };
#endif
        }

        [Tooltip("When microscene starts it immediately moves to Finishing state")]
        [SerializeField]     internal bool   m_Skip;
        [SerializeReference] internal MicrosceneNodeEntry m_Root;
        [FormerlySerializedAs("m_ExitRoot")] [SerializeReference] internal MicrosceneNodeEntry m_QuitRoot; // Is executed when node is done
        
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
            get => (executingStacks.Count > 0 || executingQuitStacks.Count > 0) && isActiveAndEnabled;
        }
        
        public MicrosceneGraphState GraphState => graphState;

        void Start()
        {
            if (context is null)
                StartExecutingMicroscene(null);
        }

        /// <summary>
        /// Starts or restarts execution of a graph
        /// </summary>
        public void StartExecutingMicroscene(object customData)
        {
#if UNITY_ASSERTIONS
            _nodeReports.Clear();
            if (customData != null && context is null)
                Debug.LogWarning("Microscene has no context but is invoked with non null custom data", this);
#endif
            if (graphState > MicrosceneGraphState.NotStarted)
            {
                executingStacks.Clear();
                executingQuitStacks.Clear();
            }
            
#if UNITY_EDITOR
            if (m_Skip)
            {
                Finish();
            }
            else
#endif
            {
                graphState = MicrosceneGraphState.Executing;
                MicrosceneStackContext ctx = new(this, customContextData);
                AdvanceGraphExecution(m_Root, 0, executingStacks, ref ctx);
            }

            customContextData = customData;
            enabled = true;
        }

        void LateUpdate()
        {
            ExecuteEntriesList(executingStacks);

            if (graphState == MicrosceneGraphState.Finishing)
            {
                ExecuteEntriesList(executingQuitStacks);

                if (executingQuitStacks.Count == 0)
                {
                    graphState = MicrosceneGraphState.Finished;
                }
            }
            // Automatically finish if no node finishes manually
            else if (graphState < MicrosceneGraphState.Finishing && !IsExecutingAnyNode)
            {
                Finish();
            }
        }

        private void ExecuteEntriesList(List<MicrosceneNodeEntry> entriesList)
        {
            MicrosceneStackContext stackContext = new(this, customContextData);
            
            for (int j = 0; j < entriesList.Count; j++)
            {
                int winnerConnection = 0;
                var nodeEntry = entriesList[j];           // Stack of nodes
                stackContext.childrenNodes = nodeEntry.nodeStack; // Pase it into context
                
                bool finished = true;

#if UNITY_ASSERTIONS
                Assert.IsNotNull(nodeEntry.stackBehaviour, "Stack behvaiour must not be null, graph was not properly serialized!");
                
                if (nodeEntry.nodeStack.Length == 0)
                {
                    Debug.LogError($"One of the stacks inside microscene has 0 children but is used in graph, " +
                                   $"this should be fixed, '{name}' scene:'{gameObject.scene.name}'", this);
                    
                    
                    entriesList.RemoveAt(j--);
                    AdvanceGraphExecution(nodeEntry, winnerConnection, entriesList, ref stackContext);
                    continue;
                }
                
                try
                {
#endif
                    // Actually important stuff
                    var result = nodeEntry.stackBehaviour.Update(ref stackContext);
                    winnerConnection = result.winnerIndex;
                    finished         = result.finished;
                    
#if UNITY_ASSERTIONS
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
                    
                if(winnerConnection < 0 || winnerConnection >= nodeEntry.connections.Length)
                    throw new System.IndexOutOfRangeException($"winnerConnection was set to invalid value of '{winnerConnection}'");
#endif 
                

                if (finished)
                {
                    entriesList.RemoveAt(j--);
                    AdvanceGraphExecution(nodeEntry, winnerConnection, entriesList, ref stackContext);
                }
            }
        }

        private void AdvanceGraphExecution(MicrosceneNodeEntry stack, int winnerConnection, 
                                           List<MicrosceneNodeEntry> targetExecutionList, ref MicrosceneStackContext context)
        {
            if(stack.connections.Length == 0)
                return;
            
            var connections = stack.connections[winnerConnection].connectionsArray;
            for (int i = 0; i < connections.Length; i++)
            {
                // This is how "AND" is implemented is microscenes
                // when node stack completes it adds 1 to all nodes it is connected to
                // Since even if stack can have multiple outputs, only one is taken at a time, adding only 1 is fine
                // And since every node is bound to hit this, last one to finish will kickstart next node execution as counter is reached
                var connection = connections[i];
                connection.reachedCount++;

                if (connection.reachedCount >= connection.InputPortsConnectionCount)
                {
                    Assert.IsTrue(connection.reachedCount <= connection.InputPortsConnectionCount,
                        "This shouldn't happen? Node was reached by more nodes than amount of stacks connected to it");

                    for (int k = 0; k < connection.nodeStack.Length; k++)
                    {
                        connection.nodeStack[k].ResetState();
                    }
                    
                    context.childrenNodes = connection.nodeStack;
                    connection.stackBehaviour.Start(ref context);
                    targetExecutionList.Add(connection);
                }
            }
        }

        internal void Finish()
        {
            if (graphState >= MicrosceneGraphState.Finishing) // Finished || Finishing
                return;
            
            MicrosceneStackContext ctx = new(this, customContextData);
                
            if (m_QuitRoot is not null && m_QuitRoot.connections.Length > 0)
            {
                graphState = MicrosceneGraphState.Finishing;
                AdvanceGraphExecution(m_QuitRoot, 0, executingQuitStacks, ref ctx);
            }
            else
            {
                graphState = MicrosceneGraphState.Finished;
            }
        }
        
#if UNITY_EDITOR

        private void OnValidate()
        {
            m_SerializedVersion = 2;
            
            if (!UnityEditor.EditorApplication.isPlaying && context != null)
                enabled = false;
        }

        private void OnDrawGizmos()
        {
            if (m_AllEntries is null)
                return;

            foreach (var entry in m_AllEntries)
            {
                foreach (var node in entry.nodeStack)
                {
                    node?.OnDrawSceneGizmo(false, this);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (m_AllEntries is null)
                return;

            foreach (var entry in m_AllEntries)
            {
                foreach (var node in entry.nodeStack)
                {
                    node?.OnDrawSceneGizmo(true, this);
                }
            }
        }
#endif
    }
}
