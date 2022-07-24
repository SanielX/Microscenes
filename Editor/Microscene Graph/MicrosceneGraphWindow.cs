using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    public class MicrosceneGraphWindow : EditorWindow
    {
        IMGUIContainer objField;

        MicrosceneGraphView graphView;

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

            graphView = new MicrosceneGraphView(this);
            graphView.StretchToParentSize();
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
                    OnSelectionChange();
                }
            };

            veRight.Add(lockedButton);

            objField = new IMGUIContainer(() =>
            {
                EditorGUILayout.BeginHorizontal();
                microscene = EditorGUILayout.ObjectField(microscene, typeof(Microscene), allowSceneObjects: true) as Microscene;

                if(microscene.value)
                {
                    EditorGUILayout.LabelField(microscene.value.gameObject.scene.name);
                }

                EditorGUILayout.EndHorizontal();
            });

            veLeft.Add(objField);

            toolbar.Add(veLeft);
            toolbar.Add(veRight);

            rootVisualElement.Add(graphView);
            rootVisualElement.Add(toolbar);

            if (microscene.value)
                graphView.GenerateMicrosceneContent(microscene, microscene.value ? new SerializedObject(microscene) : null);

            EditorApplication.playModeStateChanged += RecreateOnExitPlaymode;
        }

        private void RecreateOnExitPlaymode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
                SetMicroscene(microscene, false);
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
