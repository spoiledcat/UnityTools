using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace SpoiledCat
{
	/// <summary>
	/// MonoBehaviour that creates itself if there's no instance of it in the loaded scene when `<typeparamref name="T"/>.Instance` is first called.
	/// The gameobject attached to this component will have DontDestroyOnLoad set on it.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class ManagerSingleton<T> : MonoBehaviour
		where T : MonoBehaviour
	{
		private const string SingletonPrefix = "(singleton)";

		private static T instance = null;

		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindSingletonInScene();
					if (instance == null)
					{
						var singleton = new GameObject { name = $"{SingletonPrefix} {typeof(T)}" };
						singleton.AddComponent<T>();
					}
				}

				return instance;
			}

			protected internal set
			{
				if (instance != null)
				{
					// prevent reentrancy if someone does something funny
					if (instance != value)
					{
						T instanceToKeep = instance;
						T instanceToDestroy = value;

#if UNITY_EDITOR
						var preferOld = Application.isPlaying || !IsAutoCreatedSingleton(instance.name);
						if (!preferOld)
						{
							(instanceToKeep, instanceToDestroy) = (instanceToDestroy, instanceToKeep);
						}
#endif

#if UNITY_EDITOR
						// only destroy objects if we're in play mode OR we were the ones who created them
						if (Application.isPlaying || IsAutoCreatedSingleton(instanceToDestroy.name))
#endif
							SafeDestroy(instanceToDestroy);

						instance = instanceToKeep;
						DontDestroyOnLoad(instance.gameObject);
						MaybeInitialize(instance);
					}
				}
				else
				{
					instance = value;
					if (instance != null)
					{
						DontDestroyOnLoad(instance.gameObject);
						MaybeInitialize(instance);
					}
				}
			}
		}

		public static T UnsafeInstance => instance;

		[NonSerialized] private bool initialized;
		protected bool IsAutoCreated => IsAutoCreatedSingleton(name);

		protected internal virtual void Awake()
		{
			Instance = (T)(MonoBehaviour)this;
		}

		private void InternalInitialize()
		{
			if (initialized) return;
			initialized = true;
			Initialize();
		}

		/// <summary>
		/// This gets called when Awake would be called, use this instead of Awake
		/// </summary>
		protected virtual void Initialize()
		{ }

		private static void MaybeInitialize(T obj) => ((ManagerSingleton<T>)(object)obj).InternalInitialize();

		private static T FindSingletonInScene() =>
#if UNITY_2020_1_OR_NEWER
			Component.FindObjectOfType<T>(true);
#else // 2019 and earlier can only find active objects
			Component.FindObjectOfType<T>();
#endif

		private static bool IsAutoCreatedSingleton(string name) => name.StartsWith(SingletonPrefix);

		protected static void SafeDestroy(UnityObject obj)
		{
			if (obj != null)
			{
#if UNITY_EDITOR
				if (Application.isPlaying && !UnityEditor.EditorApplication.isPaused)
					UnityObject.Destroy(obj);
				else
					UnityObject.DestroyImmediate(obj);
#else
				UnityObject.Destroy(obj);
#endif
			}
		}
	}
}
