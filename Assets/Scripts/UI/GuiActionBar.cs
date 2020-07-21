
using Mordor;
using Data;

using UnityEngine;
using System;

namespace UI
{
	/** Holds a single action */
	public class GuiActionSlot : DDSlot
	{
		private static GuiSpellToolTip toolTip;

		public MDRAction Action { get { return (BufferIndex >= 0) ? Character.Buffers[BufferIndex] : null; } }

		private static Sprite SLOT_OVERLAY_SPRITE;

		protected MDRCharacter Character;
		protected int BufferIndex;

		public GuiActionSlot(MDRCharacter character, int bufferIndex)
			: base(0, 0)
		{
			Width = GuiAction.SIZE + 1;
			Height = GuiAction.SIZE + 1;			

			InnerShadow = false;

			if (character == null)
				throw new ArgumentNullException("character");

			EnableBackground = false;

			SLOT_OVERLAY_SPRITE = ResourceManager.GetSprite("Icons/ActionSlotFrame");				

			Character = character;
			BufferIndex = bufferIndex;

			ShowToolTipOnHover = true;

			Sync();
		}

		protected override bool showToolTip()
		{
			if (Action.Spell == null)
				return false;

			if (toolTip == null) {
				toolTip = new GuiSpellToolTip();
				gameState.Add(toolTip);
			}

			toolTip.Spell = Action.Spell;
			toolTip.PositionToMouse();

			return true;
		}


		/** Creates an GuiAction based on this slots Action property. */
		protected void Sync()
		{
			if (Action == null) {
				_ddContent = null;
				return;
			} 

			var content = (DDContent as GuiAction);

			if ((content == null) || !content.Action.CompareTo(Action)) {
				_ddContent = new GuiAction(Action.Clone());
			}
		}

		/** Make sure our contents matches our slot */
		public override void Update()
		{
			Sync();

			base.Update();

			OverlaySprite = IsEmpty ? null : SLOT_OVERLAY_SPRITE;

			if (Character.CurrentAction == this.Action)
				RingColor = Color.yellow;

		}

		/** Update our characters buffer slot to match new content. */
		protected override void SetDDContent(GuiComponent value)
		{
			base.SetDDContent(value);

			if (value == null)
				Action.Clear();
			else if (value is GuiAction)
				Action.CopyFrom(((GuiAction)value).Action);
		}
			
	}

	/** A quick shortcut action slot.  Can not be assigned to via drag/drop */
	public class GuiQuickAction : GuiButton
	{
		private static Sprite OVERLAY_STANDARD = ResourceManager.GetSprite("Icons/QuickSlotStandard");
		private static Sprite OVERLAY_SELECTED = ResourceManager.GetSprite("Icons/QuickSlotSelected");

		public MDRCharacter Character { get { return _character; } set { _character = value; } }

		private MDRCharacter _character;

		public int BufferIndex { get { return _bufferIndex; } set { setBufferIndex(value); } }

		private int _bufferIndex;

		private MDRAction Action { get { return Character == null ? null : Character.Buffers[BufferIndex]; } }

		public GuiQuickAction(MDRCharacter character, int bufferIndex)
			: base("", 24, 24)
		{
			Style = GUIStyle.none;
			Image = new GuiImage(0, 0, null);		
			Character = character;
			BufferIndex = bufferIndex;

			OnMouseClicked += delegate {
				if (Action != null)
					activate();
			};				
		}

		/** Activates an action on the action bar. */
		private void activate()
		{			
			Character.CurrentAction = Action;
			if (Action != null && Action.NeedsTargetSelection) {
				Action.SpecifiedTarget = null;
				GuiPartyInfo.State = PartyState.ChoosingLivingCharacter;
			}												
		}

		public override void Draw()
		{
			base.Draw();
			var overlaySprite = isCurrent ? OVERLAY_SELECTED : OVERLAY_STANDARD;
			if (overlaySprite != null)
				SmartUI.Draw(X - 2, Y - 2, overlaySprite);
		}

		public override void Update()
		{
			base.Update();
			updateSprite();		
		}

		/** If this quick action is currently active. */
		private bool isCurrent {
			get { return Character == null ? false : Character.CurrentAction == Action; }
		}


		private void updateSprite()
		{
			Image.Sprite = Action == null ? null : Action.Icon;
			if (Image.Sprite != null) {				
				Image.Scale = Width / (Image.Sprite.textureRect.width);
			}
		}

		private void setBufferIndex(int value)
		{
			if (value < 0 || value >= 10) {
				throw new Exception(string.Format("Invalid parameter, can not set buffer index to {0}", value));
			}
			_bufferIndex = value;
		}

	}

	/** 
	 * Stores characters hot key actions 
	 */
	public class GuiActionBar : GuiWindow
	{
		public static int NUMBER_OF_ACTIONS = 10;
		public static int HEIGHT = 40;
		public static int WIDTH = NUMBER_OF_ACTIONS * (GuiAction.SIZE + 5) + 10;

		private GuiActionSlot[] ActionContainerList;
		private GuiContainer SlotsContainer;

		/** The character for whom to display the actions of */
		public MDRCharacter Character { get { return _character; } set { setCharacter(value); } }

		private MDRCharacter _character;

		/** Creates a new actionbar */
		public GuiActionBar()
			: base(0, 0)
		{
			Sprite sprite = ResourceManager.GetSprite("Gui/ActionBar", true);

			Width = (int)sprite.rect.width;
			Height = (int)sprite.rect.height;

			Style = Engine.CreateStyle(sprite);
			Style.padding = new RectOffset(6, 6, 8, 8);

			SlotsContainer = new GuiContainer(Width, Height);
			Add(SlotsContainer);

			ActionContainerList = new GuiActionSlot[NUMBER_OF_ACTIONS];

			GuiPartyInfo.OnPartyMemberChoosen += setSpellTarget;

		}

		public override void Destroy()
		{
			base.Destroy();
			GuiPartyInfo.OnPartyMemberChoosen -= setSpellTarget;
		}

		public override void Update()
		{
			base.Update();
			processKeys();
		}

		// ------------------------------------------------
		// Private
		// ------------------------------------------------

		private void setSpellTarget(MDRCharacter selectedCharacter)
		{			
			GuiPartyInfo.State = PartyState.Normal;

			if (selectedCharacter == null || Character == null)
				return;
			
			if (Character.CurrentAction == null) {
				Trace.LogWarning("Character selected, but no action is defined.");
				return;
			}

			Character.CurrentAction.SpecifiedTarget = selectedCharacter;				

			Trace.LogDebug("Setting target for action {0} to {1}", Character.CurrentAction, selectedCharacter);
		}

		private void processKeys()
		{
			for (int lp = 1; lp <= NUMBER_OF_ACTIONS; lp++)
				if (Input.GetKeyUp(Extentions.KeyCodeAlpha(lp % 10))) {
					var action = Character.Buffers[lp - 1];
					if (!action.IsEmpty)
						activate(action);
				}					
		}

		/** Activates an action on the action bar. */
		private void activate(MDRAction action)
		{			
			Character.CurrentAction = action;
			if (action != null && action.NeedsTargetSelection) {
				action.SpecifiedTarget = null;
				GuiPartyInfo.State = PartyState.ChoosingLivingCharacter;
			}												
		}

		/**
		 * Sets the actionbar to display the actions of given character 
		 */
		private void setCharacter(MDRCharacter value)
		{
			if (_character == value)
				return;
			_character = value;
			Sync();
		}

		/**
		 * Creates GuiAction slots for currently set character 
		 */
		private void Sync()
		{
			SlotsContainer.Clear();

			if (Character == null)
				return;

			for (int lp = 0; lp < NUMBER_OF_ACTIONS; lp++) {
				var slot = new GuiActionSlot(Character, lp) { X = 2 + lp * (GuiAction.SIZE + 4) };					
				ActionContainerList[lp] = slot;

				ActionContainerList[lp].OnMouseClicked += delegate {				
					activate(slot.Action);
				};


				SlotsContainer.Add(ActionContainerList[lp]);
			}
		}
	}
}