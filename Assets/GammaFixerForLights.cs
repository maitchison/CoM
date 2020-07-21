using UnityEngine;
using System.Collections;

/** Adjusts lighting on lights for linear / gamma settings */
public class GammaFixerForLights : MonoBehaviour
{

	// Use this for initialization
	void Start()
	{
		var attachedLight = GetComponent<Light>();
		if (Settings.Advanced.LinearLight)
		if (attachedLight != null)
			attachedLight.intensity *= 0.5f;
	}
}
