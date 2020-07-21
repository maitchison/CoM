using UnityEngine;
using System.Collections;
using System;

/** Sends exceptions to a webserver */
public class WebServer : MonoBehaviour
{
	private static WebServer Instance { get; set; }

	public string ServerURL = "http://playendurance.com/WebServer/";

	/** If true messages will be send even when in editor mode. */
	public bool EnableEditorPosting = false;

	public int MaxPostsPerMinute = 60;

	/** Used to make sure we don't send duplicate errors. */
	private string previousCondition = "";

	int postCount = 0;

	/** Used to stop logging exceptions generated while we are logging exceptions. */
	int processingCount = 0;

	void Awake()
	{
		if (Instance != null) {
			DestroyImmediate(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		postCount = 0;

		StartCoroutine(tickDown());
	}

	private IEnumerator tickDown()
	{
		while (true) {
			postCount = 0;
			yield return new WaitForSeconds(60f);
		}

	}

	void OnEnable()
	{
		Trace.Log("WebServer enabled");
		Application.logMessageReceivedThreaded += OnLog;
	}

	void OnDisable()
	{
		Application.logMessageReceivedThreaded -= OnLog;
		Trace.Log("WebServer disabled");
	}

	private static void addBaseFields(WWWForm form)
	{				
		form.AddField("userid", Engine.UniqueID());
		form.AddField("systeminfo", Engine.GetSystemInfo());
		form.AddField("platform", Application.platform.ToString());
		form.AddField("version", VersionInfo.Number);			
	}

	/** Sets up for making a post by adding defaults fields to a new form and then returning it.*/
	private static WWWForm setupPost(bool addDefaultFields = true)
	{
		if (Instance == null) {
			throw new Exception("Tried to post event before webserver was initialized.");
		}

		if (!Instance.enabled)
			return null;
		
		Instance.postCount++;

		if (Instance.postCount > Instance.MaxPostsPerMinute) {
			print("Too many posts.");
			return null;
		}

		var form = new WWWForm();

		if (addDefaultFields)
			addBaseFields(form);

		return form;
	}

	/** 
	 * Corountine to processes the post. 
	 * niceName will be used when logging this post.
	 */
	private static IEnumerator processPost(WWWForm form, string relativePath, string niceName = "")
	{
		if (Instance == null) {
			throw new Exception("Tried to post event before webserver was initialized.");
		}

		var url = Instance.ServerURL + relativePath;
		var w = new WWW(url, form);

		yield return w;

		if (!String.IsNullOrEmpty(w.error))
			print(string.Format("WWW Error: {0}\n{1}\n{2}", w.error, w.text, w.responseHeaders.Listing()));
		else {
			string message = String.IsNullOrEmpty(niceName) ? "Posted to {0}" : "Posted [{1}] to {0}";
			print(string.Format(message, relativePath, niceName));
		}
		
	}

	public static void PostException(string exception, string details)
	{		
		if (Application.isEditor && !Instance.EnableEditorPosting)
			return;

		var form = setupPost();
		if (form == null)
			return;

		form.AddField("exception", exception);
		form.AddField("details", details);

		Instance.StartCoroutine(processPost(form, "/LogException.php", exception));
	}

	public static void PostEvent(string eventName, string details = "")
	{				
		var form = setupPost();
		if (form == null)
			return;

		form.AddField("event", eventName);
		form.AddField("details", details);

		Instance.StartCoroutine(processPost(form, "/LogEvent.php", eventName));
	}

	public static void PostCheckIn()
	{		
		if (!CoM.Loaded)
			return;

		var form = setupPost(false);
		if (form == null)
			return;

		var UserProfilePlayTime = CoM.Profile.TotalPlayTime;

		form.AddField("userid", Engine.UniqueID());
		form.AddField("playtime", UserProfilePlayTime);	

		Instance.StartCoroutine(processPost(form, "/LogCheckIn.php", UserProfilePlayTime.ToString()));
	}

	protected void OnLog(string condition, string stackTrace, LogType type)
	{			
		if (processingCount > 0)
			return;

		processingCount++;

		if (condition == previousCondition)
			return;
		switch (type) {
			case LogType.Assert:
			case LogType.Error:									
			case LogType.Exception:			
				
				if (String.IsNullOrEmpty(stackTrace))
					stackTrace = Environment.StackTrace;
				PostException(condition, stackTrace);
				break;
		}
		previousCondition = condition;

		processingCount--;
	}

}

