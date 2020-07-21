using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
	public Text TextControl;

	private float progress {
		get { return Application.GetStreamProgressForLevel("Game"); } 
	}

	// Update is called once per frame
	void Update()
	{
		
		TextControl.text = string.Format("{0}%", (progress * 100f).ToString("0.0"));

		if (progress == 1f) {
			Application.LoadLevel("Game");
		}
			
	}
}
