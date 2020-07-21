using UnityEngine;
using System;
using Mordor;
using UI.Generic;

namespace UI
{
	/** A panel that displays the characters portrait and their current health */
	class GuiCharacterFrame : GuiContainer, IDragDrop
	{
		public static int WIDTH = 142;
		public static int HEIGHT = 48;

		/** The character to display */
		public MDRCharacter Character { get { return _character; } set { setCharacter(value); } }

		internal GuiPartyInfo ParentPartyInfo;

		private GuiImage portrait;
		private GuiLabel stats;

		private GuiProgressBar hpBar;
		private GuiVerticalProgressBar spBar;

		private GuiImage levelIcon;
		private GuiImage pinIcon;

		/** Shortcut to primary action. */
		private GuiQuickAction primaryAction;
		/** Shortcut to secondary action. */
		private GuiQuickAction secondaryAction;

		private MDRCharacter _character;

		public GuiCharacterFrame()
			: base(WIDTH, HEIGHT)
		{			
			Style = Engine.GetStyleCopy("CharacterInfoPanel");
			DragDropEnabled = true;

			portrait = new GuiImage(4, 4, null);
			portrait.Scale = 0.5f;
			Add(portrait);

			levelIcon = new GuiImage(26, 27, ResourceManager.GetSprite("Icons/Lv"));
			Add(levelIcon);
			pinIcon = new GuiImage(18, 27, ResourceManager.GetSprite("Icons/Pin"));
			Add(pinIcon);

			levelIcon.Visible = false;
			pinIcon.Visible = false;

			stats = new GuiLabel(32 + 3 + 3, 5, "", 100, 50);
			stats.FontSize = 12;
			Add(stats);

			hpBar = new GuiProgressBar(WIDTH - 4, 6);
			hpBar.EnableBackground = false;
			hpBar.ProgressColor = Colors.CharacterInfoPanelHitsBar;
			Add(hpBar, 2, 40);

			spBar = new GuiVerticalProgressBar(6, Height - 11);
			spBar.EnableBackground = false;
			spBar.ProgressColor = Colors.CharacterInfoPanelSpellsBar;
			Add(spBar, Width - 9, 2);						

			primaryAction = new GuiQuickAction(null, 0);
			secondaryAction = new GuiQuickAction(null, 1);

			Add(primaryAction, -10 - primaryAction.Width - 3, 10);
			Add(secondaryAction, -10, 10);

			OuterShadowColor = Color.yellow;
			OuterShadowSprite = OUTER_EDGE;

			OnMouseDown += clicked;	
		}

		private void clicked(object source, EventArgs e)
		{
			if (Character == null)
				return;

			switch (GuiPartyInfo.State) {
				case PartyState.Normal:					
					CoM.Party.Selected = Character;
					break;
				case PartyState.ChoosingLivingCharacter:
					if (GuiPartyInfo.OnPartyMemberChoosen != null)
						GuiPartyInfo.OnPartyMemberChoosen(Character);
					GuiPartyInfo.State = PartyState.Normal;
					break;
			}
		}

		public override void DoDoubleClick()
		{
			base.DoDoubleClick();
			if (ParentPartyInfo != null && ParentPartyInfo.OnPartyMemberDoubleClicked != null)
				ParentPartyInfo.OnPartyMemberDoubleClicked(this, null);
		}

		/** Returns true only if the character this panel is displaying is the currently selected character in the party */
		private bool isSelected { 
			get {
				return ((Character != null) && (CoM.Party.Selected == Character));
			} 
		}

		private void setCharacter(MDRCharacter newCharacter)
		{
			if (_character == newCharacter)
				return;
			releaseTriggers();
			_character = newCharacter;

			primaryAction.Character = newCharacter;
			secondaryAction.Character = newCharacter;

			addTriggers();
		}

		private void releaseTriggers()
		{
			if (Character != null) {
				Character.OnReceiveDamage -= addSplat;
				Character.OnReceiveHealing -= addSplat;
				Character.OnWasMissed -= addSwipe;
			}
		}

		private void addTriggers()
		{
			if (Character != null) {
				Character.OnReceiveDamage += addSplat;
				Character.OnReceiveHealing += addSplat;
				Character.OnWasMissed += addSwipe;
			}
		}

		/** Adds a damage splat to characters frame. */
		private void addSplat(DamageInfo damage)
		{		
			Add(GuiSplat.CreateSplat(damage, portrait.Frame));
		}

		/** Adds a miss swipe to characters frame. */
		private void addSwipe(MDRActor source)
		{					
			var newSwipe = new GuiAttackSwipe(0.5f, 0.05f);
			newSwipe.X = 3 + Util.Roll(5);
			newSwipe.Y = 1 + Util.Roll(5);
			Add(newSwipe);		
		}

		public override void Update()
		{			
			base.Update();

			switch (GuiPartyInfo.State) {
				case PartyState.Normal:					
					OuterShadow = false;
					break;
				case PartyState.ChoosingLivingCharacter:										
					OuterShadow = !Character.IsDead;
					break;
				case PartyState.ChoosingDeadCharacter:										
					OuterShadow = Character.IsDead;
					break;

			}

			Sync();	
		}

		/** Syncs control with character */
		private void Sync()
		{
			if (Character != null) {
				DragDropEnabled = true;
				_ddContent = this;  // use ourselves as our dragable content
				portrait.Sprite = Character.Portrait;

				levelIcon.Visible = Character.CurrentMembership.ReqXP <= 0 && (!Character.CurrentMembership.IsPinned);
				pinIcon.Visible = Character.CurrentMembership.IsPinned;

				hpBar.Progress = (float)Character.Hits / Character.MaxHits;
				spBar.Progress = (float)Character.Spells / Character.MaxSpells;

				stats.Caption =
					"HP " + Character.Hits + " / " + Character.MaxHits + "\n" + "SP " + Character.Spells;

				var selectedColor = isSelected ? Colors.CharacterInfoPanelSelected : Colors.CharacterInfoPanel;

				var effectTransform = ColorTransform.Identity;

				if (Character.Poisoned) {					
					effectTransform = ColorTransform.BlackAndWhite + ColorTransform.Multiply(new Color(0.5f, 0.75f, 0.5f));
				}
				if (Character.Diseased) {							
					effectTransform = ColorTransform.BlackAndWhite + ColorTransform.Multiply(new Color(0.75f, 0.75f, 0.5f));
				}
				if (Character.IsDead) {					
					effectTransform = ColorTransform.BlackAndWhite + ColorTransform.Multiply(new Color(1f, 1f, 1f));
				}

				if (Feedback != DDFeedback.Normal)
					effectTransform += ColorTransform.Multiply(new Color(1.5f, 1.5f, 1.5f));

				Color = selectedColor;
				portrait.ColorTransform = effectTransform;
				levelIcon.ColorTransform = effectTransform;
				pinIcon.ColorTransform = effectTransform;

			} else {
				DragDropEnabled = false;
				Color = Color.clear;
				_ddContent = null;
				portrait.Sprite = null;
				stats.Caption = "";
				hpBar.Progress = 0;
				spBar.Progress = 0;
			}
		}

		public override void Destroy()
		{
			Character = null;
			base.Destroy();
		}

		#region IDragDrop implementation

		override protected void SetDDContent(GuiComponent value)
		{
			if (value is GuiItem) {
				Character.GiveItem((value as GuiItem).ItemInstance);
			}
		}

		override public bool CanReceive(GuiComponent value)
		{
			if (value is GuiItem)
				return true;
			if (value is GuiCharacterFrame)
				return true;
			if (value is GuiGold)
				return true;
			return false;
		}

		/** Override transaction to give player the item, gold etc */
		override public bool Transact(IDragDrop source)
		{
			if (source.DDContent is GuiGold) {
				Trace.Log("transfering gold...");
				var gold = (source.DDContent as GuiGold);
				Character.CreditGold(gold.Amount, true);
				gold.DebitSource();
				return true;
			}
			if (source is GuiItemSlot) {
				DDContent = source.DDContent;
				source.DDContent = new GuiItem();
				return true;
			}
			if (source is GuiCharacterFrame) {
				//swap characters
				CoM.Party.SwapCharacters(this.Character, (source as GuiCharacterFrame).Character);
				CoM.Party.Selected = (source as GuiCharacterFrame).Character;
				return true;
			}
			return false;
		}

		#endregion
	}

}

