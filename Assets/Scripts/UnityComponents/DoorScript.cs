using UnityEngine;
using Mordor;
using Culler;

public enum OpenStyle
{
	None,
	Rise,
	Physics
}

/** Used animate doors open and closed. */
public class DoorScript : MonoBehaviour
{
	public float OpenSpeed = 2f;
	public float CloseSpeed = 0.5f;

	public float MaxOpenDegree = 1f;

	public OpenStyle OpenMethod = OpenStyle.Rise;

	public GameObject PhysicsDoor;

	private float lastHeldOpenTime;

	/** 
	 *When set to true door will be held open.  
	 *However each frame it will be reset to false again, so you will need to constantly set this to keep the door open.
	 */
	public bool HoldOpen {
		get { return _holdOpen; }
		set {
			_holdOpen = value;
			if (value) {
				lastHeldOpenTime = Time.time;
				holdOpenDelayTimer = HoldOpenDelay;
			}
		}
	}

	private bool wasRecentlyToldToOpen {
		get {
			return (Time.time - lastHeldOpenTime) <= 1f;
		}
	}

	public bool Log = false;

	private bool _holdOpen = false;

	/** Set to false is this door doesn't block vision when it's closed. */
	public bool BlocksVision = true;

	private bool isClosed {
		get { return openDegree <= 0; }
	}

	private float openDegree = 0f;

	public float HoldOpenDelay;

	public string _debugString;

	private float baseHeight = 0f;

	/** Used to delay the closing of the door */
	private float countDown = 0f;
	private Vector3 origionalRotation;
	private float currentOpenDirection;
	private float holdOpenDelayTimer = 0f;

	private GridReference gridReference;

	private HingeJoint hinge;

	// Use this for initialization
	void Start()
	{
		Quaternion rotation = gameObject.transform.localRotation;
		origionalRotation = rotation.eulerAngles;
		baseHeight = transform.localPosition.y;
		gridReference = GetComponent<GridReference>();

		hinge = PhysicsDoor == null ? null : PhysicsDoor.GetComponent<HingeJoint>();
	}

	/** All we do here is match our open amount to the doors open amount */
	private void updatePhysicsDoor()
	{
		if (hinge == null) {
			Trace.LogWarning("No hingle joint for physcis door.  Disabling.");
			gameObject.SetActive(false);
			return;
		}

		// when door is 3 degrees closed and moving slowly count it as closed.
		if (Mathf.Abs(hinge.angle) > 10f)
			openDegree = 1f;
		else
			openDegree = Mathf.Clamp((Mathf.Abs(hinge.velocity) - 0.1f), 0f, 99f);		
	}

	private void updateRiseDoor()
	{
		if (isClosed && !HoldOpen)
			return;

		if (holdOpenDelayTimer <= 0)
			holdOpenDelayTimer = 0f;

		// Update out open delay
		holdOpenDelayTimer -= Time.deltaTime;

		// Modify speed so that we open quickly and then decelerate.
		float openSpeed = Util.Clamp(1 - openDegree, 0.1f, 1f);

		if (HoldOpen) {
			openDegree += openSpeed * Time.deltaTime * OpenSpeed;
			countDown = 0.25f;
		} else {
			if (countDown > 0) {
				openDegree += openSpeed * Time.deltaTime * OpenSpeed;
				countDown -= Time.deltaTime;
			} else {
				openDegree -= Time.deltaTime * CloseSpeed;
			}
		}

		openDegree = Util.Clamp(openDegree, 0f, 1.1f);

		Vector3 newPosition = gameObject.transform.localPosition;
		newPosition.y = (Mathf.Min(openDegree, MaxOpenDegree) * 0.6f) + baseHeight;
		gameObject.transform.localPosition = newPosition;

		if (holdOpenDelayTimer <= 0)
			_holdOpen = false;
		
	}

	// Update is called once per frame
	void Update()
	{
		// need to check open degree if we are physics...
		switch (OpenMethod) {
			case OpenStyle.None:
				break;
			case OpenStyle.Rise: 
				updateRiseDoor();
				break;
			case OpenStyle.Physics:
				updatePhysicsDoor();
				break;
		}

		// Update the culling.
		bool canTransit = !isClosed || !BlocksVision || wasRecentlyToldToOpen;
		AStarCuller culler = CoM.Culler;
		if (culler != null) {			
			if (gridReference != null) {
				culler.SetCanTransit(gridReference.GridX, gridReference.GridY, gridReference.Direction, canTransit);
			}
		}			

		if (Log) {
			_debugString = string.Format("HoldOpen:{0} HoldOpenTimer:{1} ", HoldOpen, holdOpenDelayTimer);
		}


	}
}