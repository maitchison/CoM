using System;
using System.Collections.Generic;

/** Measures FPS smoothed over multiple samples. */
public class SmoothFPSCounter
{
	private float[] samples;
	private int currentIndex;
	private bool fullSample;

	/** Creates new FPS counter with given number of samples. */
	public SmoothFPSCounter(int samples = 100)
	{
		setup(samples);
	}

	public void Reset()
	{
		currentIndex = 0;
		fullSample = false;
	}

	private void setup(int samplesCount)
	{
		samples = new float[samplesCount];
		currentIndex = 0;
		fullSample = false;
	}

	/** Add another delta time sample to counter.  */
	public void addSample(float deltaTime)
	{
		samples[currentIndex] = deltaTime;
		currentIndex++;
		if (currentIndex >= samples.Length) {
			currentIndex = 0;
			fullSample = true;
		}
	}

	public float AverageFPS {
		get {
			if (!fullSample)
				return 0;
			float totalDelta = 0;
			foreach (float delta in samples)
				totalDelta += delta;
			if (totalDelta == 0)
				return 0;
			return samples.Length / totalDelta;
		}
	}

}
