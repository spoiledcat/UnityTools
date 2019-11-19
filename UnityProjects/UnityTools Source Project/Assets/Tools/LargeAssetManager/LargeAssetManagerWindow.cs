using System.Threading.Tasks;
using SpoiledCat.Logging;
using SpoiledCat.SimpleIO;
using SpoiledCat.ProcessManager;
using SpoiledCat.Threading;
using SpoiledCat.UI;
using SpoiledCat.Unity;
using UnityEditor;
using UnityEngine;

public class LargeAssetManagerWindow : BaseWindow
{
	[SerializeField] private int webServerPort = 53451;
	private bool webServerRunning;
	private IProcessTask webServerTask;
	private IEnvironment environment;

	private IEnvironment Environment
	{
		get
		{
			if (environment == null)
			{
				TheEnvironment.ApplicationName = "LargeAssetManager";
				environment = TheEnvironment.instance.Environment;
			}
			return environment;
		}
	}

	private IProcessEnvironment processEnvironment;
	private IProcessEnvironment ProcessEnvironment => processEnvironment ?? (processEnvironment = new ProcessEnvironment(Environment));

	[MenuItem("Tools/Show")]
	static void Menu_Show()
	{
		GetWindow<LargeAssetManagerWindow>();
	}

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
		string folder = EditorUtility.OpenFolderPanel("Select Folder with zip files to add to the index", "../../Helpers/Helper.WebServer/files/assets".ToSPath().Resolve(), "");
		if (!string.IsNullOrEmpty(folder))
		{
			LargeAssetManager.UpdateIndexFromFilesInFolder("index-template.json".ToSPath(), "index.json".ToSPath(), folder.ToSPath());
		}
	}

	public override void Initialize(bool firstRun)
	{
		LogHelper.LogAdapter = new MultipleLogAdapter(new UnityLogAdapter());
	}

	public override void OnDataUpdate()
	{
		webServerRunning = webServerTask != null && !webServerTask.IsCompleted;
	}

	public override void OnUI()
	{
		using (new EditorGUILayout.VerticalScope())
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				webServerPort = EditorGUILayout.IntField("Port", webServerPort);
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				LogHelper.TracingEnabled = EditorGUILayout.Toggle("Trace logging", LogHelper.TracingEnabled);
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUI.DisabledGroupScope(webServerRunning))
				{
					if (GUILayout.Button("Start Web Server"))
					{
						webServerTask = new ProcessTaskLongRunning(TaskManager, ProcessEnvironment,
							"Packages/com.spoiledcat.processmanager/Tests/Helpers~/Helper.CommandLine.exe".ToSPath().Resolve(),
							$"-w -p {webServerPort}").Configure(new ProcessManager(Environment, TaskManager.Token));


						webServerTask.FinallyInUI((success, exception) => {
										 if (!success && exception != null)
											 Debug.LogException(exception);
										 else
											 Debug.Log("If we're exiting, it's because the domain is going down or you clicked Stop");
									 })
									 .Start();
					}
				}

				using (new EditorGUI.DisabledGroupScope(!webServerRunning))
				{
					if (GUILayout.Button("Stop Web Server"))
					{
						webServerTask.Stop();
					}
				}

				if (GUILayout.Button("Restart everything"))
				{
					var oldTaskManager = TaskManager;
					TaskManager = new TaskManager().Initialize();
					new TPLTask(TaskManager, ((TaskManager)oldTaskManager).Stop);
				}
			}
		}
	}
}
