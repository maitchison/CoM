using UnityEngine;
using System.Collections;

/** Pushes a door infront of player open. */
public class PushOpenDoor : MonoBehaviour
{

	// Use this for initialization
	void Start()
	{
	
	}
	
	// Update is called once per frame
	void Update()
	{
	
	}

	void OnTriggerStay(Collider other)
	{
		var script = other.GetComponent<DoorScript>();
		if (script != null)
			script.HoldOpen = true;
	}
}
