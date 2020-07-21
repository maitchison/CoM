using UnityEngine;

public class RandomColor : MonoBehaviour
{

	// Use this for initialization
	void Start()
	{
		Color color = Color.white;
		switch (Util.Roll(3)) {
		case 1:
			color = Color.red;
			break;
		case 2:
			color = Color.green;
			break;
		case 3:
			color = Color.blue;
			break;
		}

		GetComponent<Light>().color = Color.Lerp(color, new Color(1f, 0.9f, 0.7f), 0.75f);
	}
	
	// Update is called once per frame
	void Update()
	{
	
	}
}
