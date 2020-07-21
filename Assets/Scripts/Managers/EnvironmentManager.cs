
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityStandardAssets.ImageEffects;

/**
 * Handles transitions between environments.
 */
public class EnvironmentManager : MonoBehaviour
{
	private static Color goalFogColor = Color.black;
	private static float goalFogDistance = 1f;

	private static Color fogColor = Color.black;
	private static float fogDistance = 1f;

	private GlobalFog globalFog { get { return Engine.Instance.Camera.GetComponent<GlobalFog>(); } }

	private static FogMode fogMode = FogMode.Linear;

	private bool currentlyUsingDeferredFog;

	private bool needsDeferredFog;

	private static bool _dirty = true;

	void Update()
	{
		updateFog();
	}

	void Start()
	{
		StandardFog();
		Sync();
	}

	private void updateRenderSettings()
	{
		RenderSettings.fogColor = fogColor;

		RenderSettings.fog = fogDistance <= Engine.Instance.Camera.farClipPlane;

		switch (fogMode) {
			case FogMode.Linear:
				RenderSettings.fogMode = FogMode.Linear;
				RenderSettings.fogStartDistance = fogDistance / 2f;
				RenderSettings.fogEndDistance = fogDistance;
				break;
			case FogMode.Exponential:
			case FogMode.ExponentialSquared:
				RenderSettings.fogMode = fogMode;
				RenderSettings.fogDensity = 1f - (fogDistance / 10f);
				RenderSettings.fogStartDistance = 0.1f;
				RenderSettings.fogEndDistance = fogDistance;
				break;
		}
	}


	/** Use normal fog settings to apply fog. */
	private void applyStandard()
	{
		Engine.Instance.Camera.backgroundColor = fogColor;
	
		updateRenderSettings();

		if (globalFog != null)
			globalFog.enabled = false;

	}

	/** Use fog settings which will work with deffered rendering. */
	private void applyDeferred()
	{
		Engine.Instance.Camera.backgroundColor = fogColor;

		updateRenderSettings();

		if (globalFog == null)
			return;
	}

	private void updateFog()
	{
		needsDeferredFog = (CoM.Instance.Camera.renderingPath == RenderingPath.DeferredLighting || CoM.Instance.Camera.renderingPath == RenderingPath.DeferredShading);
			
		if ((fogColor != goalFogColor) || (fogDistance != goalFogDistance) || (currentlyUsingDeferredFog != needsDeferredFog))
			_dirty = true;

		if (!_dirty)
			return;

		float transitionSpeed = 20f;

		Vector4 deltaColor = (Color)(goalFogColor - fogColor);
		if (deltaColor.magnitude > 0.50f * transitionSpeed * Time.deltaTime)
			deltaColor = deltaColor.Clamp(0.50f * transitionSpeed * Time.deltaTime);
		fogColor += (Color)deltaColor;

		float delta = goalFogDistance - fogDistance;
		delta = Util.Clamp(delta, -transitionSpeed * Time.deltaTime, transitionSpeed * Time.deltaTime);
		fogDistance += delta;

		if (needsDeferredFog)
			applyDeferred();
		else
			applyStandard();

		currentlyUsingDeferredFog = needsDeferredFog;
		_dirty = false;
	}

	/** Causes fog to instantly go to desired level. */
	public static void Sync()
	{
		fogColor = goalFogColor;
		fogDistance = goalFogDistance;
		_dirty = true;
	}

	/** Transitions into a thick fog setting */
	public static void ThickFog()
	{
		goalFogColor = new Color(0.45f, 0.45f, 0.55f);
		goalFogDistance = 0.1f;
	}

	/** Sets both the fog level and view distance so that the camera can not see an object "distance" units away. */
	public static void SetFogAndDistance(float distance)
	{
		var thickness = 1f;
		thickness = Util.Clamp(thickness, 0f, 1f);
		SetViewDistance(distance);
		goalFogDistance = distance;
		Sync();

	}

	/** Sets cameras range to 8 tiles. */
	public static void NearView()
	{
		SetViewDistance(8);
	}

	/** Sets cameras range to 12 tiles. */
	public static void FarView()
	{
		SetViewDistance(12);
	}

	public static void SetViewDistance(float distance)
	{
		Engine.Instance.Camera.farClipPlane = distance;
	}

	/** Transitions into standard fog settings */
	public static void StandardFog()
	{
		goalFogColor = Color.black;
		goalFogDistance = 8f;
	}

	public static void NoFog()
	{
		goalFogColor = Color.black;
		goalFogDistance = 100f;
	}

}