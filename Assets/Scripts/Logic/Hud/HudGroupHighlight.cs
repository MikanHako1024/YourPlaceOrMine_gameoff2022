using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gameplay
{
	[RequireComponent(typeof(GpTilemapData))]
	public class HudGroupHighlight : MonoBehaviour
	{
		[SerializeField]
		protected GpTilemapData mainGpTilemap;

		[SerializeField]
		protected Tilemap borderTilemap;

		[SerializeField]
		protected Tile blueBorderTile;
		[SerializeField]
		protected Tile redBorderTile;

		protected void Awake()
		{
			if (!mainGpTilemap)
				mainGpTilemap = GetComponent<GpTilemapData>();

			ClearHighlight();
		}


		public void ClearHighlight()
		{
			mainGpTilemap.ClearMapData();
			borderTilemap.ClearAllTiles();
		}

		public void AddHighlightPos(in Vector2Int pos, GpTileData data, bool isRed)
		{
			mainGpTilemap.SetTile(pos, data);
			borderTilemap.SetTile((Vector3Int)pos, isRed ? redBorderTile : blueBorderTile);
		}

		public void RefreshTile(in Vector2Int pos)
		{
			mainGpTilemap.RefreshTile(pos);
		}
	}
}
