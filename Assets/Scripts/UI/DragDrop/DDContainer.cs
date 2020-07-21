namespace UI.DragDrop
{

	/** UI componenet that can hold dragable objects */
	public class DDContainer : GuiContainer
	{
		public DDContainer(int width, int height)
			: base(width, height)
		{
			DragDropEnabled = true;
		}

		/** The the contents of this slot (i.e. the item in it) */
		public override void DrawContents()
		{
			// draw our object 
			if (DDContent != null) {
				DDContent.Draw();
			}
			base.DrawContents();
		}

		public override string ToString()
		{
			return "Container: " + DDContent;
		}

		/** Update our contents */
		public override void Update()
		{
			base.Update();
			if (DDContent != null)
				DDContent.Update();
		}

		public override bool IsEmpty {
			get {
				return DDContent == null || DDContent.IsEmpty;
			}
		}

		#region IDragDrop

		/** Checks if this type of object can be accepted by this type of container */
		override public bool CanReceive(GuiComponent value)
		{
			return true;
		}

		#endregion


	}
}

