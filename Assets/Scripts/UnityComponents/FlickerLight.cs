using UnityEngine;

public class FlickerLight : MonoBehaviour
{

	[Range(0f, 1f)]
	public float Strength = 0.15f;
	[Range(0f, 5f)]
	public float Speed = 0.3f;
	[Range(0f, 1f)]
	public float MovementRange = 0.0f;


	private float timeOn;
	private float timeOff;
	private float changeTime = 0;

	private Color dimColor;
	private Color brightColor;

	private float dimRange;
	private float brightRange;
	private Vector3 basePosition;

	private Vector3 movementOffset;

	bool isDim = false;

	void Start()
	{
		dimColor = Color.Lerp(GetComponent<Light>().color, Color.black, Strength);
		brightColor = GetComponent<Light>().color;
		dimRange = GetComponent<Light>().range * Util.Clamp(1f - Strength, 0f, 1f);
		brightRange = GetComponent<Light>().range;
		basePosition = transform.localPosition;
		Cycle();
	}

	void Update()
	{
		if (Time.time > changeTime) {
			Change();
		}
	}

	private void Change()
	{
		isDim = !isDim;
		if (isDim) {
			Cycle();
			GetComponent<Light>().color = dimColor;
			GetComponent<Light>().range = dimRange;
			transform.localPosition = basePosition + movementOffset;
			changeTime = Time.time + timeOff;
		} else {
			GetComponent<Light>().color = brightColor;
			GetComponent<Light>().range = brightRange;
			transform.localPosition = basePosition;
			changeTime = Time.time + timeOn;
		}
	}

	private void Cycle()
	{
		timeOn = Random.value * Speed;
		timeOff = Random.value * Speed;
		movementOffset = new Vector3(Random.value * MovementRange, Random.value * MovementRange, Random.value * MovementRange);
	}

}