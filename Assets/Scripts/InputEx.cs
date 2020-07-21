using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class InputEx
{
	static private bool initialized = false;
	static private int frame = -1;
	static private float monitorAspect = 1.77f;
	static private Vector3 _mousePosition = new Vector3(0f, 0f, 0f);

	static private bool ENABLED = false;

	static public Vector3 mousePosition {
		get {
			UpdateMousePosition();
			return _mousePosition;				
		}
	}

	static private void Initialize()
	{

		if (initialized) {
			return;
		}

		Resolution resolution = Screen.currentResolution;
		Resolution[] resolutions = Screen.resolutions;
		int maxWidth = resolution.width;
		int maxHeight = resolution.height;

		for (int i = 0, imax = resolutions.Length; i < imax; i++) {

			resolution = resolutions[i];

			if (maxWidth < resolution.width) {
				maxWidth = resolution.width;
			}
				
			if (maxHeight < resolution.height) {
				maxHeight = resolution.height;
			}
		}
			
		monitorAspect = (float)maxWidth / (float)maxHeight;

		Debug.Log(string.Format("InputEx : maxWidth:{0}, maxHeight:{1}, monitorAspect:{2}", maxWidth, maxHeight, monitorAspect));

		initialized = true;

	}

	static private void UpdateMousePosition()
	{

		if (frame == Time.frameCount) {
			return;
		}
			
		frame = Time.frameCount;

		Initialize();

		_mousePosition = Input.mousePosition;

		if (!Screen.fullScreen || monitorAspect == -1f) {
			return;
		}

		float sw = Screen.width;

		float sh = Screen.height;

		float currentAspect = sw / sh;

		if (currentAspect == -1f) {
			return;
		}
			
		// HACK Workaround for Unity bug
		if (ENABLED && monitorAspect > currentAspect) {
			float wrongWidth = sh * monitorAspect;
			_mousePosition.x = Mathf.Round((_mousePosition.x / wrongWidth) * sw);
		}
	}
}
	