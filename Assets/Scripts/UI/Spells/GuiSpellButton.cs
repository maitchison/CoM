using System;
using Mordor;
using Data;

namespace UI
{
	/**
	 * A button representing a characters spell.  The spell can be dragged to an action slot.
	 * By default no action is taken when the spell is clicked, however the creater can hook the "onMouseClicked" 
	 * to selected or cast the spell as needed.
	 */
	public class GuiSpellButton : GuiSimpleImageButton
	{
		private static GuiSpellToolTip toolTip;

		private MDRSpell _spell;

		public GuiSpellButton(MDRSpell spell)
			: base(spell.Icon)
		{
			_spell = spell;

			DragDropEnabled = true;

			ShowToolTipOnHover = true;

			MDRAction action = new MDRAction(ActionType.Spell, spell.ID);
			_ddContent = new GuiAction(action);
		}

		protected override bool showToolTip()
		{
			if (toolTip == null) {
				toolTip = new GuiSpellToolTip();
				gameState.Add(toolTip);
			}

			toolTip.Spell = _spell;
			toolTip.PositionToMouse();

			return true;
		}

		#region IDragDrop implementation

		override protected void SetDDContent(GuiComponent value)
		{
			// can not receive
		}

		override public bool CanReceive(GuiComponent value)
		{
			return false;
		}

		#endregion
	}

}

