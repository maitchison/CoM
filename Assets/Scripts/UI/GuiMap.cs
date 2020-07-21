
using System;
using UnityEngine;
using Mordor;

namespace UI
{
	public enum MapMode
	{
		/** The whole map is shown without tracking */
		Full,
		/** A smaller map is shown which tracks the party */
		Small
	}

	public enum TileRenderMode
	{
		/** Renders the tile normally */
		Normal,
		/** Renders the tile including the south and west walls */
		Full,
		/** Renders only the east wall */
		EastWall,
		/** Renders only the west wall */
		NorthWall
	}

	/** Component to display the current map */
	public class GuiMap : GuiWindow
	{
		/** Pixels to keep blank at edge of map */
		private const int MAP_FRAME = 1;

		/** Size of each tile on map */
		private const int TILE_SIZE = 15;

		/** The index of the icon to use to draw the party location (with east,south,west orientations following) */
		private const int PARTY_DIRECTION_ICON = 32;

		private static int FULL_SIZE = 30 * TILE_SIZE;
		private static int SMALL_SIZE = 10 * TILE_SIZE;

		/** if true the maps background image will be fixed in place, otherwise it will be streched out to cover the 
		 * whole map and panned around */
		private static bool STATIC_BACKGROUND = false;
		/** Number of additional tiles to stretch border over.  The edge tiles will always be rock so -1 will fit the 
		 * map perfectly. */
		private static int BACKGROUND_BORDER = 0;

		/** Canvas used to draw map onto */
		public Texture2D MapCanvas;

		/** The map to display */
		public MDRMap Map {
			get { return _map; }
			set {
				if (_map != value) {
					Dirty = true;
					_map = value;
					CreateCanvas();
				}
			}
		}

		/** The party to track */
		public MDRParty Party;

		public MapMode Mode { get { return _mode; } set { SetMode(value); } }

		/** If true, and if a party is assigned then the party will be tracked, and the map centered on them */
		public bool TrackParty;

		/** If true map needs to be redrawn */
		protected bool Dirty = true;
		
		/** The icons to use to draw the map */
		protected Sprite[] MapIcons;

		/** The number of pixels from the top left to offset the map */
		protected Vector2 MapDisplayOffset;

		private MDRMap _map;
		private MapMode _mode;

		public GuiMap(int x, int y, MDRParty party = null)
			: base(x, y)
		{
			Util.Assert((CoM.Instance != null) && CoM.GraphicsLoaded, "GUI graphics must be loaded in order to create a GuiMap component.");
			MapIcons = CoM.Instance.MapIconSprites.GetSprites().ToArray(); 
			this.Party = party;
			if (party != null)
				this.Map = Party.ExploredMap;

			Background.Color = Color.white;
			Background.Sprite = ResourceManager.GetSprite("Backgrounds/DarkMap");
			if (!STATIC_BACKGROUND) {
				Background.Align = GuiAlignment.None;
				Background.Width = (32 + (BACKGROUND_BORDER * 2)) * TILE_SIZE;
				Background.Height = (32 + (BACKGROUND_BORDER * 2)) * TILE_SIZE;
			} else
				Background.Align = GuiAlignment.Full;

			Mode = MapMode.Full;
		}

		/** Draw the map */
		public override void DrawContents()
		{

			if (Map == null)
				return;

			if (Dirty)
				RenderMap();
				
			// calculate our offset
			if (TrackParty && (Party != null)) {
				MapDisplayOffset.x = MAP_FRAME + (int)((Party.LocationX) * TILE_SIZE - Width / 2);
				MapDisplayOffset.y = MAP_FRAME + (int)((30 - Party.LocationY + 1.5f) * TILE_SIZE - Height / 2);
			} else {
				MapDisplayOffset.Set(0.0f, 0.0f);
			}

			if (Background != null) {
				if (Mode == MapMode.Small) {
					Background.X = (-Party.LocationX + 5 - BACKGROUND_BORDER) * TILE_SIZE;
					Background.Y = (Party.LocationY - 32 + 6 - BACKGROUND_BORDER) * TILE_SIZE;
				} else {
					Background.X = 0;
					Background.Y = 0;
				}
				Background.Draw();
			}
				
			Rect textureRect = new Rect(MapDisplayOffset.x, (MapCanvas.height - Height - MapDisplayOffset.y), Width, Height);
		
			SmartUI.Color = new Color(0.8f, 0.8f, 0.8f);
			SmartUI.Draw(new Rect(0, 0, Width, Height), MapCanvas, textureRect, DrawParameters.Default);

			DrawParty();
		}

		/** Sets the current map display mode */
		private void SetMode(MapMode newMode)
		{
			_mode = newMode;

			switch (Mode) {
				case MapMode.Full:
					TrackParty = false;
					Width = FULL_SIZE + MAP_FRAME * 2 + Style.padding.horizontal; 
					Height = FULL_SIZE + MAP_FRAME * 2 + Style.padding.vertical + TitleHeight;
					break;
				case MapMode.Small:
					TrackParty = true;
					Width = SMALL_SIZE;
					Height = SMALL_SIZE + TitleHeight;
					break;
			}
		}

		/** Creates a canvas large enough to display the current map */
		private void CreateCanvas()
		{
			int textureWidth = (TILE_SIZE * Map.Width) + MAP_FRAME * 2;
			int textureHeight = (TILE_SIZE * Map.Height) + MAP_FRAME * 2;

			MapCanvas = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
			MapCanvas.filterMode = Engine.GuiScale < 1f ? FilterMode.Bilinear : FilterMode.Point;
			MapCanvas.wrapMode = TextureWrapMode.Clamp;
		}

		/** 
		 * Converts from map location to canvas texel co-ords of that tiles upper left corner.
		 */
		private Vector2 TileToTexel(int x, int y)
		{
			//note, mordor maps are upside down, but textures are too, so no need to reverse the y
			return new Vector2(MAP_FRAME + (x) * TILE_SIZE, MAP_FRAME + (y) * TILE_SIZE);
		}

		/** 
		 * Converts from map location to screen co-ords of that tiles upper left corner.
		 */
		private Vector2 TileToScreen(int x, int y)
		{
			return new Vector2(
				MAP_FRAME + (x) * TILE_SIZE - MapDisplayOffset.x,
				MAP_FRAME + (Map.Height - 1 - y) * TILE_SIZE - MapDisplayOffset.y);
		}

		/** If there is a party then draws the party icon at the correct orientation */
		private void DrawParty()
		{
			if (Party == null)
				return;
			Vector2 location = TileToScreen(Party.LocationX, Party.LocationY);
			Sprite partyIcon = MapIcons[PARTY_DIRECTION_ICON + Party.Facing.Sector];
			SmartUI.Draw(location.x, location.y, partyIcon);
		}

		/** Draws a fragment of a map tile (i.e. door etc) */
		private void DrawFragment(Vector2 location, int index)
		{
			int offsetX;
			int offsetY;
			offsetX = offsetY = 0;
			
			if (FieldRecord.isEastWallBit(index))
				offsetX = +7;
			 
			if (FieldRecord.isNorthWallBit(index))
				offsetY = +7;
			
			TextureMagic.BlitWithAlpha(MapIcons[index], MapCanvas, (int)location.x + offsetX, (int)location.y + offsetY);      
		}

		public override void DoDoubleClick()
		{
			if (Mode == MapMode.Full)
				Mode = MapMode.Small;
			else
				Mode = MapMode.Full;
		}

		/** 
		 * Renders a single tile to the map canvas.  The canvas will not be updated so you will need to call
		 * mapCanvas.Apply() in order to see the results 
		 * 
		 * @param accent.  Draws tile is a different way, used to indicate newly discovered tiles.
		 */
		public void RenderTile(int tileX, int tileY, TileRenderMode mode = TileRenderMode.Normal, bool accent = false)
		{
			if (!Map.InBounds(tileX, tileY))
				throw new Exception("Tile (" + tileX + "," + tileY + ") out of bounds");

			FieldRecord field = Map.GetField(tileX, tileY);
			Vector2 location = TileToTexel(tileX, tileY);

			const int startIndex = 0;
			const int endIndex = 38;

			if (mode == TileRenderMode.Normal || mode == TileRenderMode.Full) {
				if (!field.Explored) {
					TextureMagic.FillRect(MapCanvas, (int)location.x, (int)location.y, TILE_SIZE, TILE_SIZE, Colors.MAP_UNEXPLOREDTILE_COLOR);
				} else {	
					if (accent) {
						TextureMagic.FillRect(MapCanvas, (int)location.x, (int)location.y, TILE_SIZE, TILE_SIZE, new Color(0f, 0f, 0f, 0.5f));
						TextureMagic.FillRect(MapCanvas, (int)location.x + 1, (int)location.y + 1, TILE_SIZE - 1, TILE_SIZE - 1, new Color(1f, 0f, 0.5f, 0.25f));
					} else
						TextureMagic.FillRect(MapCanvas, (int)location.x, (int)location.y, TILE_SIZE, TILE_SIZE, Color.clear);
				}
			}

			for (int index = startIndex; index < endIndex; index++) {
				if (mode == TileRenderMode.EastWall && !FieldRecord.isEastWallBit(index))
					continue;
				   
				if (mode == TileRenderMode.NorthWall && !FieldRecord.isNorthWallBit(index))
					continue;

				if (field.GetBit(index))
					DrawFragment(location, index);				                                
			}

			if (mode == TileRenderMode.Full) {
				if (tileX >= 1)
					RenderTile(tileX - 1, tileY, TileRenderMode.EastWall);
				if (tileY >= 1)
					RenderTile(tileX, tileY - 1, TileRenderMode.NorthWall);
			}
		}

		/** Marks map as needing to redraw. */
		public void Repaint()
		{
			Dirty = true;
		}

		/** renders the map tiles to a texture */
		private void RenderMap()
		{
			DateTime startTime = DateTime.Now;
			
			TextureMagic.Clear(MapCanvas, new Color(0, 0, 0, 1));
			//TextureMagic.FrameRect(MapCanvas, 0, 0, MapCanvas.width, MapCanvas.height, Settings.MAP_UNEXPLOREDTILE_COLOR);

			for (int ylp = 1; ylp < Map.Height - 1; ylp++) {
				for (int xlp = 1; xlp < Map.Width - 1; xlp++) {
					// full is needed so that walls west from unexplored tiles are drawn on exlplored tiles
					RenderTile(xlp, ylp, TileRenderMode.Full);		
				}
			}
			
			DateTime endTime = DateTime.Now;

			MapCanvas.Apply();
			Dirty = false;
			
			Trace.Log("Map rendered in " + (endTime - startTime).TotalMilliseconds + "ms");
			
		}

	}
}
