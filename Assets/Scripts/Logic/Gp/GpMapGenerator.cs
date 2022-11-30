using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityRandom = UnityEngine.Random;
using SysRandom = System.Random;

namespace Gameplay
{
	public class GpMapGenerator : BaseManager<GpMapGenerator>
	{
		protected override void InitManager()
		{
			base.InitManager();
			ResetRandomSeed();
		}

		protected static readonly int MAX_RANDOM_SEED = 0x7FFFFFFF;

		protected int mLastRandomSeed = 0;
		public int RandomSeed => mLastRandomSeed;

		protected int MakeRandomSeed()
		{
			//mLastRandomSeed = MAX_RANDOM_SEED * UnityRandom.Range(0, MAX_RANDOM_SEED);
			//return mLastRandomSeed;
			return MAX_RANDOM_SEED * UnityRandom.Range(0, MAX_RANDOM_SEED);
		}

		public void ResetRandomSeed(int seed)
		{
			mLastRandomSeed = seed;
		}
		public void ResetRandomSeed()
		{
			mLastRandomSeed = MakeRandomSeed();
		}


		public SysRandom GetRandomUtil(int seed)
		{
			return new SysRandom(seed);
		}
		public SysRandom GetRandomUtil()
		{
			return new SysRandom(mLastRandomSeed);
		}


		public void GenerateRandomMap(SysRandom random, GpTilemapData gpTilemap, int mineNum)
		{
			GenerateMine(random, gpTilemap, mineNum);
			GenerateNumberForMine(random, gpTilemap);
		}


		protected static readonly float GenMine_Optimize_Threshold_Ratio = .4f;

		protected static List<Vector2Int> mTempPosList = new(64);
		protected static HashSet<Vector2Int> mTempPosSet = new(64);

		public void GenerateMine(SysRandom random, GpTilemapData gpTilemap, int mineNum)
		{
			float mineRatio = 1.0f * mineNum / (gpTilemap.MapWidth * gpTilemap.MapHeight);
			if (mineRatio >= 1.0)
			{
				Debug.LogErrorFormat("The amount {0} of mine is too many for map {1}x{2}", mineNum, gpTilemap.MapWidth, gpTilemap.MapHeight);
				foreach (var pos in gpTilemap.EachMapPos())
					gpTilemap.SetDataMine(pos);
			}
			else if (mineRatio >= GenMine_Optimize_Threshold_Ratio)
			{
				// optimize
				int rnd;
				var posList = mTempPosList;
				posList.Clear();
				foreach (var pos in gpTilemap.EachMapPos())
					posList.Add(pos);
				for (int count = 0; count < mineNum; ++count)
				{
					rnd = random.Next(posList.Count);
					posList.RemoveAt(rnd);
					gpTilemap.SetDataMine(posList[rnd]);
				}
				posList.Clear();
			}
			else
			{
				// non optimize
				int maxLoop = 1000;
				int size = gpTilemap.MapWidth * gpTilemap.MapHeight;
				int rnd;
				var posSet = mTempPosSet;
				Vector2Int pos = Vector2Int.zero;
				posSet.Clear();
				for (int count = 0, loop = 0; count < mineNum && loop < maxLoop; ++loop)
				{
					rnd = random.Next(size);
					pos.x = rnd % gpTilemap.MapWidth;
					pos.y = rnd / gpTilemap.MapWidth;
					if (!posSet.Contains(pos))
					{
						gpTilemap.SetDataMine(pos);
						posSet.Add(pos);
						++count;
					}
				}
				posSet.Clear();
			}
		}


		protected IEnumerable<Vector2Int> EachAround3X3Pos(Vector2Int center)
		{
			Vector2Int pos = Vector2Int.zero;
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					pos.x = center.x + i;
					pos.y = center.y + j;
					yield return pos;
				}
			}
		}

		public void GenerateNumberForMine(SysRandom random, GpTilemapData gpTilemap)
		{
			var posSet = mTempPosSet;
			posSet.Clear();
			foreach (var pos in gpTilemap.EachMapPos())
				if (gpTilemap.GetGpTileData(pos).hasMine)
					posSet.Add(pos);

			int count;
			foreach (var center in gpTilemap.EachMapPos())
			{
				if (posSet.Contains(center))
					continue;
				count = 0;
				foreach (var pos in EachAround3X3Pos(center))
					if (posSet.Contains(pos))
						++count;
				gpTilemap.SetDataNumber(center, count);
			}
			posSet.Clear();
		}


		protected int MinDatumRegionSplitLen = 4;
		protected int MaxDatumRegionSplitLen = 8;
		protected int DatumRegionPadding = 1;

		protected KeyValuePair<Vector2Int[], int>[] DatumShapeArray = new KeyValuePair<Vector2Int[], int>[] {
			/* . + .
			 * . + +
			 * . . .
			 */
			new KeyValuePair<Vector2Int[], int>(new Vector2Int[]{
				Vector2Int.up,
				Vector2Int.zero, Vector2Int.right,
			}, 3),
			/* . . .
			 * + + +
			 * . . .
			 */
			new KeyValuePair<Vector2Int[], int>(new Vector2Int[]{
				Vector2Int.left, Vector2Int.zero, Vector2Int.right,
			}, 3),
			/* . + +
			 * . + +
			 * . . .
			 */
			new KeyValuePair<Vector2Int[], int>(new Vector2Int[]{
				Vector2Int.up, Vector2Int.up + Vector2Int.right,
				Vector2Int.zero, Vector2Int.right,
			}, 2),
			/* + + .
			 * . + +
			 * . . .
			 */
			new KeyValuePair<Vector2Int[], int>(new Vector2Int[]{
				Vector2Int.up + Vector2Int.left, Vector2Int.up,
				Vector2Int.zero, Vector2Int.right,
			}, 2),
			/* . . +
			 * + + +
			 * . . .
			 */
			new KeyValuePair<Vector2Int[], int>(new Vector2Int[]{
				Vector2Int.up + Vector2Int.right,
				Vector2Int.left, Vector2Int.zero, Vector2Int.right,
			}, 2),
			/* . + .
			 * + + +
			 * . . .
			 */
			new KeyValuePair<Vector2Int[], int>(new Vector2Int[]{
				Vector2Int.up,
				Vector2Int.left, Vector2Int.zero, Vector2Int.right,
			}, 1),
			/* . + .
			 * + + +
			 * . + .
			 */
			new KeyValuePair<Vector2Int[], int>(new Vector2Int[]{
				Vector2Int.up,
				Vector2Int.left, Vector2Int.zero, Vector2Int.right,
				Vector2Int.down,
			}, 1),
			/* + + +
			 * . . +
			 * . . +
			 */
			new KeyValuePair<Vector2Int[], int>(new Vector2Int[]{
				Vector2Int.up + Vector2Int.left, Vector2Int.up, Vector2Int.up + Vector2Int.right,
				Vector2Int.right,
				Vector2Int.down + Vector2Int.right,
			}, 1),
		};

		protected IEnumerable<Vector2Int> RotateShapePos(Vector2Int[] posArr, int rotate)
		{
			rotate = ((rotate % 4) + 4) % 4;
			foreach (var pos in posArr)
			{
				if (rotate == 0)
					yield return pos;
				else if (rotate == 1)
					yield return new Vector2Int(pos.y, -pos.x);
				else if (rotate == 1)
					yield return new Vector2Int(-pos.x, -pos.y);
				else if (rotate == 1)
					yield return new Vector2Int(-pos.y, pos.x);
				else
					yield return pos;
			}
		}

		protected Vector2Int[] MakeRandomShape(SysRandom random)
		{
			int totalWeight = 0;
			foreach (var each in DatumShapeArray)
				totalWeight += each.Value;

			int rnd = random.Next(totalWeight);
			foreach (var each in DatumShapeArray)
			{
				rnd -= each.Value;
				if (rnd < 0)
					return each.Key;
			}
			return DatumShapeArray[0].Key;
		}

		public IEnumerable GenerateColorStep(SysRandom random, GpTilemapData gpTilemap)
		{
			// 设置若干基准点，再在基准点上随机放置一个形状

			int mapWidth = gpTilemap.MapWidth;
			int mapHeight = gpTilemap.MapHeight;

			// 基准点
			int minSplitLen = MinDatumRegionSplitLen;
			int maxSplitLen = MaxDatumRegionSplitLen;
			int splitXCount = random.Next(mapWidth / maxSplitLen, mapWidth / minSplitLen);
			int splitYCount = random.Next(mapHeight / maxSplitLen, mapHeight / minSplitLen);
			// 个数至少2x2 这样可以保证 红蓝都至少有一块
			splitXCount = Mathf.Max(2, splitXCount);
			splitYCount = Mathf.Max(2, splitYCount);

			int padding = DatumRegionPadding;
			int startByBlue = random.Next(0, 2);
			int minX, maxX, minY, maxY;
			for (int i = 0; i < splitXCount; i++)
			{
				minX = Mathf.RoundToInt(1f * i * mapWidth / splitXCount) + padding;
				maxX = Mathf.RoundToInt(1f * (i + 1) * mapWidth / splitXCount) - padding;
				for (int j = 0; j < splitYCount; j++)
				{
					minY = Mathf.RoundToInt(1f * j * mapHeight / splitYCount) + padding;
					maxY = Mathf.RoundToInt(1f * (j + 1) * mapHeight / splitYCount) - padding;

					var pos = new Vector2Int(
						minX >= maxX - 1 ? Mathf.Min(minX, maxX - 1, mapWidth - 1) : random.Next(minX, maxX),
						minY >= maxY - 1 ? Mathf.Min(minY, maxY - 1, mapHeight - 1) : random.Next(minY, maxY));

					var shape = MakeRandomShape(random);
					int rotate = random.Next(4);

					bool isBlue = (i + j + startByBlue) % 2 == 1;

					foreach (var each in RotateShapePos(shape, rotate))
					{
						if (gpTilemap.IsValidPos(pos + each))
						{
							if (isBlue)
								gpTilemap.SetDataColorBlue(pos + each);
							else
								gpTilemap.SetDataColorRed(pos + each);
							yield return null;
						}
					}
				}
			}
		}
		public void GenerateColor(SysRandom random, GpTilemapData gpTilemap)
		{
			foreach (var _ in GenerateColorStep(random, gpTilemap)) { }
		}
	}
}
