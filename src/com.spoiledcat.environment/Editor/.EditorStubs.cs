#if !UNITY_EDITOR

// Copyright 2016-2020 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;

namespace SpoiledCat.Unity.EditorStubs
{
	public class SerializeFieldAttribute : Attribute
	{}
	public class ScriptableSingleton<T>
		where T : class, new()
	{
		private static T _instance;
		public static T instance => _instance ?? (_instance = new T());

		protected void Save(bool flush)
		{ }
	}

#if !UNITY_5_6_OR_NEWER
	[Flags]
	public enum HideFlags
	{
		None = 0,
		HideInHierarchy = 1,
		HideInInspector = 2,
		DontSaveInEditor = 4,
		NotEditable = 8,
		DontSaveInBuild = 16,
		DontUnloadUnusedAsset = 32,
		DontSave = 4 + 16 + 32,
		HideAndDontSave = 1 + 4 + 8 + 16 + 32,
	}
#endif

	public class ScriptableObject
	{
		public static ScriptableObject CreateInstance<T>()
			where T : ScriptableObject
		{
			return Activator.CreateInstance<T>();
		}

		public HideFlags hideFlags { get; set; }
	}

	public static class Application
	{
		public static string productName { get; } = "DefaultApplication";
		public static string unityVersion { get; set; } = "2019.2.1f1";
		public static string projectPath { get; set; }
	}

	public static class EditorApplication
	{
		public static string applicationPath { get; set; }
		public static string applicationContentsPath { get; set; }
	}

	public static class InternalEditorUtility
	{
		public static string unityPreferencesFolder { get; set; }
		public static void LoadSerializedFileAndForget(string filepath) {}
		public static void SaveToSerializedFileAndForget<T>(T[] objs, string path, bool saveAsText) {}
	}

}
#endif
