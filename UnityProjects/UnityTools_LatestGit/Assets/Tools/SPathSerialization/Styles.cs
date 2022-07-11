using UnityEditor;
using UnityEngine;

namespace SpoiledCat
{
	static class Styles
	{
		public const float BaseSpacing = 10f,
			EntryHeight = 40f,
			TitleHeight = 16f,
			SummaryHeight = 16f,
			EntryPadding = 16f;

		private static GUIStyle label,
			boldLabel,
			labelNoWrap,
			entryTitleStyle,
			entrySummaryStyle,
			entryDetailsStyle,
			headerBoxStyle,
			entryDetailsTitleStyle,
			entryDetailsMetaInfoStyle;


		public static Texture2D EntryIcon
		{
			get { return Utility.GetIcon("icon.png", "", Utility.IsDarkTheme); }
		}

		public static GUIStyle Label
		{
			get
			{
				if (label == null)
				{
					label = new GUIStyle(GUI.skin.label);
					//label.name = "CustomLabel";

					var hierarchyStyle = GUI.skin.FindStyle("PR Label");
					label.onNormal.background = hierarchyStyle.onNormal.background;
					label.onNormal.textColor = hierarchyStyle.onNormal.textColor;
					label.onFocused.background = hierarchyStyle.onFocused.background;
					label.onFocused.textColor = hierarchyStyle.onFocused.textColor;
					label.wordWrap = true;
				}

				return label;
			}
		}

		public static GUIStyle LabelNoWrap
		{
			get
			{
				if (labelNoWrap == null)
				{
					labelNoWrap = new GUIStyle(GUI.skin.label);

					var hierarchyStyle = GUI.skin.FindStyle("PR Label");
					labelNoWrap.onNormal.background = hierarchyStyle.onNormal.background;
					labelNoWrap.onNormal.textColor = hierarchyStyle.onNormal.textColor;
					labelNoWrap.onFocused.background = hierarchyStyle.onFocused.background;
					labelNoWrap.onFocused.textColor = hierarchyStyle.onFocused.textColor;
					labelNoWrap.wordWrap = false;
				}

				return labelNoWrap;
			}
		}

		public static GUIStyle EntryTitleStyle
		{
			get
			{
				if (entryTitleStyle == null)
				{
					entryTitleStyle = new GUIStyle(LabelNoWrap);
					entryTitleStyle.contentOffset = new Vector2(BaseSpacing * 2f, 0);
					entryTitleStyle.alignment = TextAnchor.UpperLeft;
				}

				return entryTitleStyle;
			}
		}

		public static GUIStyle EntrySummaryStyle
		{
			get
			{
				if (entrySummaryStyle == null)
				{
					entrySummaryStyle = new GUIStyle(EditorStyles.miniLabel);
					var c = EditorStyles.miniLabel.normal.textColor;
					entrySummaryStyle.normal.textColor = new Color(c.r, c.g, c.b, c.a * 0.7f);

					entrySummaryStyle.onNormal.background = Label.onNormal.background;
					entrySummaryStyle.onNormal.textColor = Label.onNormal.textColor;
					entrySummaryStyle.onFocused.background = Label.onFocused.background;
					entrySummaryStyle.onFocused.textColor = Label.onFocused.textColor;

					entrySummaryStyle.contentOffset = new Vector2(BaseSpacing * 2, 0);
				}

				return entrySummaryStyle;
			}
		}

		public static GUIStyle EntryDetailsStyle
		{
			get
			{
				if (entryDetailsStyle == null)
				{
					entryDetailsStyle = new GUIStyle(EditorStyles.miniLabel);
					var c = EditorStyles.miniLabel.normal.textColor;
					entryDetailsStyle.normal.textColor = new Color(c.r, c.g, c.b, c.a * 0.7f);

					entryDetailsStyle.onNormal.background = Label.onNormal.background;
					entryDetailsStyle.onNormal.textColor = Label.onNormal.textColor;
					entryDetailsStyle.onFocused.background = Label.onFocused.background;
					entryDetailsStyle.onFocused.textColor = Label.onFocused.textColor;

					entryDetailsStyle.contentOffset = new Vector2(BaseSpacing * 2, 0);
				}

				return entryDetailsStyle;
			}
		}

		public static GUIStyle HeaderBoxStyle
		{
			get
			{
				if (headerBoxStyle == null)
				{
					headerBoxStyle = new GUIStyle("IN BigTitle");
					//headerBoxStyle.name = "HeaderBoxStyle";
					headerBoxStyle.padding = new RectOffset(5, 5, 5, 5);
					headerBoxStyle.margin = new RectOffset(0, 0, 0, 0);
				}

				return headerBoxStyle;
			}
		}

		public static GUIStyle EntryDetailsTitleStyle
		{
			get
			{
				if (entryDetailsTitleStyle == null)
				{
					entryDetailsTitleStyle = new GUIStyle(EditorStyles.boldLabel);
					//historyDetailsTitleStyle.name = "HistoryDetailsTitleStyle";
					entryDetailsTitleStyle.wordWrap = true;
				}
				return entryDetailsTitleStyle;
			}
		}

		public static GUIStyle EntryDetailsMetaInfoStyle
		{
			get
			{
				if (entryDetailsMetaInfoStyle == null)
				{
					entryDetailsMetaInfoStyle = new GUIStyle(EditorStyles.miniLabel);
					//historyDetailsMetaInfoStyle.name = "HistoryDetailsMetaInfoStyle";
					//entryDetailsMetaInfoStyle.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
				}
				return entryDetailsMetaInfoStyle;
			}
		}

	}
}