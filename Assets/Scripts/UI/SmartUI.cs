
using UnityEngine;
using System;
using System.Collections.Generic;

class BatchItem
{
	public Rect Position;
	public GUIStyle Style;

	public BatchItem(Rect position, GUIStyle style)
	{
		Position = position;
		Style = style;
	}

	public Texture Texture { 
		get { return Style.normal.background; }
	}

	/** Sorts batch list by material */
	public void SortByTexture()
	{
		// NIY:
	}

}

public struct ClippingTransform
{
	public Rect Rect;
	//rename origion
	public Vector2 Offset;

	public ClippingTransform(Rect rect)
	{
		Rect = rect;
		Offset = Vector2.zero;
	}
}

public enum SmartStyleState
{
	Normal,
	Hover,
	Deactivated,
	Depressed,
	Selected
}

enum DrawMethod
{
	DebugColor,
	Standard,
	Bordered,
	Native
}

/** Handles all the UI drawing */
public sealed class SmartUI
{
	/** Rectangle used to clip components */
	public static ClippingTransform ClipTransform = new ClippingTransform(new Rect(0, 0, Mathf.Infinity, Mathf.Infinity));

	/** Used to offset all objects when drawing */
	public static Vector2 DrawOffset = Vector2.zero;

	/** If true all components will show their bounds */
	public static bool ShowGuiBounds = false;
	
	/** If true all components will show their material */
	public static bool ShowGuiMaterial = false;

	public static bool EnableBatching = false;

	/** Normally objects only draw during 'repaint' phase of on gui.  Setting this to true will force them to draw
	 * during every phase. */
	public static bool DrawEvenIfNotRepaint = false;

	private static List<ClippingTransform> GroupStack = new List<ClippingTransform>();

	public static RectOffset BORDER_1PX = new RectOffset(1, 1, 1, 1);
	public static RectOffset BORDER_2PX = new RectOffset(2, 2, 2, 2);

	private static Stack<Color> colorStack = new Stack<Color>();

	private static bool legacyNeedsClipping;
	private static bool legacyVisible;
	private static bool legacyIgnoreGuiScale;

	public static void DrawLegacySlow(Action legacyFunction, Rect position = default(Rect), bool ignoreGuiScale = false)
	{
		beginLegacy(position, ignoreGuiScale);
		if (legacyVisible)
			legacyFunction();
		endLegacy();
	}

	/** 
	 * Used to run a legacy GUI draw.  Will apply clipping and transformation etc.
	 * Position is used to work out if draw call needs to be clipped or not.
	 * 
	 * @param ignoreGuiScale: 
	 * 		If true then draw call is based to native resoultion and ignores and guiscaling, this is a bit of a hack. 
	 * 		The position given however is always in scaled units.
	 */
	//todo: clean this up!
	//note: I have an idea hwo to fix this using just a begin group for the clipping rect then providing offest as a transformation
	//outside of the draw legacy call.
	private static void beginLegacy(Rect position = default(Rect), bool ignoreGuiScale = false)
	{		
		legacyVisible = true;		
		legacyIgnoreGuiScale = ignoreGuiScale;
		legacyNeedsClipping = false;
	
		if (position != default(Rect)) {
			Vector4 clip = new Vector4();
			position = TransformAndClipTheRect(position, ref clip);
			legacyVisible = (position.width > 0) && (position.height > 0);
			legacyNeedsClipping = (clip != Vector4.zero) && legacyVisible;
		}

		if (!legacyVisible)
			return;

		Engine.PerformanceStatsInProgress.GuiLegacyDraws++;

		if (legacyNeedsClipping) {
			if (legacyIgnoreGuiScale)
				GUI.matrix *= Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1 / Engine.GuiScale, 1 / Engine.GuiScale, 1));

			// slow and ugly, but handles clipping properly 
			Rect clipRect = ClipTransform.Rect;
			if (legacyIgnoreGuiScale)
				clipRect = clipRect.Scaled(Engine.GuiScale);

			GUI.BeginGroup(clipRect);

			GUIStyle hsv = GUIStyle.none;
			GUIStyle vsv = GUIStyle.none;
			clipRect.min = Vector2.zero;
			Vector2 offset = -(ClipTransform.Offset - ClipTransform.Rect.min);
			if (legacyIgnoreGuiScale)
				offset *= Engine.GuiScale;			
			GUI.BeginScrollView(clipRect, Vector2.zero, new Rect(offset.x, offset.y, 9999, 9999), hsv, vsv);
		} else {
			// fast and neat, but no clipping
			Engine.PushGUI();
			Matrix4x4 matrix = (ignoreGuiScale ? Engine.PureGUIMatrix : Engine.BaseGUIMatrix); 
			matrix = matrix.Translated(ClipTransform.Offset * (ignoreGuiScale ? Engine.GuiScale : 1));
			GUI.matrix = matrix;
		}
	}

	private static void endLegacy()
	{
		if (!legacyVisible)
			return;

		if (legacyNeedsClipping) {
			GUI.EndScrollView(false);
			GUI.EndGroup();
			if (legacyIgnoreGuiScale)
				GUI.matrix *= Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Engine.GuiScale, Engine.GuiScale, 1));
		} else {
			Engine.PopGUI();
		}
	}

	// -----------------------------------------
	// groups
	// -----------------------------------------

	public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, GUIStyle horizontalScrollBar, GUIStyle verticalScrollBar, bool enableScrollWheel = true)
	{
		Vector2 newScrollPosition = Vector2.zero;
		DrawLegacySlow(delegate { 
			newScrollPosition = GUI.BeginScrollView(position, scrollPosition, viewRect, horizontalScrollBar, verticalScrollBar); 
			GUI.EndScrollView(enableScrollWheel);
		});
		BeginGroup(position);
		ClipTransform.Offset -= newScrollPosition;
		return newScrollPosition;
	}

	public static void EndScrollView()
	{
		EndGroup();
	}

	/** Begins a group at given relative location */
	public static void BeginGroup(Rect newRect, bool enableClip = true)
	{
		GroupStack.Add(ClipTransform);
		
		newRect = TransformTheRect(newRect);
		ClipTransform.Offset = newRect.min;
		
		if (enableClip) {
			newRect = ClipTheRect(newRect);
			ClipTransform.Rect = newRect;
		}
	}

	/** Scales the current gui matrix by given value. */
	public static void Scale(float value)
	{	
		Vector3 offset = -ClipTransform.Offset;
		GUI.matrix *= Matrix4x4.TRS(offset * value, Quaternion.identity, Vector3.one * value);
	}

	public static void EndGroup()
	{
		if (GroupStack.Count == 0)
			throw new Exception("GUI Push pop stack mismatch.");
		
		ClipTransform = GroupStack[GroupStack.Count - 1];
		GroupStack.RemoveAt(GroupStack.Count - 1);
	}

	// -----------------------------------------
	// legacy drawing routines, they use GUI
	// -----------------------------------------
	
	/** Draws a text edit field */
	public static string TextField(Rect frame, string text, int fontSize = 14)
	{
		string value = text;
		DrawLegacySlow(delegate { 			
			var style = Engine.GetStyleCopy("TextField");
			style.fontSize = fontSize;
			value = GUI.TextField(frame, text, style);
		}, frame);
		return value;
	}

	/** Draws text using given style, but ignoring background, and with a shadow. */
	public static void TextWithShadow(Rect position, string text, GUIStyle style, int shadowDistance, float shadowOpacity = 1.0f)
	{
		PushAndSetColor(Color.black);
		Text(position.Translated(shadowDistance, shadowDistance), Util.AdjustColorCodes(text, Color.black.Faded(shadowOpacity)), style);
		PopColor();
		Text(position, text, style);
	}

	/** Draws text using given style, but ignoring background. */
	public static void Text(Rect position, string text, GUIStyle style, bool EnableBackground = false)
	{		
		if (String.IsNullOrEmpty(text))
			return;

		beginLegacy(position, true);

		if (!legacyVisible)
			return;

		Texture2D oldTexture = style.normal.background;
		if (!EnableBackground)
			style.normal.background = null;

		if (Engine.GuiScale == 1f) {			
			GUI.Label(position, text, style);
		} else {

			// Increase font size.
			int oldFontSize = style.fontSize;
			style.fontSize = (int)(style.fontSize * Engine.GuiScale);

			// Transform position into native resolution space.
			var newPosition = position.Scaled(Engine.GuiScale);

			GUI.Label(newPosition, text, style);
			style.fontSize = oldFontSize;
		}
				
		if (style.normal.background != oldTexture)
			style.normal.background = oldTexture;
		
		endLegacy();

		if (ShowGuiBounds) {	
			Color = Color.red;
			DrawMaster(position, StockTexture.SimpleRect, new Rect(0, 0, 4, 4), DrawParameters.DebugDraw, BORDER_2PX);
		}

	}

	/** Draws text using given style, including background */
	public static void SolidText(Rect position, string text, GUIStyle style)
	{
		Text(position, text, style, true);
	}

	public static bool Toggle(Rect position, bool value, string content)
	{
		bool result = false;
		DrawLegacySlow(delegate { 
			result = GUI.Toggle(position, value, content);
		}, position);
		return result;
	}

	// -----------------------------------------
	// helpers
	// -----------------------------------------

	/** Returns true if any part of given screen space rect is clipped by the clipping rect */
	public static bool isRectClipped(Rect rect)
	{
		return rect.Within(ClipTransform.Rect);
	}

	/** Clips screen space rect according to clipping, returns amount clipped in the form (left,top,right,bottom) */
	public static Rect ClipTheRect(Rect rect)
	{
		return rect.Intersection(ClipTransform.Rect);
	}

	/** Clips screen space rect according to clipping, returns amount clipped in the form (left,top,right,bottom) */
	public static Rect ClipTheRect(Rect rect, ref Vector4 clipOffsets)
	{
		Rect origional = rect;
		rect = rect.Intersection(ClipTransform.Rect);	

		clipOffsets[0] = rect.xMin - origional.xMin;
		clipOffsets[1] = rect.yMin - origional.yMin;
		clipOffsets[2] = -(rect.xMax - origional.xMax);
		clipOffsets[3] = -(rect.yMax - origional.yMax);

		return rect;
	}

	/** Transforms rect from relative space to screen space. */
	public static Rect TransformTheRect(Rect rect)
	{
		rect.x += ClipTransform.Offset.x;
		rect.y += ClipTransform.Offset.y;
		return rect;
	}

	public static Rect TransformAndClipTheRect(Rect rect, ref Vector4 clipOffsets)
	{
		rect = TransformTheRect(rect);
		return ClipTheRect(rect, ref clipOffsets);
	}
	
	// -----------------------------------------
	// -----------------------------------------

	/** Pushes current color to stacka nd sets gui color to given value */ 
	public static void PushAndSetColor(Color color)
	{
		colorStack.Push(Color);
		Color = color;
	}

	/** Restores color to previous color */
	public static void PopColor()
	{
		Color = colorStack.Pop();
	}

	public static Color Color { get { return GUI.color; } set { GUI.color = value; } }

	public static bool Enabled { get { return GUI.enabled; } set { GUI.enabled = value; } }

	// -----------------------------------------
	// sprite drawing routines
	// -----------------------------------------

	/** Draws sprite */
	public static void Draw(float x, float y, Sprite sprite)
	{
		Draw(x, y, sprite, DrawParameters.Default);
	}

	public static void Draw(float x, float y, Sprite sprite, DrawParameters dp)
	{
		Draw(new Rect(x, y, sprite.rect.width, sprite.rect.height), sprite, dp);
	}

	public static void Draw(Rect screenRect, Sprite sprite)
	{
		Draw(screenRect, sprite, DrawParameters.Default);
	}

	public static void Draw(Rect screenRect, Sprite sprite, DrawParameters dp)
	{ 
		DrawMaster(screenRect, sprite.texture, sprite.rect, dp); 
	}

	// -----------------------------------------
	// Rect drawing routines
	// -----------------------------------------

	/** Draws a rectangle frame */
	public static void DrawFrameRect(Rect rect, Color color)
	{
		if (color.a <= 0f)
			return;
		
		Color = color;
		DrawMaster(rect, StockTexture.SimpleRect, new Rect(0, 0, 3, 3), DrawParameters.Default, BORDER_2PX);
		Color = Color.white;
	}

	/** Draws sprite with 9 slicing. */
	public static void DrawSliced(Rect rect, Sprite sprite, RectOffset border)
	{
		DrawMaster(rect, sprite.texture, sprite.rect, DrawParameters.Default, border);
	}

	/** Draws sprite with 9 slicing using default border */
	public static void DrawSliced(Rect rect, Sprite sprite)
	{
		DrawMaster(rect, sprite.texture, sprite.rect, DrawParameters.Default, new RectOffset((int)sprite.border[0], (int)sprite.border[1], (int)sprite.border[2], (int)sprite.border[3]));
	}


	/** Draws a solid color rectangle */
	public static void DrawFillRect(Rect rect, Color color)
	{
		if (color.a <= 0f)
			return;
		
		Color = color;
		DrawMaster(rect, StockTexture.White, new Rect(0, 0, 2, 2), DrawParameters.Default);
		Color = Color.white;
	}

	// -----------------------------------------
	// GUI drawing routines
	// -----------------------------------------
		
	/** Draws gui style */
	public static void Draw(Rect screenRect, GUIStyle style)
	{
		Draw(screenRect, style, DrawParameters.Default);
	}

	public static void Draw(Rect screenRect, GUIStyle style, DrawParameters dp, SmartStyleState state = SmartStyleState.Normal)
	{
		Texture2D texture = style.normal.background;

		switch (state) {
			case SmartStyleState.Deactivated:
				texture = style.active.background ?? texture;
				break;
			case SmartStyleState.Depressed:
				texture = style.focused.background ?? texture;
				break;		
			case SmartStyleState.Selected:
				texture = style.onActive.background ?? texture;
				break;
			case SmartStyleState.Hover:
				texture = style.hover.background ?? texture;
				break;
		}

		if (texture == null)
			return;

		Rect uvFrame = new Rect(0, 0, texture.width, texture.height);

		if (style.fixedHeight != 0f)
			screenRect.height = style.fixedHeight;
		if (style.fixedWidth != 0f)
			screenRect.width = style.fixedWidth;	
		screenRect = style.overflow.Add(screenRect);		
		DrawMaster(screenRect, texture, uvFrame, dp, style.border);
	}

	// -----------------------------------------
	// texture drawing routines
	// -----------------------------------------
	
	public static void Draw(Rect screenRect, Texture texture, Rect uvFrame, DrawParameters dp)
	{
		DrawMaster(screenRect, texture, uvFrame, dp);
	}

	/**
	 * Calculate UV of given position on a rendered texture given the border.
	 * Returned value is [(0,0)..(textureWidth,textureHeight)].
	 */
	public static Vector2 PixelToUV(Rect drawRect, Rect textureRect, Vector2 position, RectOffset border = null)
	{
		position -= drawRect.min;
		drawRect.x = 0;
		drawRect.y = 0;

		if ((border == null) || border.IsZero()) {
			if ((drawRect.width == 0f) || (drawRect.height == 0f))
				return Vector2.zero;
			Vector2 result = new Vector2();

			result.x = textureRect.x + (position.x / drawRect.width * textureRect.width);
			result.y = textureRect.y + (position.y / drawRect.height * textureRect.height);

			return result;
		}

		Rect innerRect = border.Remove(drawRect);
		Rect innerTexture = border.Remove(textureRect);

		float x;
		float y;

		if (position.x <= innerRect.xMin)
			x = position.x;
		else if (position.x >= innerRect.xMax)
			x = (textureRect.xMax - (drawRect.width - position.x));
		else {
			if (innerRect.width == 0)
				x = innerTexture.xMin;
			else
				x = ((position.x - innerRect.xMin) / innerRect.width) * innerTexture.width + innerTexture.xMin;
		}

		if (position.y <= innerRect.yMin)
			y = position.y;
		else if (position.y >= innerRect.yMax)
			y = (textureRect.yMax - (drawRect.yMax - position.y));
		else {
			if (innerRect.height == 0)
				y = innerTexture.yMin;
			else
				y = ((position.y - innerRect.yMin) / innerRect.height) * innerTexture.height + innerTexture.yMin;
		}

		return new Vector2(x, y);
	}

	/** 
	 * Draws a texture with given parameter
	 * 
	 * @param uvFrame frame for UVs in texels.
	 */
	private static void DrawMaster(Rect screenRect, Texture texture, Rect uvFrame, DrawParameters dp, RectOffset origionalBorder = null)
	{				
		//3ms for these 4 lines
		if (!isRepaint)
			return;
		if (texture == null)
			return;
		if (!dp.IsVisibile(texture))
			return;
		if (dp.Scale.x * dp.Scale.y == 0)
			return;

		screenRect.width *= dp.Scale.x;
		screenRect.height *= dp.Scale.y;

		Material material = dp.GetPreparedMaterial(texture);
	
		// --------------------------------------
		// Transform and clip.

		Vector4 clipping = Vector4.zero;

		Rect transformedRect = TransformTheRect(screenRect);
		Rect clippedRect = transformedRect;
		Rect sourceRect = new Rect(uvFrame);

		int borderLeft = origionalBorder == null ? 0 : origionalBorder.left;
		int borderRight = origionalBorder == null ? 0 : origionalBorder.right;
		int borderTop = origionalBorder == null ? 0 : origionalBorder.top;
		int borderBottom = origionalBorder == null ? 0 : origionalBorder.bottom;

		// No clipping for rotated sprites yet.
		bool disableClip = dp.Rotation != 0;

		if (!disableClip) {
			clippedRect = ClipTheRect(transformedRect, ref clipping);
			bool hasClipped = (clipping != Vector4.zero);

			//stub:
			/*
			if (hasClipped) {
				SmartUI.Color = Color.red;
				if (screenRect.Contains(new Vector2(InputEx.mousePosition.x, InputEx.mousePosition.y)))
					Trace.LogDebug("Clipped {0} to {1}", transformedRect, clippedRect);
			} else
				SmartUI.Color = Color.green;
			*/

			if ((clippedRect.width <= 0) || (clippedRect.height <= 0))
				return;

			if (hasClipped) {
				// Calculate clipped UVs.
				sourceRect = new Rect();
				sourceRect.min = PixelToUV(transformedRect, uvFrame, clippedRect.min, origionalBorder);
				sourceRect.max = PixelToUV(transformedRect, uvFrame, clippedRect.max, origionalBorder);

				// Upside down swap.  This has something todo with the fact that the screen has (0,0) at the top left
				// and the texture has (0,0) at the bottom left.
				float newMin = uvFrame.yMin + (uvFrame.yMax - sourceRect.yMax);
				float newMax = uvFrame.yMax + (uvFrame.yMin - sourceRect.yMin);
				sourceRect.yMin = newMin;
				sourceRect.yMax = newMax;


				// Adjust border for clipping. 
				if (origionalBorder != null) {
					clipping[0] /= dp.Scale.x;
					clipping[2] /= dp.Scale.x;
					clipping[1] /= dp.Scale.y;
					clipping[3] /= dp.Scale.y;	

					borderLeft = Util.ClampInt(borderLeft - (int)clipping[0], 0, int.MaxValue);
					borderRight = Util.ClampInt(borderRight - (int)clipping[2], 0, int.MaxValue);

					borderTop = Util.ClampInt(borderTop - (int)clipping[1], 0, int.MaxValue);
					borderBottom = Util.ClampInt(borderBottom - (int)clipping[3], 0, int.MaxValue);


				}
			} 
		} 

		// Convert source rect to normalised. 
		sourceRect.xMin *= texture.texelSize.x;
		sourceRect.xMax *= texture.texelSize.x;
		sourceRect.yMin *= texture.texelSize.y;
		sourceRect.yMax *= texture.texelSize.y;

		// Work out the best way to draw this.
		DrawMethod drawMethod = DrawMethod.Standard;

		if (origionalBorder != null)
			drawMethod = DrawMethod.Bordered;

		if ((origionalBorder != null) && (texture.HighDPI()))
			drawMethod = DrawMethod.Native;

		if (ShowGuiMaterial && !dp.IsDebugDraw)
			drawMethod = DrawMethod.DebugColor;

		// Apply rotation
		if (dp.Rotation != 0) {
			Engine.PushGUI();
			var rotationMatrix = 
				Extentions.GetTranslationMatrix(clippedRect.center.x, clippedRect.center.y, 0) *
				Extentions.GetRotationMatrix(0, 0, dp.Rotation) *
				Extentions.GetTranslationMatrix(-clippedRect.center.x, -clippedRect.center.y, 0);

			GUI.matrix *= rotationMatrix;
		}

		// Draw the thing.
		switch (drawMethod) {

			case DrawMethod.DebugColor:
				Color = dp.DebugColor;
				DrawMaster(screenRect, StockTexture.White, new Rect(0, 0, 2, 2), DrawParameters.DebugDraw);
				break;
		
			case DrawMethod.Standard:			
				Graphics.DrawTexture(clippedRect, texture, sourceRect, 0, 0, 0, 0, SmartUI.Color, material);
				break;
		
			case DrawMethod.Bordered:
				Graphics.DrawTexture(clippedRect, texture, sourceRect, borderLeft, borderRight, borderTop, borderBottom, SmartUI.Color, material);
				break;
		
		// The native draw will reverse the DPI scaling on the Gui matrix and then draw the object at an adjusted size.
		// This allows things like bordered gui objects to display correctly without distortion.
		
			case DrawMethod.Native:
				const float scale = 2;
				Engine.PushGUI();
				GUI.matrix *= Matrix4x4.Scale(Vector3.one / scale);
				clippedRect = clippedRect.Scaled(scale);
				Graphics.DrawTexture(clippedRect, texture, sourceRect, (int)(borderLeft * scale), (int)(borderRight * scale), (int)(borderTop * scale), (int)(borderBottom * scale), SmartUI.Color, material);
				Engine.PopGUI();
				break;
		}

		if (ShowGuiBounds && !dp.IsDebugDraw) {	
			Color = Color.red;
			DrawMaster(screenRect, StockTexture.SimpleRect, new Rect(0, 0, 4, 4), DrawParameters.DebugDraw, BORDER_2PX);
		}

		if (dp.Rotation != 0) {
			Engine.PopGUI();
		}

		Color = Color.white;
	}

	/** Render batch isn't implemented yet */
	public static void RenderBatch()
	{
		if (EnableBatching) {
			throw new NotImplementedException("GUI batching is not implemented yet, please don't enable.");
		}
	}

	public static bool isRepaint {
		get { return DrawEvenIfNotRepaint || Engine.UnityOnGuiIsRepaint; }
	}

}
