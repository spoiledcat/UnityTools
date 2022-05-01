using System;
using System.Linq;

namespace SpoiledCat.Unity
{
#if UNITY_EDITOR
	using UnityEditorInternal;
	using UnityEngine;
#else
	using EditorStubs;
#endif

	using Logging;
	using SimpleIO;

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class LocationAttribute : Attribute
	{
		public enum Location { PreferencesFolder, ProjectFolder, LibraryFolder, UserFolder }

		private readonly string originalPath;
		private readonly Location location;

		private string filePath;
		public string FilePath {
			get {
				if (filePath != null) return filePath;

				var relativePath = originalPath.ToSPath().MakeRelative();

				switch (location)
				{
					case Location.PreferencesFolder:
						filePath = InternalEditorUtility.unityPreferencesFolder.ToSPath().Combine(relativePath).ToString(SlashMode.Forward);
						break;
					case Location.UserFolder:
						filePath = TheEnvironment.instance.Environment.UserCachePath.Combine(relativePath).ToString(SlashMode.Forward);
						break;
					case Location.LibraryFolder:
						filePath = TheEnvironment.instance.Environment.UnityProjectPath.Combine("Library", relativePath).ToString(SlashMode.Forward);
						break;
					case Location.ProjectFolder:
						filePath = TheEnvironment.instance.Environment.UnityProjectPath.Combine(relativePath).ToString(SlashMode.Forward);
						break;
				}
				return filePath;
			}
		}

		public LocationAttribute(string relativePath, Location location)
		{
			relativePath.ArgumentNotNullOrWhiteSpace(nameof(relativePath));
			this.originalPath = relativePath;
			this.location = location;
		}
	}


	public class ScriptObjectSingleton<T> : ScriptableObject where T : ScriptableObject
	{
		private static T theInstance;
		public static T Instance
		{
			get
			{
				if (theInstance == null)
					CreateAndLoad();
				return theInstance;
			}
		}

		public static T instance => Instance;

		protected ScriptObjectSingleton()
		{
			if (theInstance != null)
			{
				LogHelper.GetLogger<T>().Error("Singleton already exists!");
			}
			else
			{
				theInstance = this as T;
				System.Diagnostics.Debug.Assert(theInstance != null);
			}
		}

		private static void CreateAndLoad()
		{
			System.Diagnostics.Debug.Assert(theInstance == null);

			SPath? locationFilePath = GetFilePath();
			if (locationFilePath != null)
			{
				InternalEditorUtility.LoadSerializedFileAndForget(locationFilePath.Value.ToString(SlashMode.Forward));
			}

			if (theInstance == null)
			{
				var inst = CreateInstance<T>() as ScriptObjectSingleton<T>;
				inst.hideFlags = HideFlags.HideAndDontSave;
			}

			System.Diagnostics.Debug.Assert(theInstance != null);
		}

		protected virtual void Save(bool saveAsText)
		{
			if (theInstance == null)
			{
				LogHelper.GetLogger<T>().Error("Cannot save singleton, no instance!");
				return;
			}

			SPath? locationFilePath = GetFilePath();
			if (locationFilePath != null)
			{
				locationFilePath.Value.Parent.EnsureDirectoryExists();
				InternalEditorUtility.SaveToSerializedFileAndForget(new[] { theInstance }, locationFilePath.Value.ToString(SlashMode.Forward), saveAsText);
			}
		}

		private static SPath? GetFilePath()
		{
			return typeof(T).GetCustomAttributes(true)
				.OfType<LocationAttribute>()
				.FirstOrDefault()?.FilePath.ToSPath();
		}
	}


	static class CheckExtensions
	{
		/// <summary>
		///   Checks a string argument to ensure it isn't null or empty.
		/// </summary>
		/// <param name = "value">The argument value to check.</param>
		/// <param name = "name">The name of the argument.</param>
		public static void ArgumentNotNullOrWhiteSpace(this string value, string name)
		{
			if (value != null && value.Trim().Length > 0)
				return;
			throw new ArgumentException($"The value for '{name}' must not be empty", name);
		}
	}
}
