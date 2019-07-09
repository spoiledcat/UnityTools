#if UNITY_EDITOR || UNITY_STANDALONE

using System;
using UnityEditor;
using UnityEngine;

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

		public static string ApplicationName { get; set; }

		public void Flush()
		{
			unityApplication = Environment.UnityApplication;
			unityApplicationContents = Environment.UnityApplicationContents;
			unityAssetsPath = Environment.UnityAssetsPath;
			extensionInstallPath = Environment.ExtensionInstallPath;
			Save(true);
		}

		public IEnvironment Environment
		{
			get
			{
				if (environment == null)
				{
					environment = new UnityEnvironment(ApplicationName ?? Application.productName);
					if (unityApplication == null)
					{
						unityAssetsPath = Application.dataPath;
						unityApplication = EditorApplication.applicationPath;
						unityApplicationContents = EditorApplication.applicationContentsPath;
						extensionInstallPath = DetermineInstallationPath();
						unityVersion = Application.unityVersion;
					}

					environment.Initialize(unityVersion,
						extensionInstallPath.ToNPath(), unityApplication.ToNPath(),
						unityApplicationContents.ToNPath(), unityAssetsPath.ToNPath());
					Flush();
				}
				return environment;
			}
		}

		private NPath DetermineInstallationPath()
		{
			// Juggling to find out where we got installed
			var shim = CreateInstance<RunLocationShim>();
			var script = MonoScript.FromScriptableObject(shim);
			var scriptPath = Application.dataPath.ToNPath().Parent.Combine(AssetDatabase.GetAssetPath(script).ToNPath());
			DestroyImmediate(shim);
			return scriptPath.Parent;
		}

	}
}

#endif
