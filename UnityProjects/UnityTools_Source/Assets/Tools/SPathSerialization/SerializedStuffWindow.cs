using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpoiledCat
{
	using Unity;

	public class SerializedStuffWindow : EditorWindow
	{
		[MenuItem("Tools/Serialized Stuff Window")]
		public static void OpenWindow()
		{
			GetWindow<SerializedStuffWindow>().Show();
		}


		[SerializeField] private List<VirtualizedListControl.EntryData> data;
		[SerializeField] private VirtualizedListControl listControl;
		[SerializeField] private Vector2 detailsScroll;

		private void OnGUI()
		{
			if (Event.current.type == EventType.Layout)
			{
				MaybeUpdateData();
			}

			var lastRect = Rect.zero;

			lastRect = EditorGUILayout.BeginVertical();
			if (GUILayout.Button("Fill"))
			{
				SerializedStuff.instance.Bunch.Clear();
				foreach (var file in TheEnvironment.instance.Environment.UnityProjectPath.Files(true))
				{
					SerializedStuff.instance.Bunch.Add(file);
				}
				SerializedStuff.instance.Save();
				needsRefresh = true;
				Repaint();
			}
			EditorGUILayout.EndVertical();

			DoListGui(lastRect);
		}

		bool needsRefresh = false;

		private void MaybeUpdateData()
		{
			needsRefresh = false;
			if (listControl == null)
			{
				listControl = new VirtualizedListControl(false, true);
				needsRefresh = true;
			}

			if (data == null)
			{
				needsRefresh = true;
			}

			if (needsRefresh)
			{
				data = SerializedStuff.instance.Bunch
		                  .Select(x => new VirtualizedListControl.EntryData($"title: {x}", $"summary: {x}", $"detail: {x}"))
		                  .ToList();
			}

			if (needsRefresh || !listControl.Initialized)
				listControl.Load(data);
		}

		protected void DoListGui(Rect rect)
		{
			var listRect = new Rect(0f, rect.height, position.width, position.height - rect.height);

			var requiresRepaint = listControl.Render(listRect,
				singleClick: entry => {
					
				},
				doubleClick: entry => {
					// do something when entry is double clicked
				},
				rightClick: entry =>
				{
					listControl.SelectedIndex = -1;
				});

			if (requiresRepaint)
				Repaint();
		}
	}
}
