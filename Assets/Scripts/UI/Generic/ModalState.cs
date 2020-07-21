
using System.Collections.Generic;

using UnityEngine;

using UI.Generic;

namespace UI.Generic
{
	/** Used for states that block control. */
	public class ModalState : GuiState
	{
		protected GuiWindow Window;

		/** Creates a new game state.  To be made active it must be pushed to the top of the game state stack (Game.push(x)) */
		public ModalState(string title)
			: base(title)
		{
			TransparentDraw = true;
					
			GuiFillRect background = new GuiFillRect(0, 0, 0, 0, new Color(0.0f, 0.0f, 0.0f, 0.75f));

			background.Align = GuiAlignment.Full;

			Window = new GuiWindow(400, 400, title);

			Window.Background.Color = Color.gray;

			Add(background);
			Add(Window, 0, 0, true);
		}
	}

	public delegate void SimpleEvent();

	/** 
	 * Displays a yes/no decision window with text.  User must press either "YES" or "NO" to continue 
	 */
	public class ModalDecisionState : ModalState
	{
		public bool Result = false;

		public event SimpleEvent OnYes;
		public event SimpleEvent OnNo;

		public ModalDecisionState(string title, string text)
			: base(title)
		{	
			GuiLabel Text = new GuiLabel(10, 10, text, (int)Window.ContentsBounds.width - 20, (int)Window.Height - 20 - 70);
			Text.TextAlign = TextAnchor.MiddleCenter;
			Text.WordWrap = true;
			
			GuiButton YesButton = new GuiButton("Yes", 120, 30);
			YesButton.OnMouseClicked += delegate {
				Result = true;
				Close();
				doYes();
			};

			GuiButton NoButton = new GuiButton("No", 120, 30);
			NoButton.OnMouseClicked += delegate {
				Result = false;
				Close();
				doNo();
			};

			Window.Add(Text);
			Window.Add(YesButton, -20, -20);
			Window.Add(NoButton, 20, -20);
		}

		private void doYes()
		{
			if (OnYes != null)
				OnYes();
		}

		private void doNo()
		{
			if (OnNo != null)
				OnNo();
		}

	}

	/** Displays a confirmation window with text.  User must press "OK" to continue. */
	public class ModalNotificaionState : ModalState
	{
		public GuiButton ConfirmationButton;

		// todo: this has all kinds of problems with auto sizing
		public ModalNotificaionState(string title, string text, TextAnchor textAlignment = TextAnchor.MiddleCenter, int width = 400)
			: base(title)
		{	
			Window.Width = width;
			GuiLabel Text = new GuiLabel("", (int)Window.ContentsBounds.width - 20);
			Text.WordWrap = true;
			Text.TextAlign = textAlignment;
			Text.Caption = text;

			Window.Height = Text.Height + 20 + 45 + 30;

			// Create scroll box for really large messages.
			if (Window.Height > width * 0.75f) {
				Text.Width = width - 60; // make room for scroller
				Window.Height = (int)(width * 0.75f);
				var scrollBox = new GuiScrollableArea((int)Window.ContentsFrame.width - 10, (int)Window.ContentsFrame.height - 60, ScrollMode.VerticalOnly) {
					X = 10,
					Y = 10
				};
				scrollBox.ContentsScrollRect.height = Text.Height + 40;
				scrollBox.Add(Text);
				Window.Add(scrollBox);
			} else {
				Window.Add(Text, 10, 15);
				// this is because sometimes the width estimation is wrong and text wraps incorrently without it.
				Text.Width += 8;
			}

			ConfirmationButton = new GuiButton("OK", 150, 30);
			ConfirmationButton.OnMouseClicked += delegate {
				Close();
			};
			Window.Add(ConfirmationButton, 0, -12);

			DefaultControl = ConfirmationButton;

			PositionComponent(Window, 0, 0);
		}
	}

	/** Displays a modal window with a list of options, player can select one. */
	public class ModalOptionListState<T> : ModalState
	{
		public T Result;

		/**
		 * Creates a new ModalList state.
		 * 
		 * @param title Title to display at top of list
		 * @param objects A list of objects that will be selected from
		 * @param canCancel If true a cancel button is included which will close the window and return a null result
		 */
		public ModalOptionListState(string title, List<T> objects, bool canCancel = true)
			: base(title)
		{
			GuiListBox<T> ListBox = new GuiListBox<T>(10, 10, Window.Width - 20, Window.Height - 80);
			foreach (T item in objects)
				ListBox.Add(item);
			
			GuiButton ConfirmationButton = new GuiButton("OK", 120, 30);
			ConfirmationButton.OnMouseClicked += delegate {
				Result = ListBox.Selected;
				Close();
			};

			GuiButton CancelButton = new GuiButton("Cancel", 120, 30);
			CancelButton.OnMouseClicked += delegate {
				Result = default(T);
				Close();
			};
			
			Window.Add(ListBox);
			Window.Add(ConfirmationButton, -20, -20);
			if (canCancel)
				Window.Add(CancelButton, 20, -20);
		}
	}

}