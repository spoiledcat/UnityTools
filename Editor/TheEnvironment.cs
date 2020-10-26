// Copyright 2016-2020 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;

namespace SpoiledCat.Unity
{

#if UNITY_EDITOR
	using UnityEngine;
	using UnityEditor;
#else
	using EditorStubs;
#endif

    using SimpleIO;

	public sealed class TheEnvironment : ScriptableSingleton<TheEnvironment>
	{
		[NonSerialized] private IEnvironment environment;
		[SerializeField] private string unityApplication;
		[SerializeField] private string unityApplicationContents;
		[SerializeField] private string unityVersion;
		[SerializeField] private string projectPath;
		[SerializeField] private string extensionInstallPath;

		public void Flush()
		{
			unityApplication = Environment.UnityApplication;
			unityApplicationContents = Environment.UnityApplicationContents;
			unityVersion = Environment.UnityVersion;
			extensionInstallPath = Environment.ExtensionInstallPath;
			Save(true);
		}

		public static string ApplicationName { get; set; }

		public IEnvironment Environment
		{
			get
			{
				if (environment == null)
				{
					environment = new UnityEnvironment(ApplicationName ?? Application.productName);
					if (projectPath == null)
					{
#if UNITY_EDITOR
						projectPath = ".".ToSPath().Resolve().ToString(SlashMode.Forward);
#else
						projectPath = Application.projectPath;
#endif
						unityVersion = Application.unityVersion;
						unityApplication = EditorApplication.applicationPath;
						unityApplicationContents = EditorApplication.applicationContentsPath;
						extensionInstallPath = DetermineInstallationPath();
					}

					environment.Initialize(projectPath, extensionInstallPath, unityVersion, unityApplication, unityApplicationContents);
					Flush();
				}
				return environment;
			}
		}

		private SPath DetermineInstallationPath()
		{
#if UNITY_EDITOR
			// Juggling to find out where we got installed
			var shim = CreateInstance<RunLocationShim>();
			var script = MonoScript.FromScriptableObject(shim);
			var scriptPath = AssetDatabase.GetAssetPath(script).ToSPath().Resolve();
			DestroyImmediate(shim);
			return scriptPath.Parent;
#else
			return System.Reflection.Assembly.GetExecutingAssembly().Location.ToSPath().Parent;
#endif

		}
	}
}
