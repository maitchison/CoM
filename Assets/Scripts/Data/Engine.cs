using UnityEngine;

using System;
using System.Collections.Generic;

using UI;
using UI.Generic;
using UI.DragDrop;
using Data;
using Smaa;
using AlpacaSound;

/** Records statistics on how long it took to create the last frame */
public struct PerformanceStats
{
	public int StateDraws;
	public int StateUpdates;
	public float UpdateTime;
	public float DrawTime;
	public float FrameTime;
	public int GuiUpdates;
	public int GuiDraws;
	public int GuiLegacyDraws;
	public int GuiRenderTextureResizes;
	public int GuiRenderTextureRepaints;
	/** Number of adjustments (size or color) made to strings.  Measured in characters. */
	public int TextAdjustmentChacters;
	public List<String> GuiCallRegister;

	public void Clear()
	{
		UpdateTime = 0;
		DrawTime = 0;
		FrameTime = 0;
		StateDraws = 0;
		StateUpdates = 0;
		GuiUpdates = 0;
		GuiDraws = 0;
		GuiLegacyDraws = 0;
		GuiRenderTextureResizes = 0;
		GuiRenderTextureRepaints = 0;
		TextAdjustmentChacters = 0;
		GuiCallRegister = new List<String>();
	}
}

public struct Notification
{
	public string Message;
	public Sprite Sprite;
	public bool PlaySound;

	public Notification(string message, Sprite sprite, bool playSound = true)
	{
		Message = message;
		Sprite = sprite;
		PlaySound = playSound;
	}

	public string SoundName {
		get { 
			return PlaySound ? "Notification" : "";
		}
	}
}

public enum RenderFXStyle
{
	// standard drawing
	Normal,
	// retro effect,
	Retro,
	// a black and white sketch,
	Sketch,
	// everything looks like clay,
	_Clay
}

/** Singleton class, stores and handles the games states.  */
public class Engine : MonoBehaviour
{
	/** Minimum number of seconds between consecutive notifications */ 
	public static float NotificationDelay = 0.5f;

	/** Number of seconds user must be inactive for idle to be detected */
	public static float IDLE_TIMEOUT = 60f;

	protected static float notificationDelayTimer = 0;

	/** If set to true gui will not be visible (but can still update) */
	public static bool HideGUI = false;

	/** Number of seconds in game time.  Unlike Time.time this doesn't progress when game is paused. */
	public static float GameTime = 0;

	public GUISkin Skin;
	public Font TitleFont;
	public Font TextFont;
	public Font MonoSpaceFont;

	public Material GuiMaterial;
	public Material GuiMaterial_Solid;
	public Material GuiMaterial_ColorTransform;
	public Material GuiMaterial_Blurred;
	public Material GuiMaterial_Text;

	public AnimationCurve SpringCurve;

	// -----------------------------------

	private bool ComponentsInitialized = false;

	protected GuiLabel FPSLabel;
	private float lastFpsUpdate;

	protected GuiMessageBox LogWindow;

	/** The default gui matrix */
	public static Matrix4x4 BaseGUIMatrix;
	/** The pure, unscalled gui matrix */
	public static Matrix4x4 PureGUIMatrix;

	// -----------------------------------

	/** The container used during the drag drop process */
	public static DDDragDrop DragDrop;
	
	/** The singleton instance of this class */
	public static Engine Instance = null;

	private static Stack<Matrix4x4> GuiMatrixStack = new Stack<Matrix4x4>();

	public static List<GuiState> StateList = new List<GuiState>();

	public Camera Camera;

	/** The games currently active state */
	public static GuiState CurrentState { get { return StateList.Count == 0 ? null : StateList[0]; } }

	/** If true deferred shading will be disabled and forward will be used in high quality mode instead.  Normally this isn't needed. */
	public bool DisableDeferredShading = false;

	/** used to process some actions (such as update) only once per frame during an onGui call. */
	private static int lastOnGuiCall = -1;

	/** Used to scale the user interface. 0.5 halves the size of all elements, 2.0 doubles it. */
	public static float GuiScale { get { return GuiScaleManager.GuiScale; } }

	/** The scale dui to high dpi, usually 1.0 or 2.0.  Right now just set to guiscale */
	public static float DPIScale { get { return GuiScale; } }

	/** Records the component the mouse was over when user clicked the mouse button down. */
	public static GuiComponent ComponentClickedDownOn;

	public static PerformanceStats PerformanceStatsLastFrame;
	public static PerformanceStats PerformanceStatsInProgress;

	/** List of notifications currently in view */
	protected static List<GuiNotification> notificationList = new List<GuiNotification>();

	/** List of notifications waiting to be created (they will be on the next frame */
	protected static List<Notification> pendingNotifications = new List<Notification>();

	/** Last time user pressed a key or moved the mouse */
	private static float lastInputTime;

	private Resolution previousResolution = new Resolution();
	private float previousGuiScale = 1f;

	public static RenderFXStyle FXStyle = RenderFXStyle.Normal;

	protected static SmoothFPSCounter FPSCounter = new SmoothFPSCounter(100);

	/** True if the draw event that caused "onGui" is a repaint. */
	public static bool UnityOnGuiIsRepaint = false;

	/** Used for high frequency timing. */
	private static System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

	public static bool EnableCamera {
		get { return Instance.Camera.GetComponent<Camera>().enabled; }
		set { Instance.Camera.GetComponent<Camera>().enabled = value; }
	}

	/** Sets the cursor to cursor of given name. */
	public static void SetCursor(string name, Vector2 hotspot = default(Vector2))
	{
		//stupid webgl is bugged and I can't used custom cursors without cursor corruption.  Forcing software is terraible so I don't do that either.
		if (isWebGL)
			return;

		string fullPath = "Mouse Cursors/" + name;

		Sprite standardCursor = ResourceManager.GetSprite(fullPath);
		Sprite largeCursor = ResourceManager.GetSprite(fullPath + "@x2") ?? standardCursor;

		if (standardCursor == null) {
			Trace.LogWarning("Invalid cursor name {0}, not found.", fullPath);
			return;
		}

		bool useLargeCursor = Screen.width >= 1920;

		Texture2D cursorTexture = (useLargeCursor ? largeCursor.texture : standardCursor.texture);

		Util.Assert(TextureMagic.IsReadable(cursorTexture), "Cursor texture must be readwrite enabled.");

		Cursor.SetCursor(cursorTexture, hotspot * (useLargeCursor ? 2 : 1), CursorMode.Auto);
	}

	void Start()
	{		
		SetCursor("Cursor", new Vector2(5, 5));

		Instance = this;
		Initialize();

		GuiScaleManager.AutoSetGuiScale();

		previousResolution.width = Screen.width;
		previousResolution.height = Screen.height;
		previousGuiScale = GuiScaleManager.GuiScale;	
	}

	void Update()
	{
		if ((Mouse.Speed.magnitude > 2f) || (Input.anyKey))
			lastInputTime = Time.time;
	}

	/** Turns off lightmaping. */
	public static void DisableLightMapping()
	{
		// Disable lightmaps in scene by removing the lightmap data references
		LightmapSettings.lightmaps = new LightmapData[]{ };

	}


	/** Returns if user input is idle or not */
	public static bool Idle {
		get { return (Time.time - lastInputTime) > IDLE_TIMEOUT; }
	}

	private void UpdateStateResolutions()
	{
		foreach (GuiState state in StateList) {
			state.Width = (int)(Screen.width / GuiScale);
			state.Height = (int)(Screen.height / GuiScale);
			state.onResolutionChange();
		}
	}


	void OnGUI()
	{		
		UnityOnGuiIsRepaint = (Event.current.rawType == EventType.Repaint);

		if (!ComponentsInitialized)
			InitializeComponents();

		if (previousGuiScale != GuiScale) {
			previousGuiScale = GuiScale;
			UpdateStateResolutions();
		}

		if (Screen.height != previousResolution.height || Screen.width != previousResolution.width) {
			Trace.Log("Screen resolution changed to {0}x{1}", Screen.width, Screen.height);
			previousResolution.width = Screen.width;
			previousResolution.height = Screen.height;
			GuiScaleManager.AutoSetGuiScale();
			UpdateStateResolutions();
		}

		if (GUI.skin != Skin)
			GUI.skin = Skin;

		if (Time.frameCount != lastOnGuiCall) {
			// this gets called once per frame.
			PerformanceStatsLastFrame = PerformanceStatsInProgress;
			PerformanceStatsInProgress.Clear();
			PerformanceStatsInProgress.FrameTime = Time.deltaTime;

			PerformanceStatsInProgress.UpdateTime = MeasureExecutionTime(DoUpdate);
	
			lastOnGuiCall = Time.frameCount;
		}

		PerformanceStatsInProgress.GuiCallRegister.Add(Event.current.ToString());

		PerformanceStatsInProgress.DrawTime += MeasureExecutionTime(DoDraw);
	}

	/** Makes sure the compression is working. */
	private void TestCompression()
	{
		var testString = "The quick brown fox jumps over the lazy dog";
		var compressedString = Compressor.Compress(testString);
		var decompressedString = Compressor.Decompress(compressedString);
			
		if (decompressedString != testString)
			throw new Exception("Compressed string does not match decompressed! Orignal:" + testString + " Decompressed:" + decompressedString);

		Trace.Log("Compression overhead = " + Compressor.Compress("A").Length + "bytes.");
	}

	private void SetupLogWindow()
	{
		LogWindow = new GuiMessageBox(500, 300);
		LogWindow.WindowStyle = GuiWindowStyle.ThinTransparent;
		LogWindow.Color = new Color(0f, 0f, 0f);
		Application.logMessageReceived += HandleLog;
	}

	// used to make sure we don't try logging errors about logging
	private static bool ignoreLogs = false;

	private void HandleLog(string message, string stackTrace, LogType logType)
	{		
		if (ignoreLogs)
			return;
		if (LogWindow == null)
			return;

		ignoreLogs = true;
		try {
			if (String.IsNullOrEmpty(message))
				return;
			message = message.Split('\n')[0];

			var messageColor = Color.white;

			switch (logType) {
				case LogType.Assert:
				case LogType.Error:
					messageColor = Color.red;
					break;
				case LogType.Exception:
				case LogType.Warning:
					messageColor = Color.yellow;
					break;
			}
			
			var logString = Util.Colorise(message, messageColor);
			LogWindow.AddMessage(logString);
			LogWindow.ScrollToBottom();
		} finally {
			ignoreLogs = false;
		}
	}


	private void InitializeComponents()
	{
		if (ComponentsInitialized)
			return;

		FPSLabel = new GuiSolidLabel(0, 0, "FPS:");
		FPSLabel.Color = new Color(0f, 0f, 0f, 1f);
		FPSLabel.FontColor = Color.white;
		FPSLabel.FontSize = 14;
		
		SetupLogWindow();

		ComponentsInitialized = true;
	}

	/** Runs given function, returns the time taken to execute it in seconds. */
	public static float MeasureExecutionTime(Action function)
	{	
		watch.Reset();
		watch.Start();

		function();

		watch.Stop();

		var elapsed = watch.ElapsedTicks / 10000000f;

		return (float)elapsed;
	}

	/** Returns basic information about the system. */
	public static string GetSystemInfo()
	{
		return string.Format("{0}\n{1}", Settings.Information.DeviceInfo, Settings.Information.GraphicsInfo);
	}

	/** 
	 * Gets a string detailing various aspects of the current video settings */
	public static string GetGraphicsInfo()
	{
		string renderPath = "?";
		if ((Instance != null) && (Instance.GetComponent<Camera>() != null))
			renderPath = Instance.GetComponent<Camera>().actualRenderingPath.ToString();

		return "Video settings: " + Screen.width + "x" + Screen.height + " AA:" + QualitySettings.antiAliasing +
		"\nFiltering:" + QualitySettings.anisotropicFiltering + " Vsync:" + QualitySettings.vSyncCount +
		"\nTarget FPS:" + Application.targetFrameRate + " Render Path:" + renderPath + " Device:" + SystemInfo.graphicsDeviceVersion +
		string.Format("\nShader level: {0} Ram: {1}", SystemInfo.graphicsShaderLevel, SystemInfo.graphicsMemorySize);
	}

	/** The URL this game is running from, minus the parameters.  "Local" if this is a stand alone game. */
	public string AbsoluteURL {
		get {
			var url = Application.absoluteURL;

			if (String.IsNullOrEmpty(url))
				return "Local";

			int parametersStart = url.IndexOf('?');

			if (parametersStart > 8)
				return url.Substring(0, parametersStart - 1);
			else
				return url;
		}
		
	}

	virtual public void Initialize()
	{		
		WebServer.PostEvent("StartGame", AbsoluteURL);
	
		// Clear cache if we arn't using it.  This fixes some bugs.
		Trace.Log("Processing Cache");

		try {
			if (isWebGL) {
				if (!Caching.enabled) {
					Trace.Log("Caching is Enabled.");
				} else {
					Trace.Log("Caching is Disabled, cleaning...");
					var success = Caching.ClearCache();
					Trace.Log(success ? "Successful" : "Failed, in use");
				}
			}
		} catch (Exception error) {
			Trace.Log("Error cleaning cache: " + error.Message);
		}

		Trace.Log("Initializing Engine: [{0}] ", SystemInfo.graphicsDeviceVersion);

		if (Debug.isDebugBuild)
			Trace.Log("Debug build.");

		Trace.Log(GetGraphicsInfo());

		Trace.Log("Applying graphics settings.");

		Settings.General.OnValueChanged += delegate {
			UpdateGraphicsSettings();
		};
		Settings.Advanced.OnValueChanged += delegate {
			UpdateGraphicsSettings();
		};

		ApplyQualityLevel(Settings.General.UnityQualityLevel);
		UpdateGraphicsSettings();

		// disable the gui layout for slight performance improvement 
		this.useGUILayout = false;
		DragDrop = new DDDragDrop(); 

		Debug.developerConsoleVisible = false;
	}

	/** Makes sure the right textures are loaded. */
	private static void ApplyTextureSwapper()
	{
		if (TextureSwapper.Instance == null)
			return;
		
		if (Instance.Camera.renderingPath == RenderingPath.VertexLit) {
			TextureSwapper.Instance.CompositeOcculusion();
		} else {
			if (FXStyle == RenderFXStyle._Clay)
				TextureSwapper.Instance.Clay();
			else
				TextureSwapper.Instance.RestoreMaterials();
		}		
	}

	public static bool isEditor {
		get { return Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor; } 
	}

	public static bool isWebGL {
		get { return Application.platform == RuntimePlatform.WebGLPlayer; }
	}

	public static bool isWebPlayer {
		get { return Application.platform == RuntimePlatform.WindowsWebPlayer || Application.platform == RuntimePlatform.OSXWebPlayer; }
	}

	/** True if we are playing on either the Unity Web Player or the WebGL player. */
	public static bool isWeb {
		get { return isWebGL || isWebPlayer; }
	}

	public void ApplyQualityLevel(int newLevel)
	{
		QualitySettings.SetQualityLevel(newLevel, true);
		switch (newLevel) {
			case 0:
			// vertex lit, occlusion is baked in.
				Camera.renderingPath = RenderingPath.VertexLit;
				break;
			case 1:
			// allows me to make use of vertex lighting for non main light.
				Camera.renderingPath = RenderingPath.Forward;
				break;
			case 2:
			// best for lots of lights
				Camera.renderingPath = (DisableDeferredShading) ? RenderingPath.Forward : RenderingPath.DeferredShading;
				break;
		}
		ApplyAntiAliasing(Settings.General.RawAALevel);
		ApplySSAO(Settings.General.EnableSSAO);
		ApplyTextureSwapper();
	}

	private bool isAAEnabled {
		get {
			var aaScript = Camera.GetComponent<SMAA>();
			if (Camera.actualRenderingPath == RenderingPath.DeferredShading) {
				return (aaScript != null) && aaScript.enabled;
			} else {
				return QualitySettings.antiAliasing > 0;
			}
		}
	}

	/** Enables or disables Anti aliasing. */
	public void ApplyAntiAliasing(int aaLevel)
	{
		// We need a special case for anti aliasing in deferred mode.
		var aaScript = Camera.GetComponent<SMAA>();
		if (aaScript == null)
			return;
		if (Camera.actualRenderingPath == RenderingPath.DeferredShading) {			
			QualitySettings.antiAliasing = 0; 
			aaScript.enabled = (aaLevel != 0);
		} else {
			QualitySettings.antiAliasing = aaLevel; 
			aaScript.enabled = false;
		}
	}

	/**
	 * Applies current settings to QualitySettings */
	virtual protected void UpdateGraphicsSettings()
	{
		SmartUI.ShowGuiBounds = Settings.Advanced.ShowGuiBounds;
		SmartUI.ShowGuiMaterial = Settings.Advanced.ShowGuiMaterial;

		if (SoundManager.MusicVolume != Settings.General.MusicVolumeDB) {
			SoundManager.MusicVolume = Settings.General.MusicVolumeDB;
			Trace.Log("Music Volume to: " + Settings.General.MusicVolumeDB);
		}

		if (SoundManager.Volume != Settings.General.SFXVolumeDB) {
			SoundManager.Volume = Settings.General.SFXVolumeDB;
			Trace.Log("SFX Volume to: " + Settings.General.SFXVolumeDB);
		}

		if (GuiContainer.ENABLE_CACHING != Settings.Advanced.GuiCaching) {
			Trace.Log("Gui Caching " + Settings.Advanced.GuiCaching);
			GuiContainer.ENABLE_CACHING = (Settings.Advanced.GuiCaching);
			GuiContainer.InvalidateAll();
		}

		if (ResourceManager.UsingHighDPIGui != Settings.Advanced.UseHighDPIGui) {
			Trace.Log("HighDPI GUI changed to: " + Settings.Advanced.UseHighDPIGui);
			ResourceManager.UsingHighDPIGui = Settings.Advanced.UseHighDPIGui;
		}

		if (QualitySettings.GetQualityLevel() != Settings.General.UnityQualityLevel) {
			Trace.Log("Quality set to: " + QualitySettings.names[Settings.General.UnityQualityLevel]);
			ApplyQualityLevel(Settings.General.UnityQualityLevel);
		}
		
		if (isAAEnabled != Settings.General.EnableAntiAliasing) {
			Trace.Log("AA changed to: " + Settings.General.RawAALevel + " (from " + QualitySettings.antiAliasing + ")");
			ApplyAntiAliasing(Settings.General.RawAALevel);
		}
			
		if (isSSAOEnabled != Settings.General.EnableSSAO) {
			Trace.Log("SSAO set to: " + Settings.General.EnableSSAO);
			ApplySSAO(Settings.General.EnableSSAO);
		}
		
		if (QualitySettings.anisotropicFiltering != Settings.General.AnisotropicFiltering) {
			Trace.Log("Anistropic filtering change to: " + Settings.General.AnisotropicFiltering);
			QualitySettings.anisotropicFiltering = Settings.General.AnisotropicFiltering;
		}
		
		if (QualitySettings.vSyncCount != Settings.General.vSyncCount) {
			Trace.Log("vSync to: " + Settings.General.vSyncCount);
			QualitySettings.vSyncCount = Settings.General.vSyncCount;
		}
		
		if (Application.targetFrameRate != Settings.Advanced.TargetFrameRate) {
			Trace.Log("Framerate changed to: " + Settings.Advanced.TargetFrameRate);
			Application.targetFrameRate = Settings.Advanced.TargetFrameRate;
			Settings.General.EnablevSnyc = false; // vsync must be disabled for target frame rate to work.
		}

		if (FXStyle != Settings.General.FXStyle) {
			Trace.Log("Applying FX style [{0}] ", Settings.General.FXStyle);
			ApplyFXStyle(Settings.General.FXStyle);
		}
	}

	/** Processes global keys. */
	protected virtual void processKeys(bool ctrl, bool alt, bool shift)
	{
		if (ctrl && alt && Input.GetKeyUp(KeyCode.F)) {
			Settings.General.ShowFPS = !Settings.General.ShowFPS;
		}

		// Gui scaling for debuging. 
		if (Input.GetKey(KeyCode.P)) {
			if (Input.GetKeyUp(KeyCode.Alpha1))
				GuiScaleManager.GuiScale = 0.75f;
			if (Input.GetKeyUp(KeyCode.Alpha2))
				GuiScaleManager.GuiScale = 1f;
			if (Input.GetKeyUp(KeyCode.Alpha3))
				GuiScaleManager.GuiScale = 1.25f;
			if (Input.GetKeyUp(KeyCode.Alpha4))
				GuiScaleManager.GuiScale = 1.5f;
			if (Input.GetKeyUp(KeyCode.Alpha5))
				GuiScaleManager.GuiScale = 2f;
		}

	}

	/** Returns a string indicating the current frame rate. */
	private string getFPSDetails()
	{
		//var ram = (System.GC.GetTotalMemory(false) / 1024f / 1024f).ToString("0.0") + "mb";
		var ram = (Profiler.GetTotalAllocatedMemory() / 1024f / 1024f).ToString("0.0") + "mb";
		return string.Format("FPS: {0}\nRAM: {1}\n{2}", FPSCounter.AverageFPS.ToString("0.0"), ram, SoundManager.CurrentlyPlayingSong);
	}

	/** Provides more information than getFPSDetails, including information about the UI performance. */
	private string getAdvancedFPSDetails()
	{
		var result = "FPS:" + FPSCounter.AverageFPS.ToString("0.0");
		result += 
			" (update " + (PerformanceStatsLastFrame.UpdateTime * 1000).ToString("0.0") + "ms" +
		" draw " + (PerformanceStatsLastFrame.DrawTime * 1000).ToString("0.0") + "ms" +
		" frame " + (PerformanceStatsLastFrame.FrameTime * 1000).ToString("0.0") + "ms)" + " memory (" + (System.GC.GetTotalMemory(false) / 1024f / 1024f).ToString("0.0") + "mb)";

		if (Settings.Advanced.ShowFPSDetails) {
			result += "\nGui : " + PerformanceStatsLastFrame.GuiDraws + "/" + PerformanceStatsLastFrame.GuiUpdates + " (legacy:" + PerformanceStatsLastFrame.GuiLegacyDraws + ")";
			result += String.Format("\nCached containers: {0} Repaints:{1} Resizes:{2}", GuiContainer.RenderTextureContainers.Count, PerformanceStatsLastFrame.GuiRenderTextureRepaints, PerformanceStatsLastFrame.GuiRenderTextureResizes);
			result += "\nFocused control: " + GuiComponent.FocusedControl;
			result += "\nMouse over control: " + GuiComponent.MouseOverComponent;
			result += "\nStates: " + PerformanceStatsLastFrame.StateDraws + "/" + PerformanceStatsLastFrame.StateUpdates;
			result += String.Format("\nTextAdjustments: {0}", PerformanceStatsLastFrame.TextAdjustmentChacters);
			result += "\nGuiCalls: " + PerformanceStatsLastFrame.GuiCallRegister.Count + " = [" + String.Join(",", PerformanceStatsLastFrame.GuiCallRegister.ToArray()) + "]";
			result += String.Format("\nMouse location: {0},{1},{2} Over component location: {3}: ", Mouse.Position, Input.mousePosition, InputEx.mousePosition, (GuiComponent.MouseOverComponent == null) ? "[none]" : GuiComponent.MouseOverComponent.AbsoluteBounds.ToString());
		}
		return result;
	}

	/** 
	 * Updates the active states of the game.
	 * 
	 * Usualy only the topmost state is draw and updated.  However any state with backgroundUpdate enabled will be updated even if not topmost.
	 * 
	 */
	public virtual void DoUpdate()
	{		
		GameTime += Time.deltaTime;

		Mouse.Update();

		bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
		bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		processKeys(ctrl, alt, shift);

		FPSCounter.addSample(Time.deltaTime);

		if (notificationDelayTimer > 0)
			notificationDelayTimer -= Time.deltaTime;

		// add any notifications, 1 at a time.
		if ((notificationDelayTimer <= 0) && (pendingNotifications.Count >= 1)) {
			
			var notification = new GuiNotification(pendingNotifications[0].Message, pendingNotifications[0].Sprite);
			var soundName = pendingNotifications[0].SoundName;

			foreach (var existingNotification in notificationList)
				existingNotification.PushDown();			
			notificationList.Add(notification);

			if (!string.IsNullOrEmpty(soundName))
				SoundManager.Play(soundName);

			pendingNotifications.RemoveAt(0);
			notificationDelayTimer = NotificationDelay;
		}

		if (StateList.Count == 0)
			return;

		if (Input.GetMouseButtonDown(0))
			ComponentClickedDownOn = StateList[0].GetComponentAtPosition(Mouse.Position);

		if (DragDrop != null)
			DragDrop.Update();

		// Remove disabled states.
		for (int lp = 0; lp < StateList.Count; lp++) {
			if (StateList[lp].Active == false) {
				StateList.RemoveAt(lp);
				lp--;
				continue;
			}
		}

		// Remove disabled notifications.
		for (int lp = 0; lp < notificationList.Count; lp++) {
			if (notificationList[lp].Active == false) {
				notificationList.RemoveAt(lp);
				lp--;
				continue;
			}
		}

		// Update topmost state and any background update states.

		StateList[0].Update();

		for (int lp = 1; lp < StateList.Count; lp++) {
			var state = StateList[lp];
			if (state.BackgroundUpdate)
				state.Update();
		}

		// Apply global fx (note: I should probably render all UI to a texture and do this once. 
		for (int lp = 0; lp < StateList.Count; lp++) {
			var state = StateList[lp];

			switch (FXStyle) {
				case RenderFXStyle.Normal:				
					state.CompositeColorTransform = ColorTransform.Identity;
					state.Color = Color.white;
					break;
				case RenderFXStyle.Retro:
					var a = ColorTransform.Saturation(0.25f);
					var b = ColorTransform.Multiply(new Color(255 / 255f, 235 / 255f, 215 / 255f));
					var c = ColorTransform.Identity;
					c.Matrix = a.Matrix * b.Matrix;
					state.CompositeColorTransform = c;
					break;
				case RenderFXStyle.Sketch:
					a = ColorTransform.Saturation(0f);
					b = ColorTransform.Multiply(3f, 3f, 3f);
					c = ColorTransform.Identity;
					c.Matrix = a.Matrix * b.Matrix;
					c.ColorOffset = new Vector3(-0.3f, -0.3f, -0.3f);
					state.CompositeColorTransform = c;
					break;			
				case RenderFXStyle._Clay:				
					state.CompositeColorTransform = ColorTransform.Bronze;
					state.Color = Color.white;
					break;			
			}

		}

		// Show FPS label
		if (Time.time - lastFpsUpdate > 0.5f) {
			
			lastFpsUpdate = Time.time;

			FPSLabel.Caption = Settings.Advanced.ShowFPSDetails ? getAdvancedFPSDetails() : getFPSDetails();

			FPSLabel.Width = Settings.Advanced.ShowFPSDetails ? -1 : 100;
		}

		if (Input.GetMouseButtonUp(0))
			ComponentClickedDownOn = null;
	}

	/** 
	 * Draws all visibile game states.
	 */
	virtual public void DoDraw()
	{				
		// check for any cache updates that need to happen
		GuiContainer.ValidateAll();

		if (!EnableCamera) {
			GL.Clear(true, true, Color.black);
		}						

		// draws all layers into a render texture.
		bool layerComposite = false;

		RenderTexture compositeTexture = null;

		PureGUIMatrix = GUI.matrix;

		GUI.matrix = Matrix4x4.Scale(new Vector3(GuiScale, GuiScale, 1));
		BaseGUIMatrix = GUI.matrix;

		if (layerComposite) {
			compositeTexture = RenderTexture.GetTemporary(Screen.width, Screen.height);
			Graphics.SetRenderTarget(compositeTexture);
			GL.Clear(true, true, Color.clear);
		}

		// states are drawn backwards from the last state to block vision 
		int firstStateToDraw; 
		for (firstStateToDraw = 0; firstStateToDraw < StateList.Count; firstStateToDraw++) {
			if (!StateList[firstStateToDraw].TransparentDraw)
				break;
		}
	
		if (!HideGUI) {
			for (int lp = firstStateToDraw; lp >= 0; lp--) {				
				try {
					StateList[lp].Draw();
				} catch (Exception error) {
					Trace.LogDebug("Error drawing state {0} of {1}: {2}", lp, StateList.Count, error.Message);
				}
			}
		}

		if (layerComposite) {			
			Graphics.SetRenderTarget(null);		
			var dp = DrawParameters.Default;
			//Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), compositeTexture, new Rect(0, 0, Screen.width, Screen.height), 0, 0, 0, 0, Color.white, dp.GetPreparedMaterial(compositeTexture));
			Graphics.Blit(compositeTexture, dp.GetPreparedMaterial(compositeTexture));
			RenderTexture.ReleaseTemporary(compositeTexture);
		}

		// draw notifications
		for (int lp = 0; lp < notificationList.Count; lp++) {
			var notification = notificationList[lp];
			notification.Update();
			notification.Draw();
		}				

		if (DragDrop != null)
			DragDrop.Draw();

		if (Settings.General.ShowFPS)
			FPSLabel.Draw();

		if (Settings.Advanced.ShowLogs) {
			CurrentState.PositionComponent(LogWindow, 1, 0);
			LogWindow.Update();
			LogWindow.Draw();
		}
	}

	/** Posts a new notification.  It will display for short time in the top right corner of the screen then dissappear */
	public static void PostNotification(string message, Sprite sprite = null, bool playSound = true)
	{
		// we don't actualy create the notification as we might not be in an "onGui" call, so instead we just
		// add it to a list and handle it during update
		pendingNotifications.Add(new Notification(message, sprite, playSound));
	}

	/** Switches topmost state to new state, effectively poping the previous state and pushing the new one */
	static public void SwitchState(GuiState state)
	{
		PopState();
		PushState(state);
	}

	/** Pushes given state onto the state stack causing it to become visible */
	static public void PushState(GuiState state)
	{
		StateList.Insert(0, state);
		state.Show();
	}

	/** Removes all states from list */
	static public void PopAllStates()
	{
		foreach (var state in StateList)
			state.Close();

		StateList.Clear();
	}

	/** Pops topmost state off state list and closes it. */
	static public void PopState()
	{
		if (StateList.Count != 0) {
			StateList[0].Close();
			StateList.RemoveRange(0, 1);
		}
		if (StateList.Count != 0)
			StateList[0].Show();
	}

	/*** pops the gui matrix */
	static public void PopGUI()
	{
		if (GuiMatrixStack.Count == 0) {
			Trace.LogWarning("Gui Matrix push/pop count missmatch");
			return;
		}
		GUI.matrix = GuiMatrixStack.Pop();
	}

	/*** pushes the gui matrix */
	static public void PushGUI()
	{
		GuiMatrixStack.Push(GUI.matrix);
	}

	/** Creates a plane style from given sprite */
	static public GUIStyle CreateStyle(Sprite sprite)
	{
		var style = GetStyleCopy("Box");
		style.normal.background = sprite.texture;
		return style;
	}

	/** Selects a style from current Skin based on name.  If style can not be found then returns 'box' style and issues a warning */
	static public GUIStyle GetStyleCopy(string name)
	{
		GUIStyle style;
		try {
			style = Instance.Skin.GetStyle(name);
		} catch {
			style = null;
		}

		if (style == null) {
			Trace.LogWarning("Gui skin is missing style '" + name + "'");
			return new GUIStyle(Instance.Skin.box);
		} else {
			return new GUIStyle(style);
		}
	}

	/**
	 * Prompts user to confirm an action, if they agree delegate will be executed.
	 * @param action The action to perform if the use clicks "OK"
	 * @param text The text to show on the confirmation window 
	 */
	static public void ConfirmAction(SimpleEvent action, string text)
	{
		var state = new ModalDecisionState("Are you sure?", text);
		state.OnYes += action;
		PushState(state);	
	}

	/** 
	 * Shows a modal window, freezing the game until the window is closed.
	 * This can be used for presenting the user messagess that must be acknoledged
	 */
	static public void ShowModal(string title, string message)
	{
		PushState(new ModalNotificaionState(title, message, TextAnchor.MiddleCenter, 400));
	}

	/** 
	 * Shows a modal window, freezing the game until the window is closed.
	 * This is large the show modal and has left alignment. It's better for longer messages, such as errors.
	 */
	static public ModalNotificaionState ShowLargeModal(string title, string message)
	{
		var state = new ModalNotificaionState(title, message, TextAnchor.MiddleLeft, 600);
		PushState(state);
		return state;
	}

	/**
	 * Returns true if the game is currently running on a touch device 
	 */
	public static bool TouchDevice {
		get {
			return SystemInfo.deviceType == DeviceType.Handheld;
		}
	}

	/**
	 * True if we are playing at 1024x700 or less resoultion 
	 */
	public static bool SmallScreen {
		get {
			return (Screen.width / GuiScale <= 1024) || (Screen.height / GuiScale <= 700);
		}
	}

	private static bool isSSAOEnabled {
		get {
			var aoScript = Engine.Instance.Camera.GetComponent<SSAOPro>();
			if (aoScript == null)
				return false;
			return aoScript.enabled;
		}
	}

	/** Enables or disables SSAO. */
	private static void ApplySSAO(bool value)
	{		
		var aoScript = Engine.Instance.Camera.GetComponent<SSAOPro>();

		if (aoScript == null)
			return;
	
		Settings.General.EnableSSAO = value;

		switch (FXStyle) {
			case RenderFXStyle.Sketch:
				aoScript.enabled = true;
				aoScript.Samples = SSAOPro.SampleCount.Low;
				aoScript.Intensity = 5f;
				aoScript.Radius = 0.125f;
				aoScript.Distance = 1f;
				aoScript.Bias = 0.1f;
				aoScript.LumContribution = 0.5f;
				aoScript.Blur = SSAOPro.BlurMode.None;
				aoScript.DebugAO = true; 
				break;		
			case RenderFXStyle._Clay:
				aoScript.enabled = value;
				aoScript.Samples = SSAOPro.SampleCount.High;
				aoScript.Intensity = 2f;
				aoScript.Radius = 0.05f;
				aoScript.Distance = 1f;
				aoScript.Bias = 0.1f;
				aoScript.LumContribution = 0.5f;
				aoScript.Blur = SSAOPro.BlurMode.None;
				aoScript.DebugAO = false;
				break;
			default:
				aoScript.enabled = value;
				aoScript.Samples = SSAOPro.SampleCount.Medium;
				aoScript.Intensity = 2f;
				aoScript.Radius = 0.03f;
				aoScript.Distance = 1f;
				aoScript.Bias = 0.1f;
				aoScript.LumContribution = 0.5f;
				aoScript.Blur = SSAOPro.BlurMode.None;
				aoScript.DebugAO = false;
				break;
		}
	}

	/** Applies given FX style. */
	private static void ApplyFXStyle(RenderFXStyle newStyle)
	{
		FXStyle = newStyle;
		var retroScript = Engine.Instance.Camera.GetComponent<RetroPixel>();
		var aoScript = Engine.Instance.Camera.GetComponent<SSAOPro>();

		if (retroScript != null) {
			retroScript.enabled = (FXStyle == RenderFXStyle.Retro);
		}

		if (aoScript != null) {
			aoScript.enabled = (FXStyle == RenderFXStyle.Sketch);
			ApplySSAO(Settings.General.EnableSSAO);
		}

		ApplyTextureSwapper();
	}

	/** The unique ID for this device. */
	public static string UniqueID()
	{
		if (isWebGL) {
			if (PlayerPrefs.HasKey("UniqueID"))
				return PlayerPrefs.GetString("UniqueID");
			else {
				var random = new System.Random();
				var generatedKey = DateTime.Now.ToString("O") + random.NextDouble();
				var generatedID = Util.Hash(generatedKey);
				PlayerPrefs.SetString("UniqueID", generatedID);
				PlayerPrefs.Save();
				return generatedID;
			}
		} else
			return SystemInfo.deviceUniqueIdentifier;		
	}
}

	