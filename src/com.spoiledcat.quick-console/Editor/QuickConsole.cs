// Copyright 2018-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpoiledCat
{
	using Json;
	using NiceIO;
	using UI;
	using Extensions;

	public static class PrefabUtilityShim
	{
		public static GameObject CreateVariant(GameObject assetRoot, string path) =>
			PrefabUtility_CreateVariant_method.Invoke(null, new object[] { assetRoot, path }) as GameObject;

		private static MethodInfo PrefabUtility_CreateVariant_method { get; } =
			typeof(PrefabUtility).GetMethod("CreateVariant", BindingFlags.NonPublic | BindingFlags.Static);
	}


	public static class JsonHelper
	{
		public static T From<T>(string json)
		{
			return SimpleJson.DeserializeObject<T>(json);
		}

		public static string To<T>(T obj)
		{
			return SimpleJson.SerializeObject(obj);
		}
	}

	public class QuickConsole : BaseWindow
	{
		private static GUIStyle buttonStyle;

		private static Action EndEditingActiveTextField;
		[NonSerialized] private Compiler compiler;
		[NonSerialized] private TaskScheduler scheduler;

		[NonSerialized] private string selectedSource;
		[NonSerialized] private Rect sourceDropdownRect;
		[SerializeField] private string cmd;
		[SerializeField] private List<UnityReference> context;
		[SerializeField] private bool expandOutput;
		[SerializeField] private bool expandReferences;
		[SerializeField] private string output;
		[SerializeField] private Vector2 outputScrollPos;
		[SerializeField] private Vector2 sourceScrollPos;

		[MenuItem("Debug/Stuff")]
		public static void Stuff()
		{
			//GameObject.Find("GameObject").transform.Cast<Transform>().ForEach(x => Debug.Log(x.gameObject));
			//PrefabUtilityShim.CreateVariant(GameObject.Find("GameObject").transform.Cast<Transform>().First().gameObject, "stuff").GetComponent<MeshFilter>().sharedMesh
			var list = new Dictionary<string, Vector2Int> {
				{ "variant_Curve_002", new Vector2Int(0, 0) },
				{ "variant_Curve_003", new Vector2Int(0, 0) },
				{ "variant_Curve_004", new Vector2Int(0, 0) },
				{ "variant_Curve_005", new Vector2Int(0, 0) },
				{ "variant_Curve_006", new Vector2Int(0, 0) },
				{ "variant_Curve_007", new Vector2Int(0, 0) },
				{ "variant_Curve_008", new Vector2Int(0, 0) },
				{ "variant_Curve_009", new Vector2Int(0, 0) },
				{ "variant_Curve_010", new Vector2Int(0, 0) },
				{ "variant_Curve_011", new Vector2Int(0, 0) },
				{ "variant_Curve_012", new Vector2Int(0, 0) },
				{ "variant_Curve_013", new Vector2Int(0, 0) },
				{ "variant_Curve_014", new Vector2Int(0, 0) },
				{ "variant_Curve_015", new Vector2Int(0, 0) },
				{ "variant_Curve_016", new Vector2Int(0, 0) },
				{ "variant_Curve_017", new Vector2Int(0, 0) },
				{ "variant_Curve_018", new Vector2Int(0, 0) },
				{ "variant_Curve_029", new Vector2Int(0, 0) },
				{ "variant_Curve_020", new Vector2Int(0, 0) },
				{ "variant_Curve_021", new Vector2Int(0, 0) },
				{ "variant_Curve_022", new Vector2Int(0, 0) },
				{ "variant_Curve_023", new Vector2Int(0, 0) },
				{ "variant_Curve_024", new Vector2Int(0, 0) },
				{ "variant_Curve_025", new Vector2Int(0, 0) },
				{ "variant_Curve_026", new Vector2Int(0, 0) },
			};
			GameObject.Find("GameObject").transform.Cast<Transform>().ForEach(x => {

			});

		}

		[MenuItem("Window/Quick Console")]
		public static void Menu_Show()
		{
			GetWindow<QuickConsole>().Show();
		}

		public override void Initialize(bool firstRun)
		{
			EndEditingActiveTextField = () => EndEditingActiveTextField_method.Invoke(null, null);
			compiler = new Compiler();
			scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			if (context == null) context = new List<UnityReference>();
		}

		public override void OnUI()
		{
			using (new EditorGUILayout.VerticalScope())
			{
				expandReferences = EditorGUILayout.Foldout(expandReferences, "References", true);
				if (expandReferences)
				{
					using (new EditorGUILayout.VerticalScope())
					{
						foreach (var entry in context)
						{
							using (new EditorGUILayout.HorizontalScope())
							{
								entry.reference = EditorGUILayout.ObjectField(entry.reference, typeof(Object), true);
								entry.name = EditorGUILayout.TextField("Name", entry.name);

								EditorGUI.BeginChangeCheck();
								entry.childrenAsGameObject = EditorGUILayout.ToggleLeft("Children As GameObject", entry.childrenAsGameObject);
								if (EditorGUI.EndChangeCheck())
								{
									if (entry.childrenAsGameObject) entry.childrenAsTransform = false;
								}

								EditorGUI.BeginChangeCheck();
								entry.childrenAsTransform = EditorGUILayout.ToggleLeft("Children As Transform", entry.childrenAsTransform);
								if (EditorGUI.EndChangeCheck())
								{
									if (entry.childrenAsTransform) entry.childrenAsGameObject = false;
								}
							}
						}

						using (new EditorGUILayout.HorizontalScope())
						{
							if (EditorGUILayout.DropdownButton(new GUIContent("+"), FocusType.Keyboard, GUI.skin.button,
								GUILayout.Width((60))))
							{
								context.Add(new UnityReference { name = $"arg{context.Count}" });
							}

							using (new EditorGUI.DisabledGroupScope(context.Count == 0))
							{
								if (EditorGUILayout.DropdownButton(new GUIContent("-"), FocusType.Keyboard, ButtonStyle,
									GUILayout.Width((60))))
								{
									context.RemoveAt(context.Count - 1);
								}
							}
						}
					}
				}

				var txtID = GUIUtility.GetControlID(FocusType.Passive);
				if (Event.current.type == EventType.KeyDown && GUIUtility.keyboardControl == txtID + 1)
				{
					switch (Event.current.keyCode)
					{
						//case KeyCode.Escape:
						//    cmd = "";
						//    Event.current.Use();
						//    break;
						case KeyCode.Return:
						case KeyCode.KeypadEnter:
							if (Event.current.control || Event.current.command)
							{
								//Debug.Log("running from text field");
								Run(cmd);
								Event.current.Use();
							}
							break;
					}
				}

				using (var scrollView = new EditorGUILayout.ScrollViewScope(sourceScrollPos))
				{
					sourceScrollPos = scrollView.scrollPosition;
					cmd = EditorGUILayout.TextArea(cmd, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
				}

				using (new EditorGUILayout.HorizontalScope())
				{
					if (EditorGUILayout.DropdownButton(new GUIContent("Compile"), FocusType.Keyboard, ButtonStyle,
						GUILayout.Width((60))))
					{
						EndEditingActiveTextField();
						//Debug.Log($"compiling manually {cmd}");
						Compile(cmd);
					}

					if (EditorGUILayout.DropdownButton(new GUIContent("Run"), FocusType.Keyboard, ButtonStyle,
						GUILayout.Width((60))))
					{
						EndEditingActiveTextField();
						//Debug.Log($"running manually {cmd}");
						Run(cmd);
					}
				}

				if (compiler.SourcesByTypeName.Count > 0)
				{
					var clicked = EditorGUILayout.DropdownButton(new GUIContent(selectedSource), FocusType.Keyboard);
					if (InRepaint)
					{
						sourceDropdownRect = GUILayoutUtility.GetLastRect();
					}

					if (clicked)
					{
						var dropdown = new GenericMenu();
						foreach (var item in compiler.SourcesByTypeName)
						{
							dropdown.AddItem(new GUIContent(item.Key), selectedSource == item.Key, data => {
								output = ((KeyValuePair<string, (string snippet, string source)>) data).Value.source;
								selectedSource = ((KeyValuePair<string, (string snippet, string source)>) data).Key;
							}, item);
						}

						dropdown.DropDown(sourceDropdownRect);
					}
				}

				expandOutput = EditorGUILayout.Foldout(expandOutput, "Compiled Source", true);
				if (expandOutput)
				{
					using (var scrollView = new EditorGUILayout.ScrollViewScope(outputScrollPos))
					{
						outputScrollPos = scrollView.scrollPosition;
						output = EditorGUILayout.TextArea(output, GUILayout.ExpandHeight(true));
					}
				}

				if (EditorGUILayout.DropdownButton(new GUIContent("Edit"), FocusType.Keyboard, ButtonStyle))
				{
					if (compiler.SourcesByTypeName.ContainsKey(selectedSource))
					{
						(string snippet, _) = compiler.SourcesByTypeName[selectedSource];
						cmd = snippet;
						Redraw();
					}
				}
			}
		}

		private void Run(string txt)
		{
			output = "";
			var task = new Task<(bool success, string result, Exception exception)>(() =>
				compiler.RunCSharp(context, txt));
			task.ContinueWith(result => {
				if (!result.Result.success)
				{
					Debug.LogException(result.Result.exception);
				}
				else
				{
					Debug.Log("Compiled and Executed");
				}

				output = result.Result.result;
				Redraw();
			}, scheduler);
			task.ContinueWith(fault => Debug.LogException(fault.Exception), TaskContinuationOptions.OnlyOnFaulted);
			task.Start(scheduler);
		}

		private void Compile(string txt)
		{
			output = "";
			var task = new Task<(bool success, string result, Exception exception)>(() =>
				compiler.CompileCSharp(context, txt));
			task.ContinueWith(result => {
				if (!result.Result.success)
				{
					Debug.LogException(result.Result.exception);
				}
				else
				{
					Debug.Log("Compiled");
				}

				output = result.Result.result;
				Redraw();
			}, scheduler);
			task.ContinueWith(fault => Debug.LogException(fault.Exception), TaskContinuationOptions.OnlyOnFaulted);
			task.Start(scheduler);
		}

		private MethodInfo EndEditingActiveTextField_method { get; } =
			typeof(EditorGUI).GetMethod("EndEditingActiveTextField", BindingFlags.NonPublic | BindingFlags.Static);

		public static GUIStyle ButtonStyle {
			get {
//                if (buttonStyle != null) return buttonStyle;
				return buttonStyle = new GUIStyle(GUI.skin.button)
				{
					//name = "SpoiledCat_ButtonStyle",
					richText = true,
					wordWrap = true
				};
				//var json = SimpleJson.SerializeObject(GUI.skin.button);
				//buttonStyle = SimpleJson.DeserializeObject<GUIStyle>(json);
				//buttonStyle.name = "SpoiledCat_ButtonStyle";
				//buttonStyle.richText = true;
				//buttonStyle.wordWrap = true;
				//return buttonStyle;
			}
		}
	}


	public class Compiler
	{
		private const string templateClassHeader = "public static class {0} {{";
		private const string templateClassFooter = "}";

		private const string templateMethodHeaderStart = "public static void {0}(";

		private const string templateMethodHeaderEnd = ") {";

		private const string templateMethodFooter = "}";

		private const string templateSource = @"
// usings
{0}
// class start
{1}
	// method start
	{2}
		// method body
		{3}
	// method end
	{4}
// class end
{5}
";
		private readonly List<string> assemblies = new List<string>();
		private readonly CSharpCodeProvider compiler = new CSharpCodeProvider();

		private readonly List<string> usings = new List<string> {
			"UnityEditor",
			"UnityEngine",
			"System.Collections.Generic",
			"System.Linq",
			"System.IO",
			"SpoiledCat.Json",
			"SpoiledCat.NiceIO",
		};

		private Dictionary<string, (string className, string methodName, string source, Assembly assembly)>
			compiledAssemblies =
				new Dictionary<string, (string className, string methodName, string source, Assembly assembly)>();
		private CompilerParameters compilerParameters;

		public Compiler()
		{
			assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies()
				.Select(x => x.TryGetLocation())
				.Where(x => x != null));

			compilerParameters = new CompilerParameters(assemblies.ToArray()) {
				GenerateExecutable = false,
				GenerateInMemory = true
			};
		}

		public (bool success, string result, Exception exception) CompileCSharp(List<UnityReference> context,
			string csharp)
		{
			var sourceName = GetSourceName(context, csharp);
			if (compiledAssemblies.ContainsKey(sourceName))
			{
				return (true, compiledAssemblies[sourceName].source, null);
			}

			var args = context.Where(x => x.reference != null && !string.IsNullOrEmpty(x.name)).ToArray();
			(CompilerResults compilerResults, string source, _, _) = Compile(args, csharp);
			Exception exception = null;
			if (compilerResults.Errors.HasErrors)
			{
				exception = new CompilerException(compilerResults.Errors);
			}

			return (exception == null, source, exception);
		}

		public
			(bool success, string result, Exception exception)
			RunCSharp(List<UnityReference> context, string csharp)
		{
			bool success = false;
			string result = null;
			Exception exception = null;
			(string className, string methodName, string source) = (null, null, null);
			Assembly compiledAssembly = null;

			var args = context.Where(x => x.reference != null && !string.IsNullOrEmpty(x.name)).ToArray();
			var sourceName = GetSourceName(args, csharp);
			if (compiledAssemblies.ContainsKey(sourceName))
			{
				(className, methodName, source, compiledAssembly) = compiledAssemblies[sourceName];
			}
			else
			{
				CompilerResults compilerResults;
				(compilerResults, source, className, methodName) = Compile(args, csharp);

				if (compilerResults.Errors.HasErrors)
				{
					return (false, source, new CompilerException(compilerResults.Errors));
				}

				compiledAssembly = compilerResults.CompiledAssembly;
			}

			Type type = compiledAssembly.GetType(className);
			MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
			try
			{
				method.Invoke(null, args.Select(x => x.reference as object).ToArray());
				success = true;
			}
			catch (Exception ex)
			{
				exception = ex;
			}

			result = source;
			return (success, result, exception);
		}

		private
			(CompilerResults compilerResults, string source, string className, string methodName)
			Compile(UnityReference[] args, string csharp)
		{
			(string source, string className, string methodName) = GenerateSource(args, csharp);

			compilerParameters.OutputAssembly = $"Temp/assembly_{className}.dll".ToNPath().ToString();
			CompilerResults r = compiler.CompileAssemblyFromSource(compilerParameters, source);

			if (!r.Errors.HasErrors)
			{
				var typeName = $"{className}.{methodName}";
				var sourceName = GetSourceName(args, csharp);
				if (!compiledAssemblies.ContainsKey(sourceName))
				{
					SourcesByTypeName.Add(typeName, (csharp, source));
					compiledAssemblies.Add(sourceName, (className, methodName, source, r.CompiledAssembly));
					compilerParameters.ReferencedAssemblies.Add(compilerParameters.OutputAssembly);
				}
			}

			return (r, source, className, methodName);
		}

		private
			(string source, string className, string methodName)
			GenerateSource(UnityReference[] args, string csharp)
		{
			var className = $"Type{SourcesByTypeName.Count}";
			var methodName = $"Run{SourcesByTypeName.Count}";
			var sourceSignature =
				$@"{string.Format(templateMethodHeaderStart, methodName)} {args.GetMethodSignature()} {templateMethodHeaderEnd}";

			var body = new StringBuilder();
			args.GetMethodHeader().ForEach(x => body.AppendLine(x));
			body.AppendLine("        " + csharp.Replace("\n", "\n        "));

			return (string.Format(templateSource, string.Join(Environment.NewLine, usings.Select(x => $"using {x};")),
				string.Format(templateClassHeader, className), sourceSignature, body.ToString(),
				templateMethodFooter, templateClassFooter), className, methodName);
		}

		private static string GetSourceName(List<UnityReference> context, string csharp) =>
			$"{context.GetMethodSignature()}::{csharp}";

		private static string GetSourceName(UnityReference[] context, string csharp) =>
			$"{context.GetMethodSignature()}::{csharp}";

		public Dictionary<string, (string snippet, string source)> SourcesByTypeName { get; } = new Dictionary<string, (string snippet, string source)>();
	}

	public class CompilerException : Exception
	{
		public CompilerException(CompilerErrorCollection errors) : base($@"Error compiling expression
{string.Join(Environment.NewLine, errors.OfType<CompilerError>().Select(e => e.ErrorText).ToArray())}")
		{}
	}

	static class UnityReferenceExtensions
	{
		internal static string GetMethodSignature(this List<UnityReference> args)
		{
			return string.Join(",",
				args.Where(x => x.reference != null && !string.IsNullOrEmpty(x.name))
					.Select(x => $"{x.reference.GetType()} {x.name}"));
		}

		internal static string GetMethodSignature(this UnityReference[] args)
		{
			return string.Join(",", args.Select(x => $"{x.reference.GetType()} {(x.childrenAsGameObject || x.childrenAsTransform ? "import_" : "")}{x.name}"));
		}

		internal static IEnumerable<string> GetMethodHeader(this UnityReference[] args)
		{
			foreach (var arg in args)
			{
				if (arg.childrenAsGameObject || arg.childrenAsTransform)
				{
					yield return $"var {arg.name} = import_{arg.name}.transform.Cast<Transform>().Select(x => x{(arg.childrenAsGameObject ? ".gameObject" : "")}).ToArray();";
				}
			}
		}
	}

	[Serializable]
	public class UnityReference
	{
		[SerializeField] public bool childrenAsGameObject;
		[SerializeField] public bool childrenAsTransform;
		[SerializeField] public string name;
		[SerializeField] public Object reference;
	}

}
