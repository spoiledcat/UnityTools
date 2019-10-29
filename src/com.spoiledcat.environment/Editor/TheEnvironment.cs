// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpoiledCat.Unity
{
	using NiceIO;

	sealed class TheEnvironment : ScriptableSingleton<TheEnvironment>
	{
		[NonSerialized] private IEnvironment environment;
		[SerializeField] private string extensionInstallPath;
		[SerializeField] private string unityApplication;
		[SerializeField] private string unityApplicationContents;
		[SerializeField] private string unityAssetsPath;
		[SerializeField] private string unityVersion;

		public void Flush()
		{
#if UNITY_EDITOR
			unityApplication = Environment.UnityApplication;
			unityApplicationContents = Environment.UnityApplicationContents;
			unityVersion = Environment.UnityVersion;
#endif
			unityAssetsPath = Environment.UnityAssetsPath;
			extensionInstallPath = Environment.ExtensionInstallPath;
			Save(true);
		}

		private NPath DetermineInstallationPath()
		{
#if UNITY_EDITOR
			// Juggling to find out where we got installed
			var shim = CreateInstance<RunLocationShim>();
			var script = MonoScript.FromScriptableObject(shim);
			var scriptPath = Application.dataPath.ToNPath().Parent.Combine(AssetDatabase.GetAssetPath(script).ToNPath());
			DestroyImmediate(shim);
			return scriptPath.Parent;
#else
			return Application.dataPath.ToNPath();
#endif
		}

		public static string ApplicationName { get; set; }

		public IEnvironment Environment
		{
			get
			{
				if (environment == null)
				{
					environment = new UnityEnvironment(ApplicationName ?? Application.productName);
					if (unityAssetsPath == null)
					{
#if UNITY_EDITOR
						unityApplication = EditorApplication.applicationPath;
						unityApplicationContents = EditorApplication.applicationContentsPath;
						unityVersion = Application.unityVersion;
#endif
						unityAssetsPath = Application.dataPath;
						extensionInstallPath = DetermineInstallationPath();
					}

					environment.Initialize(
						unityAssetsPath.ToNPath(), extensionInstallPath.ToNPath()
#if UNITY_EDITOR
						,
						unityVersion,
						unityApplication.ToNPath(),
						unityApplicationContents.ToNPath()
#endif
					);
					Flush();
				}
				return environment;
			}
		}
	}
}
