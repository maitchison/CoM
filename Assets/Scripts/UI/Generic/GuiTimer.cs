using System;
using UnityEngine;

namespace UI.Generic
{
	// an invisible timer that executes every timespan
	public class GuiTimer : GuiComponent
	{
		/** The number of seconds between executions */
		public float TimeSpan;

		public GuiEvent OnTimer;

		private float delay;

		/** 
		 * Creates a new timer.  Timer will execute every 'timespan' seconds 
		 * @param timespan the number of seconds between execution
		 */

		public GuiTimer(GuiEvent onTimer, float timeSpan = 1.0f) : base(0, 0)
		{
			delay = timeSpan;
			OnTimer = onTimer;
			TimeSpan = timeSpan;
		}

		public override void Update()
		{
			base.Update();
			delay -= Time.deltaTime;
			if (delay <= 0)
				Execute();
		}

		/** Executes the action */
		public void Execute()
		{
			delay += TimeSpan;
			if (OnTimer != null)
				OnTimer(this, new EventArgs());
		}

		public override void Draw()
		{
			//
		}
	}
}

