using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
	public class GpPosMappingData
	{
		protected Dictionary<Vector2Int, Vector2Int> mCurToOriPosDict = new(64);
		protected Dictionary<Vector2Int, Vector2Int> mOriToCurPosDict = new(64);

		public void ClearPosData()
		{
			mCurToOriPosDict.Clear();
			mOriToCurPosDict.Clear();
		}

		public void OnInitPos(in Vector2Int pos)
		{
			mCurToOriPosDict.Add(pos, pos);
			mOriToCurPosDict.Add(pos, pos);
		}

		public void OnRemoveCurPos(in Vector2Int curPos)
		{
			if (mCurToOriPosDict.ContainsKey(curPos))
			{
				var oriPos = mCurToOriPosDict[curPos];
				mCurToOriPosDict.Remove(curPos);
				if (mOriToCurPosDict.ContainsKey(oriPos))
				{
					mOriToCurPosDict.Remove(oriPos);
				}
			}
		}

		public void OnAddCurPos(in Vector2Int curPos, in Vector2Int oriPos)
		{
			if (!mCurToOriPosDict.ContainsKey(curPos) && !mOriToCurPosDict.ContainsKey(oriPos))
			{
				mCurToOriPosDict.Add(curPos, oriPos);
				mOriToCurPosDict.Add(oriPos, curPos);
			}
		}

		public bool ExistsCurPos(in Vector2Int curPos)
		{
			return mCurToOriPosDict.ContainsKey(curPos);
		}
		public Vector2Int GetOriPosByCur(in Vector2Int curPos)
		{
			return mCurToOriPosDict[curPos];
		}

		public bool ExistsOriPos(in Vector2Int oriPos)
		{
			return mOriToCurPosDict.ContainsKey(oriPos);
		}
		public Vector2Int GetCurPosByOri(in Vector2Int oriPos)
		{
			return mOriToCurPosDict[oriPos];
		}
	}
}
