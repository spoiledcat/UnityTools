namespace LocalTools
{
	using SpoiledCat.Json;
	using SpoiledCat.SimpleIO;
	using SpoiledCat.Utilities;

	/// <summary>
	/// SimpleJson serializes private fields in structs
	/// </summary>
	public struct Asset
	{
		private string hash;
		private SPath path;
		private UriString url;
		private bool needsUnzip;

		public Asset(Asset asset, SPath localPath)
		{
			hash = asset.hash;
			path = asset.path;
			url = asset.url;
			needsUnzip = asset.needsUnzip;
			LocalPath = localPath;
			NeedsDownload = false;
		}

		[NotSerialized] public UriString Url { get => url; set => url = value; }
		[NotSerialized] public SPath Path { get => path; set => path = value; }
		[NotSerialized] public string Hash { get => hash; set => hash = value; }
		[NotSerialized] public bool NeedsUnzip { get => needsUnzip; set => needsUnzip = value; }
		[NotSerialized] public SPath LocalPath { get; set; }
		[NotSerialized] public string Filename => url?.Filename ?? "";
		[NotSerialized] public bool NeedsDownload { get; set; }
	}
}
