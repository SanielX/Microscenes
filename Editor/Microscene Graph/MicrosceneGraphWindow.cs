using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    public class MicrosceneGraphWindow : EditorWindow, IHasCustomMenu
    {
        IMGUIContainer      objField;
        MicrosceneGraphView graphView;
        VisualElement       nodeInspector;
        
        TwoPaneSplitView windowSplitView;

        [SerializeField] bool locked;
        // Currently inspected microscene, serialized using instance ID so reference won't get lost
        [SerializeField] InstanceIDReference<Microscene> microscene;

        private static GUIStyle lockButtonStyle;
        void ShowButton(Rect rect)
        {
            if (lockButtonStyle == null) {
                lockButtonStyle = "IN LockButton";
            }
            
            bool newLocked = GUI.Toggle(rect, this.locked, GUIContent.none, lockButtonStyle);
            if(newLocked != locked)
                FlipLocked();
        }
        
        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Locked"), locked, FlipLocked);
        }

        [MenuItem("Window/Microscene Graph Editor")]
        public static void Open()
        {
            var window = EditorWindow.GetWindow<MicrosceneGraphWindow>();
            window.Show();
        }

        private void OnSelectionChange()
        { 
            if (locked)
                return;

            foreach(var go in Selection.gameObjects)
            {
                if (EditorUtility.IsPersistent(go))
                    continue;

                if(go.TryGetComponent<Microscene>(out var scene))
                {
                    SetMicroscene(scene);
                    break;
                }
            }   
        }
        
        private static GUIStyle wrappedLabel;
        private Vector2 nodeInspectorScroll;
        private void OnEnable()
        {
            EditorSceneManager.sceneSaving += OnSceneSaved;
            
            windowSplitView = new();
            
            rootVisualElement.styleSheets.Add(MicrosceneGraphViewResources.LoadStyles());// Resources.Load<StyleSheet>("MicrosceneGraphStyles"));
            
            nodeInspector = new() { name = "node-inspector" };
            graphView = new MicrosceneGraphView(this);
            graphView.OnSelectedElement += (element) =>
            {
                if (element is MicrosceneNodeView view)
                {
                    while(nodeInspector.childCount > 0)
                        nodeInspector.RemoveAt(0);

                    IMGUIContainer imguiContainer = null;
                    imguiContainer = new IMGUIContainer(() =>
                    {
                        EditorGUILayout.LabelField($"Connected Stacks: {view.connectedPorts}");
                        EditorGUILayout.LabelField($"NodeID: {view.NodeID}");
                        
                        view.DoGUI(EditorGUIUtility.labelWidth);

                        if (view.LastException is not null)
                        {
                            wrappedLabel ??= new(EditorStyles.label) { wordWrap = true };
                            
                            EditorGUILayout.Space();
                            
                            var exceptionLog = view.LastException.ToString();
                            
                            float textSize = wrappedLabel.CalcHeight(new GUIContent(exceptionLog), imguiContainer.resolvedStyle.width);
                            textSize = Mathf.Min(500, textSize);
                            
                            nodeInspectorScroll = EditorGUILayout.BeginScrollView(nodeInspectorScroll, GUILayout.MaxHeight(500));
                            
                            EditorGUILayout.SelectableLabel(exceptionLog, wrappedLabel, GUILayout.MinHeight(textSize));
                            
                            EditorGUILayout.EndScrollView();
                        }
                    });
                    imguiContainer.name = "inspector-imgui";
                    imguiContainer.AddToClassList("imgui-container");
                    imguiContainer.AddToClassList("microscene-node-imgui-container");
                    nodeInspector.Add(imguiContainer);
                }
            };
            // graphView.StretchToParentSize();
            
            windowSplitView.Add(graphView);
            windowSplitView.Add(nodeInspector);
            windowSplitView.fixedPaneIndex = 1;
            windowSplitView.fixedPaneInitialDimension = 150; // EditorPrefs.GetFloat("MicrosceneView_Split_Size", 50);
            windowSplitView.viewDataKey = "MicrosceneView_Split_Size";
            windowSplitView.orientation = TwoPaneSplitViewOrientation.Horizontal;
            
            // windowSplitView.StretchToParentSize();
            
            titleContent = new GUIContent("Microscene Graph Editor");

            var toolbar = new Toolbar();
            toolbar.style.justifyContent = Justify.SpaceBetween;

            var veLeft  = new VisualElement();
            veLeft.style.flexDirection = FlexDirection.Row;

            var veRight = new VisualElement();
            veRight.style.flexDirection = FlexDirection.RowReverse;

            objField = new IMGUIContainer(() =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                var newMicroscene = EditorGUILayout.ObjectField(microscene, typeof(Microscene), allowSceneObjects: true) as Microscene;

                if (EditorGUI.EndChangeCheck() && microscene.value != newMicroscene)
                {
                    Undo.RecordObject(this, "Change microscene");
                    microscene = newMicroscene;
                    
                    graphView.GenerateMicrosceneContent(microscene, microscene.value? new(microscene) : null);
                }
                
                if(microscene.value)
                {
                    var sceneName = microscene.value.gameObject.scene.name;
                    if(EditorUtility.IsDirty(microscene.value))
                        sceneName += "*";
                    
                    EditorGUILayout.LabelField(sceneName);
                }

                EditorGUILayout.EndHorizontal();
            });

            veLeft.Add(objField);

            toolbar.Add(veLeft);
            toolbar.Add(veRight);

            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(windowSplitView);

            EditorApplication.playModeStateChanged += RecreateOnExitPlaymode;
        }

        void FlipLocked()
        {
            locked = !locked;

            if (!locked)
            {
                OnSelectionChange(); // Also when unlocking should check for change of target
            }
        }

        private void OnGUI()
        {
            if (!graphView.microsceneComponent && (UnityEngine.Object)microscene != null)
            {
                graphView.GenerateMicrosceneContent(microscene, microscene.value ? new SerializedObject(microscene) : null);
            }

            if (microscene.value)
            {
                graphView.UpdateNodesFromReport();
                
                if(Application.isPlaying)
                    Repaint();
            }
        }

        // When exiting play mode graph can get out of sync, since play mode changes do not save, so we regenerate it
        private void RecreateOnExitPlaymode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                graphView.GenerateMicrosceneContent(microscene,
                    microscene.value ? new SerializedObject(microscene) : null);
                
                base.autoRepaintOnSceneChange = false;
            }
            else if(change == PlayModeStateChange.EnteredPlayMode)
                base.autoRepaintOnSceneChange = true;
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneSaving -= OnSceneSaved;
            graphView.Serialize();
        }

        private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene, string _)
        {
            if (microscene.value && microscene.value.gameObject.scene == scene)
                graphView.Serialize();
        }

        public void SetMicroscene(Microscene scene, bool serializePrevious = true)
        {
            if (microscene.value == scene)
                return;

            if (microscene.value && serializePrevious)
                graphView.Serialize();

            microscene = scene;
            graphView.GenerateMicrosceneContent(microscene, microscene.value? new SerializedObject(microscene) : null);
        }
    }
}
