namespace LocalTools
{
	using System.Collections.Generic;
	using System.Linq;
	using SpoiledCat.Json;
	using SpoiledCat.SimpleIO;

	public struct Index
	{
		private List<Asset> assets;
		[NotSerialized] public List<Asset> Assets => assets ?? (assets = new List<Asset>());

		public Index(IEnumerable<Asset> assets)
		{
			this.assets = assets.ToList();
		}

		public Index(List<Asset> assets)
		{
			this.assets = assets;
		}

		public static Index Load(SPath indexFile) => indexFile.ReadAllText().FromJson<Index>(true, false);
		public static Index Load(string indexFile) => Load(indexFile.ToSPath());

		public void Save(SPath indexFile) => indexFile.WriteAllText(this.ToJson(true, false));
		public void Save(string indexFile) => Save(indexFile.ToSPath());
	}
}
