using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gameplay
{
	[RequireComponent(typeof(Tilemap))]
	public class HudArrowTilemap : MonoBehaviour
	{
		protected Tilemap mMainTilemap;

		protected void Awake()
		{
			TouchTilemap();
			ClearAllArrow();
		}

		public void TouchTilemap()
		{
			if (!mMainTilemap)
				mMainTilemap = GetComponent<Tilemap>();
		}

		public void ClearAllArrow()
		{
			TouchTilemap();
			mMainTilemap.ClearAllTiles();
		}


		#region TileList

		public List<Tile> arrowTileList = new(13);

		public Tile NoneTile => null;
		public Tile PointTile => arrowTileList[12];

		public Tile UpPlusArrowTile => arrowTileList[0];
		public Tile UpDbArrowTile => arrowTileList[1];
		public Tile UpMoveArrowTile => arrowTileList[2];

		public Tile DownPlusArrowTile => arrowTileList[3];
		public Tile DownDbArrowTile => arrowTileList[4];
		public Tile DownMoveArrowTile => arrowTileList[5];

		public Tile LeftPlusArrowTile => arrowTileList[6];
		public Tile LeftDbArrowTile => arrowTileList[7];
		public Tile LeftMoveArrowTile => arrowTileList[8];

		public Tile RightPlusArrowTile => arrowTileList[9];
		public Tile RightDbArrowTile => arrowTileList[10];
		public Tile RightMoveArrowTile => arrowTileList[11];

		#endregion TileList


		public enum ArrowDir
		{
			None,
			Point,
			Up,
			Down,
			Left,
			Right,
		}
		public enum ArrowType
		{
			None,
			Plus,
			Double,
			Move,
		}

		protected Tile GetArrowTile(ArrowDir arrowDir, ArrowType arrowType)
		{
			if (arrowDir == ArrowDir.None)
				return NoneTile;
			else if (arrowDir == ArrowDir.Point)
				return PointTile;
			else if(arrowType == ArrowType.None)
				return NoneTile;
			else if (arrowDir == ArrowDir.Up)
				return arrowType == ArrowType.Plus ? UpPlusArrowTile
					: arrowType == ArrowType.Double ? UpDbArrowTile : UpMoveArrowTile;
			else if (arrowDir == ArrowDir.Down)
				return arrowType == ArrowType.Plus ? DownPlusArrowTile
					: arrowType == ArrowType.Double ? DownDbArrowTile : DownMoveArrowTile;
			else if (arrowDir == ArrowDir.Left)
				return arrowType == ArrowType.Plus ? LeftPlusArrowTile
					: arrowType == ArrowType.Double ? LeftDbArrowTile : LeftMoveArrowTile;
			else if (arrowDir == ArrowDir.Right)
				return arrowType == ArrowType.Plus ? RightPlusArrowTile
					: arrowType == ArrowType.Double ? RightDbArrowTile : RightMoveArrowTile;
			else
				return NoneTile;
		}

		protected void SetArrow(List<Vector2Int> posList, Tile tile)
		{
			foreach (var pos in posList)
				mMainTilemap.SetTile((Vector3Int)pos, tile);
		}
		protected void SetArrow(in Vector2Int pos, Tile tile)
		{
			mMainTilemap.SetTile((Vector3Int)pos, tile);
		}

		public void SetArrow(List<Vector2Int> posList, ArrowDir arrowDir, ArrowType arrowType)
		{
			SetArrow(posList, GetArrowTile(arrowDir, arrowType));
		}

		protected ArrowDir OffsetDirToArrowType(in Vector2Int dir)
		{
			if (dir == Vector2Int.up)
				return ArrowDir.Up;
			else if (dir == Vector2Int.down)
				return ArrowDir.Down;
			else if (dir == Vector2Int.right)
				return ArrowDir.Right;
			else if (dir == Vector2Int.left)
				return ArrowDir.Left;
			else
				return ArrowDir.Up;
		}
		public void SetPlusArrow(List<Vector2Int> posList, in Vector2Int dir)
		{
			SetArrow(posList, GetArrowTile(OffsetDirToArrowType(dir), ArrowType.Plus));
		}
		public void SetDoubleArrow(List<Vector2Int> posList, in Vector2Int dir)
		{
			SetArrow(posList, GetArrowTile(OffsetDirToArrowType(dir), ArrowType.Double));
		}
		public void SetMoveArrow(List<Vector2Int> posList, in Vector2Int dir)
		{
			SetArrow(posList, GetArrowTile(OffsetDirToArrowType(dir), ArrowType.Move));
		}

		public void SetArrowPoint(in Vector2Int pos)
		{
			SetArrow(pos, GetArrowTile(ArrowDir.Point, ArrowType.None));
		}
	}
}
