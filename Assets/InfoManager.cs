using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Data;

public class InfoManager : MonoBehaviour
{

	public Text SaveInfoText;

	/** Returns information about the current save file. */
	private string saveInfo { 
		get { 
			string result = string.Format("Data is saved at {0}\n", StateStorage.SaveDataLocation.ToString());

			if (SaveFile.HasSave)
				result += string.Format(
					"Save file present. v{0}", Settings.Information.SaveVersion
				);
			else
				result += "No save file present.";
			return result;		
		}
	}

	// Use this for initialization
	void Start()
	{
		if (SaveInfoText != null)
			SaveInfoText.text = saveInfo;
	}
	
	// Update is called once per frame
	void Update()
	{
	
	}
}
