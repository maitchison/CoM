namespace UI
{
	/** Represents a single page on a GuiTabControl.  A page can only exist on one tab control at a time. */
	public class GuiTabPage : GuiContainer
	{
		/** The tile of this page.  Display on the title bar of the parent tab control when selected */ 
		public string Title;

		/** The index this page is within a tab control. */
		public int Index;

		public GuiTabPage(int x = 0, int y = 0, int width = 100, int height = 100) : base(width, height)
		{
			X = x;
			Y = y;
			EnableBackground = false;
		}
	}
}
