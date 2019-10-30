namespace LocalTools
{
	using System.Collections.Generic;
	using System.Linq;
	using SpoiledCat.Json;
	using SpoiledCat.NiceIO;

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

		public static Index Load(NPath indexFile) => indexFile.ReadAllText().FromJson<Index>(true, false);
		public static Index Load(string indexFile) => Load(indexFile.ToNPath());

		public void Save(NPath indexFile) => indexFile.WriteAllText(this.ToJson(true, false));
		public void Save(string indexFile) => Save(indexFile.ToNPath());
	}
}
