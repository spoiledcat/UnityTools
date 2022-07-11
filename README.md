# SpoiledCat Utilities - Runtime

Runtime utility classes and singleton implementations.

#### ManagerSingleton

A MonoBehaviour singleton that initializes itself when `MyManagerSingleton.Instance` is called. If there is an instance of this type in the scene,
that will be used. If there isn't one in the scene, a new gameobject will be created called `(singleton)MyManagerSingleton` and the component added to it.
If automatically created, the gameobject will be marked as `DontDestroyOnLoad`.

#### ManagerPrefabSingleton

A MonoBehaviour singleton that uses several different strategies to initialize itself when `MyManagerPrefabSingleton.Instance` is called.

Use this when you need to access singleton objects in play mode in the editor, but not all of your scenes have the singletons included in them.

This will try to:
1 - Use a scene gameobject instance that has the `MyManagerPrefabSingleton.Instance` component;
2 - Use the script default reference to instantiate a prefab with the `MyManagerPrefabSingleton.Instance` component (Editor only);
3 - Create a game object with prefix "(singleton)" and add the `MyManagerPrefabSingleton.Instance` component to it;

Example:

Create the following two scripts in your project:

```c#
using SpoiledCat;

public class SingletonPrefab : ManagerPrefabSingleton<SingletonPrefab>
{
	public bool theDefault;
}

```

```c#
public class SingletonTest : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
		var ins = SingletonPrefab.Instance;
		Debug.Log($"The Default is {ins.theDefault}");
	}
}
```

Create an empty gameobject in a scene, add the `SingletonPrefab` component to it, check the `theDefault` box in the game object inspector, save this game object as a prefab, and remove it from the scene.

In the Unity project view, click on the `SingletonPrefab.cs` asset. In the Inspector, above the source of the script, there will be a `Source Prefab` field. Drag the prefab you just created to it.

In a new scene, put the `SingletonTest` component on a game object.

When you enter playmode, you should see a `The Default is true` message on your console, and you should see a `SingletonPrefab(Clone)` object in the `DontDestroyOnLoad` list in the scene hierarchy.

This shows that when `SingletonPrefab.Instance` was called in the `SingletonTest` script, the custom prefab was used to instantiate the `SingletonPrefab` object.

This happens because there was no `SingletonPrefab` component in the scene, so the default reference set on the script was used to create one. This only happens in the editor - at runtime, it will either use an instance in the scene, or create a new one with `AddComponent`.