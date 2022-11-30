using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
//using System.Linq;

namespace Gameplay
{
	/// <summary>
	/// 图块地图数据类
	/// 可操作图块显示和储存图块数据
	/// </summary>
	[RequireComponent(typeof(Tilemap))]
	public class GpTilemapData : MonoBehaviour
	{
		protected void Awake()
		{
			TouchNativeTilemap();
		}

		public virtual void ClearMapData()
		{
			mMapWidth = 0;
			mMapHeight = 0;
			mGpTileDataDict.Clear();
			TouchNativeTilemap().ClearAllTiles();
		}


		#region TileList

		[SerializeField]
		protected List<Tile> whiteNonNumTiles = new(6);
		[SerializeField]
		protected List<Tile> redNonNumTiles = new(6);
		[SerializeField]
		protected List<Tile> blueNonNumTiles = new(6);
		[SerializeField]
		protected List<Tile> grayRedNonNumTiles = new(6);
		[SerializeField]
		protected List<Tile> grayBlueNonNumTiles = new(6);

		[SerializeField]
		protected List<Tile> whiteNumTiles = new(9);
		[SerializeField]
		protected List<Tile> redNumTiles = new(9);
		[SerializeField]
		protected List<Tile> blueNumTiles = new(9);
		[SerializeField]
		protected List<Tile> grayRedNumTiles = new(9);
		[SerializeField]
		protected List<Tile> grayBlueNumTiles = new(9);

		#endregion TileList


		#region GetTile

		public static readonly int GpTileData_MAX_Value = 8;

		public bool IsValidValue(int value)
		{
			return 0 < value && value <= GpTileData_MAX_Value;
		}

		public List<Tile> GetNumberTiles(in GpTileData data)
		{
			return data.IsColorRed ? (data.grayColor ? grayRedNumTiles : redNumTiles)
				: data.IsColorBlue ? (data.grayColor ? grayBlueNumTiles : blueNumTiles)
				: whiteNumTiles;
		}
		public List<Tile> GetNonNumberTiles(in GpTileData data)
		{
			return data.IsColorRed ? (data.grayColor ? grayRedNonNumTiles : redNonNumTiles)
				: data.IsColorBlue ? (data.grayColor ? grayBlueNonNumTiles : blueNonNumTiles)
				: whiteNonNumTiles;
		}


		public int GetNumberTileIndex(in GpTileData data)
		{
			return IsValidValue(data.numValue) ? data.numValue : 0;
		}
		public int GetNonNumberTileIndex(in GpTileData data)
		{
			if (data.opened)
				return data.hasMine ? 5 : 2;
			else if (data.holded)
				return data.IsFlagMark ? 7 : (data.IsFlagMine ? 4 : 1);
			else
				return data.IsFlagMark ? 6 : (data.IsFlagMine ? 3 : 0);
		}

		public Tile GetTile(in GpTileData data)
		{
			if (data.opened && data.IsNumber)
				return GetNumberTiles(data)[GetNumberTileIndex(data)];
			else
				return GetNonNumberTiles(data)[GetNonNumberTileIndex(data)];
		}

		#endregion GetTile


		#region NativeTilemap

		protected Tilemap mNativeTilemap;
		protected TilemapRenderer mTilemapRenderer;

		protected void InitNativeTilemap()
		{
			if (mNativeTilemap == null)
			{
				mNativeTilemap = GetComponent<Tilemap>();
				mTilemapRenderer = GetComponent<TilemapRenderer>();
			}
		}

		public Tilemap TouchNativeTilemap()
		{
			InitNativeTilemap();
			return mNativeTilemap;
		}

		public void SetTilemapVisible(bool visible)
		{
			if (mTilemapRenderer)
				mTilemapRenderer.enabled = visible;
		}

		#endregion NativeTilemap


		#region SetTile

		protected Dictionary<Vector2Int, GpTileData> mGpTileDataDict = new(64);

		public void SetTile(in Vector2Int pos, GpTileData gpTileData)
		{
			if (mGpTileDataDict.ContainsKey(pos))
				mGpTileDataDict[pos] = gpTileData;
			else
				mGpTileDataDict.Add(pos, gpTileData);
			mNativeTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), GetTile(gpTileData));
		}

		public void RefreshTile(in Vector2Int pos)
		{
			var gpTileData = GetGpTileData(pos);
			if (gpTileData != null)
				mNativeTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), GetTile(gpTileData));
		}

		#endregion SetTile


		#region EmptyTile

		protected GpTileData CreateEmptyTileData(in Vector2Int pos)
		{
			GpTileData data = new(pos, 0);
			data.SetGrayColor(true);
			data.SetOpened(true);
			return data;
		}

		public void SetEmptyTile(in Vector2Int pos)
		{
			SetTile(pos, CreateEmptyTileData(pos));
		}

		#endregion EmptyTile

		// TODO : 对象池


		#region RemoveTile

		public void RemoveTile(in Vector2Int pos)
		{
			if (mGpTileDataDict.ContainsKey(pos))
				mGpTileDataDict.Remove(pos);
			mNativeTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), null);
		}

		#endregion


		#region MapSize

		protected int mMapWidth = 0;
		protected int mMapHeight = 0;

		public int MapWidth => mMapWidth;
		public int MapHeight => mMapHeight;

		public void InitMapSize(int width, int height)
		{
			if (width <= 0 || height <= 0)
			{
				Debug.LogWarningFormat("Invalid map size {0}x{1}", width, height);
				return;
			}

			ClearMapData();
			foreach (var pos in EachMapPos(width, height))
			{
				var data = new GpTileData(pos);
				SetTile(pos, data);
			}
			mMapWidth = width;
			mMapHeight = height;
		}

		public IEnumerable<Vector2Int> EachMapPos(int width, int height)
		{
			Vector2Int pos = Vector2Int.zero;
			for (int i = 0; i < width; ++i)
			{
				pos.x = i;
				for (int j = 0; j < height; ++j)
				{
					pos.y = j;
					yield return pos;
				}
			}
		}

		#endregion MapSize


		#region GetTileData

		public bool IsValidPos(in Vector2Int pos)
		{
			return mGpTileDataDict.ContainsKey(pos);
		}

		public GpTileData GetGpTileData(in Vector2Int pos)
		{
			if (IsValidPos(pos))
				return mGpTileDataDict[pos];
			else
				return null;
		}

		public IEnumerable<Vector2Int> EachMapPos()
		{
			return mGpTileDataDict.Keys;
		}

		#endregion GetTileData


		#region SetDataValue

		public void SetDataColorWhite(in Vector2Int pos)
		{
			GetGpTileData(pos)?.SetColorWhite();
			RefreshTile(pos);
		}
		public void SetDataColorRed(in Vector2Int pos)
		{
			GetGpTileData(pos)?.SetColorRed();
			RefreshTile(pos);
		}
		public void SetDataColorBlue(in Vector2Int pos)
		{
			GetGpTileData(pos)?.SetColorBlue();
			RefreshTile(pos);
		}

		public void SetDataGrayColor(in Vector2Int pos, bool value)
		{
			GetGpTileData(pos)?.SetGrayColor(value);
			RefreshTile(pos);
		}

		public void SetDataMine(in Vector2Int pos)
		{
			GetGpTileData(pos)?.SetMine();
			RefreshTile(pos);
		}
		public void SetDataNumber(in Vector2Int pos, int value)
		{
			if (IsValidPos(pos))
			{
				GetGpTileData(pos)?.SetNumber(value);
				RefreshTile(pos);
			}
		}

		public void SetDataFlagNone(in Vector2Int pos)
		{
			GetGpTileData(pos)?.SetFlagNone();
			RefreshTile(pos);
		}
		public void SetDataFlagMine(in Vector2Int pos)
		{
			GetGpTileData(pos)?.SetFlagMine();
			RefreshTile(pos);
		}
		public void SetDataFlagMark(in Vector2Int pos)
		{
			GetGpTileData(pos)?.SetFlagMark();
			RefreshTile(pos);
		}

		public void SetDataHolded(in Vector2Int pos, bool value)
		{
			GetGpTileData(pos)?.SetHolded(value);
			RefreshTile(pos);
		}
		public void SetDataOpened(in Vector2Int pos, bool value)
		{
			GetGpTileData(pos)?.SetOpened(value);
			RefreshTile(pos);
		}

		#endregion SetDataValue


		// 区分 显示层 和 数据层/逻辑层


		#region PosOffset

		protected Vector2Int mPosOffset = Vector2Int.zero;
		public Vector2Int PosOffset => mPosOffset;

		public void SetPosOffset(Vector2Int offset)
		{
			mPosOffset = offset;
		}

		public void ClearPosOffset()
		{
			SetPosOffset(Vector2Int.zero);
		}

		#endregion PosOffset
	}
}
