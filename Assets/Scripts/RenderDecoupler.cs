using UnityEngine;
using System.Collections;

// todo:
// pass through
// frame hold

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
/** Allows adjustment of 3d rendering resolution and framerate with effecting the UI. */
public class RenderDecoupler : MonoBehaviour
{
	public float Ratio = 1f;

	/** Holds the last frame, no updates. */
	public bool FrameHold = false;

	public float FPS = -1;

	private float oldRatio = 0f;
	private RenderTexture buffer;
	private float lastRenderFrameTime;

	// Use this for initialization
	void Start()
	{
	
	}

	/** Sets decoupler to passthrough mode. */
	private void PassThru(bool value)
	{
		if (value) {
			Engine.Instance.Camera.targetTexture = null;
		} else {
			Engine.Instance.Camera.targetTexture = buffer;
		}

	}
	
	// Update is called once per frame
	void Update()
	{
		if (Ratio == 1 && (FPS == -1)) {
			PassThru(true);
			return;
		}

		if (Ratio != oldRatio) {			
			oldRatio = Ratio;
			if (buffer != null)
				buffer.Release();
			if (Ratio <= 0)
				return;

			int width = (int)(Ratio * Screen.width);
			int height = (int)(Ratio * Screen.height);

			if (width * height == 0)
				return;
			
			buffer = new RenderTexture(width, height, 0, RenderTextureFormat.Default);
			buffer.useMipMap = false;
			buffer.filterMode = FilterMode.Bilinear;
			buffer.Create();
			Trace.Log("Sampling at {0}x{1}", buffer.width, buffer.height);
			Engine.Instance.Camera.targetTexture = buffer;
		}
			
		// frame hold
		if (Application.isPlaying) {

			bool wantsNewFrame = true;

			if (FPS >= 1) {
				if (Time.time - lastRenderFrameTime < 1f / FPS) {
					wantsNewFrame = false;
				}
			}

			if (FrameHold)
				wantsNewFrame = false;

			if (wantsNewFrame) {
				Engine.EnableCamera = true;
				lastRenderFrameTime = Time.time;
			} else
				Engine.EnableCamera = false;
		} else {
			Engine.EnableCamera = true;
		}

		PassThru(false);
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (buffer == null)
			return;		
		Graphics.Blit(buffer, destination);
	}
}