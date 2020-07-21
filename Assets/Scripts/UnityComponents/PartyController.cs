
using UnityEngine;

using System;

using Mordor;
using Data;
using UI;

/** Handles movement of party */
public class PartyController : MonoBehaviour
{
	
	public Camera PartyCamera;

	public MDRParty Party { get { return _party; } set { setParty(value); } }

	/** When enabled camera moves instantly to new position. */
	public bool Instant = false;

	/** How far the camera is set back from center of tile */
	public float CameraSetback = 0.15f;

	public float HeightOffset = 0f;

	private MDRParty _party;

	/** Gives control over camera. (WASD) */
	public bool FreeMove = false;

	/** Makes the party dizzy if they turn too much. */
	public bool EnableDizzy = true;

	/** If true detects doors infront of party and opens them */
	private bool enableDoorOpener = true;

	public bool EnableDoorOpener {
		get {
			return enableDoorOpener;
		}
		set {
			setEnableDoorOpener(value);
		}
	}

	private Vector3 currentPosition;
	private Vector3 currentOrientation;

	private Vector3 targetOrientation;
	private Vector3 targetPosition;

	/** Angle to look in from parties default orientation */
	public Quaternion LookOffset;
	private Quaternion dizzyOffset;

	//private Vector3 cameraVelocity = new Vector3(0, 0, 0);
	//private Vector3 cameraTurningVelocity = new Vector3(0, 0, 0);

	private float momentium;
	private float turningMomentium;

	private Vector3 cameraOrientation;

	public float DizzyFactor = 0f;

	float elapsedTime = 0f;

	/** If true camera movements and turns will be eased in aswells as eased out */
	public Boolean EaseIn = true;

	//Stub remove: no need for an instance
	public static PartyController Instance;


	public PartyController()
	{
		Instance = this;	
		targetOrientation = new Vector3(0, 0, 0);
		targetPosition = new Vector3(0, 0, 0);
		LookOffset = Quaternion.identity;
		dizzyOffset = Quaternion.identity;
	}

	private void setParty(MDRParty value)
	{
		_party = value;
		SyncCamera();
	}

	private void setEnableDoorOpener(bool value)
	{
		if (enableDoorOpener == value)
			return;
		enableDoorOpener = value;

		foreach (var item in GameObject.FindGameObjectsWithTag("DoorOpener")) {
			var itemCollider = item.GetComponent<Collider>();
			if (itemCollider != null)
				itemCollider.enabled = value;
		}
	}

	private void ProcessDirectorKeys()
	{		
		var moveSpeed = 1f;
		var moveDelta = moveSpeed * Time.deltaTime;
		CoM.Culler.Disable();


		bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);


		if (Input.GetKey(KeyCode.PageUp))
			PartyCamera.transform.localPosition = PartyCamera.transform.localPosition + new Vector3(0, -moveDelta, 0);
		if (Input.GetKey(KeyCode.PageDown))
			PartyCamera.transform.localPosition = PartyCamera.transform.localPosition + new Vector3(0, +moveDelta, 0);

		var rotation = PartyCamera.transform.localEulerAngles;

		if (ctrl) {
			if (Input.GetKey(KeyCode.LeftArrow))
				PartyCamera.transform.localEulerAngles = rotation + new Vector3(0, -moveDelta * 50, 0);
			if (Input.GetKey(KeyCode.RightArrow))
				PartyCamera.transform.localEulerAngles = rotation + new Vector3(0, +moveDelta * 50, 0);
			if (Input.GetKey(KeyCode.UpArrow))
				PartyCamera.transform.localEulerAngles = rotation + new Vector3(-moveDelta * 50, 0, 0);
			if (Input.GetKey(KeyCode.DownArrow))
				PartyCamera.transform.localEulerAngles = rotation + new Vector3(+moveDelta * 50, 0, 0);
			if (Input.GetKey(KeyCode.Space))
				PartyCamera.transform.localEulerAngles = new Vector3(Mathf.Round(rotation.x / 90) * 90, Mathf.Round(rotation.y / 90) * 90, Mathf.Round(rotation.z / 90) * 90);
		} else {
			if (Input.GetKey(KeyCode.LeftArrow))
				PartyCamera.transform.localPosition = PartyCamera.transform.localPosition + new Vector3(-moveDelta, 0, 0);
			if (Input.GetKey(KeyCode.RightArrow))
				PartyCamera.transform.localPosition = PartyCamera.transform.localPosition + new Vector3(+moveDelta, 0, 0);
			if (Input.GetKey(KeyCode.UpArrow))
				PartyCamera.transform.localPosition = PartyCamera.transform.localPosition + new Vector3(0, 0, +moveDelta);
			if (Input.GetKey(KeyCode.DownArrow))
				PartyCamera.transform.localPosition = PartyCamera.transform.localPosition + new Vector3(0, 0, -moveDelta);							
				
		}
	}

	private void ProcessGlobalKeys()
	{
		if (FreeMove)
			ProcessDirectorKeys();

		bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
		bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

		if (control && Input.GetKeyUp(KeyCode.D)) {
			this.FreeMove = !this.FreeMove;
			EnableDoorOpener = this.FreeMove;
		}
			
		if (Input.GetKeyDown(KeyCode.Alpha1) && (alt))
		if (Party[0] != null)
			Party.SelectedCharacterIndex = 0;
		if (Input.GetKeyDown(KeyCode.Alpha2) && (alt))
		if (Party[1] != null)
			Party.SelectedCharacterIndex = 1;
		if (Input.GetKeyDown(KeyCode.Alpha3) && (alt))
		if (Party[2] != null)
			Party.SelectedCharacterIndex = 2;
		if (Input.GetKeyDown(KeyCode.Alpha4) && (alt))
		if (Party[3] != null)
			Party.SelectedCharacterIndex = 3;
	}

	private void ProcessCheatKeys()
	{
		if (!Input.GetKey(KeyCode.LeftShift))
			return;
		
		if (Input.GetKeyUp(KeyCode.L)) {
			Party.Selected.CurrentMembership.XP += Party.Selected.CurrentMembership.ReqXP;
			Party.Selected.GainLevel();
		}

		if (Input.GetKeyUp(KeyCode.K)) {
			Party.Selected.ReceiveDamage(20);
		}

		if (Input.GetKeyUp(KeyCode.M)) {
			Party.ExploredMap.Clear();
			if (CoM.Instance.DungeonState != null)
				CoM.Instance.DungeonState.AutoMap.Repaint();
		}


		if (Input.GetKeyUp(KeyCode.O)) {
			PartyController.Instance.EnableDoorOpener = !PartyController.Instance.EnableDoorOpener;
		}

		//stub: remove or use for testing
		/*
		if (Input.GetKeyUp(KeyCode.H)) {
			var spell = CoM.Spells.ByName("Minor Heal");
			var result = spell.Cast(Party.Selected, Party.Selected);
			CoM.PostMessage(result.Formatted);
		} */

		// give cursed item
		if (Input.GetKeyUp(KeyCode.C)) {
			Party.Selected.GiveItem(MDRItemInstance.Create(CoM.Items.ByName("Ball and Chain")));
		}

		// uncurse all items
		if (Input.GetKeyUp(KeyCode.U)) {
			CoM.PostMessage("Uncursing all equiped items");
			foreach (MDRItemSlot item in Party.Selected.Equiped)
				if (!item.IsEmpty)
					item.ItemInstance.Cursed = false;
		}

		if (Input.GetKeyUp(KeyCode.M))
			CoM.PostMessage("fish and chips " + Time.frameCount);
		if (Input.GetKeyUp(KeyCode.N))
			Engine.PostNotification("Fish and chips is a long sentance that takes 3 lines:" + Time.frameCount, Party.Selected.Portrait);
	}

	/** Check for any user input that needs to be applied, and update our camera. */
	void Update()
	{
		if (Party == null)
			return;

		if (Settings.Advanced.PowerMode)
			ProcessCheatKeys();

		ProcessGlobalKeys();

		UpdateDizzy();

		UpdateCameraTarget();

		if (FreeMove)
			return;

		if (Instant)
			ProgressCamera(1f);
		else {
			elapsedTime += Time.deltaTime;

			float stepSize = 1f / 120f;

			// by using a fixed step we get consistent camera smoothness at different frame rates. 
			while (elapsedTime >= stepSize) {				
				ProgressCamera(stepSize);
				elapsedTime -= stepSize;
			}
		}
	}

	private void UpdateDizzy()
	{
		if (!EnableDizzy || DizzyFactor == 0f) {
			dizzyOffset = Quaternion.Euler(0, 0, 0);
			return;
		}

		float x = (float)Math.Sin(Time.time * 5f / 6) * DizzyFactor * 0.7f;
		float y = (float)Math.Sin(Time.time * 7f / 6) * DizzyFactor * 1.0f;
		float z = (float)Math.Sin(Time.time * 13f / 6) * DizzyFactor * 1.3f;
		dizzyOffset = Quaternion.Euler(x, y, z);
	}

	/** This is my old method of smoothing the camera location, it's not great but works ok */
	private void ProgressCamera(float deltaTime)
	{
		float step = 6f * deltaTime;
		// Note. this method looks a bit strange when turning is still smooth, but postion has got close enough to quickly set it'self
		// I think that's what's going on.  Anyway just look at the dizzy effect and you will see it.
		Vector3 delta = (currentPosition - Vector3.Lerp(currentPosition, targetPosition, step));
		if (delta.magnitude > momentium && EaseIn)
			momentium = Util.Clamp(momentium + delta.magnitude * step * 0.65f, 0f, delta.magnitude);
		else
			momentium = delta.magnitude;

		if (delta.magnitude > momentium)
			delta = delta.normalized * momentium;

		currentPosition -= delta;

		// this will just make sure we're [0..360] 
		if (currentOrientation.y < 0)
			currentOrientation.y += 360;
		if (currentOrientation.y >= 360)
			currentOrientation.y -= 360;

		// adjust for a case where say we're during from 0 degrees to 270, instead we turn from 0 to -90
		if ((targetOrientation.y - currentOrientation.y) < -180)
			targetOrientation.y += 360;
		if ((targetOrientation.y - currentOrientation.y) > 180)
			targetOrientation.y -= 360;

		float turningDelta = (currentOrientation - Vector3.Slerp(currentOrientation, targetOrientation, step)).magnitude;

		if (turningDelta > turningMomentium && EaseIn)
			turningMomentium = Util.Clamp(turningMomentium + turningDelta * step * 0.75f, 0f, turningDelta);
		else
			turningMomentium = turningDelta;

		currentOrientation += (targetOrientation - currentOrientation).Clamp(turningMomentium);

		PartyCamera.transform.localEulerAngles = currentOrientation;
		PartyCamera.transform.localPosition = currentPosition + Quaternion.Euler(currentOrientation) * new Vector3(0.0f, 0.0f, -CameraSetback);
	}

	/* Forces camera to move to target location and rotation */
	public void SyncCamera()
	{
		UpdateCameraTarget();
		currentPosition = targetPosition;
		currentOrientation = targetOrientation;
	}

	/** Updates the camera based on party location */
	private void UpdateCameraTarget()
	{
		if (Party == null)
			return;

		if (Settings.Advanced.BirdsEyeView) {
			targetOrientation = (Quaternion.Euler(new Vector3(90, 0, 0)) * (LookOffset * dizzyOffset)).eulerAngles;
			targetPosition = new Vector3(Party.LocationX, 5, Party.LocationY);
			EnvironmentManager.NoFog();
		} else {
			float angle = Party.Facing;
			float lookAngle = +10 + (Party.CameraHeight * 15f);
			targetOrientation = new Vector3(lookAngle, angle, 0) + Util.ClipAngles((LookOffset * dizzyOffset).eulerAngles);
			targetPosition = new Vector3(Party.LocationX, Party.CameraHeight + HeightOffset, Party.LocationY);		
		}
	}
}
