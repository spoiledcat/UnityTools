// Copyright 2016-2022 Andreia Gaita
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif
using System.Linq;
using UnityEngine;

namespace SpoiledCat
{
	/// <summary>
	/// A MonoBehaviour singleton that uses several different strategies to initialize itself when `<typeparamref name="T"/>.Instance` is called.
	/// Use this when you need to access singleton objects in play mode in the editor, but not all of your scenes have the singletons included in them.
	///
	/// This will try to:
	/// 1 - Use a scene gameobject instance that has the <typeparamref name="T"/> component;
	/// 2 - Use the script default reference to instantiate a prefab with the <typeparamref name="T"/> component (Editor only);
	/// 3 - Create a game object with prefix "(singleton)" and add the <typeparamref name="T"/> component to it;
	/// </summary>
	public abstract class ManagerPrefabSingleton<T> : ManagerSingleton<T>
		where T : MonoBehaviour
	{
		[SerializeField, HideInInspector] private T sourcePrefab;

		private static SingletonConfiguration configuration;

		protected internal override void Awake()
		{
#if UNITY_EDITOR
			if (IsAutoCreated)
			{
				var script = MonoScript.FromMonoBehaviour(this);
				var scriptPath = AssetDatabase.GetAssetPath(script);
				var monoImporter = AssetImporter.GetAtPath(scriptPath) as MonoImporter;
				var value = monoImporter.GetDefaultReference(nameof(sourcePrefab));
				if (value != null)
				{
					SafeDestroy(gameObject);
					Instance = (T)Instantiate(value);
					return;
				}
			}
#else
			//if (IsAutoCreated)
			//{
			//	if (TryInstantiateFromDefaultPrefab(out var inst))
			//	{
			//		Instance = inst;
			//		return;
			//	}
			//}
#endif
			base.Awake();
		}

		private bool TryInstantiateFromDefaultPrefab(out T inst)
		{
			inst = null;
			if (TryGetConfiguration(out var config))
			{
				if (config.ScriptPrefabMap.TryGetValue(GetType().FullName, out var guidAndPath))
				{
					var parts = guidAndPath.Split(';');
					var (_, path) = (parts[0], parts[1]);
					var asset = Resources.Load<T>(path);
					if (asset != null)
						inst = Instantiate(asset);

					if (inst != null)
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool TryGetConfiguration(out SingletonConfiguration config)
		{
			if (configuration == null)
			{
				configuration = Resources.FindObjectsOfTypeAll<SingletonConfiguration>().FirstOrDefault();
			}

			config = configuration;
			return configuration != null;
		}

#if UNITY_EDITOR

		private void OnValidate()
		{
			//InternalValidate();
		}

		private void InternalValidate()
		{
			var script = MonoScript.FromMonoBehaviour(this);
			var scriptPath = AssetDatabase.GetAssetPath(script);
			var monoImporter = AssetImporter.GetAtPath(scriptPath) as MonoImporter;
			var value = monoImporter.GetDefaultReference(nameof(sourcePrefab));
			AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out var guid, out long localId);

			var valuePath = AssetDatabase.GUIDToAssetPath(guid);
			valuePath = valuePath.Substring(valuePath.LastIndexOf("/Resources/") + 11);
			valuePath = Path.GetFileNameWithoutExtension(valuePath);
			var fullName = monoImporter.GetScript().GetClass().FullName;

			var configGuid = AssetDatabase.FindAssets($"t:{nameof(SingletonConfiguration)}").FirstOrDefault();
			SingletonConfiguration config = null;
			if (configGuid == null)
			{
				config = new SingletonConfiguration();
				if (!AssetDatabase.IsValidFolder("Assets/Resources"))
					AssetDatabase.CreateFolder("Assets", "Resources");
				AssetDatabase.CreateAsset(config, "Assets/Resources/singletonconfig.asset");
			}
			else
			{
				config = AssetDatabase.LoadAssetAtPath<SingletonConfiguration>(
					AssetDatabase.GUIDToAssetPath(configGuid));
			}

			if (!config.ScriptPrefabMap.ContainsKey(fullName))
				config.ScriptPrefabMap.Add(fullName, guid);
			else
				config.ScriptPrefabMap[fullName] = $"{guid};{valuePath}";
		}
#endif
	}
}
