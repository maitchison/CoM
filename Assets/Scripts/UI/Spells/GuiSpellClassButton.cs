using System;
using Mordor;

namespace UI
{
	/** Button describing a spell class. */
	public class GuiSpellClassButton : GuiButton
	{
		private GuiImage icon;
		private GuiLabel info;

		public GuiSpellClassButton(MDRSpellClass spellClass)
			: base("")
		{			
			Style = Engine.GetStyleCopy("SquareButton");
			InnerShadow = true;
			CaptionDropShadow = true;
			Width = (int)(Style.padding.horizontal + spellClass.Icon.rect.width);
			Height = (int)(Style.padding.vertical + spellClass.Icon.rect.height);
			Image = new GuiImage(0, 0, spellClass.Icon);
			Caption = spellClass.ShortName;
		}
	}


}

