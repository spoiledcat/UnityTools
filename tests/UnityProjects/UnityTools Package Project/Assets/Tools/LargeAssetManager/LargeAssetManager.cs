using System;
using System.Collections.Generic;
using System.Linq;
using LocalTools;
using SpoiledCat.Json;
using SpoiledCat.Logging;
using SpoiledCat.NiceIO;
using SpoiledCat.Threading;
using SpoiledCat.Utilities;
using UnityEditor;

public class LargeAssetManager
{
	public static ITaskManager TaskManager;

	public static int DefaultWebServerPort = 58392;
	public static string DefaultWebServerUrl = "http://localhost";

	private static ILogging logger = LogHelper.GetLogger<LargeAssetManager>();

	static LargeAssetManager()
	{
		PocoJsonSerializerStrategy.RegisterCustomTypeHandler<NPath>(
			value => (((NPath)value).ToString(SlashMode.Forward), true),
			(value, type) => {
				string str = value as string;
				if (!string.IsNullOrEmpty(str))
				{
					return (new NPath(str), true);
				}
				return (NPath.Default, true);
			});

		PocoJsonSerializerStrategy.RegisterCustomTypeHandler<UriString>(value => (value.ToString(), true),
			(value, type) => (new UriString(value as string), true));
	}


	public static void LockEditor()
	{
		var autoRefresh = EditorPrefs.GetBool("kAutoRefresh");
		EditorPrefs.SetBool("kAutoRefresh_backup", autoRefresh);
		EditorPrefs.SetBool("kAutoRefresh", false);
		EditorApplication.LockReloadAssemblies();
		EditorUtility.ClearProgressBar();
	}

	public static void UnlockEditor()
	{
		EditorUtility.ClearProgressBar();
		EditorApplication.UnlockReloadAssemblies();
		var autoRefresh = EditorPrefs.GetBool("kAutoRefresh_backup");
		EditorPrefs.DeleteKey("kAutoRefresh_backup");
		EditorPrefs.SetBool("kAutoRefresh", autoRefresh);
		AssetDatabase.Refresh();
	}

	private static void ShowProgress(string title, string message, float pct)
	{
		TaskManager.RunInUI(() => EditorUtility.DisplayProgressBar(title, message, pct), "Updating progress");
	}

	private static void ClearProgress()
	{
		TaskManager.RunInUI(() => EditorUtility.ClearProgressBar(), "Clearing progress");
	}


	public static void UpdateIndexFromFilesInFolder(NPath template, NPath indexPath, NPath path)
	{
		var runTask = UpdateIndexFromFilesInFolderTask(path, indexPath, template);

		runTask
			.FinallyInUI((success, ex) => {
				if (!success)
					logger.Error(ex);
				logger.Info($"Index updated with result: {success}");
				EditorUtility.ClearProgressBar();
			})
			.Start();
	}

	public static ITask UpdateIndexFromFilesInFolderTask(NPath folderWithFiles, NPath indexFileToGenerate, NPath? indexTemplate = null)
	{
		Index? templateIndex = null;
		if (indexTemplate.HasValue && indexTemplate.Value.FileExists())
			templateIndex = Index.Load(indexTemplate);
		folderWithFiles = folderWithFiles.MakeAbsolute();
		var dt = DateTimeOffset.Now.Date.ToString("yyyyMMdd");
		TaskQueue<Asset> hashTask = new TaskQueue<Asset>(TaskManager) { Message = "Calculating hashes..." };
		foreach (var file in folderWithFiles.Files())
		{
			Asset asset = default;

			if ((templateIndex?.Assets.Any(x => x.Filename == file.FileName)) ?? false)
			{
				var templateAsset = templateIndex.Value.Assets.First(x => x.Filename == file.FileName);
				asset.LocalPath = file;
				asset.Path = templateAsset.Path;
				asset.Url = templateAsset.Url.Combine("assets")
				                         .Combine(dt)
				                         .Combine(file.FileName);
				asset.NeedsUnzip = templateAsset.NeedsUnzip;
			}
			else
			{
				asset.LocalPath = file;
				asset.Path = file.FileNameWithoutExtension.ToNPath();
				asset.NeedsUnzip = file.ExtensionWithDot == ".zip" ? true : false;
				asset.Url = $"{DefaultWebServerUrl}:{DefaultWebServerPort}/assets/{dt}/{file.FileName}";
			}

			hashTask.Queue(new FuncTask<Asset, Asset>(TaskManager, (_, x) => {
					x.Hash = x.LocalPath.ToMD5();
					x.LocalPath = default;
					return x;
				},
				() => asset) { Message = file.FileName });
		}

		hashTask.Progress(progress => ShowProgress(progress.Message, progress.InnerProgress?.Message, progress.Percentage));

		var runTask = hashTask
			.Then((success, list) => {
				Index newIndex = default;
				if (indexFileToGenerate.FileExists())
				{
					newIndex = Index.Load(indexFileToGenerate);
					foreach (var newAsset in list)
					{
						if (newIndex.Assets.Any(x => x.Filename == newAsset.Filename))
						{
							var originalAsset = newIndex.Assets.First(x => x.Filename == newAsset.Filename);
							if (originalAsset.Hash != newAsset.Hash || originalAsset.Url != newAsset.Url)
							{
								newIndex.Assets.Remove(originalAsset);
								newIndex.Assets.Add(newAsset);
							}
						}
						else
						{
							newIndex.Assets.Add(newAsset);
						}
					}
				}
				else
				{
					newIndex.Assets.AddRange(list);
				}
				newIndex.Save(indexFileToGenerate);
			});
		return runTask;
	}

	public static void DownloadAndUnzip()
	{
		var index = Index.Load("index.json");

		TaskManager
			.RunInUI(LockEditor, "Lock editor")
			.Then(DownloadIfNeeded(index))
			.Then(RunUnzip)
			.FinallyInUI((success, ex) => {
				if (!success)
					logger.Error(ex);
				UnlockEditor();
				logger.Info($"Unzipping is done with result: {success}");
			})
			.Start();
	}

	public static void JustUnzip()
	{
		var index = Index.Load("index.json");

		var downloads = "Downloads".ToNPath().MakeAbsolute();
		List<Asset> assetList = new List<Asset>();
		foreach (var asset in index.Assets.Where(x => x.NeedsUnzip))
		{
			NPath file = downloads.Combine(asset.Filename);
			if (file.FileExists())
				assetList.Add(new Asset(asset, downloads.Combine(asset.Filename)));
		}

		TaskManager
			.RunInUI(LockEditor, "Lock editor")
			.Then(Unzip(assetList))
			.FinallyInUI((success, ex) => {
				if (!success)
					logger.Error(ex);
				UnlockEditor();
				logger.Info($"Unzipping is done with result: {success}");
			})
			.Start();

	}

	private static TaskQueue<Asset> CalculateWhatNeedsToBeDownloaded(Index index)
	{
		var downloads = "Downloads".ToNPath().MakeAbsolute();
		List<Asset> downloadList = new List<Asset>();

		TaskQueue<Asset> t = new TaskQueue<Asset>(TaskManager) { Message = "Calculating hashes..." };
		foreach (var entry in index.Assets)
		{
			t.Queue(new FuncTask<Asset, Asset>(TaskManager, (_, asset) => {
				NPath file;
				if (asset.NeedsUnzip)
				{
					file = downloads.Combine(asset.Filename);
				}
				else
				{
					file = asset.Path.Combine(asset.Filename);
				}
				var md5 = file.FileExists() ? file.ToMD5() : null;
				return new Asset {
					Hash = md5,
					Path = asset.Path,
					Url = asset.Url,
					NeedsUnzip = asset.NeedsUnzip,
					NeedsDownload = md5 != asset.Hash,
					LocalPath = asset.NeedsUnzip ? downloads.Combine(asset.Filename) : asset.Path.Combine(asset.Filename).MakeAbsolute()
				};
			}, () => entry) { Message = entry.Filename });
			t.Progress(progress => ShowProgress(progress.Message, progress.InnerProgress?.Message, progress.Percentage));
		}
		return t;
	}

	public static ITask<List<Asset>> DownloadIfNeeded(Index index)
	{
		return CalculateWhatNeedsToBeDownloaded(index)
			.Then((_, downloadList) => {

				if (!downloadList.Any(x => x.NeedsDownload))
				{
					logger.Info("Nothing to download!");
					return downloadList;
				}

				// the Downloader is a TaskQueue-type task which
				// handles firing up a series of concurrent tasks,
				// aggregating all of the data from each of the tasks
				// and returning it all together
				var downloader = new Downloader(TaskManager);
				foreach (var asset in downloadList.Where(x => x.NeedsDownload))
					downloader.QueueDownload(asset.Url, asset.LocalPath.Parent, retryCount: 2);

				downloader.Progress(progress => { ShowProgress(progress.Message, progress.InnerProgress?.Message, progress.Percentage); });

				downloader.OnStart += __ => logger.Info("Downloading assets...");
				downloader.OnEnd += (___, __, success, ex) => logger.Info($"Downloader is done with result: {success}");

				// we run this synchronously because we know we're in a thread
				// the queued download tasks will each run in their own thread
				// concurrently, so while this blocks, it doesn't keep the concurrent
				// downloads from happening.
				downloader.RunSynchronously();
				return downloadList;
			});
	}

	public static TaskQueue<NPath> Unzip(List<Asset> assetList)
	{
		var unzipper = new TaskQueue<NPath>(TaskManager);
		foreach (var asset in assetList.Where(x => x.NeedsUnzip))
		{
			var unzipStamp = asset.LocalPath.Parent.Combine($".{asset.Filename}");
			if (unzipStamp.FileExists() && unzipStamp.ReadAllText() == asset.Hash)
				continue;
			var task = new UnzipTask(TaskManager, asset.LocalPath, asset.Path.MakeAbsolute());
			task.OnEnd += (t, extractedPath, success, exception) => {
				if (success)
				{
					unzipStamp.WriteAllText(asset.Hash);
				}
			};
			unzipper.Queue(task);
		}

		unzipper.Progress(progress => {

			var title = progress.InnerProgress?.Message ?? progress.Message;
			var msg = "Extracting ";
			if (progress.InnerProgress?.InnerProgress != null)
			{
				msg += progress.InnerProgress.InnerProgress.Message;
			}
			ShowProgress(title, msg, progress.Percentage);

		});

		unzipper.OnStart += _ => logger.Info("Unzipping assets...");

		return unzipper;
	}

	private static void RunUnzip(bool success, List<Asset> assetList)
	{
		if (assetList == null || assetList.Count == 0)
			return;
		var unzip = Unzip(assetList);
		unzip.RunSynchronously();
	}
}
