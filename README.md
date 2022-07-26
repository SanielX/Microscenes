# Microscenes
Microscenes is a highly experimental visual scripting solution I developed to design small gameplay sections.
Its main idea is to provide UX with minimum need for technical knowledge & fast iteration, without having to constantly update node database like Bolt forces you to.
All graphs are stored in scene, not as scriptable objects so nodes can contain references to scene objects without any limitations.

Minimum supported unity version: 2020.2.5f1

**Icons shown in examples are not available in this package. You can find example nodes in `Runtime/Example Nodes` directory.
The API will change in the future, no backwards compatability is guaranteed.**

![](Git/Example.png)

Microscene system is designed for linear games and has 2 types of nodes, based on the next observation:
- Game waits for player to do something, hence Precondition nodes
- Game responds to completed condition with an action (playing animation, etc)

Thus, a node can be either an action or a precondition. Main difference is how they are executed, which is explained in the next picture.

![](Git/Rules.png)
![](Git/Mixing.png)

Node can also support being both action and precondition. Such nodes are called "hybdrid" nodes.

## Creating graph
Just add Microscene component to any GameObject and open graph using corresposing button. You can also open editor window from `Window/Microscene Graph Editor` menu. This window will automatically find Microscene component in your selection and regenerate graph. You can lock the window if you don't want selection change to change graph.
Graph is saved along with the scene, when you change target object or when closing window.

![](Git/EditorWindow.png)

Graph is then executed as soon as component is enabled in `LateUpdate()`.


## Creating nodes
To add node all you have to do is inherit your class from MicroAction or MicroPrecondition. Since every node is just a class serialized using `[SerializeReference]`, you don't need to create separate file for each one.
You node will be available in Create Node dropdown automatically.

```csharp

using Microscenes;

[MicrosceneNodeType(MicrosceneNodeType.Action)]  // Specify if node is action, precondition or hybrid
// This is small enum which will unfold into "Abstract/Empty" path
// You may as well just use path as an argument
[SerializeReferencePath(SRPathType.Abstract, "Empty")] 
internal class EmptyActionNode : MicrosceneNode
{
    protected override void OnStart(in MicrosceneContext ctx)
    {
        Complete(); // Marks node as completed so graph can move forward
    }
}

// Example of checking condition every frame
[MicrosceneNodeType(MicrosceneNodeType.Precondition)]
[SerializeReferencePath(SRPathType.Abstract, "Scene Is Loaded")]
public class SceneIsLoadedNode : MicrosceneNode
{
    [SerializeField] string m_SceneName;
    protected override void OnUpdate(in MicrosceneContext ctx)
    {
        ResetState(); // Clear result of previous frame
        // This way condition will be rechecked every frame. If you want to check for condition once and not clear result
        // You can do 
        // if(base.State == MicrosceneNodeState.Finished)
        //    return;
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            // Condition will be true every frame where we have scene with 
            // some name loaded
            if(SceneManager.GetSceneAt(i).name == m_SceneName)
            {
                Complete(); // Set condition as satisfied for this frame
                break;
            }
        }
    }
}

// Example of a hybdrid node
// Waiting may be treated as action but also you might want to have condition
// E.g. "5 seconds pass or player entered trigger"
// But as an action, you might want to chain it "start animation then wait 1 sec"
// In graph view, hybrid node can be placed in any stack 
// You can also change its type by using context menu
[MicrosceneNodeType(MicrosceneNodeType.Hybrid)]
[SerializeReferencePath(SRPathType.Abstract, "Wait")]
public class WaitHybdridNode : MicrosceneNode, INameableNode // for INameableNode explanation see Dynamic Node Name
{
    float timer;
    [SerializeField, Min(0)] float m_WaitTime = 1f;
    protected override void OnStart(in MicrosceneContext ctx)
    {
        timer = 0;
    }
    protected override void OnUpdate(in MicrosceneContext ctx)
    {
        timer += Time.deltaTime;
        if (timer >= m_WaitTime)
            Complete();
    }
    public string GetNiceNameString()
    {
        return m_WaitTime == 1f ? "Wait 1 second" : $"Wait {m_WaitTime} seconds";
    }
}
```


## Type Icon
`TypeIconAttribute` allows to add an icon to a node. You can initialize it using string 'filter', then the next set of rules is applied:
* If filter starts with "Assets/" or "Packages/", then icon is loaded using absolute path using  [AssetDatabase.LoadAssetAtPath\<Texture>](https://docs.unity3d.com/ScriptReference/AssetDatabase.LoadAssetAtPath.html).
* If filter starts with "Resources/" then rest of the path is used for [Resources.Load(string)](https://docs.unity3d.com/ScriptReference/Resources.Load.html) call.</br>
* If filter starts with t: and contains '.' symbol, icon is retrieved from type with the same name (Must match whole name including namespace)</br>
* If filter starts with t: and <b>does not</b> contain '.', then search through types is used but only for Type.Name, first match is used to retrieve the icon. </br>
* If type is found, then rules are the same as for intiialization with `System.Type`
* If none of these criteria are met, icon is loaded using [EditorGUIUtility.IconContent(string)](https://docs.unity3d.com/ScriptReference/EditorGUIUtility.IconContent.html)

Initialization with Type works as follows:
* If type is `MonoBehaviour` then finds `MonoScript` asset and retrieves icon from it.
* If type is `ScriptableObject` then creates temporary instance and gets icon from it.
* If type is built in unity type, then `AssetPreview.GetMiniTypeThumbnail(Type)` is used

## Dynamic node name
Just implement `INameableNode` interface. This will make graph view will set node name to whetever GetNiceNameString returns.
Rich text is supported.
```csharp
class NamedAction : MicroAction, INameableNode
{
    [SerializeField] string m_NodeName;

    // This name will be displayed in graph view
    public string GetNiceNameString() => m_NodeName;
}
```

## Context System
So one other thing I noticed is that microscenes can be nicely used for interactions with objects, but in this case some nodes would need additional data such as lookDirection and so on. Therefore I made context system. Basically, any node can define `RequireContext` attribute, which will make it to not be available until context request is satisfied.
To provide context you have to implement `IMicrosceneContextProvider` interface. If microscene finds `IMicrosceneContextProvider` component on the same game object, it will disable itself and activation will only be possible from external source. See example

```csharp

public class InteractionData
{
    public Vector3 ViewDirection;
}

public class InteractableItem : MonoBehaviour, IMicrosceneContextProvider
{
    public Type MicrosceneContext => typeof(InteractionData);


    /// ... somewhere in code ... ///
    void enableScene(Vector3 direction)
    {
        InteractionData data = new() { ViewDirection = direction };
        GetComponent<Microscene>().StartExecutingMicroscene(data); // This will enable microscene

        // You can also check whether is completed by using
        // Microscene.IsExecutingAnyNode
    }
}

[RequireContext(typeof(InteractionData))] // Node won't be available unless Microscene has component which provides such context
public class InteractionDependentAction : MicroAction
{
    public override OnUpdateExecute(in MicrosceneContext ctx)
    {
        var interactionData = ctx.customData as InteractionData; // Given custom data
    }
}
```

## Problems
* The fact that graph is serialized in scene means we can not have actual tree because of serialization nesting limitations which adds a lot of unwanted complexity, especially for generating nodes back from serialized data. And frankly, runtime execution code looks messy as well
* No Undo/Redo in graph view.
* Connections to stack nodes without nodes inside are not restored when opening graph
* Since intended for internal use, some editor code is really junky, sorry if you break your leg there :)
* Appearently, graph view is going to get [deprecated](https://forum.unity.com/threads/graph-tool-foundation.1057667/page-2#post-8098055). Great =)
* Search window in graph view is not very good and repeats some entries on search
* No loops

