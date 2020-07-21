using UnityEngine;

namespace UI.DragDrop
{
	/** Container and functions perform a drag drop action */
	public class DDDragDrop : DDContainer
	{
		private static int MOUSE_OFFSET_X = 0;
		private static int MOUSE_OFFSET_Y = 0;

		// amount of offset (difference in location between mouse and item at drag point) to be used in final offset)
		// set to 0 to move item to mouse, set to 1 to have item's offset to mouse maintained.
		private static float MOUSE_RAW_OFFSET = 0.0f;

		/** Returns true if we are currently in a drag / drop operation */
		public bool IsDragging { get { return DDContent != null; } }

		private IDragDrop origionalSender;

		/** Offset is used to position the dragging object correctly under the mouse */
		private Vector2 offset = new Vector2();

		/** These variables delay the drag drop until the mouse moves a little */
		private IDragDrop initialSource;
		private float initialTime;
		private int initialX;
		private int initialY;

		public DDDragDrop()
			: base(64, 64)
		{
			EnableBackground = false;
		}

		/** Simple draw for debuging, you will probably want to override this */
		public override void Draw()
		{
			// draw our object 
			if (DDContent != null) {
				// position ourselves under the mouse 
				DDContent.X = (int)(Mouse.Position.x + offset.x);
				DDContent.Y = (int)(Mouse.Position.y + offset.y);
				DDContent.Draw();
			}
			UpdateDragDrop();
		}

		/** 
		 * Begins the drag / drop process 
  		 * @param forceStart Forces drag drop to begin now rather than waiting for the mouse to move a little
		 */
		public void BeginDragDrop(IDragDrop source, bool forceStart = false)
		{
			if (IsDragging)
				return;
			if (source.DDContent == null)
				return;
			if (source.DDContent.IsEmpty)
				return;
				
			// make sure we've moved a little
			if (!forceStart) {
				if (initialSource == null) {
					initialSource = source;
					initialTime = Time.time;
					initialX = (int)Mouse.Position.x;
					initialY = (int)Mouse.Position.y;
					return;
				} else {
					if ((new Vector2(Mouse.Position.x - initialX, Mouse.Position.y - initialY).magnitude < Settings.Advanced.DragMovementThreshold) && (Time.time - initialTime < Settings.Advanced.DragDelay))
						return;
				}
			}

			// begin the drag, and make a copy of this object to show under mouse 
			if (source is GuiComponent) {
				offset.x = MOUSE_OFFSET_X - MOUSE_RAW_OFFSET * (Mouse.Position.x - (source as GuiComponent).AbsoluteBounds.x);
				offset.y = MOUSE_OFFSET_Y - MOUSE_RAW_OFFSET * (Mouse.Position.y - (source as GuiComponent).AbsoluteBounds.y);
			}
			DDContent = (GuiComponent)source.DDContent.Clone();
			origionalSender = source;
			source.DDContent.BeingDragged = true;

		}

		/** Ends the drag / drop process */
		public void EndDragDrop(IDragDrop destination)
		{
			// make sure we have something to drop
			if (!IsDragging)
				return;

			if (origionalSender.DDContent != null)
				origionalSender.DDContent.BeingDragged = false;

			// transfer object destination
			if (destination.CanReceive(DDContent) && origionalSender.CanSend(destination)) {
				destination.Transact(origionalSender);
				DDContent = null;
				origionalSender = null;
			}
		}

		/** Cancels current drag drop action, restoring payload to origional container */
		public void Cancel()
		{
			if (origionalSender.DDContent != null)
				origionalSender.DDContent.BeingDragged = false;
			DDContent = null;
		}

		/** Checks if we are dragging an object.  If so draws it, and tests for when to release it */
		private void UpdateDragDrop()
		{
			// reset initial source when mousebutton is up so that when we click next time we can detect the fresh click 
			if (Input.GetMouseButton(0) == false)
				initialSource = null;

			// release our object, objects update first so if we get this this point no one has 'accepted' the payload... just cancel the trade.
			if (IsDragging && Input.GetMouseButtonUp(0))
				Cancel();


		}
		
	}
}
