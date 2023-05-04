using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    public class MicrosceneGraphWindow : EditorWindow
    {
        IMGUIContainer      objField;
        MicrosceneGraphView graphView;
        VisualElement       nodeInspector;
        
        TwoPaneSplitView windowSplitView;

        [SerializeField] bool locked;
        // Currently inspected microscene, serialized using instance ID so reference won't get lost
        [SerializeField] InstanceIDReference<Microscene> microscene;

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

        private void OnEnable()
        {
            EditorSceneManager.sceneSaving += OnSceneSaved;
            
            windowSplitView = new();
            
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(MicrosceneGraphViewResources.STYLE_PATH));// Resources.Load<StyleSheet>("MicrosceneGraphStyles"));
            
            nodeInspector = new() { name = "node-inspector" };
            graphView = new MicrosceneGraphView(this);
            graphView.OnSelectedElement += (element) =>
            {
                if (element is MicrosceneNodeView view)
                {
                    while(nodeInspector.childCount > 0)
                        nodeInspector.RemoveAt(0);

                    IMGUIContainer imguiContainer = new IMGUIContainer(() =>
                    {
                        EditorGUILayout.LabelField($"Connected Stacks: {view.connectedPorts}");
                        
                        view.DoGUI(EditorGUIUtility.labelWidth);
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

            ToolbarButton lockedButton = new ToolbarButton();
            Image @lock = new Image();
            @lock.style.maxWidth  = 16;
            @lock.style.maxHeight = 16;
            if (locked)
                @lock.image = new EditorIcon("IN LockButton on act@2x");
            else
                @lock.image = new EditorIcon("IN LockButton@2x");

            lockedButton.Add(@lock);
            lockedButton.clicked += () =>
            {
                locked = !locked;

                if(locked)
                {
                    @lock.image = new EditorIcon("IN LockButton on act@2x");
                }
                else
                {
                    @lock.image = new EditorIcon("IN LockButton@2x");
                    OnSelectionChange(); // Also when unlocking should check for change of target
                }
            };

            veRight.Add(lockedButton);

            objField = new IMGUIContainer(() =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                microscene = EditorGUILayout.ObjectField(microscene, typeof(Microscene), allowSceneObjects: true) as Microscene;

                if (EditorGUI.EndChangeCheck())
                {
                    graphView.GenerateMicrosceneContent(microscene, microscene.value? new(microscene) : null);
                }
                
                if(microscene.value)
                {
                    EditorGUILayout.LabelField(microscene.value.gameObject.scene.name);
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

        private void OnGUI()
        {
            if (!graphView.scene && (UnityEngine.Object)microscene != null)
            {
                graphView.GenerateMicrosceneContent(microscene, microscene.value ? new SerializedObject(microscene) : null);
            }
        }

        // When exiting play mode graph can get out of sync, since play mode changes do not save, so we regenerate it
        private void RecreateOnExitPlaymode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
                graphView.GenerateMicrosceneContent(microscene, microscene.value ? new SerializedObject(microscene) : null);
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
