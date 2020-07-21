/**
 * 2014 Matthew Aitchison
 * Last reviewed: 5/Jul/2014
 */

using UnityEngine;
using UI.Generic;

namespace UI.State.Menu
{
	/** Displays a 3d dungeon behind the menu states */
	public class MenuDungeonBackground : GuiState
	{
		protected Vector3 basePosition;

		public MenuDungeonBackground()
			: base("MenuBackground")
		{			
			BackgroundUpdate = true;
			basePosition = new Vector3(5 + Util.Roll(20), 0.7f, 5 + Util.Roll(20));
		}

		public override void Show()
		{
			base.Show();
			UpdateBirdsEyeView();
			// force a generate with no ceiling.
			CoM.GetDungeonBuilder().Initialize();
			CoM.GetDungeonBuilder().GenerateCeiling = false;
			CoM.GetDungeonBuilder().SelectedMap = 1;
			CoM.GetDungeonBuilder().GenerateCeiling = true;
			CoM.GetDungeonBuilder().MarkAllMapsAsDirty();
			CoM.GetDungeonBuilder().RebuildSelectedMap();
			CoM.Culler.RemoveAllObjects();
			CoM.Culler.Disable();
			EnvironmentManager.StandardFog();
			EnvironmentManager.NearView();
			EnvironmentManager.Sync();
			PartyController.Instance.EnableDoorOpener = false;
		}

		public override void Update()
		{
			base.Update();
			UpdateBirdsEyeView();
		}

		/**
		 * Slowly movies camera around showing a birds eye view of the map.
	 	 */
		private void UpdateBirdsEyeView()
		{
			Camera camera = CoM.Instance.Camera;
			
			float rotation = Time.time;
			
			var targetPosition = basePosition;
			var targetOrientation = Quaternion.Euler(11f, 18f + 2 * rotation, 6.5f);
			
			camera.transform.localPosition = targetPosition;
			camera.transform.localRotation = targetOrientation;
		}
	}
}
