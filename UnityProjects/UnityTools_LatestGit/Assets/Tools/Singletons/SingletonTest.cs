using UnityEngine;
using UnityEngine.UI;

public class SingletonTest : MonoBehaviour
{
	public Text text;
	// Start is called before the first frame update
	void Start()
	{
		var ins = SingletonPrefab.Instance;
		Debug.Log($"The Default is {ins.theDefault}");
		text.text = $"The Default in {ins.name} is {ins.theDefault}";
	}
}
