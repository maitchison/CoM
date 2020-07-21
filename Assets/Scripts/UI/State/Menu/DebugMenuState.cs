
using UnityEngine;
using System.Collections.Generic;
using System;
using UI;
using UI.Generic;
using Data;
using UI.State;

namespace UI.State.Menu
{
	/** Allows user to import character or game files */
	public class DebugMenuState : GuiState
	{
		public DebugMenuState()
			: base("DebugMenu")
		{
			GuiWindow window = new GuiWindow(400, 500, "DEBUG");
			Add(window, 0, 0);

			int buttonIndex = 0;

			GuiButton testSerializationButton = new GuiButton("Serialization", -1);
			window.Add(testSerializationButton, 0, 50 + 40 * buttonIndex++);
				
			GuiButton ResetButton = new GuiButton("Reset", -1);
			ResetButton.ColorTransform = ColorTransform.BlackAndWhite;
			ResetButton.Color = new Color(1f, 0.4f, 0.3f);
			window.Add(ResetButton, 0, 50 + 40 * buttonIndex++);

			testSerializationButton.OnMouseClicked += delegate {
				testDataSerialization();
			};
				
			ResetButton.OnMouseClicked += delegate {
				CoM.ConfirmAction(performGameReset, "Are you sure you want to reset the game?");
			};
		
			window.Add(Util.CreateBackButton(), 0, -10);
		}

		private void testDataSerialization()
		{
			var dataObject = new TestClass();

			StateStorage.SaveData("TestClass", dataObject);

			ValidateReadWrite();
		
		}

		/** Resets the game and pops a window up stopping the application from continuing */
		private void performGameReset()
		{
			CoM.FullReset();
			
			var modalState = new ModalNotificaionState("Game Reset", "The game has been reset and must be restarted.");
			modalState.ConfirmationButton.Visible = false;
			
			Engine.PushState(modalState);
			CoM.SaveOnExit = false;
		}

		/** Writes out each object in library, reads it back in, and makes sure they are the same */
		private bool Validate<T>(DataLibrary<T> library) where T: NamedDataObject
		{
			List<string> differences = new List<string>();
			foreach (NamedDataObject item in library) {
				StateStorage.SaveData("validationtest", item);
				object loadedItem = StateStorage.LoadData<T>("validationtest");
				if (!Util.CompareProperites(item, loadedItem, ref differences)) {
					Trace.LogWarning("Validation failed on " + typeof(T) + ": " + String.Join(",", differences.ToArray()) + "]");
					return false;
				} 
			}

			Trace.Log("Validation passed for " + typeof(T));

			return true;
		}

		/** Writes objects to XML and reads them back in again, checking that they are identical */
		private void ValidateReadWrite()
		{
			Debug.Log("Validating Read/Write routines:");
			Validate(CoM.ItemClasses);
			Validate(CoM.ItemTypes);
			Validate(CoM.Items);
			Validate(CoM.MonsterClasses);
			Validate(CoM.MonsterTypes);
			Validate(CoM.Monsters);
			Validate(CoM.Spells);
			Validate(CoM.Guilds);
			Validate(CoM.Races);
		}

	}

	[DataObjectAttribute("Test", true)]
	public class TestClass : DataObject
	{
		public string TestString = "Hello";
		public string[] Value = { "one", "two", "three" };

		[FieldAttr("GuildByID", FieldReferenceType.ID)]
		public List<MDRGuild> GuildsList;

		[FieldAttr("GuildByName", FieldReferenceType.Name)]
		public MDRGuild[] GuildsArray;

		[FieldAttr("GuildFull", FieldReferenceType.Full)]
		public MDRGuild[] GuildsArray2;

		public TestClass()
		{
			GuildsList = new List<MDRGuild>();
			GuildsList.Add(CoM.Guilds[0]);
			GuildsList.Add(CoM.Guilds[1]);
			GuildsArray = GuildsList.ToArray();
			GuildsArray2 = GuildsList.ToArray();
		}

	}
}