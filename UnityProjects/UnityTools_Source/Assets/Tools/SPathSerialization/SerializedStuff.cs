using System.Collections.Generic;
using SpoiledCat.SimpleIO;
using SpoiledCat.Unity;
using UnityEngine;

[Location("./referencer.yaml", LocationAttribute.Location.ProjectFolder)]
public class SerializedStuff : ScriptObjectSingleton<SerializedStuff>
{
	[SerializeField] SPath aPath;
	[SerializeField] List<SPath> aBunchOfPaths;

	public SPath APath
	{
		get => aPath;
		set
		{
			aPath = value;
			Save(true);
		}
	}

	public List<SPath> Bunch
	{
		get => aBunchOfPaths ?? (aBunchOfPaths = new List<SPath>());
		set
		{
			aBunchOfPaths = value;
			Save(true);
		}
	}

	public void Save() => base.Save(true);
}
