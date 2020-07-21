using UnityEngine;

/**
 * Slowly bring light brightness up when activated 
 */
public class FadeLightOn : MonoBehaviour
{
	float origionalBrightness;
	private Light ourLight;

	private float power = 1.0f;

	public bool EnableLight = true;

	// Use this for initialization
	void Start()
	{
		origionalBrightness = GetComponent<Light>().intensity;
		ourLight = GetComponent<Light>();
	}

	/** Causes lights power to instantly be set to correct value. */
	public void Sync()
	{
		if (ourLight == null)
			return;
		power = EnableLight ? 1 : 0;
		ourLight.intensity = power * origionalBrightness;
	}
	
	// Update is called once per frame
	void Update()
	{
		if (!EnableLight && (!ourLight.enabled))
			return;

		if (EnableLight) {
			ourLight.enabled = true;
			power = Util.Clamp(power + Time.deltaTime * 4f, 0, 1);
		} else {
			power = Util.Clamp(power - Time.deltaTime, 0, 1);
			if (power == 0) {
				GetComponent<Light>().intensity = 0;
				ourLight.enabled = false;
				return;
			}
		}

		ourLight.intensity = power * origionalBrightness;

	}
}
