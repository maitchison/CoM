using UnityEngine;
using Mordor;
using UI;
using UI.Generic;
using Data;

namespace UI.State.Town
{
	public class LibraryRecordsView: GuiContainer
	{
		public LibraryRecordsView()
			: base(500 - 16, 350 - 18)
		{
			Align = GuiAlignment.Full;
			int xpos = 20;
			int ypos = 10;
			foreach (GameRecord record in CoM.GameStats.Records) {
				Add(createLabelForRecord(record), xpos, ypos);
				ypos += 42;
			}
		}

		/** Creates a gui label representing the given game record */
		private GuiLabel createLabelForRecord(GameRecord record)
		{
			string recordString = 
				"<B>" + record.Name + "</B>\n" + "<Size=12>" + record.ToString() + "</Size>";
			return new GuiLabel(recordString);
		}
	}

	public class LibraryMonstersView: GuiContainer
	{
		public GuiListBox<MDRMonster> MonsterList;
		public GuiMonsterToolTip MonsterInfo;

		public LibraryMonstersView()
			: base(500 - 16, 350 - 18)
		{
			Align = GuiAlignment.Full;

			var scrollableMonsterList = new ScrollableListBox<MDRMonster>(200 - 4, 450 - 37) { X = 4 };

			MonsterList = scrollableMonsterList.ListBox;
			MonsterList.EnableBackground = true;

			MonsterInfo = new GuiMonsterToolTip();
			MonsterInfo.X = 200;
			MonsterInfo.Y = 0;
			MonsterInfo.Width = Width - 200;
			MonsterInfo.Height = Height;
			MonsterInfo.ShowEncounterStats = true;
			MonsterInfo.WindowStyle = GuiWindowStyle.ThinTransparent;
			MonsterInfo.Color = Color.black;

			MonsterList.OnSelectedChanged += SyncMonsterInfo;

			Add(scrollableMonsterList);
			Add(MonsterInfo);
			CreateListings();
		}

		/** Updates the monster info to match the selected item */
		private void SyncMonsterInfo(object source, System.EventArgs args)
		{
			MonsterInfo.Monster = MonsterList.Selected;
		}

		/** Updates list of monsters based on the current GameStats */
		private void CreateListings()
		{
			foreach (MonsterStatRecord record in CoM.GameStats.MonsterStats.Values) {
				if (record.NumberSeen == 0)
					continue;
				MonsterList.Add(record.Monster);
			}
			MonsterInfo.Monster = MonsterList.Selected;
		}
	}

	public class LibraryItemsView: GuiContainer
	{
		public GuiListBox<MDRItemInstance> ItemList;
		public GuiItemToolTip ItemInfo;

		public LibraryItemsView()
			: base(500 - 16, 350 - 18)
		{
			Align = GuiAlignment.Full;

			var scrollableItemList = new ScrollableListBox<MDRItemInstance>(200 - 4, 450 - 37) { X = 4 };

			ItemList = scrollableItemList.ListBox;
			ItemList.EnableBackground = true;

			ItemInfo = new GuiItemToolTip();
			ItemInfo.X = 200;
			ItemInfo.Y = 0;
			ItemInfo.Width = 200;
			ItemInfo.Height = Height;

			ItemInfo.AutoFit = false;
			ItemInfo.WindowStyle = GuiWindowStyle.ThinTransparent;
			ItemInfo.Color = Color.black;

			ItemList.OnSelectedChanged += SyncItemInfo;

			Add(scrollableItemList);
			Add(ItemInfo);
			CreateListings();
		}

		/** Updates the item info to match the selected item */
		private void SyncItemInfo(object source, System.EventArgs args)
		{
			ItemInfo.ItemInstance = ItemList.Selected;
		}

		/** Updates list of items based on the current GameStats */
		private void CreateListings()
		{
			foreach (ItemStatRecord record in CoM.GameStats.ItemStats.Values) {
				if (record.NumberFound == 0)
					continue;
				ItemList.Add(MDRItemInstance.Create(record.Item, record.IDLevel));
			}
			SyncItemInfo(null, null);
		}
	}

	/** The state to handle the town library */
	public class LibraryState : TownBuildingState
	{
		public GuiComponent ItemsWindow;
		public GuiComponent MonstersWindow;
		public GuiComponent RecordsWindow;

		public LibraryState()
			: base("Library")
		{
			Util.Assert(CoM.AllDataLoaded, "Data must be loaded before LibraryState can be created.");

			MainWindow.Width = 600;
			MainWindow.Height = 480;

			// --------------------------------------

			ItemsWindow = new LibraryItemsView();
			MonstersWindow = new LibraryMonstersView();
			RecordsWindow = new LibraryRecordsView();

			// --------------------------------------

			var buttonGroup = new GuiRadioButtonGroup();
			buttonGroup.EnableBackground = true;

			buttonGroup.OnValueChanged += delegate {
				ItemsWindow.Visible = buttonGroup.SelectedIndex == 0;
				MonstersWindow.Visible = buttonGroup.SelectedIndex == 1;
				RecordsWindow.Visible = buttonGroup.SelectedIndex == 2;
			};

			buttonGroup.AddItem("Items");
			buttonGroup.AddItem("Monsters");
			buttonGroup.AddItem("Records");
			buttonGroup.ButtonSize = new Vector2(120, 28);
			buttonGroup.Height = 45;
			buttonGroup.Style = Engine.GetStyleCopy("Solid");
			buttonGroup.Color = Color.black.Faded(0.5f);
			buttonGroup.Width = MainWindow.Width;
			buttonGroup.Height = 45;
			MainWindow.Add(buttonGroup);

			buttonGroup.SelectedIndex = 0;

			// --------------------------------------

			var BodySection = new GuiContainer(MainWindow.Width, MainWindow.Height - buttonGroup.Height) { Y = buttonGroup.Height };
			MainWindow.Add(BodySection);

			BodySection.Add(ItemsWindow);
			BodySection.Add(MonstersWindow);
			BodySection.Add(RecordsWindow);

			// --------------------------------------

			RepositionControls();

			MainWindow.Background.Sprite = ResourceManager.GetSprite("Gui/InnerWindow");
			MainWindow.Background.Color = new Color(0.4f, 0.42f, 0.62f);

		}
	}
}