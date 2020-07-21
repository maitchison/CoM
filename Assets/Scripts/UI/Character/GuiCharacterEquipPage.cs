
using UnityEngine;

namespace UI
{
	/** Page to display a characters equiped items and some basic stats */
	public class GuiCharacterEquipPage : GuiCharacterPage
	{
		private GuiImage CharacterPortrait;
		private GuiLabel StatNames;
		private GuiLabel StatValues;
		private GuiItemSlot[] EquipedSlot;
		private GuiLabel CharacterInfo;
		private GuiCoinAmount goldInfo;

		/** Constructs a new character page */
		public GuiCharacterEquipPage()
		{
			// ---------------------------------

			CacheMode = CacheMode.Solid;
			AutoRefreshInterval = 0.5f;

			GuiPanel Panel = new GuiPanel(0, 90);
			Panel.Align = GuiAlignment.Top;
			Panel.PanelMode = GuiPanelMode.Square;
			Panel.Color = new Color(0.15f, 0.15f, 0.35f, 0.00f);
			Add(Panel);

			CharacterInfo = new GuiLabel(0, 0, "", 200);
			CharacterInfo.TextAlign = TextAnchor.MiddleCenter;
			CharacterInfo.DropShadow = true;
			CharacterInfo.FontSize = 18;
			Panel.Add(CharacterInfo, 85, 20);

			CharacterPortrait = new GuiImage(0, 0, null);
			CharacterPortrait.Framed = true;
			CharacterPortrait.InnerShadow = true;
			Panel.Add(CharacterPortrait, 25, 10);

			goldInfo = new GuiCoinAmount();
			Add(goldInfo, 14, 210);

			// ---------------------------------

			// basic stats
			StatNames = new GuiLabel(10, 100, "", 80, 200);
			StatNames.FontColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
			StatNames.Caption = 
				"HP" + "\n" +
			"SP" + "\n" +
			"\n" +
			"Damage" + "\n" +
			"Armour" + "\n";
			
			Add(StatNames);

			StatValues = new GuiLabel(10, 100, "", 100, 200);
			StatValues.FontColor = Color.Lerp(Color.white, Color.red, 0.25f);
			StatValues.TextAlign = TextAnchor.UpperRight;
			Add(StatValues);

			// create equiped item slots
			EquipedSlot = new GuiItemSlot[16];

			EquipedSlot[0] = CreateEquipedSlot(1, 1);
			EquipedSlot[1] = CreateEquipedSlot(2, 1);
			EquipedSlot[2] = CreateEquipedSlot(3, 1);

			EquipedSlot[3] = CreateEquipedSlot(1, 2);
			EquipedSlot[4] = CreateEquipedSlot(2, 2);
			EquipedSlot[5] = CreateEquipedSlot(3, 2);

			EquipedSlot[6] = CreateEquipedSlot(1, 3);
			EquipedSlot[7] = CreateEquipedSlot(2, 3);
			EquipedSlot[8] = CreateEquipedSlot(3, 3);

			EquipedSlot[9] = CreateEquipedSlot(1, 4);
			EquipedSlot[10] = CreateEquipedSlot(3, 4);

			EquipedSlot[11] = CreateEquipedSlot(1, 5);
			EquipedSlot[12] = CreateEquipedSlot(2, 5);
			EquipedSlot[13] = CreateEquipedSlot(3, 5);

			var itemTrashSlot = new GuiItemTrash(0, 0);
			Add(itemTrashSlot, 10, -30);

			Sync();
		}

		private GuiItemSlot CreateEquipedSlot(int x, int y)
		{
			GuiItemSlot result = new GuiItemSlot(120 + (x - 1) * 50, 80 + (y - 1) * 50);
			Add(result);
			return result;
		}

		/** Updates controls to display the current characters stats */
		override public void Sync()
		{
			Invalidate();

			if (Character == null) {
				CharacterPortrait.Sprite = null;
				StatValues.Caption = "";
				CharacterInfo.Caption = "";
			} else {
				CharacterPortrait.Sprite = Character.Portrait;

				goldInfo.Value = Character.PersonalGold;

				StatValues.Caption = 
					Character.Hits + "/" + Character.MaxHits + "\n" +
				Character.Spells + "/" + Character.MaxSpells + "\n" +
				"\n" +
				Character.NominalDamage + "\n" +
				Character.ItemArmour + "\n" +
				"\n" +
				"\n";

				CharacterInfo.Caption = 
					Character.Name + "\n" +
				Util.SizeCode(Character.Race + " " + Character.CurrentGuild + " [" + Character.CurrentMembership.CurrentLevel + "]", 16);

				for (int lp = 0; lp < Character.Equiped.Count; lp++) {
					EquipedSlot[lp].DataLink = Character.Equiped[lp];
				}
			}
		}

	}
}
