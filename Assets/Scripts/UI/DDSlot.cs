
using UnityEngine;

using UI.DragDrop;

namespace UI
{
	/** 
	 * Base class for drag drop slots.  
	 * 
	 * Displays the slot with the folllowing layers
	 * 
	 * Background (the slots background image)
	 * Indicator (a solid color indicator underlay)
	 * Object (the object this slot contains)
	 * Ring (overlay ring used to hilight various objects)
	 * 
	 * The slot is also able to give feedback on receiving about item
	 */
	public class DDSlot : DDContainer
	{
		/** The sprite used to draw this slot when it's empty. */
		protected Sprite EmptySlotSprite;

		/** The sprite used to draw this slot when it's full. */
		protected Sprite FullSlotSprite;

		/** The sprite used to draw the hilight ring over this slot. */
		protected Sprite RingSprite;

		/** Drawn over slot.  Used for shadows, frames etc */
		protected Sprite OverlaySprite;
		
		/** Color to use to display a halo / ring over the container.  Clear for none */
		protected Color RingColor = Color.clear;
		
		/** Color to draw behind the object */ 
		protected Color IndicatorColor = Color.clear;
	
		/** If true the ring color will be set to yellow when the mouse is over a selected slot */
		protected bool ShowMouseOverRing;

		public DDSlot(int x, int y)
			: base(42, 42)
		{
			X = x;
			Y = y;
			EmptySlotSprite = ResourceManager.GetSprite("Icons/SlotEmpty"); 
			FullSlotSprite = ResourceManager.GetSprite("Icons/SlotFull"); 
			RingSprite = ResourceManager.GetSprite("Icons/SlotRing"); 
		}

		/** Update our Item to reflect the contents of our slot */
		public override void Update()
		{
			base.Update();
			
			RingColor = Color.clear;
			
			if (IsMouseOver && ShowMouseOverRing && !(CoM.TouchDevice))
				RingColor = Color.Lerp(RingColor, Color.yellow, 0.85f);
			
		}

		/** Draws the slot. */
		protected virtual void drawSlot()
		{
			var sprite = IsEmpty ? EmptySlotSprite : (FullSlotSprite ?? EmptySlotSprite);
			if (sprite != null) {
				SmartUI.Draw(X, Y, sprite);
			}
		}

		/** Draws the indicator under the object */
		protected virtual void drawIndicator()
		{
			if (IndicatorColor != Color.clear) {
				var backgroundSprite = ResourceManager.GetSprite("Icons/SlotInlay");
				SmartUI.PushAndSetColor(IndicatorColor);
				SmartUI.Draw(X, Y, backgroundSprite);
				SmartUI.PopColor();
			}
		}

		protected virtual void drawOverlay()
		{
			if (OverlaySprite != null)
				SmartUI.Draw(X, Y, OverlaySprite);
		}

		/** Draws hilight ring over slot. */
		protected virtual void drawRing()
		{
			if (RingColor != Color.clear && RingSprite != null) {				
				Color oldColor = SmartUI.Color;
				SmartUI.Color = RingColor;
				SmartUI.DrawSliced(new Rect(X, Y, Width, Height), RingSprite, new RectOffset(4, 4, 4, 4));
				SmartUI.Color = oldColor;
			}
		}

		/** Override drawContents to display a halo on quality items */
		protected override void DrawBackground()
		{					
			drawSlot();
			drawIndicator();
		}

		public override void Draw()
		{
			base.Draw();
			drawOverlay();
			drawRing();

		}
	}
}