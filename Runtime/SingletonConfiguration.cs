using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpoiledCat
{
	[Serializable]
	//[CreateAssetMenu(fileName = "singletonconfig.asset", menuName = "SpoiledCat/Create settings asset")]
	public class SingletonConfiguration : ScriptableObject
	{
		[SerializeField] private ScriptPrefabDictionary scriptPrefabMap;
		public Dictionary<string, string> ScriptPrefabMap => scriptPrefabMap;
	}

	[Serializable]
	class ScriptPrefabDictionary : SerializedDictionary<string, string>
	{ }
}
