
using System;
 
using UnityEngine;

namespace UI.Generic
{
	/** Represents a game state */
	public class GuiState : GuiContainer
	{
		/** Allows states under this state to be drawn. */
		public bool TransparentDraw = false;

		/** Updates state even if not topmost. */
		public bool BackgroundUpdate = false;

		/** Control to take focus when state is shown */
		public GuiComponent DefaultControl;

		/** Called before the state closes */
		public GuiEvent OnStateClose;
		/** Called before the state hides */
		public GuiEvent OnStateHide;
		/** Called before the states shows */
		public GuiEvent OnStateShow;

		/** Creates a new game state.  To be made active it must be pushed to the top of the game state stack (Game.push(x)) */
		public GuiState(string name)
			: base(Screen.width, Screen.height)
		{
			Align = GuiAlignment.Full;
			Name = name;
			EnableBackground = false;
		}

		virtual internal void onResolutionChange()
		{
			UpdateAlignment();		
		}

		/** Called when state is made visible */
		virtual public void Show()
		{
			if (OnStateShow != null)
				OnStateShow(this, new EventArgs());

			Visible = true;
			Active = true;

			GuiComponent.FocusedControl = DefaultControl;

			ForceAlignment();
		}

		/** Returns if this is the topmost state or not */
		public bool TopMost()
		{
			return (Engine.CurrentState == this);
		}

		/** Called when state is no longer in view, but potentially still in the state stack */
		virtual public void Hide()
		{
			if (OnStateHide != null)
				OnStateHide(this, new EventArgs());
			Visible = false;
		}

		/** Closes the state, it will be removed from statelist next update, also triggers hide */
		virtual public void Close()
		{
			Trace.Log("State [{0}] is closing.", Name);
			if (OnStateClose != null)
				OnStateClose(this, new EventArgs());
			Hide();
			Active = false;
		}

		public override void Update()
		{
			Engine.PerformanceStatsInProgress.StateUpdates++;
			base.Update();
		}

		public override void Draw()
		{
			Engine.PerformanceStatsInProgress.StateDraws++;
			base.Draw();
		}

	}
}
