
using System;
using UnityEngine;
using UI.Generic;

namespace UI
{
	/** Defines the alignment of a control */
	public enum GuiAlignment
	{
		None,
		Top,
		Bottom,
		Left,
		Right,
		Fixed,
		Full

	}

	public class DDFeedback
	{
		// normal container state
		public static DDFeedback Normal = new DDFeedback(Color.white);

		// container indicates acceptance of object
		public static DDFeedback Accept = new DDFeedback(Color.green);

		// container indicates rejection of object
		public static DDFeedback Reject = new DDFeedback(Color.red);

		public Color Color;

		public DDFeedback(Color color)
		{
			this.Color = color;
		}
	}

	/** Interface for objects that can receive drops */
	public interface IDragDrop
	{
		void HandleDragDrop();

		/** returns if this control can receive the given content */
		bool CanReceive(GuiComponent value);

		/** Returns if we can send our content to this control */
		bool CanSend(IDragDrop destination);

		bool Transact(IDragDrop source);

		GuiComponent DDContent { get; set; }
	}

	public delegate void GuiEvent(object source,EventArgs e);

	/** Prototype for retained gui componenets */
	public class GuiComponent : IDragDrop, ICloneable
	{
		public static Sprite OUTER_EDGE = ResourceManager.GetSprite("Icons/OuterEdge");
		public static Sprite OUTER_SHADOW = ResourceManager.GetSprite("Icons/OuterShadow");

		/** Maximum length a caption can be, Unity seems limited to around 16k. */
		public static int MAX_CAPTION_LENGTH = 10000;

		public event GuiEvent OnMouseClicked;
		public event GuiEvent OnMouseDown;
		public event GuiEvent OnDoubleClicked;
		public event GuiEvent OnDDContentChanged;

		/** Containers use this to decide when to draw a component. */
		public int Layer = 0;

		/** Used for various purposes. */
		public int Tag = 0;

		/** The control that currently has focus */
		public static GuiComponent FocusedControl;

		public bool NeverUpdated = true;

		public int X;
		public int Y;

		/** Indicates the control is in a selected state. Used for radio buttons etc. */
		public bool SelectedState = false;

		/** If true uses a bluring shader instead of the normal one. */
		public bool SoftBlur = false;

		public Vector2 Position {
			set {
				X = (int)value.x;
				Y = (int)value.y;
			}
		}

		public int Width { get { return _width; } set { setWidth(value); } }

		public int Height { get { return _height; } set { setHeight(value); } }

		public string Name;

		/** ID value used to identify this componenet.  Not unique.  Often used for a series of buttons etc */
		public int Id;

		private int _width;
		private int _height;

		/** Set to true when user issues a mouse down on the component.  If they release the mouse button over the same componenet a onClicked is fired */
		public bool Depressed;

		/** Frame we where last updated on.  Used to detect multiple updates per frame */
		private int lastUpdate = -1;

		/** If true an inner shadow is draw over contentFrame contents. */
		public bool InnerShadow = false;

		/** If true an outer shadow is draw over under the control. */
		public bool OuterShadow = false;

		/** The outer shadow color, set to non black to create a glow instead. */
		public Color OuterShadowColor = new Color(0f, 0f, 0f, 0.5f);

		public Sprite OuterShadowSprite;

		protected GUIStyle InnerShadowStyle;

		/** True if this control is able to receive focus, and therefore keyboard input */
		public bool CanReceiveFocus;

		/** 
		 * If set to true then this component will not clip when drawing its contents. 
		 */
		public bool IgnoreClipping;

		public Color Color;

		/** If true "showToolTip" will be called when mouse hovers over control. */
		public bool ShowToolTipOnHover = false;

		/**
		 * Used to remap colors 
		 */
		public ColorTransform ColorTransform = ColorTransform.Identity;

		/** Text to display on the control */
		public string Caption {
			get { return _caption; }
			set {
				setCaption(value);
			}
		}

		public bool CaptionDropShadow = false;

		/** Returns the GameState this component belongs to. */
		protected GuiState gameState {
			get {

				if (Parent == null)
					return null;
				if (Parent is GuiState) {
					return (GuiState)Parent;
				} else {
					return Parent.gameState;
				}
			}
		}

		virtual protected void setCaption(string value)
		{
			if (value.Length > MAX_CAPTION_LENGTH) {
				Trace.LogWarning("Maximum caption length exceeded, was {0}, but trimming down to {1}", value.Length, MAX_CAPTION_LENGTH);
				value = value.Substring(0, MAX_CAPTION_LENGTH);
			}
				
			// This would improve the performance a bit, but some code sets caption to the same value to force a size update, so uncommenting these 
			// lines will make things like title be the wrong size.

			// if (_caption == value)
			//	return;
			
			_caption = value;
			_nativeSizedCaption = null;
			RecalculateSize();
		}

		/** Caption with any size tags adjusted to native resolution */
		public string NativeSizedCaption {
			get {  
				if (_nativeSizedCaption == null)
					_nativeSizedCaption = Util.AdjustSizeCodes(Caption, Engine.GuiScale);  
				return _nativeSizedCaption;  
			}
		}

		/** Our caption with color codes adjusted to to be black. */
		protected string shadowCaption {
			get {
				if (_shadowCaption == null)
					_shadowCaption = Util.AdjustColorCodes(Util.Colorise(NativeSizedCaption, Color.black), Color.black.Faded(FontColor.a));				
				return _shadowCaption;
			}
		}

		protected string _shadowCaption = null;

		private string _nativeSizedCaption = null;
		private string _caption;

		/** Changes the default color to use for drawing text within the control */
		public Color FontColor { get { return Style.normal.textColor; } set { Style.normal.textColor = value; } }

		/** Changes the default font to use for drawing text within the control */
		public Font Font {
			get { return Style.font; }
			set {
				Style.font = value;
				RecalculateSize();
			}
		}

		/** Changes the default font size to use for drawing text within the control */
		public int FontSize {
			get { return Style.fontSize; }
			set {
				Style.fontSize = value;
				RecalculateSize();
			}
		}

		/** The fontsize used to render components text if text is rendered in native resolution */
		public int NativeFontSize {
			get { return (int)(FontSize * Engine.GuiScale); }
		}

		/** The style to use when drawing the objects content */
		public GUIStyle Style;

		/** If true component will be drawn */
		public bool Visible = true;

		/** If true component with be updated */
		public bool Active = true;

		/** If true this component is tagged to be deleted.  It will be removed next frame. */
		internal bool tagDelete = false;

		/** True if component can be interacted with.  Disabled components will still draw and update, but not receive mouse clicks */
		public bool Enabled { get { return SelfEnabled && HierarchyEnabled; } }

		public bool SelfEnabled { get { return _selfEnabled; } set { setSelfEnabled(value); } }

		internal bool HierarchyEnabled { get { return _hierarchyEnabled; } set { setHierarchyEnabled(value); } }

		/** Interactive controls are able to receive mouse clicks and can be dragged.  Non interactive controls send any
		 * input to their parent control. */
		public bool Interactive = true;

		/** If true a background will be drawn underneath the component */
		public bool EnableBackground = true;

		public GuiContainer Parent { get { return _parent; } set { setParent(value); } }

		internal GuiContainer _parent;

		/** The bounds for the controls contents, excluding controls borders. */
		public Rect ContentsFrame { get { return GetContentsBounds().Translated(-X, -Y); } }

		/** The frame for the controls contents, excluding controls borders. */
		public Rect ContentsBounds { get { return GetContentsBounds(); } }

		/** A (0,0) rect of (Width,Height) dimentions */
		public Rect Frame { get { return new Rect(0, 0, Width, Height); } }

		/** A (X,Y) rect of (Width,Height) dimentions */
		public Rect Bounds { get { return new Rect(X, Y, Width, Height); } }

		/** Number of seconds mouse needs to hover over control before 'onHover' is called */
		protected static float HOVER_TIME = 0.5f;

		/** Number if seconds the mouse has been hovering over us for */
		private float accumulatedHoverTime = 0;

		/** Records if the mouse was over this control last frame */
		private bool mouseOverLastFrame = false;

		private bool mouseOverThisFrame = false;

		private bool mouseInsideThisFrame = false;

		/** Is the mouse over this contol, and not an interactive subcontrol. */
		public bool IsMouseOver { get { return mouseOverThisFrame; } }

		/** Is the mouse inside this control at all. */
		public bool IsMouseInside { get { return mouseInsideThisFrame; } }

		/** The top most componenet on the stack that the mouse is over */
		public static GuiComponent MouseOverComponent { get { return _mouseOverComponent; } }

		private static GuiComponent _mouseOverComponent;

		/** Amount to offset controls content when control is depressed */
		protected int DepressedOffset = 0;

		public GuiAlignment Align { get { return _align; } set { setAlign(value); } }

		/** The offsets to used when align is in fixed mode */
		public Vector2 FixedAlignment = new Vector2(0, 0);

		private GuiAlignment _align = GuiAlignment.None;

		/** Time this component was last clicked.  Used for double clicking */
		private float lastClickTime = 0;

		public TextAnchor TextAlign { get { return Style.alignment; } set { Style.alignment = value; } }

		public bool WordWrap { get { return Style.wordWrap; } set { Style.wordWrap = value; } }

		public bool AutoWidth = false;
		public bool AutoHeight = false;

		private bool _selfEnabled = true;
		private bool _hierarchyEnabled = true;

		/** The focused control receives keyboard input */
		public bool Focused {
			get { return FocusedControl == this; }
			set { 
				if (value)
				if (CanReceiveFocus)
					FocusedControl = this;
				else if (Focused)
					FocusedControl = null;
			}
		}

		/** 
		 * Makes sure this object is atleast "padding" pixels away from other object, if it's not this component is moved. 
		 * note: this isn't working properly now, and only functions on the xaxis...
		 */
		public void InsurePadding(GuiComponent otherObject, int padding = 10)
		{
			int xEdge = 0;

			if (Bounds.xMin < otherObject.Bounds.center.x) {
				if (Bounds.xMax + padding > (otherObject.Bounds.xMin))
					xEdge = (int)(Bounds.xMax - (otherObject.Bounds.xMin - padding));
			} else {
				if (Bounds.xMin - padding < (otherObject.Bounds.xMax))
					xEdge = (int)(Bounds.xMin - (otherObject.Bounds.xMax + padding));
			}
				
			/*
			if (Bounds.yMin < otherObject.Bounds.center.y) {				
				if (Bounds.yMax + padding > (otherObject.Bounds.yMin))
					yEdge = (int)(Bounds.yMax - (otherObject.Bounds.yMin - padding));
			} else {
				if (Bounds.yMin - padding < (otherObject.Bounds.yMax))
					yEdge = (int)(Bounds.yMin - (otherObject.Bounds.yMax + padding));			
			} 	 */

			X -= xEdge;
				
		}

		/** 
		 * Override to show a tool tip when mouse hovers over component. 
		 * @returns if a tool tip was shown or not.
		 */
		protected virtual bool showToolTip()
		{			
			return false;
		}

		public GuiComponent(int width = -1, int height = -1)
		{
			AutoWidth = (width == -1);
			AutoHeight = (height == -1);

			this._width = width;
			this._height = height;

			this.Depressed = false;
			this.IgnoreClipping = false;

			Color = Color.white;
			Style = new GUIStyle(GUIStyle.none);
			InnerShadowStyle = Engine.GetStyleCopy("InnerShadow");
			OuterShadowSprite = ResourceManager.GetSprite("Icons/OuterShadow");

			this.Font = Engine.Instance.TextFont;

			CanReceiveFocus = false;

			RecalculateSize();
		}

		/** Returns if this object, and all parents, are visible */
		protected bool RecursiveVisibile {
			get {
				if (Parent != null)
					return Visible && (Parent.RecursiveVisibile);
				else
					return Visible;
			}
		}

		protected void DrawOuterShadow()
		{	
			if (OuterShadowSprite == null)
				return;
				
			var shadowBounds = this.Bounds.Enlarged(16);

			SmartUI.Color = OuterShadowColor;
			SmartUI.DrawSliced(shadowBounds, OuterShadowSprite);
			SmartUI.Color = Color.white;
		}

		/** Draws this instance of the control */
		virtual public void Draw()
		{							
			// Newly created components will be drawn before their update is called so we call it once here.
			if (NeverUpdated)
				Update();

			SmartUI.Color = Color.white;

			Engine.PerformanceStatsInProgress.GuiDraws++;

			if (OuterShadow)
				DrawOuterShadow();

			if (EnableBackground)
				DrawBackground();

			SmartUI.BeginGroup(ContentsBounds, !IgnoreClipping);
			DrawContents();
			SmartUI.EndGroup();

			if (InnerShadow)
				DrawInnerShadow();			

			if (Caption != "") {
				Rect textBounds = new Rect(Bounds);
				if (Depressed)
					textBounds.y += DepressedOffset;

				if (CaptionDropShadow) {
					int ShadowDistance = 2;
					SmartUI.Text(textBounds.Translated(new Vector2(ShadowDistance, ShadowDistance)), shadowCaption, Style);
				}					
				
				SmartUI.Text(textBounds, NativeSizedCaption, Style);
			}				
		}

		/** 
		 * Draws a shadow over components contents 
		 */
		protected void DrawInnerShadow()
		{
			SmartUI.Draw(ContentsBounds, InnerShadowStyle);
		}

		/** Adjusts the size of the control according to content */
		virtual protected void RecalculateSize()
		{
			if (!AutoWidth && !AutoHeight)
				return;
				
			/** 
			 * When rendering content we are going to render at native resolution, so we need to calculate the 
			 * size according to the native font size and then scale back.  An alternative is to just return the
			 * size calculated from the normal fontsize and scale it when rendering, however a 2x sized font wont
			 * always be 2x the size so this is required for accuracy.
			 */
			bool CalculateNativeSize = (NativeFontSize != FontSize); 
			var oldSize = Style.fontSize;

			if (CalculateNativeSize)
				Style.fontSize = NativeFontSize;

			Vector2 newSize = Style.CalcSize(new GUIContent(NativeSizedCaption));

			// We need to rework the height if word wrapping is on due to an error in the CalcSize calculation.
			if (Style.wordWrap)
				newSize.y = Style.CalcHeight(new GUIContent(NativeSizedCaption), newSize.x);

			if (CalculateNativeSize) {
				newSize /= Engine.GuiScale;
				Style.fontSize = oldSize;
			}

			int padWidth = Style.padding.horizontal;
			int padHeight = Style.padding.vertical;

			if (AutoWidth)
				_width = padWidth + (int)newSize.x;
			if (AutoHeight)
				_height = padHeight + (int)newSize.y;
		}

		/** Returns true if any part of this control is visible within its parent's bounds */
		public bool InBounds {
			get {
				return (_parent == null) ? true : _parent.RectInBounds(Bounds);
			}
		}

		/** Returns the controls frame in absolute co-ordinates */
		public Rect AbsoluteBounds { 
			get {
				if (_parent != null)
					return _parent.TransformRect(Bounds, (Layer == 0));
				else
					return Bounds;
			} 
		}

		/** 
		 * Returns the controls frame in absolute co-ordinates, adjusted to a minimum size. 
		 * This will only apply an adjustment on TouchDevices (which need the larger hit detection box 
		 */
		public Rect AdjustedAbsoluteBounds {
			get {
				if (!Engine.TouchDevice)
					return AbsoluteBounds;

				int minimumSize = (int)(24 * Engine.DPIScale);
				Rect adjustedFrame = AbsoluteBounds;
				if (adjustedFrame.width < minimumSize) {
					int delta = minimumSize - (int)adjustedFrame.width;
					adjustedFrame.x -= delta / 2;
					adjustedFrame.width += delta;
				}
				if (adjustedFrame.height < minimumSize) {
					int delta = minimumSize - (int)adjustedFrame.height;
					adjustedFrame.y -= delta / 2;
					adjustedFrame.height += delta;
				}
				return adjustedFrame;
			}
		}

		private void processMouseClicks()
		{
			if (Depressed) {				
				// release depress when mouse is up.
				if (Input.GetMouseButtonUp(0)) {
					Depressed = false;
					if (mouseOverThisFrame) {
						if (((Time.time - lastClickTime) < Settings.Advanced.DoubleClickDelay) && (Mouse.PreviousClickLocation - Mouse.Position).magnitude < Settings.Advanced.DoubleClickMovementThreshold) {						
							DoClick();
							DoDoubleClick();
						} else {
							DoClick();
						}
					}
				}
			} else {
				// depress the control when mouse clicks on it
				if (Input.GetMouseButtonDown(0)) {
					if (mouseOverThisFrame) {
						DoMouseDown();
						Depressed = true;
					} else {
						Depressed = false;
					}
				}
			}
			
			// check for drag begin
			if (!mouseOverThisFrame && mouseOverLastFrame)
				DoMouseLeave();
			
			if (mouseOverThisFrame && !mouseOverLastFrame)
				DoMouseEnter();
			
			if (mouseOverThisFrame) {
				DoMouseOver();
				if (accumulatedHoverTime > HOVER_TIME)
					DoMouseHover();
				accumulatedHoverTime += Time.deltaTime;
				
			} else {
				accumulatedHoverTime = 0;
			}
		}

		/** Process any keys pressed while we have focus */
		virtual protected void ProcessKeyboardInput()
		{			
			if (Input.GetKeyUp(KeyCode.Return))
				DoClick();
		}

		/** Updates the control */
		virtual public void Update()
		{
			if (lastUpdate == Time.frameCount) {
				return;
			}				

			Engine.PerformanceStatsInProgress.GuiUpdates++;

			lastUpdate = Time.frameCount;

			if (RecursiveVisibile) {
				UpdateAlignment();

				bool mouseOverParent = _parent == null ? true : (_parent.IgnoreClipping || _parent.IsMouseInside);

				bool mouseOverUs = mouseOverParent && AdjustedAbsoluteBounds.Contains(Mouse.Position);

				bool mouseOverChildComponent = mouseOverUs && (GetComponentAtPosition(Mouse.Position, true) != this);		

				mouseInsideThisFrame = mouseOverUs;

				mouseOverThisFrame = mouseOverUs && !mouseOverChildComponent;
				    
				if (mouseOverThisFrame)
					_mouseOverComponent = this;

				if (Enabled)
					processMouseClicks();

				// if this is out first update we ignore any keyboard commands.  Otherwise pressing enter on a button that creates a new button will trigger the second button aswell. */
				if (Enabled && Focused && !NeverUpdated)
					ProcessKeyboardInput();

				mouseOverLastFrame = mouseOverThisFrame;

				if (DragDropEnabled)
					HandleDragDrop();
			}

			NeverUpdated = false;

		}

		/** Returns the draw parameters for this component */
		virtual protected DrawParameters GetDrawParameters {
			get { 
				return new DrawParameters(ColorTransform, SoftBlur);
			}
		}

		/** Draws the background of the control */
		virtual protected void DrawBackground()
		{
			var state = SmartStyleState.Normal;

			if (mouseOverThisFrame)
				state = SmartStyleState.Hover;

			if (Depressed)
				state = SmartStyleState.Depressed;

			if (SelectedState)
				state = SmartStyleState.Selected;

			if (!Enabled)
				state = SmartStyleState.Deactivated;

			SmartUI.Color = Color;
			SmartUI.Draw(Bounds, Style, GetDrawParameters, state);
			SmartUI.Color = Color.white;	
		}

		/** 
		 * Draws the controls contents.  
		 * In general it is better to override this than Draw() as during the DrawContents
		 * call the GUI transform is adjusted so that 0,0 is the top left corner of the contents frame.  
		 * However if you need to draw outside the contents frame you will need to override Draw instead.
		 */
		virtual public void DrawContents()
		{			
		}


		/** Called when mouse clicks on the component */
		virtual public void DoClick()
		{
			lastClickTime = Time.time;
			Focused = true;
			if (OnMouseClicked != null)
				OnMouseClicked(this, new EventArgs());
		}

		/** Called when mouse double clicks on the component */
		virtual public void DoDoubleClick()
		{
			if (OnDoubleClicked != null)
				OnDoubleClicked(this, new EventArgs());
		}

		/** Called when mouse is over this component */
		virtual public void DoMouseOver()
		{
			//
		}

		/** Called when mouse button is first pressed down over this component */
		virtual public void DoMouseDown()
		{
			if (OnMouseDown != null)
				OnMouseDown(this, new EventArgs());
		}

		/** Called the first frame mouse enters this component */
		virtual public void DoMouseEnter()
		{
			//
		}

		/** Called the first frame mouse leaves this component */
		virtual public void DoMouseLeave()
		{
			//
		}

		/** Called when mouse is hovering over component (after a second or so) */
		virtual public void DoMouseHover()
		{			
			if (ShowToolTipOnHover)
				showToolTip();
		}

		/** set the parent for this control.  The control will be removed from any previous parent */
		private void setParent(GuiContainer value)
		{
			if (_parent == value)
				return;

			if (_parent != null)
				_parent.Remove(this);

			_parent = value;

			if (value != null)
				value.Add(this);

			OnParentChanged();
		}

		/**
		 * Called after this objects parent has been changed 
		 */
		protected virtual void OnParentChanged()
		{
			UpdateAlignment();
		}

		virtual protected Rect GetContentsBounds()
		{			
			var rect = new Rect(X, Y, Width, Height);

			rect = Style.padding.Remove(rect);

			if (Depressed)
				rect.y += DepressedOffset;
			return rect;
		}

		/** Forces this component, and any children to update alignment */
		virtual public void ForceAlignment()
		{
			UpdateAlignment();
		}

		/** Process the alignment of this control */
		virtual public void UpdateAlignment()
		{
			if (Align == GuiAlignment.None)
				return;

			Rect parentsFrame;

			if (Parent == null)
				parentsFrame = new Rect(0, 0, Screen.width / Engine.GuiScale, Screen.height / Engine.GuiScale);
			else
				parentsFrame = (Parent.ContentsBounds);

			switch (Align) {
				case GuiAlignment.Full:
					X = 0;
					Y = 0;
					Width = (int)parentsFrame.width;
					Height = (int)parentsFrame.height;
					break;
				case GuiAlignment.Top:
					X = 0;
					Y = Style.margin.top;
					Width = (int)parentsFrame.width;
					break;
				case GuiAlignment.Bottom:
					X = 0;
					Y = (int)parentsFrame.height - Height;
					Width = (int)parentsFrame.width;
					break;
				case GuiAlignment.Left:
					X = 0;
					Y = 0;
					Height = (int)parentsFrame.height;
					break;
				case GuiAlignment.Right:
					X = (int)parentsFrame.width - Width;
					Y = 0;
					Height = (int)parentsFrame.height;
					break;
				case GuiAlignment.Fixed:
					if (Parent != null)
						Parent.PositionComponent(this, (int)FixedAlignment.x, (int)FixedAlignment.y);
					break;
			}
		}

		private void setAlign(GuiAlignment value)
		{
			_align = value;
			UpdateAlignment();
		}

		/** Called when control is destroyed.  Use this to free any used resources */
		virtual public void Destroy()
		{
			_parent = null;
		}

		public override string ToString()
		{
			return Name + " [" + this.GetType() + "]";
		}

		#region ICloneable implementation

		public object Clone()
		{
			return this.MemberwiseClone();
		}

		#endregion


		#region IDragDrop implementation

		public bool BeingDragged = false;

		virtual public bool IsEmpty { get { return false; } }

		protected GuiComponent _ddContent;

		/** Used to give UI feedback about weither an object will be accepted or not */
		public DDFeedback Feedback = DDFeedback.Normal;

		/** If true this control can not be modified */
		public bool Locked = false;

		/** Set this to true to enable drag/drop on this componenet */
		public bool DragDropEnabled = false;

		/** Sets the width, disables auto width */
		private void setWidth(int value)
		{
			AutoWidth = false;
			_width = value;
			if (AutoHeight)
				RecalculateSize();
		}

		/** Sets the height, disables auto height */
		private void setHeight(int value)
		{
			AutoHeight = false;
			_height = value;
		}

		/** Deletes this component next frame */
		public void Remove()
		{
			if (Parent == null)
				return;
			Parent.needsClean = true;
			this.tagDelete = true;
		}

		/**
		 * Adjusts the size of the componenet to allow given space within the contentsRect 
		 */
		public void SizeForContent(int contentWidth, int contentHeight)
		{
			Width = contentWidth + Style.padding.horizontal;
			Height = contentHeight + Style.padding.vertical;
		}

		/** Sets this components self active. */
		virtual protected void setSelfEnabled(bool value)
		{
			_selfEnabled = value;
		}

		/** Sets this components hierarchy active. */
		virtual protected void setHierarchyEnabled(bool value)
		{
			_hierarchyEnabled = value;
		}

		/**
		 * Detects if point is within this component.  Returns this component, or a child component, at that location 
		 * 
		 * @param point Screen location to test
		 * @param onlyInteractive if true only interactive components will be returned.
		 * 
		 */
		public virtual GuiComponent GetComponentAtPosition(Vector2 screenLocation, bool onlyInteractive = false)
		{
			if (!Interactive && onlyInteractive)
				return null;
			if (AdjustedAbsoluteBounds.Contains(screenLocation))
				return this;
			return null;
		}

		/** Handles the logic for drag/drop */
		public void HandleDragDrop()
		{				
			if (!DragDropEnabled)
				return;

			Feedback = DDFeedback.Normal;

			// check for drag begin
			if ((DDContent != null) && !Locked && Input.GetMouseButton(0))
			if (Depressed)
				Engine.DragDrop.BeginDragDrop(this);
			
			// check for drag over
			if ((Engine.DragDrop.DDContent != null)) {
				if (IsMouseOver) {
					if (!Locked && CanReceive(Engine.DragDrop.DDContent)) {
						Feedback = DDFeedback.Accept;
					} else {
						Feedback = DDFeedback.Reject;
					}
				}				
			}
			
			// check for drag drop 
			if ((Engine.DragDrop.DDContent != null) && (Input.GetMouseButtonUp(0))) {
				if (InBounds && IsMouseOver && !Locked && CanReceive(Engine.DragDrop.DDContent)) {
					Engine.DragDrop.EndDragDrop(this);
				}
			}
		}

		virtual public bool CanReceive(GuiComponent value)
		{
			return false;
		}

		virtual public bool CanSend(IDragDrop destination)
		{
			return true;
		}

		/** 
		 * Performs a transaction with this object and source object
		 * @returns true if transaction completed. 
		 */
		virtual public bool Transact(IDragDrop source)
		{
			Trace.LogDebug("Transact: this={0}, source {1}", this, source);
			GuiComponent temp = source.DDContent;
			source.DDContent = DDContent;
			DDContent = temp;
			return true;
		}

		public GuiComponent DDContent {
			get { return _ddContent; }
			set {				
				if (CanReceive(value))
					SetDDContent(value);
			}
		}

		/**
		 * Forces content to given item even if normally this control would not accept it 
		 */
		public void ForceContent(GuiComponent value)
		{
			SetDDContent(value);
		}

		/** Sets the contents of this drag drop container */
		virtual protected void SetDDContent(GuiComponent value)
		{
			_ddContent = value;
			if (value != null) {
				value.X = 0;
				value.Y = 0;
			}
			DDContentChanged();
		}

		/** Called when drag drop containers contents changes */
		virtual public void DDContentChanged()
		{			
			if (OnDDContentChanged != null)
				OnDDContentChanged(this, null);
		}

		#endregion

	}
}

