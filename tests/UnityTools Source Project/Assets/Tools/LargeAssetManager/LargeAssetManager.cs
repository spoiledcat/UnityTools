using SpoiledCat.Json;
using SpoiledCat.Logging;
using SpoiledCat.NiceIO;
using SpoiledCat.Threading;
using SpoiledCat.UI;
using SpoiledCat.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using LocalTools;
using System;

namespace LocalTools
{
    public static class Extensions
    {
        public static string ToMD5(this NPath path)
        {
            byte[] computeHash;
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = NPath.FileSystem.OpenRead(path.MakeAbsolute()))
                {
                    computeHash = md5.ComputeHash(stream);
                }
            }

            return System.BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
        }
    }

    public struct Asset
    {
        private string hash;
        private NPath path;
        private UriString url;
        private bool needsUnzip;

        public Asset(Asset asset, NPath localPath)
        {
            this.hash = asset.hash;
            this.path = asset.path;
            this.url = asset.url;
            this.needsUnzip = asset.needsUnzip;
            this.LocalPath = localPath;
            this.NeedsDownload = false;
        }

        [NotSerialized]
        public UriString Url { get => url; set => url = value; }
        [NotSerialized]
        public NPath Path { get => path; set => path = value; }
        [NotSerialized]
        public string Hash { get => hash; set => hash = value; }
        [NotSerialized]
        public bool NeedsUnzip { get => needsUnzip; set => needsUnzip = value; }
        [NotSerialized]
        public NPath LocalPath { get; set; }
        [NotSerialized]
        public string Filename => url?.Filename ?? "";
        [NotSerialized]
        public bool NeedsDownload { get; set; }
    }

    public struct Index
    {
        private List<Asset> assets;
        [NotSerialized]
		public List<Asset> Assets => assets ?? (assets = new List<Asset>());
        public Index(IEnumerable<Asset> assets)
        {
            this.assets = assets.ToList();
        }

        public Index(List<Asset> assets)
        {
            this.assets = assets;
        }

        public static Index Load(NPath indexFile) => indexFile.ReadAllText().FromJson<Index>(true, false);
        public static Index Load(string indexFile) => Load(indexFile.ToNPath());

        public void Save(NPath indexFile) => indexFile.WriteAllText(this.ToJson(true, false));
        public void Save(string indexFile) => Save(indexFile.ToNPath());
    }
}

public class LargeAssetManagerWindow : BaseWindow
{
    [MenuItem("Tools/Unlock")]
    static void Menu_Unlock()
    {
		LargeAssetManager.UnlockEditor();
    }

    [MenuItem("Tools/Download assets")]
    static void Menu_DownloadAssets()
    {
		GetWindowDontShow<LargeAssetManagerWindow>().DownloadAndUnzip();
    }

    [MenuItem("Tools/Unzip assets")]
    static void Menu_UnzipAssets()
    {
		GetWindowDontShow<LargeAssetManagerWindow>().JustUnzip();
    }

	[MenuItem("Tools/Create Index")]
	static void Menu_CreateIndex()
	{
		GetWindowDontShow<LargeAssetManagerWindow>().CreateIndex();
	}

	private void DownloadAndUnzip()
	{
		LargeAssetManager.TaskManager = TaskManager;
		LargeAssetManager.DownloadAndUnzip();
	}

	private void JustUnzip()
	{
		LargeAssetManager.TaskManager = TaskManager;
		LargeAssetManager.JustUnzip();
	}

	private void CreateIndex()
	{
		LargeAssetManager.TaskManager = TaskManager;
		string folder = EditorUtility.OpenFolderPanel("Select Folder with zip files to add to the index", "../TestWebServer/files/assets", "");
		if (!string.IsNullOrEmpty(folder))
		{
			LargeAssetManager.UpdateIndexFromFilesInFolder("index-template.json".ToNPath(), "index.json".ToNPath(), folder.ToNPath());
		}
	}
}

public class LargeAssetManager
{
	public static ITaskManager TaskManager;

	public static int DefaultWebServerPort = 58392;
	public static string DefaultWebServerUrl = "http://localhost";

    private static ILogging logger = LogHelper.GetLogger<LargeAssetManager>();
    static LargeAssetManager() {
        PocoJsonSerializerStrategy.RegisterCustomTypeHandler<NPath>(
			value => (((NPath)value).ToString(SlashMode.Forward), true),
			(value, type) => {
                string str = value as string;
                if (!string.IsNullOrEmpty(str))
                {
                    return ( new NPath(str), true) ;
                }
                return ( NPath.Default, true );
            });

        PocoJsonSerializerStrategy.RegisterCustomTypeHandler<UriString>(value => (value.ToString(), true), (value, type) => (new UriString(value as string), true));

		LogHelper.LogAdapter = new MultipleLogAdapter(new UnityLogAdapter());
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
			var asset = new Asset { Hash = file.ToMD5(), };

			if ((templateIndex?.Assets.Any(x => x.Filename == file.FileName)) ?? false)
			{
				var templateAsset = templateIndex.Value.Assets.First(x => x.Filename == file.FileName);
				asset.Path = templateAsset.Path;
				asset.Url = templateAsset.Url.Combine("assets")
				                         .Combine(dt)
				                         .Combine(file.FileName);
				asset.NeedsUnzip = templateAsset.NeedsUnzip;
			}
			else
			{
				asset.Path = file.FileNameWithoutExtension.ToNPath();
				asset.NeedsUnzip = file.ExtensionWithDot == ".zip" ? true : false;
				asset.Url = $"{DefaultWebServerUrl}:{DefaultWebServerPort}/assets/{dt}/{file.FileName}";
			}

			hashTask.Queue(new FuncTask<Asset>(TaskManager, () => asset) { Message = file.FileName });
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

        ITask task = TaskManager
			.RunInUI(LockEditor, "Lock editor")
            .Then(DownloadIfNeeded(index))
            .Then(RunUnzip)
            .FinallyInUI((success, ex) => {
                if (!success)
                    logger.Error(ex);
                UnlockEditor();
                logger.Info($"Unzipping is done with result: {success}");
            });

        task.Start();
    }

    public static void JustUnzip()
    {
        var index = Index.Load("index.json");

        var downloads = "Downloads".ToNPath().MakeAbsolute();
        List<Asset> assetList= new List<Asset>();
        foreach (var asset in index.Assets.Where(x => x.NeedsUnzip))
        {
            NPath file = downloads.Combine(asset.Filename);
            if (file.FileExists())
                assetList.Add(new Asset(asset, downloads.Combine(asset.Filename)));
        }


        ITask task = TaskManager
			.RunInUI(LockEditor, "Lock editor")
            .Then(Unzip(assetList))
            .FinallyInUI((success, ex) => {
                if (!success)
                    logger.Error(ex);
                UnlockEditor();
                logger.Info($"Unzipping is done with result: {success}");
            });

        task.Start();
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
                if (asset.NeedsUnzip) {
                    file = downloads.Combine(asset.Filename);
                } else
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

                var downloader = new Downloader(TaskManager);
                foreach (var asset in downloadList.Where(x => x.NeedsDownload))
                    downloader.QueueDownload(asset.Url, asset.LocalPath.Parent, retryCount: 2);

                downloader.Progress(progress =>
                {
                    ShowProgress(progress.Message, progress.InnerProgress?.Message, progress.Percentage);
                });

                downloader.OnStart += __ => logger.Info("Downloading assets...");
                downloader.OnEnd += (___, __, success, ex) => logger.Info($"Downloader is done with result: {success}");

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
            task.OnEnd += (t, extractedPath, success, exception) =>
            {
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
                if (progress.InnerProgress?.InnerProgress != null) {
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
