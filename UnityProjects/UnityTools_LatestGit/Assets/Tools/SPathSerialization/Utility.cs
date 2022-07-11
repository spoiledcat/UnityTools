using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpoiledCat
{
	class Utility
	{
		private static Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();

		public static bool IsDarkTheme
		{
			get
			{
				var defaultTextColor = Styles.Label.normal.textColor;
				return defaultTextColor.r > 0.5f && defaultTextColor.g > 0.5f && defaultTextColor.b > 0.5f;
			}
		}

		public static Texture2D GetIcon(string filename, string filename2x = "", bool invertColors = false)
		{
			if (EditorGUIUtility.pixelsPerPoint > 1f && !string.IsNullOrEmpty(filename2x))
			{
				filename = filename2x;
			}

			var key = invertColors ? "dark_" + filename : "light_" + filename;

			if (iconCache.ContainsKey(key))
			{
				return iconCache[key];
			}

			Texture2D texture2D = TryGetTexture(filename);

			if (texture2D != null)
			{
				texture2D.hideFlags = HideFlags.HideAndDontSave;
				if (invertColors)
				{
					texture2D.InvertColors();
				}
				iconCache.Add(key, texture2D);
			}
			else
			{
				texture2D = GetTextureFromColor(Color.red);
			}

			return texture2D;
		}

		private static Texture2D TryGetTexture(string resource)
		{
			string possiblePath = $"Assets/Editor Default Resources/Icons/{resource}";
			if (File.Exists(possiblePath))
			{
				return AssetDatabase.LoadAssetAtPath<Texture2D>(possiblePath);
			}

			using (var stream = TryGetStream("Icons", resource))
			{
				return stream.ToTexture2D();
			}
		}

		private static Stream TryGetStream(string basePath, string resource)
		{
			string possiblePath = $"{basePath.Replace('\\', '/').TrimEnd('/')}/{resource}";
			if (File.Exists(possiblePath))
			{
				return new MemoryStream(File.ReadAllBytes(possiblePath));
			}
			return null;
		}


		public static Texture2D GetTextureFromColor(Color color)
		{
			Color[] pix = new Color[1];
			pix[0] = color;

			Texture2D result = new Texture2D(1, 1);
			result.hideFlags = HideFlags.HideAndDontSave;
			result.SetPixels(pix);
			result.Apply();

			return result;
		}
	}
}