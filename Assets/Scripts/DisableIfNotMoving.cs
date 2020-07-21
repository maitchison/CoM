using UnityEngine;
using System.Collections;

public class DisableIfNotMoving : MonoBehaviour
{

	Vector3 previousLocation;

	public float Threshold = 1f;

	// Use this for initialization
	void Start()
	{
		previousLocation = transform.position;	
	}
	
	// Update is called once per frame
	void Update()
	{

		if (Application.isEditor && !Application.isPlaying)
			return;

		float speed = (transform.position - previousLocation).magnitude / Time.deltaTime;

		var components = GetComponents<Collider>();
		for (int lp = 0; lp < components.Length; lp++)
			components[lp].enabled = speed > Threshold;

		previousLocation = transform.position;
	}
}
