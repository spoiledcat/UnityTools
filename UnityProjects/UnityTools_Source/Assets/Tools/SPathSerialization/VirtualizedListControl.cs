using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpoiledCat
{

	[Serializable]
	public class VirtualizedListControl
	{
		public struct EntryData
		{
			public static EntryData Default = new EntryData();

			public string Id;
			public string Title;
			public string Summary;
			public string Detail;

			public EntryData(string title, string summary, string detail)
			{
				Id = $"{title.GetHashCode()}+{summary.GetHashCode()}+{detail.GetHashCode()}";
				Title = title;
				Summary = summary;
				Detail = detail;
			}
		}

		[SerializeField] private Vector2 scroll;
		[SerializeField] private Vector2 detailsScroll;
		[SerializeField] private List<EntryData> entries = new List<EntryData>();
		[SerializeField] private bool cacheData;
		[SerializeField] private bool showDetails;

		private List<EntryData> rawData;

		private List<EntryData> Entries
		{
			get
			{
				if (cacheData) return entries;
				return rawData;
			}
			set
			{
				if (cacheData)
					entries = value;
				else
					rawData = value;
			}
		}

		public bool Initialized => Entries != null;

		[SerializeField] private int selectedIndex = -1;

		[NonSerialized] private Action<EntryData> rightClickNextRender;
		[NonSerialized] private EntryData rightClickNextRenderEntry;
		[NonSerialized] private int controlId;

		public int SelectedIndex
		{
			get => selectedIndex;
			set => selectedIndex = value;
		}

		public EntryData SelectedEntry =>
			Entries == null || SelectedIndex < 0 ? EntryData.Default : Entries[SelectedIndex];

		public VirtualizedListControl(bool cacheData, bool showDetails)
		{
			this.cacheData = cacheData;
			this.showDetails = showDetails;
		}

		public bool Render(Rect containingRect, Action<EntryData> singleClick = null,
			Action<EntryData> doubleClick = null, Action<EntryData> rightClick = null)
		{
			if (Entries == null)
			{
				//Debug.LogError("[VirtualizedListControl] [Render] You must call Load before calling Render");
				return false;
			}

			if (rightClick != null && showDetails)
			{
				var r = rightClick;
				rightClick = e =>
				{
					r(e);
					SelectedIndex = -1;
				};
			}

			var requiresRepaint = false;
			scroll = GUILayout.BeginScrollView(scroll);
			{
				controlId = GUIUtility.GetControlID(FocusType.Keyboard);

				if (Event.current.type != EventType.Repaint)
				{
					if (rightClickNextRender != null)
					{
						rightClickNextRender.Invoke(rightClickNextRenderEntry);
						rightClickNextRender = null;
						rightClickNextRenderEntry = EntryData.Default;
					}
				}

				var startDisplay = scroll.y;
				var endDisplay = scroll.y + containingRect.height;

				var rect = new Rect(containingRect.x, containingRect.y, containingRect.width, 0);

				for (var index = 0; index < Entries.Count; index++)
				{
					var entry = Entries[index];

					var entryRect = new Rect(rect.x, rect.y, rect.width, Styles.EntryHeight);

					var shouldRenderEntry = !(entryRect.y > endDisplay || entryRect.yMax < startDisplay);
					if (shouldRenderEntry && Event.current.type == EventType.Repaint)
					{
						RenderEntry(entryRect, entry, index);
					}

					var entryRequiresRepaint =
						HandleInput(entryRect, entry, index, singleClick, doubleClick, rightClick);
					requiresRepaint = requiresRepaint || entryRequiresRepaint;

					rect.y += Styles.EntryHeight;
				}

				GUILayout.Space(rect.y - containingRect.y);
			}
			GUILayout.EndScrollView();

			if (showDetails && SelectedIndex >= 0)
			{
				RenderDetails(SelectedEntry);
			}

			return requiresRepaint;
		}

		private void RenderEntry(Rect entryRect, EntryData entry, int index)
		{
			var isSelected = index == SelectedIndex;
			var titleRect = new Rect(entryRect.x, entryRect.y + Styles.BaseSpacing / 2, entryRect.width,
				Styles.TitleHeight + Styles.BaseSpacing);
			var summaryRect = new Rect(entryRect.x, entryRect.yMax - Styles.SummaryHeight - Styles.BaseSpacing / 2,
				entryRect.width, Styles.SummaryHeight);

			var hasKeyboardFocus = GUIUtility.keyboardControl == controlId;

			Styles.Label.Draw(entryRect, GUIContent.none, false, false, isSelected, hasKeyboardFocus);
			Styles.EntryTitleStyle.Draw(titleRect, entry.Title, false, false, isSelected, hasKeyboardFocus);

			Styles.EntryDetailsStyle.Draw(summaryRect, entry.Summary, false, false, isSelected, hasKeyboardFocus);

			const float LocalIndicatorSize = 6f;
			var localIndicatorRect = new Rect(entryRect.x + (Styles.BaseSpacing - 2),
				titleRect.y + Styles.BaseSpacing / 2, LocalIndicatorSize, LocalIndicatorSize);

			DrawTimelineRectAroundIconRect(entryRect, localIndicatorRect);

			GUI.DrawTexture(localIndicatorRect, Styles.EntryIcon);

			titleRect.Set(localIndicatorRect.xMax, titleRect.y, titleRect.width - LocalIndicatorSize, titleRect.height);
		}

		public void RenderDetails(EntryData entry)
		{
			detailsScroll = GUILayout.BeginScrollView(detailsScroll, GUILayout.Height(100));
			{
				GUILayout.BeginVertical(Styles.HeaderBoxStyle);
				GUILayout.Label(entry.Title, Styles.EntryDetailsTitleStyle);

				GUILayout.Space(-5);

				GUILayout.BeginHorizontal();
				GUILayout.Label(entry.Summary, Styles.EntryDetailsMetaInfoStyle);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				GUILayout.Space(3);
				GUILayout.EndVertical();

				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

				GUILayout.Label(entry.Detail, Styles.Label);
			}
			GUILayout.EndScrollView();
		}


		private bool HandleInput(Rect rect, EntryData entry, int index, Action<EntryData> singleClick = null,
			Action<EntryData> doubleClick = null, Action<EntryData> rightClick = null)
		{
			var requiresRepaint = false;
			var clickRect = new Rect(0f, rect.y, rect.width, rect.height);
			if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
			{
				Event.current.Use();
				GUIUtility.keyboardControl = controlId;

				SelectedIndex = index;
				requiresRepaint = true;
				var clickCount = Event.current.clickCount;
				var mouseButton = Event.current.button;

				if (mouseButton == 0 && clickCount == 1 && singleClick != null)
				{
					singleClick(entry);
				}

				if (mouseButton == 0 && clickCount > 1 && doubleClick != null)
				{
					doubleClick(entry);
				}

				if (mouseButton == 1 && clickCount == 1 && rightClick != null)
				{
					rightClickNextRender = rightClick;
					rightClickNextRenderEntry = entry;
				}
			}

			// Keyboard navigation if this child is the current selection
			if (GUIUtility.keyboardControl == controlId && index == SelectedIndex &&
			    Event.current.type == EventType.KeyDown)
			{
				var directionY = Event.current.keyCode == KeyCode.UpArrow ? -1 :
					Event.current.keyCode == KeyCode.DownArrow ? 1 : 0;
				if (directionY != 0)
				{
					Event.current.Use();

					if (directionY > 0)
					{
						requiresRepaint = SelectNext(index) != index;
					}
					else
					{
						requiresRepaint = SelectPrevious(index) != index;
					}
				}
			}

			return requiresRepaint;
		}

		private void DrawTimelineRectAroundIconRect(Rect parentRect, Rect iconRect)
		{
			Color timelineBarColor = new Color(0.51F, 0.51F, 0.51F, 0.2F);

			// Draw them lines
			//
			// First I need to figure out how large to make the top one:
			// I'll subtract the entryRect.y from the mergeIndicatorRect.y to
			// get the difference in length. then subtract a little more for padding
			float topTimelineRectHeight = iconRect.y - parentRect.y - 2;
			// Now let's create the rect
			Rect topTimelineRect = new Rect(
				parentRect.x + Styles.BaseSpacing,
				parentRect.y,
				2,
				topTimelineRectHeight);

			// And draw it
			EditorGUI.DrawRect(topTimelineRect, timelineBarColor);

			// Let's do the same for the bottom
			float bottomTimelineRectHeight = parentRect.yMax - iconRect.yMax - 2;
			Rect bottomTimelineRect = new Rect(
				parentRect.x + Styles.BaseSpacing,
				parentRect.yMax - bottomTimelineRectHeight,
				2,
				bottomTimelineRectHeight);
			EditorGUI.DrawRect(bottomTimelineRect, timelineBarColor);
		}

		public void Load(List<EntryData> loadEntries)
		{
			if (loadEntries == null)
			{
				//Debug.LogError("[VirtualizedListControl] [Load] Cannot load null data");
				return;
			}

			var selectedId = SelectedEntry.Id;
			var scrollValue = scroll.y;

			var previousCount = Entries?.Count ?? 0;

			var scrollIndex = (int) (scrollValue / Styles.EntryHeight);

			Entries = loadEntries;

			var selectionPresent = false;
			for (var index = 0; index < Entries.Count; index++)
			{
				var entry = Entries[index];
				if (entry.Id.Equals(selectedId))
				{
					selectedIndex = index;
					selectionPresent = true;
					break;
				}
			}

			if (!selectionPresent)
			{
				selectedIndex = -1;
			}

			if (scrollIndex > Entries.Count)
			{
				ScrollTo(0);
			}
			else
			{
				var scrollOffset = scrollValue % Styles.EntryHeight;

				ScrollTo(scrollIndex, scrollOffset);
				//var scrollIndexFromBottom = previousCount - scrollIndex;
				//var newScrollIndex = Entries.Count - scrollIndexFromBottom;

				//ScrollTo(newScrollIndex, scrollOffset);
			}
		}

		private int SelectNext(int index)
		{
			index++;

			if (index < Entries.Count)
			{
				SelectedIndex = index;
			}
			else
			{
				index = -1;
			}

			return index;
		}

		private int SelectPrevious(int index)
		{
			index--;

			if (index >= 0)
			{
				SelectedIndex = index;
			}
			else
			{
				SelectedIndex = -1;
			}

			return index;
		}

		public void ScrollTo(int index, float offset = 0f)
		{
			scroll.Set(scroll.x, Styles.EntryHeight * index + offset);
		}
	}
}