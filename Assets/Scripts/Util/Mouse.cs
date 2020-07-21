
using UnityEngine;

using System;

public enum CursorType
{
	Standard,
	Custom,
	Select
}


/**
 * Class to handle mouse and touch input 
 */
public static class Mouse
{
	/** Pixels per second that mouse must be moving at to trigger a swipe */
	private const int SWIPE_SPEED = 30;

	private const float SMOOTH_VELOCITY_TIME = 0.05f;

	/** Mouse position over the last few frames */
	private static Vector3[] mousePositionList = new Vector3[64];
	
	private static Vector3 _mousePosition;
	private static Vector3 _mouseSpeed;
	private static Vector3 _lastMousePosition;
	private static Vector3 _origionalMouseClickPosition;
	private static float _mouseClickTime;
	private static Vector3 _mouseQuickSpeed;

	public static CursorType CursorMode { get { return _cursorMode; } set { setCursorMode(value); } }

	private static CursorType _cursorMode;

	/** Location of the most recent mouse click */
	public static Vector2 PreviousClickLocation;
	/** Location of the previous mouse click */
	public static Vector2 CurrentClickLocation;

	// ----------- Gestures ------------

	/** User has taped the screen.  Differs from mouseClicks in that swipes are not registered. */
	public static bool Tap;

	/** User has swiped left. */
	public static bool SwipeLeft;
	/** User has swiped right. */
	public static bool SwipeRight;
	/** User has swiped up. */
	public static bool SwipeUp;
	/** User has swiped down. */
	public static bool SwipeDown;

	/** Mouse position adjusted for guiScale. */
	static public Vector2 Position
	{ get { return _mousePosition; } }

	/** Mouse speed in units per second (adjust for guiScale) and smoothed over a short time period */
	static public Vector3 Speed
	{ get { return _mouseSpeed; } }

	/** Mouse speed in units per second (adjust for guiScale) calculated from this frame and the last */
	static public Vector3 QuickSpeed
	{ get { return _mouseQuickSpeed; } }

	static public Vector3 DeltaPosition { get { return _mouseSpeed - _lastMousePosition; } }

	/** Returns the delta the mouse has traveled since the last click began */
	static public Vector3 ClickTravel
	{ get { return  _mousePosition - _origionalMouseClickPosition; } }

	/** Updates mouse position and speed */
	public static void Update()
	{
		UpdatePositionAndSpeed();

		// record time and position when mouse last clicked
		if (Input.GetMouseButtonDown(0)) {
			_mouseClickTime = Time.time;
			_origionalMouseClickPosition = _mousePosition;
		}

		UpdateGestures();

	}

	/** True if mouse is currently moving */
	public static bool Moving()
	{
		return (QuickSpeed.sqrMagnitude > 0);
	}

	private static void UpdatePositionAndSpeed()
	{
		_lastMousePosition = _mousePosition;

		_mousePosition = InputEx.mousePosition;
		_mousePosition.y = Screen.height - _mousePosition.y - 1;
		_mousePosition /= Engine.GuiScale;

		_mouseQuickSpeed = (_mousePosition - _lastMousePosition) / Time.deltaTime; 

		if (Input.GetMouseButton(0)) {
			PreviousClickLocation = CurrentClickLocation;
			CurrentClickLocation = _lastMousePosition;
		}

		bool recordLocation = true;

		// Disable locaiton recording when user is not touching the screen.
		if (CoM.TouchDevice && (!Input.GetMouseButton(0)))
			recordLocation = false;
	
		if (recordLocation) {
			mousePositionList[Time.frameCount % mousePositionList.Length] = _mousePosition;
			mousePositionList[Time.frameCount % mousePositionList.Length].z = Time.time;
		}

		// find a frame 0.1 seconds ago and measure speed from then
		_mouseSpeed = (_mousePosition - GetMousePositionAtTime(Time.time - SMOOTH_VELOCITY_TIME)) / SMOOTH_VELOCITY_TIME;		
	}

	/**
	 * Returns the mouse's location at the given time, or as close to that time as possiable 
	 */
	private static Vector3 GetMousePositionAtTime(float time)
	{
		Vector3 bestResult = new Vector3();
		float closestTime = Mathf.Infinity;
		for (int lp = 0; lp < mousePositionList.Length; lp++) {
			Vector3 pos = mousePositionList[lp];
			float delta = Math.Abs(pos.z - time);
			if (delta < closestTime) {
				closestTime = delta;
				bestResult = pos;
			}
		}
		bestResult.z = 0;
		return bestResult;
	}

	/**
	 * Works out if there are any gestures being performed 
	 */
	private static void UpdateGestures()
	{
		Tap = false;
		SwipeLeft = false;
		SwipeRight = false;
		SwipeUp = false;
		SwipeDown = false;

		// Look for completion of gesture.
		if (Input.GetMouseButtonUp(0)) {

			Vector3 delta = _mousePosition - _origionalMouseClickPosition;

			float deltaTime = Time.time - _mouseClickTime;

			if (deltaTime < 0.5f) {
				if (delta.magnitude < 25) {
					Tap = true;
				} else {
					if (Math.Abs(Mouse.Speed.x) > (Math.Abs(Mouse.Speed.y))) {
						if (Mouse.Speed.x < -SWIPE_SPEED)
							SwipeLeft = true;
						if (Mouse.Speed.x > +SWIPE_SPEED)
							SwipeRight = true;
					} else {
						if (Mouse.Speed.y < -SWIPE_SPEED)
							SwipeUp = true;
						if (Mouse.Speed.y > +SWIPE_SPEED)
							SwipeDown = true;
					}
						
				}
			}
		}

		if (Settings.Advanced.LookupBool("LogGestures")) {
			if (Tap)
				Trace.Log("Tap");
			if (SwipeLeft)
				Trace.Log("SwipeLeft");
			if (SwipeRight)
				Trace.Log("SwipeRight");
			if (SwipeUp)
				Trace.Log("SwipeUp");
			if (SwipeDown)
				Trace.Log("SwipeDown");
		}
	}

	/** Sets cursor to a standard mode. */
	private static void setCursorMode(CursorType mode)
	{
		_cursorMode = mode;
		switch (mode) {
			case CursorType.Standard:
				Engine.SetCursor("Cursor", new Vector2(5, 5));
				break;
			case CursorType.Select:
				Engine.SetCursor("CursorSelect", new Vector2(5, 5));
				break;
		}
	}

	/** Sets a custom cursor. */
	public static void SetCustomCursor(string name, Vector2 hotspot = default(Vector2))
	{
		_cursorMode = CursorType.Custom;
		Engine.SetCursor(name, hotspot);
	}
}
	