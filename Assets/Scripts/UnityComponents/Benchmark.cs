using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml.Linq;

using Data;
using Mordor;
using UI;

public class BenchStats : DataObject
{
	public Dictionary<string,string> FrameRateLog;

	public string Info;

	public BenchStats()
	{
		FrameRateLog = new Dictionary<string, string>();
	}

	private string getInfo()
	{
		return DateTime.Now + " Castle of Mordor v" + VersionInfo.AsString + " " + CoM.GetGraphicsInfo();
	}

	#region implemented abstract members of DataObject

	public override void WriteNode(XElement node)
	{
		WriteValue(node, "Info", Info);
		WriteValue(node, "Framerates", FrameRateLog);
	}

	public override void ReadNode(XElement node)
	{
		throw new NotImplementedException();
	}

	#endregion


	public void Save()
	{
		Info = getInfo();
		StateStorage.SaveData("Benchmark", this);
	}

}

/** 
 * Runs a simple benchmark by clicking through the game 
 */
public class Benchmark : MonoBehaviour
{
	private float[] FrameTimes;
	private BenchStats Stats;

	private GuiComponent ClickButton;

	private const int FPS_SAMPLE_LENGTH = 100;

	private bool hasStarted = false;

	// Use this for initialization
	void Start()
	{
		FrameTimes = new float[FPS_SAMPLE_LENGTH];
		Stats = new BenchStats();
	}
	
	// Update is called once per frame
	void Update()
	{
		FrameTimes[Time.frameCount % FPS_SAMPLE_LENGTH] = Time.deltaTime;

		if ((CoM.BenchmarkModeEnabled) && !hasStarted) {
			StartCoroutine(RunBenchmark());
		}
	}

	// process fake clicks
	void OnGUI()
	{
		if (ClickButton != null) {
			ClickButton.DoClick();
			ClickButton = null;
		}
	}

	private float getFrameRate()
	{
		float total = 0;
		foreach (var delta in FrameTimes)
			total += delta;
		if (total == 0)
			return 0;
		return (FPS_SAMPLE_LENGTH / total);
	}

	/**
	 * Logs the framerate at this location, and takes a photo. 
	 */
	private void TakeSample(string name)
	{
		CoM.TakeScreenshot(name);

		Stats.FrameRateLog[name] = getFrameRate().ToString("0.0"); 

		Stats.Save();
		Trace.Log("Sampled " + name + " at " + Stats.FrameRateLog[name]);
	}

	private IEnumerator RunBenchmark()
	{
		hasStarted = true;

		while (!CoM.AllDataLoaded) {
			yield return new WaitForSeconds(1f);
		}

		Trace.Log("Starting runthrough...");

		// make sure we are running at full speed 
		Settings.ResetSettings();
		Settings.General.EnablevSnyc = false;
		Settings.Advanced.TargetFrameRate = -1;
		Settings.General.ShowFPS = true;
		Settings.General.HighQualityLighting = true;
		Settings.General.EnableAnisotropicFiltering = true;
		Settings.General.FXStyle = RenderFXStyle.Normal;

		// make sure we have a character
		if (CoM.CharacterList.Count == 0) {
			var newHero = MDRCharacter.Create("Tony", true);
			for (int lp = 0; lp < 10; lp++)
				newHero.GainLevel();
		}

		// initialize party, return to town fully healed.
		MDRCharacter hero = CoM.CharacterList[0];
		hero.Hits = hero.MaxHits + 100; //make sure we don't die.

		yield return new WaitForSeconds(2.0f);
		TakeSample("MainMenu");

		ClickButton = Engine.CurrentState.FindControl("Play");
		yield return new WaitForSeconds(2.0f);
		TakeSample("SelectCharacter");

		ClickButton = Engine.CurrentState.FindControl("Select");
		yield return new WaitForSeconds(2.0f);
		TakeSample("Town");

		ClickButton = Engine.CurrentState.FindControl("General Store");
		yield return new WaitForSeconds(2.0f);
		TakeSample("GeneralStore");
		ClickButton = Engine.CurrentState.FindControl("Back");
		yield return new WaitForSeconds(0.1f);

		ClickButton = Engine.CurrentState.FindControl("Guild");
		yield return new WaitForSeconds(2.0f);
		TakeSample("Guild");
		ClickButton = Engine.CurrentState.FindControl("Back");
		yield return new WaitForSeconds(0.1f);

		ClickButton = Engine.CurrentState.FindControl("Morgue");
		yield return new WaitForSeconds(2.0f);
		TakeSample("Morge");
		ClickButton = Engine.CurrentState.FindControl("Back");
		yield return new WaitForSeconds(0.1f);

		ClickButton = Engine.CurrentState.FindControl("Dungeon");
		yield return new WaitForSeconds(4.0f);
		TakeSample("Dungeon");

		Settings.General.EnablevSnyc = true;

		Application.Quit();

	}
}
