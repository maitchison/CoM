using System;
using UnityEngine;
using Mordor;

namespace UI
{
	public class GuiSplat: GuiFadingImage
	{
		protected Sprite sprite;

		public GuiSplat(Sprite sprite)
			: base(sprite)
		{									
		}

		/** 
		 * Creates the appropritate splat for given damage profile. 
		 * @frame The frame of the object we are going to add this splat to.
		 */
		public static GuiSplat CreateSplat(DamageInfo damage, Rect frame)
		{
			float extraTime = (0.1f * Mathf.Log(damage.Amount, 2));

			GuiSplat result;

			bool isSpell = damage.DamageType.ID != 0;

			if (isSpell) {
				result = new GuiSpellSplat(damage.DamageType);
			} else
				result = new GuiDamageSplat();				

			result.Value = damage.Amount;
			result.ShowDelay = 0.05f;

			int varience = 5;

			result.X = varience - Util.Roll(varience * 2);
			result.Y = varience - Util.Roll(varience * 2);


			result.X += (int)frame.xMax - 5;
			result.Y += (int)frame.yMax - 5;

			if (isSpell) {
				result.Life = 1.25f;
			} else {
				result.Life = 0.5f + extraTime;
			}

			result.X -= (int)result.Frame.width / 2;
			result.Y -= (int)result.Frame.height / 2;

			return result;
		}
	}

	public class GuiSpellSplat : GuiSplat
	{
		public GuiSpellSplat(MDRDamageType damageType)
			: base(ResourceManager.GetSprite(damageType.SpriteName))
		{								
			Label.FontColor = Color.white;
		}
	}

	public class GuiDamageSplat : GuiSplat
	{
		public GuiDamageSplat()
			: base(ResourceManager.GetSprite("Icons/Splat"))
		{			
			Image.Color = new Color(0.6f, 0.25f, 0.15f);
			Image.Rotation = Util.Roll(360);
			Scale = 0.4f;
		}
	}

	public class GuiAttackSwipe : GuiFadingImage
	{
		private static Sprite swipeSprite {
			get {
				_swipeSprite = _swipeSprite ?? ResourceManager.GetSprite("Icons/Swish");		
				return _swipeSprite;
			}
		}

		private static Sprite _swipeSprite;

		public GuiAttackSwipe(float life = 2f, float showDelay = 0.0f)
			: base(swipeSprite, 0, life, showDelay)
		{
			Image.Rotation = Util.Roll(360);
			Scale = 0.4f;
		}
	}
}