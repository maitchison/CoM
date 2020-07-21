using UnityEngine;
using System.Collections;

/** Unity 5.1 has a bug where hinge joint's connected achor is not properly set when created via prefab.  This fixes it. */
public class FixHingeJoint : MonoBehaviour
{

	// Use this for initialization
	void Start()
	{
		var hingeJoint = GetComponentInParent<HingeJoint>();
		if (hingeJoint == null) { 
			Trace.LogWarning("Using hinge joint fixed on object with no HingeJoint.");
			return;
		}

		hingeJoint.connectedAnchor = transform.localToWorldMatrix * hingeJoint.anchor;

	}
	
	// Update is called once per frame
	void Update()
	{
	
	}
}
