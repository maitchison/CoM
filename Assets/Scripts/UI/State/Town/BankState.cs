using UnityEngine;
using UI;

namespace UI.State.Town
{

	/** The state to handle the town guild */
	public class BankState : TownBuildingState
	{
		private GuiLabel BankGoldLabel;
		private GuiLabel BankGoldValueLabel;
		private GuiLabel InHandGoldLabel;
		private GuiLabel InHandGoldValueLabel;

		private GuiGold GoldTransfer;

		private GuiItemInventory Inventory;

		private GuiButton DepositButton;

		public BankState() : base("Bank")
		{
			GoldTransfer = new GuiGold();

			MainWindow.Width = 600;
			MainWindow.Height = 450;

			BankGoldLabel = new GuiLabel(0, 0, "Gold:");
			MainWindow.Add(BankGoldLabel, 20, 20);

			BankGoldValueLabel = new GuiLabel(0, 0, "");
			BankGoldValueLabel.DragDropEnabled = true;
			BankGoldValueLabel.ForceContent(GoldTransfer);
			MainWindow.Add(BankGoldValueLabel, 100, 20);

			InHandGoldLabel = new GuiLabel(0, 0, "In Hand:");
			MainWindow.Add(InHandGoldLabel, 20, 50);
			
			InHandGoldValueLabel = new GuiLabel(0, 0, "");
			MainWindow.Add(InHandGoldValueLabel, 100, 50);

			DepositButton = new GuiButton("Deposit");
			MainWindow.Add(DepositButton, 20, 80);

			Inventory = new GuiItemInventory(300, 330);
			MainWindow.Add(Inventory, 200, 10);

			DepositButton.OnMouseClicked += delegate {
				Character.GoldInBank += Character.GoldInHand;
				Character.GoldInHand = 0;
			};

			RepositionControls();

		}

		public override void Update()
		{
			base.Update();
			Sync();
		}

		private void Sync()
		{
			GoldTransfer.Source = Character;
			Inventory.Source = Character.BankItems;
			BankGoldValueLabel.Caption = Util.Colorise(Util.Comma(Character.GoldInBank), Color.yellow);
			InHandGoldValueLabel.Caption = Util.Colorise(Util.Comma(Character.GoldInHand), Color.yellow);
		}
		
	}
}