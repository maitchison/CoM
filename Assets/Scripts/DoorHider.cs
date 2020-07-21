using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

/** Makes any doors that are super close to camera invisible */
public class DoorHider : MonoBehaviour
{

	// Use this for initialization
	void Start()
	{
	
	}
	
	// Update is called once per frame
	void Update()
	{
	
	}

	void OnTriggerEnter(Collider other)
	{
		var script = other.GetComponent<DoorScript>();

		if (script != null)
			script.gameObject.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
	}

	void OnTriggerExit(Collider other)
	{
		var script = other.GetComponent<DoorScript>();
		if (script != null)
			script.gameObject.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
	}


}
