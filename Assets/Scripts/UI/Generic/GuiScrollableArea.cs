using UnityEngine;

namespace UI
{
	public enum ScrollMode
	{
		None,
		Auto,
		VerticalOnly,
		HorizonalOnly,
		Both
	}

	/** Defines a componenet with an internal scrollable area */
	public class GuiScrollableArea : GuiContainer
	{
		/** Rectangle defining the size of the scrollable area, of which this control displays a portion of. */
		public Rect ContentsScrollRect;

		/** The scrolling offset for scrollable controls */
		public Vector2 ScrollLocation;

		public ScrollMode ScrollMode;

		/** Used for touch based scrolling */
		protected Vector3 Velocity;

		public GuiScrollableArea(int width = 100, int height = 50, ScrollMode scrollMode = ScrollMode.Auto)
			: base(width, height)
		{
			EnableBackground = false;
			ScrollLocation = new Vector2(0.0f, 0.0f);
			ScrollMode = scrollMode;

			switch (ScrollMode) {
				case ScrollMode.Auto:
				case ScrollMode.None:
					Style.padding = new RectOffset(0, 0, 0, 0);
					break;
				case ScrollMode.VerticalOnly:
					Style.padding = new RectOffset(0, (int)GUI.skin.verticalScrollbar.fixedWidth, 0, 0);
					break;
				case ScrollMode.HorizonalOnly:
					Style.padding = new RectOffset(0, 0, 0, (int)GUI.skin.horizontalScrollbar.fixedHeight);
					break;
				case ScrollMode.Both:
					Style.padding = new RectOffset(0, (int)GUI.skin.verticalScrollbar.fixedWidth, 0, (int)GUI.skin.horizontalScrollbar.fixedHeight);
					break;
			}

			ContentsScrollRect = new Rect(0, 0, ContentsBounds.width, ContentsBounds.height);
		}

		/** Enable touch to scroll, and mousewheel scroll */
		public override void Update()
		{
			base.Update();

			if (ScrollMode == ScrollMode.None)
				return;

			bool touchToScroll = Engine.TouchDevice;
			bool scrolling = false;
			float dragStrength = 10f;

			if (touchToScroll) {				
				dragStrength = 10f;
				if ((Input.touchCount >= 1) && (Depressed) && (!CoM.DragDrop.IsDragging)) {
					var touch = Input.GetTouch(0);
					Velocity = touch.deltaPosition / Time.deltaTime / Engine.DPIScale;
					scrolling = true;					
				} 

			} else {
				dragStrength = 10f;
				if (IsMouseInside && Input.mouseScrollDelta.magnitude != 0) {
					Vector3 impluse = Input.mouseScrollDelta * -500;
					Velocity = Velocity + impluse;
					scrolling = true;
				}

			}

			if (!scrolling) {
				Vector3 drag = -Velocity * Time.deltaTime * dragStrength;
				float mangnitude = Util.Clamp(drag.magnitude, 1, 1000);
				drag.Normalize();
				drag = drag * mangnitude;

				if (drag.magnitude > Velocity.magnitude)
					Velocity = new Vector3(0, 0, 0);
				else
					Velocity += drag;
			}
				
			ScrollLocation.x += Velocity.x * Time.deltaTime;
			ScrollLocation.y += Velocity.y * Time.deltaTime;

		}

		/** Draws the controls contents */
		public override void Draw()
		{
			if (EnableBackground) {
				SmartUI.Color = Color;
				DrawBackground();
				SmartUI.Color = Color.white;	
			}

			GUIStyle vsb = (ScrollMode == ScrollMode.HorizonalOnly || ScrollMode == ScrollMode.None) ? GUIStyle.none : GUI.skin.verticalScrollbar;
			GUIStyle hsb = (ScrollMode == ScrollMode.VerticalOnly || ScrollMode == ScrollMode.None) ? GUIStyle.none : GUI.skin.horizontalScrollbar;

			ScrollLocation = SmartUI.BeginScrollView(Bounds, ScrollLocation, ContentsScrollRect, hsb, vsb, false);

			DrawContents();
			SmartUI.EndScrollView();
		}

		
		/** Transforms given frame from our co-rd system to parents co-ord system */
		override public Rect TransformRect(Rect frame, bool contentsArea = true)
		{
			var referenceFrame = contentsArea ? ContentsBounds : Bounds;
			Rect result = new Rect(referenceFrame.x + frame.x - ScrollLocation.x, referenceFrame.y + frame.y - ScrollLocation.y, frame.width, frame.height);
			return (Parent != null) ? Parent.TransformRect(result) : result;
		}

		/**
		 * Sizes scroll area to fit all children 
		 */
		override public void FitToChildren(int minPadding = 0)
		{
			int maxX = 0;
			int maxY = 0;
			foreach (GuiComponent child in Children) {
				if (child.Align == GuiAlignment.None) {
					if (child.Bounds.xMax > maxX)
						maxX = (int)child.Bounds.xMax;
					if (child.Bounds.yMax > maxY)
						maxY = (int)child.Bounds.yMax;
				}
			}
			ContentsScrollRect.width = maxX;
			ContentsScrollRect.height = maxY;
		}

		
		/** 
		 * Returns true if any part of the given rectangle is currently visible within this container.  
		 * 
		 * @param rect the rectangle to check with co-ords relative to this container
		 */
		override public bool RectInBounds(Rect rect)
		{
			rect.x -= ScrollLocation.x;
			rect.y -= ScrollLocation.y;
			return ((rect.xMax > 0) && (rect.x < Width) && (rect.yMax > 0) && (rect.y < Height)); 
		}
	}
}