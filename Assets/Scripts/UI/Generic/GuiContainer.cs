
using System;
using System.Collections.Generic;

using UnityEngine;

namespace UI
{
	/** A simple container fully aligned to given parent.  Useful for grouping components together */
	public class GuiGroup : GuiContainer
	{
		public GuiGroup(GuiContainer parent)
			: base(0, 0)
		{
			EnableBackground = false;
			Align = GuiAlignment.Full;
			parent.Add(this);
		}
	}

	public enum CacheMode
	{
		/** Control will be rendered each  frame. */
		Disabled,
		/** Control will be cached and re-rendered on demand.  Composited with alpha channel. */
		Enabled,
		/** Control will be cached and re-rendered on demand.  Composited without alpha channel (faster). */
		Solid
	}

	/** A component that can contain other nested components */
	public class GuiContainer : GuiLabeledComponent
	{
		// -------------------------
		// Static

		public static List<GuiContainer> RenderTextureContainers = new List<GuiContainer>();

		// -------------------------
		// Gui Caching


		/** If disabled all render caching will be turned off */
		public static bool ENABLE_CACHING = true;

		/** Number seconds between auto repaints for cached containers.  0 = off */
		public float AutoRefreshInterval = 0f;

		// records the last time the component was repainted.
		private float lastRepaint = 0;

		/** If enabled control will be rendered to a render texture before being displayed.  This can be faster
		 * for controls that do not update often.  The control will need to manually be told to repaint via 'invalidate' */
		public CacheMode CacheMode { get { return ENABLE_CACHING ? _cacheMode : CacheMode.Disabled; } set { setCacheMode(value); } }

		private CacheMode _cacheMode = CacheMode.Disabled;

		/** Returns if this control is using caching or not. */
		protected bool isCached { get { return CacheMode != CacheMode.Disabled; } }

		/** Used to force draw() command to draw normally when painting a render texture. */
		private bool forceStandardDraw = false;

		private bool hasBackgroundChildren = false;

		/** Set to true when a clean is needed. */
		internal bool needsClean = false;

		/** Disables any click interaction with child components, all interaction will be directed towards this container instead. */
		public bool DisableChildInteraction = false;

		// -------------------------
		// Composite Rendering

		protected RenderTexture compositeRenderTexture;

		private bool dirtyRenderTexture = true;

		/** Returns if this control has a render texture (i.e. composited rendering) to draw it's self) */
		protected bool hasRenderTexture {
			get {
				return compositeRenderTexture != null;
			}
		}

		/** Returns if this control needs a render texture in order to draw it's self. */
		private bool needsRenderTexture {
			get { return isCached || !CompositeColorTransform.IsIdentity || (CompositeAlpha < 1f && CompositeAlpha > 0f); }
		}

		/** Used to modify the alpha of a control after the control has been composed (flattened).  
		 * This allows for fading out a control without the subobjects staying opace.  
		 * There is a performance hit for doing this as it enables compisite rendering, so use color.a on simpler controls. */
		public float CompositeAlpha = 1f;

		/** Adjusts the color of a composite control.  
		 *  There is a performance hit for doing this as it enables compisite rendering, so use color.a on simpler controls. */
		public ColorTransform CompositeColorTransform = ColorTransform.Identity;

		// -------------------------
		// Other

		protected List<GuiComponent> Children;

		// -----------------------------------------------------------------------------
		// Methods
		// -----------------------------------------------------------------------------

		/** Renderes any composite containers that are marked as dirty.
		 * @param force If true re-renders all composite containers. */
		public static void ValidateAll(bool force = false)
		{
			for (int lp = 0; lp < RenderTextureContainers.Count; lp++) {
				var container = RenderTextureContainers[lp];
				container.Validate(force);
			}
		}

		/** Marks all composite containers as requiring a refresh. */
		public static void InvalidateAll()
		{
			for (int lp = 0; lp < RenderTextureContainers.Count; lp++) {
				var container = RenderTextureContainers[lp];
				container.Invalidate();
			}
		}
			
		// -----------------------------------------------------------------------------

		public GuiContainer(int width, int height)
			: base(width, height)
		{
			Children = new List<GuiComponent>();
		}

		/** 
		 * Adds a component, but underneith the container.  Clipping is always ignored for these children. 
		*/
		public GuiComponent AddUnder(GuiComponent child, int? hAlign = null, int? vAlign = null, bool fixedAlignment = false)
		{			
			return Add(child, hAlign, vAlign, fixedAlignment, true);
		}

		/** 
		 * Adds given componenet as a child of this component, returns added child 
		 * 
		 * @param child Component to add
		 * @param hAlign Position to align componenet too, or null for no aligment
		 * @param vAlign Position to align componenet too, or null for no aligment
		 * @param fixedAlignment If true the component will try to keep the given alignment values as it's parent changes|
		 * @param backDraw If true child will be drawn underneith the container
		 * size.
		 * 
		 * @returns The added child.
		 */
		public GuiComponent Add(GuiComponent child, int? hAlign = null, int? vAlign = null, bool fixedAlignment = false, bool backgroundDraw = false)
		{
			Invalidate();

			if (Children.IndexOf(child) < 0) {
				Children.Add(child);
				PositionComponent(child, hAlign, vAlign);
			}
			child.Parent = this;

			if (backgroundDraw) {
				hasBackgroundChildren = true;
				child.Layer = -1;
			}			

			if (fixedAlignment) {
				child.FixedAlignment.x = hAlign ?? 0;
				child.FixedAlignment.y = vAlign ?? 0;
				child.Align = GuiAlignment.Fixed;
			}	

			return child;
		}

		/** Removes given componenet as a child of this component */
		public void Remove(GuiComponent child)
		{
			Invalidate();
			if (Children.IndexOf(child) >= 0)
				Children.Remove(child);
			child._parent = null;
		}

		/** Removes given componenet as a child of this component by index */
		public void RemoveAt(int index)
		{
			Invalidate();
			var child = Children[index];
			Children.RemoveAt(index);
			child.Parent = null;
		}

		public int Count {
			get { return Children.Count; }
		}

		/** Returns true if any part of the given rectangle is currently visible within this container.  
		 * @param rect the rectangle to check with co-ords relative to this container */
		virtual public bool RectInBounds(Rect rect)
		{
			if (IgnoreClipping)
				return true;
			else
				return ((rect.xMax > 0) && (rect.x < Width) && (rect.yMax > 0) && (rect.y < Height)); 
		}

		protected override void setSelfEnabled(bool value)
		{
			if (SelfEnabled == value)
				return;
			base.setSelfEnabled(value);
			for (int lp = 0; lp < Children.Count; lp++) {
				Children[lp].HierarchyEnabled = value;
			}
		}

		protected override void setHierarchyEnabled(bool value)
		{
			if (HierarchyEnabled == value)
				return;
			base.setHierarchyEnabled(value);
			for (int lp = 0; lp < Children.Count; lp++) {
				Children[lp].HierarchyEnabled = value;
			}
		}

		/** Draws containers contents */
		public override void DrawContents()
		{
			base.DrawContents();
			DrawChildren();
		}

		/** Causes all children objects to align */
		override public void ForceAlignment()
		{
			base.ForceAlignment();
			for (int lp = 0; lp < Children.Count; lp++) {
				var child = Children[lp];
				child.ForceAlignment();
			}
		}

		public override void Draw()
		{			
			if (CompositeAlpha <= 0f)
				return;

			if (hasRenderTexture && SmartUI.isRepaint && !forceStandardDraw) {
				// setup for render texture draw
				var dp = (CacheMode == CacheMode.Solid) ? DrawParameters.NoAlpha : DrawParameters.Default;
				dp.Transform = CompositeColorTransform;				
				if (repaintedTextureThisFrame && Settings.Advanced.ShowCacheRenders)
					dp = new DrawParameters(ColorTransform.TintAndMultiply(Color.red, Color.gray));
				if (CompositeAlpha < 1f) {
					var colorOffset = dp.Transform.ColorOffset;
					colorOffset[3] = ((1f - colorOffset[3]) * CompositeAlpha) - 1f;
					dp.Transform.ColorOffset = colorOffset;
					dp.AlphaBlend = true;
				}					
				SmartUI.Draw(Bounds, compositeRenderTexture, new Rect(0, 0, compositeRenderTexture.width, compositeRenderTexture.height), dp);

			} else {

				if (hasBackgroundChildren) {
					SmartUI.BeginGroup(Bounds, false);
					DrawChildren(-1);
					SmartUI.EndGroup();
				}

				base.Draw();
			}
		}

		/** Draws all children components */
		protected void DrawChildren(int layerToDraw = 0)
		{			
			for (int lp = 0; lp < Children.Count; lp++) {
				var child = Children[lp];
				try {
					if (child.Layer == layerToDraw && child.Visible && child.InBounds)
						child.Draw();
				} catch (Exception e) {
					Trace.LogError("Control: " + child + " threw exception " + e.Message + ": " + e.StackTrace);
				}
			} 
		}

		/** Transforms given frame from our co-rd system to parents co-ord system.
		 * @param contentsArea if true the control is inside this containers contents area, otherwise the bounds are used instead.
		 */
		virtual public Rect TransformRect(Rect frame, bool contentsArea = true)
		{
			var referenceFrame = contentsArea ? ContentsBounds : Bounds;
			Rect result = new Rect(referenceFrame.x + frame.x, referenceFrame.y + frame.y, frame.width, frame.height);
			return (Parent != null) ? Parent.TransformRect(result, (Layer == 0)) : result;
		}

		/** Updates all children componenets */
		protected void UpdateChildren()
		{
			for (int lp = 0; lp < Children.Count; lp++) {
				var child = Children[lp];
				if (child.Active)
					child.Update();
			}
		}

		public override void Destroy()
		{
			if (_cacheMode != CacheMode.Disabled) {
				releaseRenderTexture();
				RenderTextureContainers.Remove(this);
			}

			for (int lp = 0; lp < Children.Count; lp++) {
				Children[lp].Destroy();
			}

			base.Destroy();
		}

		/** 
		 * Removes and destroys all children componenets.
		 * 
		 * @param ignoreDestroy If true children components will be removed but destroyed.
		 */
		public void Clear(bool ignoreDestroy = false)
		{
			if (!ignoreDestroy)
				foreach (GuiComponent child in Children)
					child.Destroy();
			hasBackgroundChildren = false;
			Children.Clear();
			Invalidate();
		}

		/**
		 * Sizes component to fit all children 
		 * @param minPadding The minimum space between objects and frame edge.
		 */
		virtual public void FitToChildren(int minPadding = 0)
		{
			int maxX = 0;
			int maxY = 0;
			for (int lp = 0; lp < Children.Count; lp++) {
				var child = Children[lp];
				if (child.Align == GuiAlignment.None) {
					if (child.Bounds.xMax > maxX)
						maxX = (int)child.Bounds.xMax + minPadding;
					if (child.Bounds.yMax > maxY)
						maxY = (int)child.Bounds.yMax + minPadding;
				}
			}
			SizeForContent(maxX, maxY);
		}

		/** Positions component on a grid so that centre of component aligns with column. */
		public void PositionComponentToColumns(GuiComponent component, int index, int columns, int expansion = 0)
		{
			component.X = (int)(((float)(index) / (columns + 1) * (ContentsFrame.width + expansion))) - (component.Width / 2) - (expansion / 2);			
		}

		/** 
		 * Aligns given object onto this control.
		 * 
		 * @param hAlign.  Number of pixels to place the object's bounds away from screens edge.  Positive represents pixels from left edge, negative right,
		 * specifiying zero will center the object on the screen.
		 * 
		 * @param vAlign.  Number of pixels to place the object's bounds away from screens edge.  Postive represents pixels from top edge, negative bottom,
		 * specifiying zero will center the object on the screen.
		 * 
		 **/
		public void PositionComponent(GuiComponent component, int? hAlign, int? vAlign)
		{		
			int innerWidth = (int)(ContentsBounds.width);
			int innerHeight = (int)(ContentsBounds.height);

			if (hAlign != null) {
				if (hAlign == 0)
					component.X = (innerWidth - component.Width) / 2;
				else if (hAlign > 0)
					component.X = (int)(hAlign);
				else
					component.X = (int)((innerWidth - component.Width) + hAlign);
			}

			if (vAlign != null) {
				if (vAlign == 0)
					component.Y = (innerHeight / 2) - (int)(component.Height / 2);
				else if (vAlign > 0)
					component.Y = (int)(vAlign);
				else
					component.Y = (int)((innerHeight - component.Height) + vAlign);
			}
		}

		/** Places given componenet at the centre of this componenet */
		public void CentreComponenet(GuiComponent component)
		{
			component.X = (int)(ContentsBounds.width / 2) - (int)(component.Width / 2);
			component.Y = (int)(ContentsBounds.height / 2) - (int)(component.Height / 2);
		}

		/** Forces the replainting of a cached control */
		public virtual void Invalidate()
		{
			dirtyRenderTexture = true;
		}

		/** Removes any children that need deleting. */
		private void Clean()
		{
			var newChildrenList = new List<GuiComponent>();
			for (int lp = 0; lp < Children.Count; lp++) {
				var child = Children[lp];
				if (!child.tagDelete) {
					newChildrenList.Add(child);
				}
			}
			Children = newChildrenList;
			needsClean = false;
		}

		public override void Update()
		{
			base.Update();

			// check for deletes
			if (needsClean)
				Clean();

			if (needsRenderTexture && !hasRenderTexture) {
				createRenderTexture();
			}

			if (hasRenderTexture && !needsRenderTexture) {
				releaseRenderTexture();
			}

			// force composited controls that aren't cached to render every frame.
			if (hasRenderTexture && !isCached) {
				Invalidate();
				repaintRenderTexture();
			}			

			if (isCached) {
				var autoRepaintTimerExpired = false;
				if (AutoRefreshInterval != 0)
					autoRepaintTimerExpired = (Time.time - lastRepaint > AutoRefreshInterval * (1000 + Util.Roll(100)) / 1000);
				
				// a simple way to force updates when user interacts with object.
				// some variation so we don't re render all items on the same frame.
				if (IsMouseInside || autoRepaintTimerExpired) {
					Invalidate();
				}
			}

			UpdateChildren();
		}

		/** Indexer to child components by name */
		public GuiComponent this [string name] {
			get { return FindControl(name, false); }
		}

		/** Compiles a list of this containers children, and subschildren */
		public List<GuiComponent> AllChildren()
		{
			List<GuiComponent> result = new List<GuiComponent>();

			for (int lp = 0; lp < Children.Count; lp++) {
				var child = Children[lp];
				result.Add(child);
				if (child is GuiContainer) {
					result.AddRange((child as GuiContainer).AllChildren());
				}
			}
			return result;
		}

		/**
		 * Finds the first child component of this container matching the given name, or null if no matching component.
		 * 
		 * @param name The name of the component to find (not case sensitive)
		 * @param recursive If true through this containers children containers and so on.
		 * 
		 * @returns The first component found matching the name, or null
		 */ 
		public GuiComponent FindControl(string name, bool recursive = true)
		{
			for (int lp = 0; lp < Children.Count; lp++) {
				var child = Children[lp];
				if (String.Compare(child.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
					return child;
				if (recursive && (child is GuiContainer)) {
					var result = (child as GuiContainer).FindControl(name, true);
					if (result != null)
						return result;
				}
			}
			return null;
		}

		/**
		 * Detects if point is within this component.  Returns this component, or a child component, at that location 
		 */
		override public GuiComponent GetComponentAtPosition(Vector2 screenLocation, bool onlyInteractive = false)
		{
			if (!Visible || !Enabled)
				return null;


			bool ClickedWithinBounds = (AdjustedAbsoluteBounds.Contains(screenLocation));

			if (onlyInteractive && DisableChildInteraction)
				return ClickedWithinBounds ? this : null;
			
			// check children
			GuiComponent childClicked = null;
			for (int lp = 0; lp < Children.Count; lp++) {
				var child = Children[lp];
				var result = child.GetComponentAtPosition(screenLocation, onlyInteractive);
				if (result != null)
					childClicked = result;
			}



			if (IgnoreClipping) {
				return childClicked;
			} else {
				if (ClickedWithinBounds)
					return childClicked ?? this;
				else
					return null;
			}
		}

		// -----------------------------------------------------------------------------
		// Composite rendering
		// -----------------------------------------------------------------------------

		/** Checks if control is dirty, if it is repaints. */
		internal void Validate(bool force = false)
		{
			if (!hasRenderTexture)
				return;
			if (dirtyRenderTexture || force)
				repaintRenderTexture();
		}

		/** Releases texture memory for render cache if there is one. */
		private void releaseRenderTexture()
		{
			if (compositeRenderTexture != null)
				compositeRenderTexture.Release();
			compositeRenderTexture = null;
			RenderTextureContainers.Remove(this);
		}

		/** Creates a new render texture at the approriate size for this control. */
		private void createRenderTexture()
		{
			if (compositeRenderTexture != null)
				releaseRenderTexture();


			if (!RenderTextureContainers.Contains(this))
				RenderTextureContainers.Add(this);			

			compositeRenderTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32);
			compositeRenderTexture.useMipMap = false;
			compositeRenderTexture.antiAliasing = 1;
			compositeRenderTexture.filterMode = FilterMode.Bilinear;

			if (Width * Height > 0)
				compositeRenderTexture.Create();
			
			dirtyRenderTexture = true;
		}

		/** Causes the control to redraw it's contents to the render texture */
		protected void repaintRenderTexture()
		{
			// We can only repaint cache during an onGui 'repaint' event.
			if (!SmartUI.isRepaint)
				return;

			validateRenderTexture();

			Engine.PerformanceStatsInProgress.GuiRenderTextureRepaints++;

			var previousRenderTexture = RenderTexture.active;

			RenderTexture.active = compositeRenderTexture;

			GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));

			int oldX = X;
			int oldY = Y;
			X = 0;
			Y = 0;

			// unity scale to readjust opengl transformation matrix.
			SmartUI.Scale(Engine.GuiScale);
			forceStandardDraw = true;
			base.Draw();
			forceStandardDraw = false;
			SmartUI.Scale(1f / Engine.GuiScale);

			X = oldX;
			Y = oldY;

			RenderTexture.active = previousRenderTexture;

			dirtyRenderTexture = false;
			lastRepaint = Time.time;
		}


		/** Check our render texture size is right and adjust it if needed. */
		private void validateRenderTexture()
		{
			int adjustedWidth = (int)(Width * Engine.GuiScale);
			int adjustedHeight = (int)(Height * Engine.GuiScale);

			if ((compositeRenderTexture.width != adjustedWidth) || (compositeRenderTexture.height != adjustedHeight)) {
				Engine.PerformanceStatsInProgress.GuiRenderTextureResizes++;
				compositeRenderTexture.Release();
				compositeRenderTexture.width = adjustedWidth;
				compositeRenderTexture.height = adjustedHeight;
				compositeRenderTexture.Create();
				Invalidate();
			}
		}

		/** Returns true if a composite repaint occured this frame. */
		private bool repaintedTextureThisFrame {
			get {
				return (hasRenderTexture) && (lastRepaint == Time.time);
			}
		}

		/** Enables or disables caching on this control. */
		private void setCacheMode(CacheMode value)
		{
			if (_cacheMode == value)
				return;

			_cacheMode = value;
		}

	}
}