#pragma warning disable 649

using System;
using System.Collections.Generic;
using System.Linq;

namespace SpoiledCat.Git
{
	using Json;
	using Logging;
	using NiceIO;
	using Threading;
	using Unity;
	using Utilities;

	public class DugiteReleaseManifest
	{
		private long id;
		private UriString url;
		private UriString assets_url;
		private string tag_name;
		private string name;
		private DateTimeOffset published_at;
		private List<Asset> assets;

		public struct Asset
		{
			private long id;
			private string name;
			private string content_type;
			private long size;
			private DateTimeOffset updated_at;
			private UriString browser_download_url;


			[NotSerialized] public string Name => name;
			[NotSerialized] public string ContentType => content_type;
			[NotSerialized] public DateTimeOffset Timestamp => updated_at;
			[NotSerialized] public UriString Url => browser_download_url;
			public string Hash { get; set; }
		}

		[NotSerialized]
		public TheVersion Version => TheVersion.Parse(tag_name.Substring(1));

		[NotSerialized]
		public DateTimeOffset Timestamp => published_at;

		[NotSerialized]
		public Asset DugitePackage { get; private set; }

		private (Asset zipFile, Asset shaFile) GetAsset(IEnvironment environment)
		{
			var arch = environment.Is32Bit ? "x86" : "x64";
			var os = environment.IsWindows ? "windows"
					: environment.IsMac ? "macOS"
					: "ubuntu";
			var name = os;
			if (environment.IsWindows)
				name += $"-{arch}";
			name += ".tar.gz";
			return (assets.FirstOrDefault(x => x.Name.EndsWith(name)), assets.FirstOrDefault(x => x.Name.EndsWith(name + ".sha256")));
		}

		public static DugiteReleaseManifest Load(NPath path, IEnvironment environment)
		{
			var manifest = path.ReadAllText().FromJson<DugiteReleaseManifest>(true, false);
			var (zipAsset, shaAsset) = manifest.GetAsset(environment);
			var shaAssetPath = environment.UserCachePath.Combine("downloads", shaAsset.Name);
			if (!shaAssetPath.FileExists())
			{
				var downloader = new Downloader();
				downloader.QueueDownload(shaAsset.Url, shaAssetPath.Parent, shaAssetPath.FileName);
				var shaFile = downloader.RunSynchronously().FirstOrDefault();
			}
			zipAsset.Hash = shaAssetPath.ReadAllText();
			manifest.DugitePackage = zipAsset;
			return manifest;
		}

		public static DugiteReleaseManifest Load(NPath localCacheFile, UriString packageFeed, IEnvironment environment)
		{
			DugiteReleaseManifest package = null;
			//NPath localCacheFeed = environment.UserCachePath.Combine("embedded-git.json");
			var filename = localCacheFile.FileName;
			var key = localCacheFile.FileNameWithoutExtension + "_updatelastCheckTime";
			var now = DateTimeOffset.Now;

			if (!localCacheFile.FileExists() || now.Date > environment.UserSettings.Get<DateTimeOffset>(key).Date)
			{
				localCacheFile = new DownloadTask(TaskManager.Instance.Token, environment.FileSystem, packageFeed, environment.UserCachePath, filename)
						 .Catch(ex => {
							 LogHelper.Warning(@"Error downloading package feed:{0} ""{1}"" Message:""{2}""", packageFeed, ex.GetType().ToString(), ex.GetExceptionMessageShort());
							 return true;
						 })
						 .RunSynchronously();

				if (localCacheFile.IsInitialized)
					environment.UserSettings.Set<DateTimeOffset>(key, now);
			}

			if (!localCacheFile.IsInitialized)
			{
				// try from assembly resources
				localCacheFile = AssemblyResources.ToFile(ResourceType.Platform, packageFeed.Filename, environment.UserCachePath, environment);
			}

			if (localCacheFile.IsInitialized)
			{
				try
				{
					package = Load(localCacheFile, environment);
				}
				catch (Exception ex)
				{
					LogHelper.Error(ex);
				}
			}
			return package;

		}
	}
}
