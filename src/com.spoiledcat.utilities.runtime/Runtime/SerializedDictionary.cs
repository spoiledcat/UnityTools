// Copyright 2016-2022 Andreia Gaita
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
