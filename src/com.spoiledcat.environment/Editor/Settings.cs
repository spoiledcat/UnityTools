// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SpoiledCat.Unity
{
	using Json;
	using Logging;
	using NiceIO;

	public interface ISettings
	{
		void Initialize();
		bool Exists(string key);
		string Get(string key, string fallback = "");
		T Get<T>(string key, T fallback = default(T));
		void Set<T>(string key, T value);
		void Unset(string key);
		void Rename(string oldKey, string newKey);
		NPath SettingsPath { get; set; }
	}

	public abstract class BaseSettings : ISettings
	{
		public abstract bool Exists(string key);
		public abstract string Get(string key, string fallback = "");
		public abstract T Get<T>(string key, T fallback = default(T));
		public abstract void Initialize();
		public abstract void Rename(string oldKey, string newKey);
		public abstract void Set<T>(string key, T value);
		public abstract void Unset(string key);
		public NPath SettingsPath { get; set; }

		protected virtual string SettingsFileName { get; set; }
	}

	public class JsonBackedSettings : BaseSettings
	{
		private readonly ILogging logger;
		protected Dictionary<string, object> cacheData;
		private string cachePath;

		public JsonBackedSettings()
		{
			logger = LogHelper.GetLogger(GetType());
		}

		public override void Initialize()
		{
			cachePath = SettingsPath.Combine(SettingsFileName);
			LoadFromCache(cachePath);
		}

		public override bool Exists(string key)
		{
			if (cacheData == null)
				Initialize();

			return cacheData.ContainsKey(key);
		}

		public override string Get(string key, string fallback = "")
		{
			return Get<string>(key, fallback);
		}

		public override T Get<T>(string key, T fallback = default(T))
		{
			if (cacheData == null)
				Initialize();

			if (cacheData.TryGetValue(key, out var value))
			{
				if (typeof(T) == typeof(DateTimeOffset))
				{
					DateTimeOffset dt;
					if (DateTimeOffset.TryParseExact(value?.ToString() ?? "", DateTimeFormatConstants.Iso8601Formats,
						CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
					{
						value = dt;
						cacheData[key] = dt;
					}
				}

				if (value == null && fallback != null)
				{
					value = fallback;
					cacheData[key] = fallback;
				}
				else if (!(value is T))
				{
					try
					{
						value = value.FromObject<T>();
						cacheData[key] = value;
					}
					catch
					{
						value = fallback;
						cacheData[key] = fallback;
					}
				}
				return (T)value;
			}
			return fallback;
		}

		public override void Set<T>(string key, T value)
		{
			if (cacheData == null)
				Initialize();

			try
			{
				object val = value;
				if (value is DateTimeOffset)
					val = ((DateTimeOffset)(object)value).ToString(DateTimeFormatConstants.Iso8601Format);
				if (!cacheData.ContainsKey(key))
					cacheData.Add(key, val);
				else
					cacheData[key] = val;
				SaveToCache(cachePath);
			}
			catch (Exception e)
			{
				logger.Error(e, "Error storing to cache");
				throw;
			}
		}

		public override void Unset(string key)
		{
			if (cacheData == null)
				Initialize();

			if (cacheData.ContainsKey(key))
				cacheData.Remove(key);
			SaveToCache(cachePath);
		}

		public override void Rename(string oldKey, string newKey)
		{
			if (cacheData == null)
				Initialize();

			if (cacheData.TryGetValue(oldKey, out var value))
			{
				cacheData.Remove(oldKey);
				Set(newKey, value);
			}
			SaveToCache(cachePath);
		}

		protected virtual void LoadFromCache(string path)
		{
			EnsureCachePath(path);

			var npath = path.ToNPath();
			if (!npath.FileExists())
			{
				cacheData = new Dictionary<string, object>();
				return;
			}

			var data = npath.ReadAllText(Encoding.UTF8);

			try
			{
				cacheData = data.FromJson<Dictionary<string, object>>();
			}
			catch(Exception ex)
			{
				logger.Error(ex, "LoadFromCache Error");
				cacheData = null;
			}

			if (cacheData == null)
			{
				// cache is corrupt, remove
				npath.Delete();
				cacheData = new Dictionary<string, object>();
			}
		}

		protected virtual bool SaveToCache(string path)
		{
			EnsureCachePath(path);

			var npath = path.ToNPath();
			try
			{
				var data = cacheData.ToJson();
				npath.WriteAllText(data);
			}
			catch (Exception ex)
			{
				logger.Error(ex, "SaveToCache Error");
				return false;
			}

			return true;
		}

		private void EnsureCachePath(string path)
		{
			var npath = path.ToNPath();
			if (npath.FileExists())
				return;
			npath.EnsureParentDirectoryExists();
		}
	}

	public class LocalSettings : JsonBackedSettings
	{
		private const string RelativeSettingsPath = "ProjectSettings";
		private readonly string settingsFileName;

		public LocalSettings(IEnvironment environment)
		{
			SettingsPath = environment.UnityProjectPath.Combine(RelativeSettingsPath);
			settingsFileName = environment.ApplicationName + "-local.log";
		}

		protected override string SettingsFileName => settingsFileName;
	}

	public class UserSettings : JsonBackedSettings
	{
		private const string settingsFileName = "usersettings.json";
		private const string oldSettingsFileName = "settings.json";

		public UserSettings(IEnvironment environment)
		{
			SettingsPath = environment.UserCachePath;
		}

		public override void Initialize()
		{
			var cachePath = SettingsPath.Combine(settingsFileName);
			if (!cachePath.FileExists())
			{
				var oldSettings = SettingsPath.Combine(oldSettingsFileName);
				if (oldSettings.FileExists())
					oldSettings.Copy(cachePath);
			}
			base.Initialize();
		}

		protected override string SettingsFileName => settingsFileName;
	}

	public class SystemSettings : JsonBackedSettings
	{
		private const string settingsFileName = "systemsettings.json";
		private const string oldSettingsFileName = "settings.json";

		public SystemSettings(IEnvironment environment)
		{
			SettingsPath = environment.SystemCachePath;
		}

		public override void Initialize()
		{
			var cachePath = SettingsPath.Combine(settingsFileName);
			if (!cachePath.FileExists())
			{
				var oldSettings = SettingsPath.Combine(oldSettingsFileName);
				if (oldSettings.FileExists())
					oldSettings.Copy(cachePath);
			}
			base.Initialize();
		}

		protected override string SettingsFileName => settingsFileName;
	}
}
