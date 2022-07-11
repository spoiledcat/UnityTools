using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpoiledCat
{
	[Serializable]
	public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
		[Serializable]
		struct SerializedDictItem
		{
			public TKey key;
			public TValue value;

			public SerializedDictItem(TKey key, TValue value)
			{
				this.key = key;
				this.value = value;
			}
		}
		[SerializeField] private List<SerializedDictItem> items = new List<SerializedDictItem>();

		// save the dictionary to lists
		public virtual void OnBeforeSerialize()
		{
			items.Clear();
			foreach (var pair in this)
			{
				items.Add(new SerializedDictItem(pair.Key, pair.Value));
			}
		}

		// load dictionary from lists
		public virtual void OnAfterDeserialize()
		{
			Clear();

			for (var i = 0; i < items.Count; i++)
			{
				Add(items[i].key, items[i].value);
			}
		}
	}
}
