using System;
using UI.Generic;
using UI;
using Engines;
using UnityEngine;

namespace State
{
	public class CombatLogState : ModalState
	{
		public CombatLogState()
			: base("Combat Log")
		{
			Window.Width = 800;
			Window.Height = 600;

			PositionComponent(Window, 0, 0);

			Window.Add(Util.CreateBackButton("Close"), 0, -10);

			var combatMessageBox = new GuiMessageBox((int)Window.ContentsFrame.width, (int)Window.ContentsFrame.height - 50);	
			//combatMessageBox.Messages = CombatEngine.CombatLog;
			combatMessageBox.Style = Engine.GetStyleCopy("Frame");
			combatMessageBox.Label.TextAlign = TextAnchor.UpperLeft;
			combatMessageBox.ReversedMessageText = true;
			combatMessageBox.Label.DropShadow = true;
			combatMessageBox.Color = Color.white.Faded(0.5f);
			combatMessageBox.MaxMessages = 250;
			Window.Add(combatMessageBox, 0, 1); 
		}


	}
}

