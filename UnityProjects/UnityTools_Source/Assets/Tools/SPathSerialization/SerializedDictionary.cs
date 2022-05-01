using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace SpoiledCat
{
	[Serializable]
	public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
		[SerializeField] private List<TKey> keys = new List<TKey>();
		[SerializeField] private List<TValue> values = new List<TValue>();

		// save the dictionary to lists
		public virtual void OnBeforeSerialize()
		{
			keys.Clear();
			values.Clear();
			foreach (var pair in this)
			{
				keys.Add(pair.Key);
				values.Add(pair.Value);
			}
		}

		// load dictionary from lists
		public virtual void OnAfterDeserialize()
		{
			Clear();

			if (keys.Count != values.Count)
			{
				throw new SerializationException(
					$"There are {keys.Count} keys and {values.Count} values after deserialization. Make sure that both key and value types are serializable.");
			}

			for (var i = 0; i < keys.Count; i++)
			{
				Add(keys[i], values[i]);
			}
		}
	}
}
