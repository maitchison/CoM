using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GuiScaleManager : MonoBehaviour
{
	/** This is the ideal width, anything higher or lower than this will be scaled. */
	public int IdealWidth = 1024;
	/** This is the ideal height, anything higher or lower than this will be scaled. */
	public int IdealHeight = 768;

	/** The scale factors that are allowed to be used.*/
	public float[] AllowedScales = { 0.75f, 1.0f, 1.25f, 1.5f, 2f };

	private static GuiScaleManager instance;

	public static float GuiScale {
		get { return _guiScale; }
		set {
			_guiScale = value;
			Settings.Advanced.GuiScale = value;
			Settings.Advanced.UseHighDPIGui = value > 1f; 
		}
	}

	private static float _guiScale = 1f;

	// Use this for initialization
	void Start()
	{		
		instance = this;
	}

	public static void AutoSetGuiScale()
	{
		if (instance == null) {
			Trace.LogWarning("Tried to auto set scale, but guiscale manager isn't setup yet.");
			return;
		}

		// using screen.currentResolution gives screen dimentions, not window dimentions. 
		var currentWindowResolution = createResolution(Screen.width, Screen.height);
		var scaleToUse = instance.getGuiScaleForResolution(currentWindowResolution);

		Trace.Log("Defaulting GUI Scale to {0}, resolution {1} DPI {2}", scaleToUse.ToString("0.00"), currentWindowResolution.ToString(), Screen.dpi);

		GuiScale = scaleToUse;
	}

	private static Resolution createResolution(int width, int height)
	{
		var result = new Resolution();
		result.width = width;
		result.height = height;
		result.refreshRate = Screen.currentResolution.refreshRate;
		return result;
	}

	/** Shows the scale that would be used at each resolution. */
	public void PrintScales()
	{		
		Resolution[] sampleResolutions = {
			createResolution(640, 480),
			createResolution(800, 600),
			createResolution(1024, 768),
			createResolution(1280, 800),
			createResolution(1280, 1024),
			createResolution(1680, 1050),
			createResolution(1920, 1080),
			createResolution(2560, 1440),
			createResolution(3840, 2160),			
		};

		foreach (Resolution resolution in sampleResolutions) {
			Trace.Log("GUI Scale for {0}: {1}", resolution, getGuiScaleForResolution(resolution));
		}
	}

	/** Gets the default scale based on current resolution. */
	private float getGuiScaleForResolution(Resolution resolution)
	{
		if (resolution.width * resolution.height == 0) {
			Trace.LogWarning("Could not calculate GuiScale as resolution is too small: {0}", resolution);
			return 1f;
		}

		float widthRatio = (float)resolution.width / IdealWidth;
		float heightRatio = (float)resolution.height / IdealHeight;
		float ratio = Mathf.Min(widthRatio, heightRatio);
		float selectedRatio = AllowedScales[0];

		/** Find the best scale to use. */
		for (int lp = 0; lp < AllowedScales.Length; lp++) {
			if (AllowedScales[lp] <= ratio)
				selectedRatio = AllowedScales[lp];
		}

		return selectedRatio;
	}
}
