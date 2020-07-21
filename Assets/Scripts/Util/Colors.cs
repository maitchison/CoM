using System;
using UnityEngine;

/** Static colors used in the game */
public class Colors
{
	public static Color STAT_REQ_NOT_MET = Color.red;

	public static Color MAP_UNEXPLOREDTILE_COLOR = Color.black.Faded(0.75f);

	// -------------------------------
	// Misc
	// -------------------------------

	/** This is just a color I used for text all the time, it's 90% white and 90% opacity */
	public static Color FourNines = new Color(0.9f, 0.9f, 0.9f, 0.9f);

	public static Color BackgroundGray = new Color(0.5f, 0.5f, 0.5f);
	public static Color BackgroundRed = new Color(1f, 0.22f, 0.12f);
	public static Color BackgroundYellow = new Color(1f, 0.62f, 0.32f);
	public static Color BackgroundBlue = new Color(0.4f, 0.42f, 0.62f);

	// -------------------------------
	// General
	// -------------------------------

	public static Color GeneralHilightValueColor = Util.HexToColor("70b8ff");
	public static Color GeneralValueColor = new Color(1f, 0.9f, 0.5f);
	public static Color GeneralPercentPostiveColor = new Color(1f, 0.9f, 0.5f);
	public static Color GeneralPercentNegitiveColor = new Color(1f, 0.5f, 0.5f);

	// -------------------------------
	// Character Info Panel
	// -------------------------------

	public static Color CharacterInfoPanelSelected = new Color(0.5f, 0.6f, 1f);
	public static Color CharacterInfoPanel = Color.gray;

	public static Color CharacterInfoPanelHitsBar = new Color(180 / 255f, 40 / 255f, 40 / 255f, 0.5f);
	public static Color CharacterInfoPanelSpellsBar = new Color(45 / 255f, 64 / 255f, 143 / 255f, 0.75f);
	public static Color CharacterInfoPanelXPBar = new Color(1f, 1f, 1f, 0.5f);
	public static Color CharacterInfoPanelPinnedXPBar = new Color(1f, 1f, 0.75f, 0.75f);

	// -------------------------------
	// Store
	// -------------------------------

	public static Color StoreItemInfoBackgroundColor = new Color(0.4f, 0.42f, 0.62f);

	// -------------------------------
	// Used for Item Slots.
	// -------------------------------

	/** Ring color to indicate a slot that can equip a given item. */
	public static Color ItemCanEquipRing = Color.yellow;
	/** Ring color to indicate a slot that can accept an item, but the character stats or level to use it. */
	public static Color ItemCanNotEquipedRing = Color.red;

	/** Background color for items that are known to be cursed. */
	public static Color ItemCursed = new Color(0.5f, 0f, 0f, 0.75f);
	/** Background color for items that was cursed but is not uncursed. */
	public static Color ItemUncursed = new Color(0.5f, 0.25f, 0.25f, 0.50f);

	/** Background color for items that can be used (i.e. potions). */
	public static Color ItemUsable = new Color(0.5f, 1.0f, 0.5f, 0.33f);
	/** Background color for items that can be used (i.e. potions), but the character doesn't have the stats for. */
	public static Color ItemNotUsable = new Color(0.5f, 0.5f, 0.5f, 0.25f);

	/** Ring color to indicate items that are not yet identified. */
	public static Color ItemUnidentifiedRing = new Color(0.25f, 0.5f, 1f);

	// -------------------------------
	// Colors Used for Formatting the Message Log.
	// -------------------------------

	public static Color CHARACTER_COLOR = Color.green;
	public static Color MONSTER_COLOR = Color.yellow;
	public static Color VALUES_COLOR = Color.magenta;
	public static Color SPELL_COLOR = Color.white;


}