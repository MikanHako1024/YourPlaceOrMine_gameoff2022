using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
	/// <summary>
	/// 图块数据类
	/// </summary>
	[System.Serializable]
	public class GpTileData
	{
		#region Color

		public enum FlagColor
		{
			White,
			Red,
			Blue,
		}

		public FlagColor flagColor;

		public void SetColorWhite()
		{
			flagColor = FlagColor.White;
		}
		public void SetColorRed()
		{
			flagColor = FlagColor.Red;
		}
		public void SetColorBlue()
		{
			flagColor = FlagColor.Blue;
		}

		public bool IsColorWhite => flagColor == FlagColor.White;
		public bool IsColorRed => flagColor == FlagColor.Red;
		public bool IsColorBlue => flagColor == FlagColor.Blue;

		#endregion Color

		#region Gray

		public bool grayColor;

		public void SetGrayColor(bool value)
		{
			grayColor = value;
		}

		#endregion Gray

		#region Content

		public bool hasMine;
		public int numValue;

		public void SetMine()
		{
			hasMine = true;
			numValue = 0;
		}
		public void SetNumber(int value)
		{
			hasMine = false;
			numValue = value;
		}

		public bool IsNumber => !hasMine;

		#endregion Content

		#region FlagState

		public enum FlagState
		{
			None,
			Mine,
			Mark,
		}

		public FlagState flagState;

		public void SetFlagNone()
		{
			flagState = FlagState.None;
		}
		public void SetFlagMine()
		{
			flagState = FlagState.Mine;
		}
		public void SetFlagMark()
		{
			flagState = FlagState.Mark;
		}

		public bool IsFlagNone => flagState == FlagState.None;
		public bool IsFlagMine => flagState == FlagState.Mine;
		public bool IsFlagMark => flagState == FlagState.Mark;

		#endregion FlagState

		#region Holded

		public bool holded;

		public void SetHolded(bool value)
		{
			holded = value;
		}

		#endregion Holded

		#region Opened

		public bool opened;

		public void SetOpened(bool value)
		{
			opened = value;
		}

		#endregion Opened

		#region Structure

		public void ClearData()
		{
			flagColor = FlagColor.White;
			flagState = FlagState.None;
			holded = false;
			opened = false;
			hasMine = false;
			numValue = 0;

			originalPos = Vector2Int.zero;
		}

		public GpTileData(int numValue)
		{
			ClearData();
			SetNumber(numValue);
		}
		public GpTileData()
		{
			ClearData();
			SetNumber(0);
		}
		public GpTileData(bool hasMine)
		{
			ClearData();
			if (hasMine)
				SetMine();
			else
				SetNumber(0);
		}

		public GpTileData(in Vector2Int pos, int numValue)
		{
			ClearData();
			SetNumber(numValue);
			SetOriginalPos(pos);
			//SetCurrentPos(pos);
		}
		public GpTileData(in Vector2Int pos)
		{
			ClearData();
			SetNumber(0);
			SetOriginalPos(pos);
			//SetCurrentPos(pos);
		}
		public GpTileData(in Vector2Int pos, bool hasMine)
		{
			ClearData();
			if (hasMine)
				SetMine();
			else
				SetNumber(0);
			SetOriginalPos(pos);
			//SetCurrentPos(pos);
		}

		#endregion Structure

		#region Clone

		public GpTileData Clone()
		{
			var newData = new GpTileData
			{
				flagColor = flagColor,
				flagState = flagState,
				holded = holded,
				opened = opened,
				hasMine = hasMine,
				numValue = numValue,

				originalPos = originalPos,
			};
			return newData;
		}

		#endregion Clone

		#region OriginalPos

		public Vector2Int originalPos = Vector2Int.zero;

		public void SetOriginalPos(in Vector2Int pos)
		{
			originalPos = pos;
		}

		#endregion OriginalPos
	}
}
