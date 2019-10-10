using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace SpoiledCat.Unity
{
	using NiceIO;
	using System.Linq;

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class LocationAttribute : Attribute
	{
		public enum Location { PreferencesFolder, ProjectFolder, LibraryFolder, UserFolder }

		private string relativePath;
		private Location location;

		private string filePath;
		public string FilePath
		{
			get
			{
				if (filePath != null) return filePath;

				if (relativePath[0] == '/')
					relativePath = relativePath.Substring(1);

#if UNITY_EDITOR
					if (location == Location.PreferencesFolder)
						filePath = InternalEditorUtility.unityPreferencesFolder + "/" + relativePath;
					else if (location == Location.UserFolder)
						filePath = TheEnvironment.Instance.Environment.UserCachePath.Combine(relativePath).ToString(SlashMode.Forward);
					else if (location == Location.LibraryFolder)
						filePath = "Library".ToNPath().Combine("gfu", relativePath);
#endif
				return filePath;
			}
		}

		public LocationAttribute(string relativePath, Location location)
		{
			this.relativePath = relativePath;
			this.location = location;
		}
	}


	public class ScriptObjectSingleton<T> : ScriptableObject where T : ScriptableObject
	{
		private static T instance;
		public static T Instance
		{
			get
			{
				if (instance == null)
					CreateAndLoad();
				return instance;
			}
		}

		protected ScriptObjectSingleton()
		{
			if (instance != null)
			{
				Debug.Log("Singleton already exists!");
			}
			else
			{
				instance = this as T;
				System.Diagnostics.Debug.Assert(instance != null);
			}
		}

		private static void CreateAndLoad()
		{
			System.Diagnostics.Debug.Assert(instance == null);

			string filePath = GetFilePath();
			if (!string.IsNullOrEmpty(filePath))
			{
#if UNITY_EDITOR
					InternalEditorUtility.LoadSerializedFileAndForget(filePath);
#else
				if (PlayerPrefs.HasKey(filePath))
				{
					var inst = CreateInstance<T>() as ScriptObjectSingleton<T>;
					inst.hideFlags = HideFlags.HideAndDontSave;
					JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(filePath), inst);
				}
#endif
			}

			if (instance == null)
			{
				var inst = CreateInstance<T>() as ScriptObjectSingleton<T>;
				inst.hideFlags = HideFlags.HideAndDontSave;
				inst.Save(true);
			}

			System.Diagnostics.Debug.Assert(instance != null);
		}

		protected virtual void Save(bool saveAsText)
		{
			if (instance == null)
			{
				Debug.Log("Cannot save singleton, no instance!");
				return;
			}

			NPath? filePath = GetFilePath();
			if (filePath != null)
			{
#if UNITY_EDITOR
					filePath.Value.Parent.EnsureDirectoryExists();
					InternalEditorUtility.SaveToSerializedFileAndForget(new[] { instance }, filePath, saveAsText);
#else
				PlayerPrefs.SetString(filePath, JsonUtility.ToJson(instance));
#endif
			}
		}

		private static NPath? GetFilePath()
		{
			var attr = typeof(T).GetCustomAttributes(true)
								.Select(t => t as LocationAttribute)
								.FirstOrDefault(t => t != null);
			//LogHelper.Instance.Debug("FilePath {0}", attr != null ? attr.filepath : null);

			if (attr == null)
				return null;
			return attr.FilePath.ToNPath();
		}
	}
}
