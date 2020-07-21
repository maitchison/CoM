using UI.Store;
using System;

namespace UI.State.Town
{
	/** The state to handle the town store */
	public class StoreState : TownBuildingState
	{
		private GuiStore Store;

		public StoreState() : base("Store") 
		{
			int storeWidth = 800;
			int storeHeight = 600 - 150;

			if (Engine.SmallScreen) {
				storeWidth = 640;
				storeHeight = 480 - 50;
			}

			Store = new GuiStore(CoM.Store, storeWidth, storeHeight);
			Store.EnableBackground = false;
			MainWindow.Add(Store);

			MainWindow.FitToChildren();

			RepositionControls();

			DefaultControl = Store;
		}

		// Destroy on close.
		public override void Close()
		{
			base.Close();
			this.Destroy();
		}

	}

}