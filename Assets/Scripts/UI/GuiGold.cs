
using Mordor;

namespace UI
{
	public class GuiGold : GuiImage
	{
		public MDRCharacter Source;

		public int Amount { get { return Source.GoldInBank; } }

		public GuiGold() : base(0, 0, CoM.Instance.IconSprites["Coin_Pile"])
		{
			
		}

		public void DebitSource()
		{
			Source.GoldInBank -= Amount;
		}
	}
}