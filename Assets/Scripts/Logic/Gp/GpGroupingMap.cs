using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
	[RequireComponent(typeof(GpTilemapData))]
	public class GpGroupingMap : MonoBehaviour
	{
		[SerializeField]
		protected GpTilemapData mainGpTilemap;

		public GpTilemapData main => mainGpTilemap;

		protected void Awake()
		{
			if (!mainGpTilemap)
				mainGpTilemap = GetComponent<GpTilemapData>();
		}


		protected void ClearTilemap()
		{
			if (mainGpTilemap)
				mainGpTilemap.ClearMapData();
		}

		public void ClearGroupData()
		{
			mPosToGroupDict.Clear();
			mGroupColorBlueDict.Clear();
			mGroupPosSetDict.Clear();
			mLastGroupId = EmptyGroupId;

			InitPickedData();
			ClearTilemap();
		}


		#region GroupId

		protected static readonly int EmptyGroupId = 0;
		protected int mLastGroupId = EmptyGroupId;

		protected Dictionary<int, bool> mGroupColorBlueDict = new(16);
		protected Dictionary<int, HashSet<Vector2Int>> mGroupPosSetDict = new(16);

		protected int CreateNewGroup(bool colorBlue)
		{
			int groupId = ++mLastGroupId;
			mGroupColorBlueDict.Add(groupId, colorBlue);
			mGroupPosSetDict.Add(groupId, new HashSet<Vector2Int>(16));
			return groupId;
		}

		public bool IsValidGroupId(int groupId)
		{
			return 0 < groupId && groupId <= mLastGroupId;
		}

		#endregion GroupId

		// TODO : HashSet<Vector2Int> 对象池


		#region Un/RegisterGroupId

		protected Dictionary<Vector2Int, int> mPosToGroupDict = new(64);

		protected void RegisterGroupId(in Vector2Int pos, int groupId)
		{
			if (!IsValidGroupId(groupId))
				return;

			if (mPosToGroupDict.ContainsKey(pos))
			{
				if (mPosToGroupDict[pos] == groupId)
					return;
				UnregisterGroupId(pos);
			}
			mPosToGroupDict.Add(pos, groupId);
			mGroupPosSetDict[groupId].Add(pos);
		}

		protected void UnregisterGroupId(in Vector2Int pos)
		{
			if (mPosToGroupDict.ContainsKey(pos))
			{
				int groupId = mPosToGroupDict[pos];
				mPosToGroupDict.Remove(pos);
				if (IsValidGroupId(groupId))
				{
					mGroupPosSetDict[groupId].Remove(pos);
				}
			}
		}

		#endregion Un/RegisterGroupId


		#region GetGroupId

		public int GetGroupId(in Vector2Int pos)
		{
			if (mPosToGroupDict.ContainsKey(pos))
				return mPosToGroupDict[pos];
			else
				return 0;
		}
		public IEnumerable<Vector2Int> EachPosByGroupId(int groupId)
		{
			if (IsValidGroupId(groupId))
			{
				foreach (var pos in mGroupPosSetDict[groupId])
					yield return pos;
			}
		}

		public IEnumerable<int> EachGroupId()
		{
			for (int groupId = 1, l = mLastGroupId; groupId <= l; ++groupId)
			{
				if (mGroupPosSetDict[groupId].Count > 0)
						yield return groupId;
			}
		}

		public IEnumerable<int> EachBlueGroupId()
		{
			foreach (var groupId in EachGroupId())
			{
				if (mGroupColorBlueDict[groupId])
					yield return groupId;
			}
		}
		public IEnumerable<int> EachRedGroupId()
		{
			foreach (var groupId in EachGroupId())
			{
				if (!mGroupColorBlueDict[groupId])
					yield return groupId;
			}
		}

		#endregion


		#region InitGroupData

		protected HashSet<Vector2Int> mTempVisitedPos = new(64);
		protected HashSet<Vector2Int> mTempNoVisitPos = new(64);
		protected Queue<Vector2Int> mTempTaskPos = new(16);

		public void InitGroupData()
		{
			var gpTilemap = mainGpTilemap;

			var visited = mTempVisitedPos;
			var noVisit = mTempNoVisitPos;
			var task = mTempTaskPos;
			visited.Clear();
			noVisit.Clear();
			task.Clear();

			foreach (var pos in gpTilemap.EachMapPos())
			{
				// 不加入无颜色的图块
				if (!gpTilemap.GetGpTileData(pos).IsColorWhite)
					noVisit.Add(pos);
			}

			Vector2Int nextPos = Vector2Int.zero;
			int maxLoop = gpTilemap.MapWidth * gpTilemap.MapHeight;
			for (int i = 0; i < maxLoop; ++i)
			{
				if (noVisit.Count <= 0)
					break;

				foreach (var pos in noVisit)
				{
					nextPos = pos;
					break;
				}

				int groupId = CreateNewGroup(gpTilemap.GetGpTileData(nextPos).IsColorBlue);
				RegisterGroupId(nextPos, groupId);

				task.Clear();
				visited.Add(nextPos);
				noVisit.Remove(nextPos);
				task.Enqueue(nextPos);

				var targetColor = gpTilemap.GetGpTileData(nextPos).flagColor;

				for (int j = 0; j < maxLoop; ++j)
				{
					if (task.Count <= 0)
						break;
					nextPos = task.Dequeue();
					foreach (var pos in EachAround4Pos(nextPos))
					{
						if (noVisit.Contains(pos)
							&& targetColor == gpTilemap.GetGpTileData(pos)?.flagColor)
						{
							visited.Add(pos);
							noVisit.Remove(pos);
							task.Enqueue(pos);
							RegisterGroupId(pos, groupId);
						}
					}
				}
			}

			visited.Clear();
			noVisit.Clear();
			task.Clear();
		}

		#endregion InitGroupData


		protected IEnumerable<Vector2Int> EachAround4Pos(Vector2Int center)
		{
			yield return center + Vector2Int.left;
			yield return center + Vector2Int.right;
			yield return center + Vector2Int.up;
			yield return center + Vector2Int.down;
		}


		#region PickUpGroup

		protected int mPickedGroupId = 0;

		protected void InitPickedData()
		{
			mPickedGroupId = 0;
		}

		public bool CanPickUpGroup(in Vector2Int pos, bool checkSameId)
		{
			int groupId = GetGroupId(pos);
			if (!IsValidGroupId(groupId))
				return false;

			if (checkSameId && mPickedGroupId == groupId)
				return false;
			return true;
		}

		public void PickUpGroup(int groupId)
		{
			if (IsValidGroupId(groupId) && mPickedGroupId != groupId)
			{
				if (mPickedGroupId > 0)
					PutDownGroup();

				mPickedGroupId = groupId;
			}
		}
		public void PickUpGroup(in Vector2Int pos)
		{
			PickUpGroup(GetGroupId(pos));
		}

		public void PutDownGroup()
		{
			if (mPickedGroupId > 0)
			{
				InitPickedData();
			}
		}

		public bool IsPickedGroup()
		{
			return mPickedGroupId > 0;
		}

		#endregion PickUpGroup


		// 分离
		// 从一个组中移除一部分 或 将一个组分成多个组

		public void RemoveFromGroup(in Vector2Int pos)
		{
			UnregisterGroupId(pos);
		}


		// 划分 (刷新分离)

		protected HashSet<Vector2Int> mTempLinkedPos = new(64);
		protected HashSet<int> mTempSplitedGroup = new(16);

		public void RefreshSplitGroup()
		{
			// 用判断计数是否相等的方式 判断组是否有分离的部分

			// TODO : 提取出 遍历 的工具方法

			var gpTilemap = mainGpTilemap;

			var splited = mTempSplitedGroup;
			var linked = mTempLinkedPos;
			splited.Clear();
			linked.Clear();

			var visited = mTempVisitedPos;
			var noVisit = mTempNoVisitPos;
			var task = mTempTaskPos;
			visited.Clear();
			noVisit.Clear();
			task.Clear();

			foreach (var pos in gpTilemap.EachMapPos())
				if (IsValidGroupId(GetGroupId(pos)))
					noVisit.Add(pos);

			Vector2Int nextPos = Vector2Int.zero;
			int maxLoop = gpTilemap.MapWidth * gpTilemap.MapHeight;
			for (int i = 0; i < maxLoop; ++i)
			{
				if (noVisit.Count <= 0)
					break;

				foreach (var pos in noVisit)
				{
					nextPos = pos;
					break;
				}
				int groupId = GetGroupId(nextPos);

				// 是否已分离
				bool isSplited = splited.Contains(groupId);

				if (isSplited)
				{
				}
				else
				{
					if (!mGroupPosSetDict.ContainsKey(groupId) || mGroupPosSetDict[groupId].Count == 0)
						continue;
				}

				task.Clear();
				visited.Add(nextPos);
				noVisit.Remove(nextPos);
				task.Enqueue(nextPos);

				int count = 1;
				linked.Clear();
				linked.Add(nextPos);
				for (int j = 0; j < maxLoop; ++j)
				{
					if (task.Count <= 0)
						break;
					nextPos = task.Dequeue();
					foreach (var pos in EachAround4Pos(nextPos))
					{
						if (noVisit.Contains(pos) && GetGroupId(pos) == groupId)
						{
							visited.Add(pos);
							noVisit.Remove(pos);
							task.Enqueue(pos);

							++count;
							linked.Add(pos);
						}
					}
				}

				if (isSplited)
				{
					// 已分离
					// 增加新的组
					int newGroupId = CreateNewGroup(mGroupColorBlueDict[groupId]);
					foreach (var pos in linked)
						RegisterGroupId(pos, newGroupId);
				}
				else
				{
					// 新的组
					if (mGroupPosSetDict[groupId].Count != count)
					{
						// 被分离
						// 该组无需处理
						splited.Add(groupId);
					}
					else
					{
						// 未分离
					}
				}
			}

			visited.Clear();
			noVisit.Clear();
			task.Clear();

			splited.Clear();
			linked.Clear();
		}


		// 占领
		// 向一个组添加一部分
		// 可以与其他组连接

		public void OccupyToGroup(in Vector2Int pos, int gourpId, bool connect)
		{
			RegisterGroupId(pos, gourpId);
			if (connect)
				RefreshConnectForOccupy(pos, gourpId);
		}


		// 连接 (刷新占领)

		protected HashSet<Vector2Int> mTempPosList = new(64);
		protected HashSet<int> mTempGroupList = new(64);

		public void RefreshConnectForOccupy(in Vector2Int srcPos, int gourpId)
		{
			if (GetGroupId(srcPos) != gourpId)
				return;

			var posSet = mTempPosList;
			var groupSet = mTempGroupList;
			posSet.Clear();
			groupSet.Clear();

			var data = mainGpTilemap.GetGpTileData(srcPos);
			var grayColor = data.grayColor;
			var flagColor = data.flagColor;
			foreach (var pos in EachAround4Pos(srcPos))
			{
				if (mainGpTilemap.IsValidPos(pos))
				{
					int changeGroup = GetGroupId(pos);
					if (changeGroup == gourpId)
						continue;
					if (groupSet.Contains(changeGroup))
						continue;
					data = mainGpTilemap.GetGpTileData(pos);
					if (data.grayColor == grayColor && data.flagColor == flagColor)
					{
						foreach (var each in EachPosByGroupId(changeGroup))
						{
							posSet.Add(each);
						}
					}
					groupSet.Add(changeGroup);
				}
			}

			foreach (var pos in posSet)
			{
				RegisterGroupId(pos, gourpId);
			}

			posSet.Clear();
			groupSet.Clear();
		}


		// 边界

		public void GetGroupBorder(int groupId, in Vector2Int dir,
			in List<Vector2Int> grayList, in List<Vector2Int> whiteList, in List<Vector2Int> redList, in List<Vector2Int> wallList)
		{
			grayList?.Clear();
			whiteList?.Clear();
			redList?.Clear();
			wallList?.Clear();
			if (!IsValidGroupId(groupId))
				return;

			bool first = true;
			bool forRed = true;
			foreach (var pair in mPosToGroupDict)
			{
				if (pair.Value == groupId)
				{
					if (first)
					{
						forRed = mainGpTilemap.GetGpTileData(pair.Key).IsColorRed;
						first = false;
					}

					var pos = pair.Key + dir;
					if (mainGpTilemap.IsValidPos(pos))
					{
						if (groupId != GetGroupId(pos))
						{
							var data = mainGpTilemap.GetGpTileData(pos);
							if (data.grayColor)
								grayList?.Add(pair.Key);
							else if (data.IsColorWhite)
								whiteList?.Add(pair.Key);
							else
							{
								if (!forRed && data.IsColorRed)
									redList?.Add(pair.Key);
								else if (forRed && data.IsColorBlue)
									redList?.Add(pair.Key);
							}
						}
					}
					else
					{
						wallList?.Add(pair.Key);
					}
				}
			}
			// TODO : 优化计算方式
		}

		// TODO : 缓存结果
	}
}
