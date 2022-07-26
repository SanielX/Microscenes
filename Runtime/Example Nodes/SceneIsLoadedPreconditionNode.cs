using UnityEngine;
using UnityEngine.SceneManagement;

namespace Microscenes
{
    // Example of checking condition every frame
    [MicrosceneNodeType(MicrosceneNodeType.Precondition)]
    [SerializeReferencePath(SRPathType.Abstract, "Scene Is Loaded")]
    public class SceneIsLoaded : MicrosceneNode
    {
        [SerializeField] string m_SceneName;

        protected override void OnUpdate(in MicrosceneContext ctx)
        {
            ResetState(); // Clear result of previous frame

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if(SceneManager.GetSceneAt(i).name == m_SceneName)
                {
                    Complete(); // Set condition as satisfied for this frame
                    break;
                }
            }
        }
    }
}