
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace UI
{
	public enum MessageBoxHideStyle
	{
		// message box fades with with opacity
		Fading,
		// message box fades with with opacity but is visible when mouse is over control
		FadingMouse,
		// message box moves out of view
		Retraction
	}

	/** An entry in the message log */
	public class MessageEntry
	{
		public string Message;
		public float TimeStamp;
		public Color Color;
		/** If true message will be faded out and removed */
		public bool FadeAndRemove;

		/** Creates a new message entry from string, if no timestamp is given the current time will be used */
		public MessageEntry(string message, float timestamp = 0)
		{
			if (timestamp == 0)
				timestamp = Time.time;
			Message = message;
			TimeStamp = timestamp;
			Color = Color.white;
		}

		/** Creates a new message entry from string, if no timestamp is given the current time will be used */
		public MessageEntry(string message, Color color)
		{			
			Message = message;
			TimeStamp = Time.time;
			Color = color;
		}

		/** Returns a copy of the message with the current color applied */
		public string FormattedMessage {
			get { return Color == Color.white ? Message : Util.Colorise(Util.AdjustColorCodes(Message, Color), Color); }
		}
	}

	public class GuiMessageBox : GuiWindow
	{
		/** If the message list belongs to us or not */
		protected bool OwnsMessages = true;

		/** The list of messages to display */
		public List<MessageEntry> Messages {
			get { return _messages; }
			set {
				_messages = value;
				OwnsMessages = false;
			}
		}

		/** If true scrollbox will automatically scroll to the bottom when new text is added */
		public bool AutoScrollToBottom;

		/** If non zero message box will fade out and be hidden after this many seconds of no message appearing */
		public float AutoHideTimeout = 0.0f;

		private List<MessageEntry> _messages;

		/** The maximum number of messages to show.  If the message window owns the message list the list will also be trimmed. */
		public int MaxMessages = 50;

		private GuiScrollableArea scrollBox;
		public GuiLabel Label;

		public MessageBoxHideStyle HideStyle;

		/** If true new messages will be displayed at the top instead of the bottom */
		public bool ReversedMessageText;

		/** If not zero messages will be removed after this number of seconds */
		public float AutoRemoveMessageTimeout = 0.0f;

		public bool ScrollOn;

		/** Time in seconds to fade message box in. */
		public float FadeInTime = 0.5f;
		/** Time in seconds to fade message box out. */
		public float FadeOutTime = 1f;

		/** If true message box will be composited first before fading out.  This won't work well if the background of the message box is transparient. */
		public bool CompositedFade = true;

		private float goalHideValue = 0;
		private float hideValue = 0;
		private int lastMessageCount = -1;
		private float mostRecentTimeStamp = -99;
		private float removeMessageTimer = 0;


		private float yoffset = 0;

		/** Creates a new message box */
		public GuiMessageBox(int width = 400, int height = 100, bool disableScrolling = false) : base(width, height)
		{			
			this.scrollBox = new GuiScrollableArea((int)ContentsBounds.width, (int)ContentsBounds.height, disableScrolling ? ScrollMode.None : ScrollMode.VerticalOnly);
			Add(scrollBox);
			this.Label = new GuiLabel(0, 0, "", (int)scrollBox.ContentsScrollRect.width);
			this.Label.FontColor = new Color(0.9f, 0.9f, 0.9f);
			this.Label.WordWrap = true;
			this.scrollBox.Add(Label);
			this.Label.FontSize = 14;
			this.HideStyle = MessageBoxHideStyle.FadingMouse;
			this.Messages = new List<MessageEntry>();
			fitScrollBox();
		}

	
		public void AddMessage(String item)
		{
			Messages.Add(new MessageEntry(item));
			removeMessageTimer = AutoRemoveMessageTimeout;
			Invalidate();
			if (AutoScrollToBottom)
				ScrollToBottom();
		}

		/** Forces text to be refreshed next update */
		public override void Invalidate()
		{
			base.Invalidate();
			lastMessageCount = -1;
		}

		/** Fits scrollbox to window size */
		private void fitScrollBox()
		{
			int additionalPadding = 0;

			if (WindowStyle == GuiWindowStyle.Normal)
				additionalPadding = 6;

			scrollBox.X = additionalPadding;
			scrollBox.Y = additionalPadding;
			scrollBox.Width = (int)ContentsBounds.width - additionalPadding * 2;
			scrollBox.Height = (int)ContentsBounds.height - additionalPadding * 2;
		}

		/** Removes any messages in excess of our limit.  But only if we own the message list */
		private void TrimMessages()
		{
			if (!OwnsMessages)
				return;

			if (Messages.Count > MaxMessages)
				Messages.RemoveRange(0, Messages.Count - MaxMessages);
		}

		/** Updates the text to display, and recalculates the scroll height */
		private void RefreshText()
		{			
			TrimMessages();

			int firstMessage = Util.ClampInt(Messages.Count - MaxMessages, 0, int.MaxValue);
			int lastMessage = Messages.Count;

			StringBuilder stringBuilder = new StringBuilder();

			if (ReversedMessageText) {
				for (int lp = lastMessage - 1; lp >= firstMessage; lp--)
					stringBuilder.Append(Messages[lp].FormattedMessage + "\n");
			} else {
				for (int lp = firstMessage; lp < lastMessage; lp++)
					stringBuilder.Append(Messages[lp].FormattedMessage + "\n");
			}
				
			string text = stringBuilder.ToString().TrimEnd('\n', ' ');
				
			Label.Caption = text;
			scrollBox.ContentsScrollRect.height = Label.Height;

			lastMessageCount = Messages.Count;
			if (Messages.Count > 0)
				mostRecentTimeStamp = Messages[Messages.Count - 1].TimeStamp;
		}

		private bool needsUpdate {
			get { return (Messages.Count != lastMessageCount); }
		}

		public override void Draw()
		{				
			if (hideValue >= 1f) {
				CompositeAlpha = 1f;
				return;
			}

			int oldY = Y;
			var oldAlpha = Color.a;
			var oldFontColor = Label.FontColor;

			var adjustedAlpha = 1f;
			switch (HideStyle) {
			case MessageBoxHideStyle.Fading:
			case MessageBoxHideStyle.FadingMouse:
				adjustedAlpha = (1f - hideValue);
				break;
			case MessageBoxHideStyle.Retraction:
				Y -= (int)(hideValue * Height);
				break;
			}

			Y -= (int)yoffset;

			if (CompositedFade)
				CompositeAlpha = adjustedAlpha;
			else {
				Color.a *= adjustedAlpha;
				Label.FontColor = Label.FontColor.Faded(adjustedAlpha);
			}

			base.Draw();

			Y = oldY;
			Color.a = oldAlpha;
			Label.FontColor = oldFontColor;
		}

		/** Returns the number of new messages added after the last RefreshText. */ 
		private int newMessageCount()
		{
			int result = 0;
			foreach (var message in Messages) {
				if (message.TimeStamp > mostRecentTimeStamp)
					result++;
			}
			return result;
		}

		public override void Update()
		{
			fitScrollBox();

			if (Messages == null) {
				Label.Caption = "";
				return;
			}

			yoffset = Util.Clamp(yoffset - Time.deltaTime * 100, 0, 32);

			// Detect any added messages 
			int newMessages = newMessageCount();
			if (newMessages > 0) {
				// a new message has been added, so invalidate.
				Invalidate();
				removeMessageTimer = AutoRemoveMessageTimeout;
				if (ScrollOn)
					yoffset += 16 * newMessages;
				if (AutoScrollToBottom)
					ScrollToBottom();
			}

			// Auto delete
			if (AutoRemoveMessageTimeout != 0.0f) {
				if (Messages.Count > 0) {
					removeMessageTimer -= Time.deltaTime;
					if (removeMessageTimer <= 0) {
						for (int lp = 0; lp < Messages.Count; lp++) {
							if (!Messages[lp].FadeAndRemove) {
								Messages[lp].FadeAndRemove = true;
								break;
							}
						}
						removeMessageTimer = 0.33f;
					}
				} 
			}

			// Retraction
			if (hideValue != goalHideValue) {

				if (hideValue < goalHideValue)
					hideValue = (FadeOutTime == 0) ? goalHideValue : Util.Clamp(hideValue + Time.deltaTime / FadeOutTime, 0, goalHideValue);

				if (hideValue > goalHideValue)
					hideValue = (FadeInTime == 0) ? goalHideValue : Util.Clamp(hideValue - Time.deltaTime / FadeInTime, goalHideValue, 1);
			}

			// Auto hide.
			if (AutoHideTimeout != 0.0f) {
				if (Time.time > mostRecentTimeStamp + AutoHideTimeout)
					goalHideValue = 1.0f;
				else
					goalHideValue = 0.0f;
			}
				
			// Fade out and remove messages.
			for (int lp = Messages.Count - 1; lp >= 0; lp--) {
				if (Messages[lp].FadeAndRemove) {
					Invalidate();
					Messages[lp].Color.a -= Time.deltaTime;
					if (Messages[lp].Color.a <= 0)
						Messages.RemoveAt(lp);
				}
			}
				
			// Refresh text if needed. 
			if (needsUpdate) {
				RefreshText();
			}

			// Auto fading
			if (HideStyle == MessageBoxHideStyle.FadingMouse) {
				if (IsMouseOver)
					goalHideValue = 0f;
			}

			base.Update();
		}

		/** Causes message box to scroll to the bottom most item */
		public void ScrollToBottom()
		{
			scrollBox.ScrollLocation.y = float.PositiveInfinity;
		}

		public string DebugTrace()
		{
			return string.Format("{0} {1} {2} {3} {4} {5}", hideValue, goalHideValue, hasRenderTexture, CompositeAlpha, Color, Time.frameCount);
		}
	}
}

