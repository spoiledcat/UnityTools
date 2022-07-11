using UnityEngine;

public class SingletonTest : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
		var ins = SingletonPrefab.Instance;
		Debug.Log($"The Default is {ins.theDefault}");
	}
}
