
using System;
using System.Collections;
using UnityEngine;
using UI.Generic;
using UI.State.Menu;
using Data;

//todo: this needs a rewrite

namespace UI.State
{

	/** Delegate to create a modal state. */
	public delegate ModalState CreateModalFunction();
	public delegate void SimpleAction();


	/** Really simple state that loads the games content */
	public class LoadingState : GuiState
	{

		/** Used to create pop ups, as we can't run gui commands from a co-routine */
		private CreateModalFunction requestPopup;

		/** A message describing the current error */
		private string errorMessage;
		/** Action to perform after the user closes the error window */
		private SimpleAction errorAction;

		public LoadingState()
			: base("LoadingState")
		{
		}

		public override void Show()
		{
			LoadGame();
		}

		private void HandleException(string action, Exception e)
		{
			errorMessage = e.Message;
			Trace.LogError(errorMessage + "\n" + e.StackTrace);
			requestPopup = CreateErrorNotice;
		}

		/** Loads game content. */
		private void LoadGame()
		{
			// 1. Load graphics
			try {
				CoM.LoadGraphics();

			} catch (Exception e) {
				HandleException("Load Graphics", e);
				return;
			} 

			// 2. Load the game data		
			if (!CoM.HasAllStaticData())
				requestPopup = CreateMissingGameFilesNotice;

			try {
				CoM.LoadGameData();
			} catch (Exception e) {
				errorMessage = 
					"An error has occured while loading the game data.\n" +
				"The game can not run.\n" +
				"\n" + e.Message + "\n" + e.StackTrace;

				Trace.LogError("Error loading game data: " + e.Message + "\n" + e.StackTrace);
				requestPopup = CreateErrorNotice;
				return;
			}

			// 3. Load save file
			try {
				if (!SaveFile.HasSave) {
					Trace.Log("No save file detected, resetting game.");
					CoM.State.Reset();
				} else
					CoM.State.Load();				
			} catch (Exception e) {
				errorMessage = 
					"An error has occured while loadind the save file.\n" +
				"Really sorry about this.  The only way to continue is to reset the save game.\n" +
				"All characters and progress will be lost.\n" +
				"Press OK to continue.\n" +
				"\n" + e.Message + "\n" + e.StackTrace;
				
				errorAction = CoM.State.Reset;

				string longError = "Error loading save file: " + e.Message + "\n" + e.StackTrace + "\n";
				if (e.InnerException != null) {
					string additionalErrorInformation = "\n" + e.InnerException.Message + "\n" + e.InnerException.StackTrace;
					longError += additionalErrorInformation;
					errorMessage += additionalErrorInformation;
				}

				Trace.LogWarning(longError);
				requestPopup = CreateErrorNotice;
				return;
			}					
		}

		/** Informs the player that a general error has occured. */
		private ModalState CreateErrorNotice()
		{ 
			var notice = new ModalNotificaionState(
				             "An Error Has Occured",
				             errorMessage,
				             TextAnchor.UpperLeft,
				             600             
				//Screen.width - 100
			             );

			return notice;
		}

		/** Informs the player that game files are missing. */
		private ModalState CreateMissingGameFilesNotice()
		{ 
			var notice = new ModalNotificaionState(
				             "Game Files Missing",
				             "The game files required to run are not present." + "\n" 
			             );
			return notice;
		}

		/** Creates any modal pop up's requested by the loading co-routine. */
		public override void Update()
		{
			base.Update();

			if (requestPopup != null) {
				var modalState = requestPopup();
				if (errorAction != null) {
					var errorActionCopy = errorAction;
					modalState.OnStateClose += delegate {
						errorActionCopy();
					};
				}
				Engine.PushState(modalState);
				requestPopup = null;
				errorAction = null;
			} 
		}

	}
}