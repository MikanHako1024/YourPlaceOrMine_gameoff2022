using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityRandom = UnityEngine.Random;
using SysRandom = System.Random;

namespace Gameplay
{
	public class GameLogicContext
	{
		public GpMapController mapController;
		public GpGroupingMap gpGrouping;
		public HudArrowTilemap arrowHudTilemap;
		public HudGroupHighlight highlightHud;
		public HudGroupHighlight oriHighlightHud;
		public GpTilemapData mainGpTilemap;
		public GpTilemapData originalGpTilemap;
		public GpPosMappingData posMapping;
		public UIFaceButton uiFaceBlue;
		public UIFaceButton uiFaceRed;
		public GpColorCounter colorCounter;
		public UICounter uiCounterBlue;
		public UICounter uiCounterRed;
		public BorderMoveContext borderMoveContext;
		public Coroutine redActionCoroutine;
		public GameState gameState;
	}

	public enum GameState
	{
		None,
		Running,
		OverBlueWin,
		OverBlueLose,
		OverPlayEven,
	}

	public enum BorderMoveType
	{
		None,
		AttackAndOccupy,
		Move,
	}

	public class BorderMoveContext
	{
		public int groupId;
		public bool isRed;
		public Vector2Int dir;

		public List<Vector2Int> grayList;
		public List<Vector2Int> whiteList;
		public List<Vector2Int> redList;
		public List<Vector2Int> wallList;

		public BorderMoveType actionType;
		public Vector2Int move;

		public bool isValid = false;
		public bool isChecked = false;
		public bool isDone = false;

		public BorderMoveContext(List<Vector2Int> grayList, List<Vector2Int> whiteList, List<Vector2Int> redList, List<Vector2Int> wallList)
		{
			this.grayList = grayList;
			this.whiteList = whiteList;
			this.redList = redList;
			this.wallList = wallList;

			Clear();
		}

		public void Clear()
		{
			groupId = 0;
			isRed = false;
			dir = Vector2Int.zero;

			grayList?.Clear();
			whiteList?.Clear();
			redList?.Clear();
			wallList?.Clear();

			actionType = BorderMoveType.None;
			move = Vector2Int.zero;

			isValid = false;
			isChecked = false;
			isDone = false;
		}

		public void Init(int groupId, bool isRed, in Vector2Int dir)
		{
			Clear();

			this.groupId = groupId;
			this.isRed = isRed;
			this.dir = dir;

			isValid = CheckValid();
		}

		public bool CheckValid()
		{
			return groupId > 0;
		}
	}

	public class GpMapGameLogic : BaseManager<GpMapGameLogic>
	{
		#region ProcessClickL

		public void ProcessClickL(GameLogicContext ctx, in Vector2Int hitPos)
		{
			var current = PosHitInTilemap(ctx, hitPos, out var realPos);
			if (CanOpenOutTile(ctx, current, realPos))
			{
				OpenOutTile(ctx, current, realPos);
				RefreshSplitGroup(ctx);
				ClearHighlightHud(ctx);
				RefreshColorCounter(ctx);
				ProcessColorCountReduced(ctx);
			}
		}

		protected bool CanOpenOutTile(GameLogicContext ctx, bool current, in Vector2Int hitPos)
		{
			if (!IsValidPos(ctx, current, hitPos))
				return false;

			var data = GetGpTileData(ctx, current, hitPos);
			return !data.opened && !data.IsFlagMine && data.IsColorBlue;
		}

		protected void OpenOutTile(GameLogicContext ctx, bool current, in Vector2Int hitPos)
		{
			if (current)
			{
				OpenOutTileAuto(ctx, hitPos);
			}
			else
			{
				if (TryPosConvertTo(ctx, CURRENT, hitPos, out var curPos))
				{
					OpenOutTileAuto(ctx, curPos);
				}
			}
		}

		#endregion ProcessClickL


		#region ProcessClickR

		protected static readonly bool AllowRightHitCurMap = false;

		public void ProcessClickR(GameLogicContext ctx, in Vector2Int hitPos)
		{
			var current = PosHitInTilemap(ctx, hitPos, out var realPos);
			if (AllowRightHitCurMap || current == ORIGINAL)
			{
				if (CanFlagTo(ctx, current, realPos))
				{
					FlagToTileAuto(ctx, current, realPos);
				}
			}
		}

		#endregion ProcessClickR


		#region ProcessClickLR

		public void ProcessClickLR(GameLogicContext ctx, in Vector2Int hitPos)
		{
			var current = PosHitInTilemap(ctx, hitPos, out var realPos);
			if (current == ORIGINAL)
			{
				if (CanOpenOut3X3Tile(ctx, realPos))
				{
					OpenOut3X3Tile(ctx, realPos);
					RefreshSplitGroup(ctx);
					ClearHighlightHud(ctx);
					RefreshColorCounter(ctx);
					ProcessColorCountReduced(ctx);
				}
			}
		}

		protected bool CanOpenOut3X3Tile(GameLogicContext ctx, in Vector2Int oriPos)
		{
			if (!IsValidPos(ctx, ORIGINAL, oriPos))
				return false;

			var data = GetGpTileData(ctx, ORIGINAL, oriPos);
			if (data.IsNumber && data.opened)
				return data.numValue == CountMine3X3Tile(ctx, oriPos);
			else
				return false;
		}

		protected int CountMine3X3Tile(GameLogicContext ctx, in Vector2Int oriPos)
		{
			int count = 0;
			foreach (var each in EachPosRange3X3(oriPos))
			{
				// 这里包括了自身位置
				if (IsValidPos(ctx, ORIGINAL, each))
				{
					var eachData = GetGpTileData(ctx, ORIGINAL, each);
					if (!eachData.opened && eachData.IsFlagMine)
						++count;
					else if (eachData.opened && eachData.hasMine)
						++count;
				}
			}
			return count;
		}

		protected void OpenOut3X3Tile(GameLogicContext ctx, in Vector2Int centerOriPos)
		{
			foreach (var eachOriPos in EachPosRange3X3(centerOriPos))
			{
				if (TryPosConvertTo(ctx, CURRENT, eachOriPos, out var curPos))
				{
					if (IsValidPos(ctx, CURRENT, curPos))
					{
						var data = GetGpTileData(ctx, CURRENT, curPos);
						if (!data.opened)
						{
							if (data.IsColorBlue)
							{
								OpenOutTileAuto(ctx, curPos);
							}
							else
							{
								if (data.IsFlagNone)
									FlagToNumberTile(ctx, CURRENT, curPos);
							}
						}
					}
				}
			}
		}

		#endregion ProcessClickLR


		#region ProcessPressHit

		public void ProcessPressLHit(GameLogicContext ctx, in Vector2Int hitPos, bool value)
		{
			var current = PosHitInTilemap(ctx, hitPos, out var realPos);
			if (IsValidPos(ctx, current, realPos))
				SetTileDataHolded(ctx, current, realPos, value);
		}

		public void ProcessPressRHit(GameLogicContext ctx, in Vector2Int hitPos, bool value)
		{
			var current = PosHitInTilemap(ctx, hitPos, out var realPos);
			if (IsValidPos(ctx, current, realPos))
				SetTileDataHolded(ctx, current, realPos, value);
		}

		public void ProcessPressLRHit(GameLogicContext ctx, in Vector2Int hitPos, bool value)
		{
			var current = PosHitInTilemap(ctx, hitPos, out var realPos);
			if (current == ORIGINAL && IsValidPos(ctx, current, realPos))
			{
				foreach (var pos in EachPosRange3X3(realPos))
					SetTileDataHolded(ctx, current, pos, value);
			}
		}

		#endregion ProcessPressHit


		#region ProcessDragR

		public bool CanDragRStart(GameLogicContext ctx, in Vector2Int startPos)
		{
			if (PosHitInCurrentTilemap(ctx, startPos, out var curPos))
			{
				var data = GetGpTileData(ctx, CURRENT, curPos);
				if (data != null && data.IsColorBlue && !data.grayColor)
					return CanPickUpGroup(ctx, curPos);
				else
					return false;
			}
			else
				return false;
		}

		public void ProcessDragRStart(GameLogicContext ctx, in Vector2Int startPos)
		{
			ClearHighlightHud(ctx);
			PickUpGroup(ctx, startPos);
			SetHighlightHudByGroup(ctx, GetGroupId(ctx, startPos), BLUECOLOR);
			RefreshArrowHud(ctx, startPos, Vector2Int.zero);
		}

		public void ProcessDragRUpdate(GameLogicContext ctx, in Vector2Int startPos, in Vector2Int hitPos)
		{
			RefreshArrowHud(ctx, startPos, hitPos - startPos);
		}


		protected Vector2Int DragOffsetToDir(in Vector2Int offset)
		{
			if (offset == Vector2Int.zero)
				return Vector2Int.zero;
			else if (offset.y >= Mathf.Abs(offset.x))
				return Vector2Int.up;
			else if (offset.y <= -Mathf.Abs(offset.x))
				return Vector2Int.down;
			else if (offset.x >= Mathf.Abs(offset.y))
				return Vector2Int.right;
			else if (offset.x <= -Mathf.Abs(offset.y))
				return Vector2Int.left;
			else
				return Vector2Int.zero;
		}

		protected bool useDistLimit = true;
		protected float dragDistMinLimit = 2f;
		protected float dragDistMaxLimit = 20f;

		protected Vector2Int DragOffsetToDirWithLimit(in Vector2Int offset)
		{
			if (useDistLimit)
			{
				if (dragDistMinLimit <= offset.magnitude && offset.magnitude <= dragDistMaxLimit)
					return DragOffsetToDir(offset);
				else
					return Vector2Int.zero;
			}
			else
			{
				return DragOffsetToDir(offset);
			}
		}

		protected BorderMoveContext mTempBorderMoveContext = new(new(16), new(16), new(16), new(16));

		protected void RefreshArrowHud(GameLogicContext ctx, in Vector2Int startPos, in Vector2Int offset)
		{
			ClearArrowHud(ctx);
			SetArrowHudPoint(ctx, startPos);

			var offsetDir = DragOffsetToDirWithLimit(offset);
			if (offsetDir != Vector2Int.zero)
			{
				var bmCtx = mTempBorderMoveContext;
				bmCtx.Init(GetGroupId(ctx, startPos), false, offsetDir);
				GetGroupBorder(ctx, bmCtx);
				if (bmCtx.whiteList.Count > 0 || bmCtx.redList.Count > 0)
				{
					SetArrowHudPlus(ctx, bmCtx.whiteList, offsetDir);
					SetArrowHudDouble(ctx, bmCtx.redList, offsetDir);
				}
				else if (bmCtx.wallList.Count > 0)
				{
				}
				else if (bmCtx.grayList.Count > 0)
				{
					SetArrowHudMove(ctx, bmCtx.grayList, offsetDir);
				}
				bmCtx.Clear();
			}
			else
			{
				// ...
			}
		}


		public void ProcessDragRMouseDownL(GameLogicContext ctx, in Vector2Int startPos, in Vector2Int hitPos,
			BorderMoveContext bmCtx, 
			out BorderMoveType borderMoveType)
		{
			borderMoveType = BorderMoveByPos(ctx, startPos, hitPos, bmCtx);
			RefreshColorCounter(ctx);
			ProcessColorCountReduced(ctx);
		}


		public bool CanDragRContinue(GameLogicContext ctx, in Vector2Int startPos)
		{
			return ctx.gameState == GameState.Running && CanDragRStart(ctx, startPos);
		}

		public void ProcessDragRContinue(GameLogicContext ctx, in Vector2Int startPos, in Vector2Int hitPos)
		{
			ClearHighlightHud(ctx);
			SetHighlightHudByGroup(ctx, GetGroupId(ctx, startPos), BLUECOLOR);
			RefreshArrowHud(ctx, startPos, hitPos - startPos);
		}


		public void ProcessDragREnd(GameLogicContext ctx, in Vector2Int startPos, in Vector2Int endPos)
		{
			ClearHighlightHud(ctx);
			ClearArrowHud(ctx);
		}

		#endregion ProcessDragR

		// TODO : 优化 减少重复调用 GetGroupBorder


		#region ProcessNoDrag

		public void ProcessNoDragUpdate(GameLogicContext ctx, in Vector2Int hitPos)
		{
			if (IsValidPos(ctx, CURRENT, hitPos) && !GetGpTileData(ctx, CURRENT, hitPos).IsColorWhite)
			{
				if (CanPickUpGroup(ctx, hitPos, true))
				{
					ClearHighlightHud(ctx);
					PickUpGroup(ctx, hitPos);
					SetHighlightHudByGroup(ctx, GetGroupId(ctx, hitPos), BLUECOLOR);
				}
			}
			else if (IsPickedGroup(ctx))
			{
				ClearHighlightHud(ctx);
			}
		}

		#endregion ProcessNoDrag


		#region ProcessClearAll

		public void ProcessClearAll(GameLogicContext ctx)
		{
			ctx.mainGpTilemap.TouchNativeTilemap();
			ctx.mainGpTilemap.ClearMapData();
			ctx.mainGpTilemap.SetTilemapVisible(true);
			ctx.gpGrouping.ClearGroupData();
			ctx.arrowHudTilemap.ClearAllArrow();
			ctx.highlightHud.ClearHighlight();
			ctx.oriHighlightHud.ClearHighlight();
			ctx.originalGpTilemap.TouchNativeTilemap();
			ctx.originalGpTilemap.ClearMapData();
			ctx.originalGpTilemap.SetTilemapVisible(true);
			ctx.posMapping.ClearPosData();
			ctx.colorCounter.Clear();
			ctx.uiCounterBlue.Clear();
			ctx.uiCounterRed.Clear();
			StopOpponentActionCoroutine(ctx);
			ctx.mapController.SetControllable(false);
			ctx.gameState = GameState.None;
		}

		#endregion ProcessClearAll


		#region ProcessGenerateMap

		public void ProcessGenerateMap(GameLogicContext ctx, int width, int height, int mine)
		{
			var gpGrouping = ctx.gpGrouping;
			var gpTilemap = ctx.mainGpTilemap;
			gpGrouping.ClearGroupData();
			gpTilemap.TouchNativeTilemap();
			gpTilemap.InitMapSize(width, height);

			var mainOffset = new Vector2Int(0, 0);
			gpTilemap.SetPosOffset(mainOffset);

			var leftDownPos = new Vector3(mainOffset.x, mainOffset.y);
			gpTilemap.transform.position = leftDownPos;
			ctx.highlightHud.transform.position = leftDownPos;
			ctx.arrowHudTilemap.transform.position = leftDownPos;

			var random = GpMapGenerator.Inst.GetRandomUtil();
			GpMapGenerator.Inst.GenerateRandomMap(random, gpTilemap, mine);
			GpMapGenerator.Inst.GenerateColor(random, gpTilemap);
			gpGrouping.InitGroupData();

			var oriPosOffset = new Vector2Int(width + 1, 0);
			ctx.originalGpTilemap.SetPosOffset(mainOffset + oriPosOffset);

			ctx.originalGpTilemap.transform.position = leftDownPos + (Vector3Int)oriPosOffset;
			ctx.oriHighlightHud.transform.position = leftDownPos + (Vector3Int)oriPosOffset;

			var z = Camera.main.transform.position.z;
			Camera.main.transform.position = new Vector3(18.5f, 10, z); // temp

			var counter = ctx.colorCounter;
			counter.Init(0, 0, 0);
			foreach (var pos in gpTilemap.EachMapPos())
			{
				var data = gpTilemap.GetGpTileData(pos).Clone();
				ctx.originalGpTilemap.SetTile(pos, data);

				ctx.posMapping.OnInitPos(pos);

				if (data.IsColorBlue)
					counter.AddBlue();
				else if (data.IsColorRed)
					counter.AddRed();
				else
					counter.AddWhite();
			}
			RefreshColorCounter(ctx);

			ctx.uiFaceBlue.SetColorBlue();
			ctx.uiFaceBlue.SetFaceNormal();
			ctx.uiFaceRed.SetColorWhite();
			ctx.uiFaceRed.SetFaceNormal();
		}

		#endregion ProcessGenerateMap


		#region ProcessStartNewGame

		public void ProcessStartNewGame(GameLogicContext ctx)
		{
			//ctx.mapController.StartNewGame();

			ProcessClearAll(ctx);
			GpMapGenerator.Inst.ResetRandomSeed();
			//ctx.mapController.GenerateMap_18x20m50();
			var mapSize = ctx.mapController.NormalMapSize;
			ProcessGenerateMap(ctx, mapSize.x, mapSize.y, mapSize.z);

			ctx.gameState = GameState.Running;

			//ctx.mapController.SetControllable(true);
			ProcessWaitBlueActionStart(ctx);
		}

		#endregion ProcessStartNewGame


		#region ProcessWinOrLose

		public enum GameOverType
		{
			None,
			BlueWin,
			BlueLose,
			PlayEven,
		}

		public GameOverType CheckWinOrLose(GameLogicContext ctx)
		{
			var counter = ctx.colorCounter;
			if (counter.BlueCount <= 0)
			{
				if (counter.RedCount > 0)
					return GameOverType.BlueLose;
				else
					return GameOverType.PlayEven;
			}
			else if (counter.RedCount <= 0)
			{
				if (counter.BlueCount > 0)
					return GameOverType.BlueWin;
				else
					return GameOverType.PlayEven;
			}
			else
			{
				return GameOverType.None;
			}
		}

		public void ProcessActionEnd(GameLogicContext ctx)
		{
			if (ctx.gameState == GameState.Running)
			{
				var gameOverType = CheckWinOrLose(ctx);
				if (gameOverType == GameOverType.BlueWin)
					ProcessBlueWin(ctx);
				else if (gameOverType == GameOverType.BlueLose)
					ProcessBlueLose(ctx);
				else if (gameOverType == GameOverType.PlayEven)
					ProcessPlayEven(ctx);
			}
		}

		public void ProcessColorCountReduced(GameLogicContext ctx)
		{
			ProcessActionEnd(ctx);
		}

		public void ProcessBlueWin(GameLogicContext ctx)
		{
			ctx.mapController.SetControllable(false);

			ctx.gameState = GameState.OverBlueWin;

			ctx.uiFaceBlue.SetColorBlue();
			ctx.uiFaceRed.SetColorRed();
			ctx.uiFaceBlue.SetFaceWin();
			ctx.uiFaceRed.SetFaceLose();

			ctx.uiCounterRed.SetEndFlag();

			ProcessOpenAllOnGameOver(ctx);

			Debug.Log("Game Over : Blue Win");
		}

		public void ProcessBlueLose(GameLogicContext ctx)
		{
			ctx.mapController.SetControllable(false);

			ctx.gameState = GameState.OverBlueLose;

			ctx.uiFaceBlue.SetColorBlue();
			ctx.uiFaceRed.SetColorRed();
			ctx.uiFaceBlue.SetFaceLose();
			ctx.uiFaceRed.SetFaceWin();

			ctx.uiCounterBlue.SetEndFlag();

			ProcessOpenAllOnGameOver(ctx);

			Debug.Log("Game Over : Red Win");
		}

		public void ProcessPlayEven(GameLogicContext ctx)
		{
			ctx.mapController.SetControllable(false);

			ctx.gameState = GameState.OverPlayEven;

			ctx.uiFaceBlue.SetColorBlue();
			ctx.uiFaceRed.SetColorRed();
			ctx.uiFaceBlue.SetFaceLose();
			ctx.uiFaceRed.SetFaceLose();

			ctx.uiCounterBlue.SetEndFlag();
			ctx.uiCounterRed.SetEndFlag();

			ProcessOpenAllOnGameOver(ctx);

			Debug.Log("Game Over : Play Even");
		}

		protected void ProcessOpenAllOnGameOver(GameLogicContext ctx)
		{
			//foreach (var oriPos in ctx.originalGpTilemap.EachMapPos())
			//{
			//	SetTileDataOpened(ctx, ORIGINAL, oriPos, true);
			//}
			// ？只打开 原始地图 和 当前地图的有颜色的部分 ...

			var oriGpTilemap = ctx.originalGpTilemap;
			foreach (var oriPos in oriGpTilemap.EachMapPos())
			{
				oriGpTilemap.SetDataOpened(oriPos, true);
			}

			var mainGpTilemap = ctx.mainGpTilemap;
			foreach (var curPos in mainGpTilemap.EachMapPos())
			{
				var data = mainGpTilemap.GetGpTileData(curPos);
				if (data != null && !data.IsColorWhite && !data.grayColor && !data.opened)
					mainGpTilemap.SetDataOpened(curPos, true);
			}
		}

		#endregion ProcessWinOrLose


		#region ActionProcess

		public void ProcessWaitBlueActionStart(GameLogicContext ctx)
		{
			ctx.mapController.SetControllable(true);
		}
		public void ProcessWaitBlueActionEnd(GameLogicContext ctx)
		{
			ctx.mapController.SetControllable(false);
			ProcessActionEnd(ctx);

			if (ctx.gameState == GameState.Running)
			{
				ProcessDoRedActionStart(ctx);
			}
		}

		public void ProcessDoRedActionStart(GameLogicContext ctx)
		{
			ctx.mapController.SetControllable(false);
			ProcessOpponentAction(ctx);
		}
		public void ProcessDoRedActionEnd(GameLogicContext ctx)
		{
			ctx.mapController.SetControllable(false);
			StopOpponentActionCoroutine(ctx); // ?
			ProcessActionEnd(ctx);

			if (ctx.gameState == GameState.Running)
			{
				ProcessWaitBlueActionStart(ctx);
			}
		}

		#endregion ActionProcess


		#region OpponentAICoroutine

		protected void ProcessOpponentAction(GameLogicContext ctx)
		{
			StopOpponentActionCoroutine(ctx);
			if (OpponentSeqActionReady(ctx))
			{
				ctx.redActionCoroutine = ctx.mapController.StartCoroutine(OpponentActionSeq(ctx));
			}
			else
			{
			}
		}

		protected bool OpponentSeqActionReady(GameLogicContext ctx)
		{
			var random = MakeOpponentAIRandom(); // temp

			ctx.borderMoveContext.Clear();
			var bmType = OpponentAIReadyAction(ctx, ctx.borderMoveContext, random);
			if (bmType != BorderMoveType.None)
			{
				ctx.uiFaceBlue.SetColorWhite();
				ctx.uiFaceBlue.SetFaceNormal();
				ctx.uiFaceRed.SetColorRed();
				ctx.uiFaceRed.SetFaceNormal();
				return true;
			}
			else
			{
				ctx.uiFaceBlue.SetColorBlue();
				ctx.uiFaceBlue.SetFaceNormal();
				ctx.uiFaceRed.SetColorWhite();
				ctx.uiFaceRed.SetFaceNormal();
				ctx.borderMoveContext.Clear();
				return false;
			}
		}

		protected void StopOpponentActionCoroutine(GameLogicContext ctx)
		{
			if (ctx.redActionCoroutine != null)
				ctx.mapController.StopCoroutine(ctx.redActionCoroutine);
		}

		protected IEnumerator OpponentActionSeq(GameLogicContext ctx)
		{
			OpponentActionShowHighlightHud(ctx);
			yield return new WaitForSeconds(ctx.mapController.RedActionSeqDuration);
			OpponentActionShowArrowHud(ctx);
			yield return new WaitForSeconds(ctx.mapController.RedActionSeqDuration);
			OpponentActionDoAction(ctx);

			ProcessDoRedActionEnd(ctx);
		}

		protected void OpponentActionShowHighlightHud(GameLogicContext ctx)
		{
			ctx.uiFaceRed.SetFacePress();

			OpponentAIShowHighlightHud(ctx, ctx.borderMoveContext);
		}

		protected void OpponentActionShowArrowHud(GameLogicContext ctx)
		{
			OpponentAIShowArrowHud(ctx, ctx.borderMoveContext);
		}

		protected void OpponentActionDoAction(GameLogicContext ctx)
		{
			OpponentAIDoAction(ctx, ctx.borderMoveContext);

			//uiFace.SetColorBlue();
			//uiFace.SetFaceNormal();
			ctx.uiFaceBlue.SetColorBlue();
			ctx.uiFaceBlue.SetFaceNormal();
			ctx.uiFaceRed.SetColorWhite();
			ctx.uiFaceRed.SetFaceNormal();
			ctx.uiFaceRed.SetPressState(false);
		}

		#endregion OpponentAICoroutine


		#region OpenOut

		protected void OpenOutTileAuto(GameLogicContext ctx, in Vector2Int curPos)
		{
			if (!IsValidPos(ctx, CURRENT, curPos))
				return;

			var data = GetGpTileData(ctx, CURRENT, curPos);
			if (data.opened)
			{
			}
			else if (data.IsFlagMine)
			{
			}
			else if (data.hasMine)
			{
				BombTileLinked(ctx, curPos);
			}
			else if (data.numValue > 0)
			{
				OpenOutNumberTile(ctx, curPos);
			}
			else
			{
				OpenOutEmptyTileLinked(ctx, curPos);
			}
		}

		protected void OpenOutEmptyTileLinked(GameLogicContext ctx, in Vector2Int srcCurPos)
		{
			var data = GetGpTileData(ctx, CURRENT, srcCurPos);
			if (!(data.IsNumber && data.numValue == 0))
				return;

			var visitedSet = mTempVisitedPos;
			var taskQueue = mTempTaskPos;
			visitedSet.Clear();
			taskQueue.Clear();

			Vector2Int pos = srcCurPos;
			visitedSet.Add(pos);
			taskQueue.Enqueue(pos);

			int maxLoop = ctx.mainGpTilemap.MapWidth * ctx.mainGpTilemap.MapHeight;
			for (int i = 0; i < maxLoop; ++i)
			{
				if (!taskQueue.TryDequeue(out pos))
					break;
				data = GetGpTileData(ctx, CURRENT, pos);
				if (data.opened)
				{
				}
				else if (data.hasMine || data.numValue > 0) // data.numValue > 0
				{
					// 空格及其连接的空格的3x3周围不会有雷
					// 所以直接视为数字打开

					// 暂时 只翻开自己的区域
					if (data.IsColorBlue)
						OpenOutNumberTile(ctx, pos);
					else
						FlagToNumberTile(ctx, CURRENT, pos);
				}
				else
				{
					OpenOutEmptyTile(ctx, pos);

					foreach (var tPos in EachPosRange3X3(pos))
					{
						if (IsValidPos(ctx, CURRENT, tPos) && !visitedSet.Contains(tPos))
						{
							if (!GetGpTileData(ctx, CURRENT, tPos).IsFlagMine)
							{
								visitedSet.Add(tPos);
								taskQueue.Enqueue(tPos);
							}
						}
					}
				}
			}

			visitedSet.Clear();
			taskQueue.Clear();
		}

		protected void OpenOutNumberTile(GameLogicContext ctx, in Vector2Int curPos)
		{
			SetTileDataOpened(ctx, CURRENT, curPos, true);
			SetTileDataFlagNone(ctx, CURRENT, curPos);
			// 打开后 清除 flagState
		}

		protected void OpenOutEmptyTile(GameLogicContext ctx, in Vector2Int curPos)
		{
			//SetTileDataGrayColor(ctx, pos, true); // 暂不
			SetTileDataOpened(ctx, CURRENT, curPos, true);
			SetTileDataFlagNone(ctx, CURRENT, curPos);

			//gpGrouping.main.SetDataColorWhite(pos); // 暂不 ?
			//gpGrouping.RemoveFromGroup(pos); // 暂不 ?

			// 暂时只将无颜色的数字0图块设灰
			var data = GetGpTileData(ctx, CURRENT, curPos);
			if (data != null && data.IsNumber && data.numValue == 0 && data.IsColorWhite)
			{
				SetTileDataGrayColor(ctx, CURRENT, curPos, true);

				// 还要移除这个图块
				RemoveCurTileGroup(ctx, CURRENT, curPos);

				if (data != null && !data.grayColor)
				{
					CountForRemoveCurTile(ctx, data);
				}
			}
		}

		#endregion OpenOut


		#region FlagTo

		protected bool CanFlagTo(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			if (!IsValidPos(ctx, current, pos))
				return false;
			var data = GetGpTileData(ctx, current, pos);
			return !data.opened;
		}

		protected void FlagToTileAuto(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			if (IsValidPos(ctx, current, pos))
			{
				var data = GetGpTileData(ctx, current, pos);
				if (!data.opened)
				{
					if (data.IsFlagNone)
						SetTileDataFlagMine(ctx, current, pos);
					else if (data.IsFlagMine)
						SetTileDataFlagMark(ctx, current, pos);
					else // IsFlagMark
						SetTileDataFlagNone(ctx, current, pos);
				}
			}
		}
		protected void FlagToMineTile(GameLogicContext ctx, in Vector2Int pos)
		{
			if (IsValidPos(ctx, CURRENT, pos))
			{
				var data = GetGpTileData(ctx, CURRENT, pos);
				if (!data.opened && !data.IsFlagMine)
					SetTileDataFlagMine(ctx, CURRENT, pos);
			}
		}
		protected void FlagToNumberTile(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			if (IsValidPos(ctx, current, pos))
			{
				var data = GetGpTileData(ctx, current, pos);
				if (!data.opened && !data.IsFlagMark)
					SetTileDataFlagMark(ctx, current, pos);
			}
		}

		#endregion FlagTo


		#region DefeatTile

		protected void DefeatTile(GameLogicContext ctx, in Vector2Int curPos)
		{
			var data = GetGpTileData(ctx, CURRENT, curPos);
			if (data != null && !data.grayColor)
			{
				CountForRemoveCurTile(ctx, data);
			}

			SetTileDataGrayColor(ctx, CURRENT, curPos, true);
			SetTileDataOpened(ctx, CURRENT, curPos, true);
			SetTileDataFlagNone(ctx, CURRENT, curPos);

			RemoveCurTileGroup(ctx, CURRENT, curPos);
		}

		#endregion DefeatTile


		#region BombTile

		protected void BombTileLinked(GameLogicContext ctx, in Vector2Int srcPos)
		{
			var visited = mTempVisitedPos;
			var task = mTempTaskPos;
			visited.Clear();
			task.Clear();

			visited.Add(srcPos);
			task.Enqueue(srcPos);
			BombTile(ctx, srcPos);

			int maxLoop = ctx.mainGpTilemap.MapWidth * ctx.mainGpTilemap.MapHeight;
			for (int i = 0; i < maxLoop; ++i)
			{
				if (task.Count <= 0)
					break;

				foreach (var pos in EachPosRange3X3(task.Dequeue()))
				{
					if (!visited.Contains(pos) && IsValidPos(ctx, CURRENT, pos))
					{
						visited.Add(pos);
						if (GetGpTileData(ctx, CURRENT, pos).hasMine)
							task.Enqueue(pos);
						BombTile(ctx, pos);
					}
				}
			}

			visited.Clear();
			task.Clear();
		}
		protected void BombTile(GameLogicContext ctx, in Vector2Int curPos)
		{
			var data = GetGpTileData(ctx, CURRENT, curPos);
			if (data != null && !data.grayColor)
			{
				CountForRemoveCurTile(ctx, data);
			}

			SetTileDataGrayColor(ctx, CURRENT, curPos, true);
			SetTileDataOpened(ctx, CURRENT, curPos, true);
			SetTileDataFlagNone(ctx, CURRENT, curPos);

			RemoveCurTileGroup(ctx, CURRENT, curPos);
		}

		#endregion BombTile


		#region BorderMove

		protected BorderMoveType BorderMoveByPos(GameLogicContext ctx, in Vector2Int startPos, in Vector2Int endPos, BorderMoveContext bmCtx)
		{
			var dir = DragOffsetToDirWithLimit(endPos - startPos);
			if (dir == Vector2Int.zero)
				return BorderMoveType.None;
			int groupId = GetGroupId(ctx, startPos);
			var data = GetGpTileData(ctx, CURRENT, startPos);
			if (data == null)
				return BorderMoveType.None;
			var isRed = data.IsColorRed;

			bmCtx.Init(groupId, isRed, dir);

			GetGroupBorder(ctx, groupId, dir, bmCtx);
			var borderMoveType = BorderMove(ctx, bmCtx);
			//bmCtx.Clear();

			return borderMoveType;
		}

		protected BorderMoveType BorderMove(GameLogicContext ctx, BorderMoveContext bmCtx)
		{
			var bmType = DoBorderMove(ctx, bmCtx.groupId, bmCtx.isRed, bmCtx.dir, out bmCtx.move, bmCtx.grayList, bmCtx.whiteList, bmCtx.redList, bmCtx.wallList);
			bmCtx.isDone = true;
			return bmType;
		}


		protected BorderMoveType DoBorderMove(GameLogicContext ctx, int groupId, bool isRed, in Vector2Int dir, out Vector2Int move,
			in List<Vector2Int> grayList, in List<Vector2Int> whiteList, in List<Vector2Int> redList, in List<Vector2Int> wallList)
		{
			move = Vector2Int.zero;
			if (dir == Vector2Int.zero)
				return BorderMoveType.None;
			if (whiteList.Count > 0 || redList.Count > 0)
			{
				// 攻击和占领
				DoBorderMoveAttackAndOccupy(ctx, groupId, isRed, dir, whiteList, redList);
				return BorderMoveType.AttackAndOccupy;
			}
			else if (wallList.Count > 0)
			{
				return BorderMoveType.None;
			}
			else if (grayList.Count > 0)
			{
				// 移动
				DoBorderMoveGroupMove(ctx, dir, grayList);
				move = dir;
				return BorderMoveType.Move;
			}
			else
			{
				return BorderMoveType.None;
			}
		}

		protected void DoBorderMoveAttackAndOccupy(GameLogicContext ctx, int groupId, bool isRed, in Vector2Int dir,
			in List<Vector2Int> whiteList, in List<Vector2Int> redList)
		{
			// 攻击和占领
			// 占领同时攻击炸弹 考虑其顺序问题 需要先占领再摧毁
			var gpGrouping = ctx.gpGrouping;
			foreach (var pos in whiteList)
			{
				if (isRed)
					SetTileDataColorRed(ctx, CURRENT, pos + dir);
				else // isBlue
					SetTileDataColorBlue(ctx, CURRENT, pos + dir);
				gpGrouping.OccupyToGroup(pos + dir, groupId, true);
				CountForOccupyCurTile(ctx, isRed);
			}
			foreach (var pos1 in redList)
			{
				var pos2 = pos1 + dir;
				var data1 = GetGpTileData(ctx, CURRENT, pos1);
				var data2 = GetGpTileData(ctx, CURRENT, pos2);

				if (data1.IsNumber)
				{
					if (data1.numValue > 0)
						OpenOutNumberTile(ctx, pos1);
					else
						OpenOutEmptyTile(ctx, pos1);
				}
				if (data2.IsNumber)
				{
					if (data2.numValue > 0)
						OpenOutNumberTile(ctx, pos2);
					else
						OpenOutEmptyTile(ctx, pos2);
				}

				if (data1.hasMine)
				{
					BombTileLinked(ctx, pos1);
				}
				else if (data2.hasMine)
				{
					BombTileLinked(ctx, pos2);
				}
				else if (data1.numValue < data2.numValue)
				{
					DefeatTile(ctx, pos1);
				}
				else if (data1.numValue > data2.numValue)
				{
					DefeatTile(ctx, pos2);
				}
				else if (data1.numValue == data2.numValue)
				{
					DefeatTile(ctx, pos1);
					DefeatTile(ctx, pos2);
				}
				else
				{
				}
			}
			RefreshSplitGroup(ctx);
		}


		protected Dictionary<Vector2Int, GpTileData> mTempTileDatas = new(16);

		protected void DoBorderMoveGroupMove(GameLogicContext ctx, in Vector2Int dir, in List<Vector2Int> grayList)
		{
			// 移动
			var gpGrouping = ctx.gpGrouping;
			var tileDatas = mTempTileDatas;
			tileDatas.Clear();

			int groupId = GetGroupId(ctx, grayList[0]);
			foreach (var pos in gpGrouping.EachPosByGroupId(groupId))
			{
				tileDatas.Add(pos, GetGpTileData(ctx, CURRENT, pos));
				SetEmptyCurrentTile(ctx, pos);
			}
			foreach (var pos in tileDatas.Keys)
			{
				gpGrouping.RemoveFromGroup(pos);
			}
			foreach (var pair in tileDatas)
			{
				SetCurrentTile(ctx, pair.Key + dir, pair.Value);
				gpGrouping.OccupyToGroup(pair.Key + dir, groupId, true);
			}
			tileDatas.Clear();
		}

		#endregion BorderMove


		#region OpponentAI

		// 随机进行一步
		// 或是根据玩家上次操作 进行一步


		protected static readonly int MAX_RANDOM_SEED = 0x7FFFFFFF;

		protected int MakeOpponentAIRandomSeed()
		{
			return MAX_RANDOM_SEED * UnityRandom.Range(0, MAX_RANDOM_SEED);
		}

		public SysRandom MakeOpponentAIRandom()
		{
			return new SysRandom(MakeOpponentAIRandomSeed());
		}


		protected List<int> mTempRedGroupIdList = new(8);
		protected List<Vector2Int> mTempDirList = new(4);

		protected static readonly Vector2Int[] Direction4List = new Vector2Int[4] {
			Vector2Int.up,
			Vector2Int.down,
			Vector2Int.left,
			Vector2Int.right,
		};

		protected IEnumerable<KeyValuePair<int, Vector2Int>> EachRandomRedGroupAndDir(GameLogicContext ctx, SysRandom random)
		{
			var groupList = mTempRedGroupIdList;
			var dirList = mTempDirList;
			groupList.Clear();
			dirList.Clear();

			foreach (var groupId in ctx.gpGrouping.EachRedGroupId())
				groupList.Add(groupId);

			for (int i = 0, l = groupList.Count; i < l; ++i)
			{
				var groupId = groupList[random.Next(groupList.Count)];
				groupList.Remove(groupId);

				dirList.Clear();
				dirList.AddRange(Direction4List);

				for (int j = 0; j < 4; ++j)
				{
					var dir = dirList[random.Next(dirList.Count)];
					dirList.Remove(dir);

					yield return new(groupId, dir);
				}
			}
		}

		protected BorderMoveType CalcBorderMoveType(GameLogicContext ctx, BorderMoveContext bmCtx)
		{
			BorderMoveType result;
			if (bmCtx.whiteList.Count > 0 || bmCtx.redList.Count > 0)
			{
				// 攻击和占领
				result = BorderMoveType.AttackAndOccupy;
			}
			else if (bmCtx.wallList.Count > 0)
			{
				result = BorderMoveType.None;
			}
			else if (bmCtx.grayList.Count > 0)
			{
				// 移动
				result = BorderMoveType.Move;
			}
			else
			{
				result = BorderMoveType.None;
			}
			//bmCtx.actionType = result;
			return result;
		}

		public BorderMoveType MakeRandomRedBorderMove(GameLogicContext ctx, BorderMoveContext bmCtx, SysRandom random)
		{
			bmCtx.Clear();
			bool firstFoundMove = true;
			int tempGroupId = 0;
			Vector2Int tempDir = Vector2Int.zero;
			foreach (var pair in EachRandomRedGroupAndDir(ctx, random))
			{
				bmCtx.Init(pair.Key, true, pair.Value);
				GetGroupBorder(ctx, bmCtx);
				var bmType = CalcBorderMoveType(ctx, bmCtx);
				if (firstFoundMove && bmType == BorderMoveType.Move)
				{
					firstFoundMove = false;
					tempGroupId = pair.Key;
					tempDir = pair.Value;
				}
				else if (bmType != BorderMoveType.None)
				{
					bmCtx.actionType = bmType;
					bmCtx.isChecked = true;
					return bmType;
				}
			}
			if (!firstFoundMove && tempGroupId > 0 && tempDir != Vector2Int.zero)
			{
				bmCtx.Init(tempGroupId, true, tempDir);
				GetGroupBorder(ctx, bmCtx);
				var bmType = CalcBorderMoveType(ctx, bmCtx);
				bmCtx.actionType = bmType;
				bmCtx.isChecked = true;
				return bmType;
			}
			//bmCtx.Clear();
			return BorderMoveType.None;
		}

		public BorderMoveType OpponentAIOneStep(GameLogicContext ctx, BorderMoveContext bmCtx, SysRandom random)
		{
			var bmType = MakeRandomRedBorderMove(ctx, bmCtx, random);
			if (bmType != BorderMoveType.None)
				return BorderMove(ctx, bmCtx);
			else
				return BorderMoveType.None;
		}


		public BorderMoveType OpponentAIReadyAction(GameLogicContext ctx, BorderMoveContext bmCtx, SysRandom random)
		{
			return MakeRandomRedBorderMove(ctx, bmCtx, random);
		}

		public BorderMoveType OpponentAIDoAction(GameLogicContext ctx, BorderMoveContext bmCtx)
		{
			ClearArrowHud(ctx);
			ClearHighlightHud(ctx);
			var bmType = BorderMove(ctx, bmCtx);
			RefreshColorCounter(ctx);
			return bmType;
		}

		#endregion OpponentAI

		// TODO : 多次移动


		// TODO : 优化Hud提示 -> 整理代码 -> 优化地图生成(雷、涂色) -> 优化流程提示

		// TODO : 音效、动效等 ; 开始界面完善 ; 设置界面 ; 优化程序代码 ; 优化操作

		// TODO : 更好的流程提示 以及多次移动提示 等


		#region OpponentAI Hud

		public void OpponentAIShowHighlightHud(GameLogicContext ctx, BorderMoveContext bmCtx)
		{
			SetHighlightHudByGroup(ctx, bmCtx.groupId, REDCOLOR);
		}

		public void OpponentAIShowArrowHud(GameLogicContext ctx, BorderMoveContext bmCtx)
		{
			ShowRedArrowHud(ctx, bmCtx);
		}

		protected void ShowRedArrowHud(GameLogicContext ctx, BorderMoveContext bmCtx)
		{
			ClearArrowHud(ctx);
			//SetArrowHudPoint(ctx, startPos);

			if (bmCtx.dir != Vector2Int.zero)
			{
				if (bmCtx.actionType == BorderMoveType.AttackAndOccupy)
				{
					SetArrowHudPlus(ctx, bmCtx.whiteList, bmCtx.dir);
					SetArrowHudDouble(ctx, bmCtx.redList, bmCtx.dir);
				}
				else if (bmCtx.actionType == BorderMoveType.Move)
				{
					SetArrowHudMove(ctx, bmCtx.grayList, bmCtx.dir);
				}
				else
				{
				}
			}
		}

		#endregion OpponentAI Hud


		#region Utils

		protected HashSet<Vector2Int> mTempVisitedPos = new(64);
		protected Queue<Vector2Int> mTempTaskPos = new(64);

		protected IEnumerable<Vector2Int> EachPosRange3X3(Vector2Int center)
		{
			Vector2Int pos = Vector2Int.zero;
			for (int i = -1; i <= 1; ++i)
			{
				for (int j = -1; j <= 1; ++j)
				{
					pos.x = center.x + i;
					pos.y = center.y + j;
					yield return pos;
				}
			}
		}

		#endregion Utils


		#region Cur/Ori Pos

		protected static readonly bool CURRENT = true;
		protected static readonly bool ORIGINAL = false;

		protected Vector2Int mTempPosResult = Vector2Int.zero;

		protected bool TryPosConvertTo(GameLogicContext ctx, bool current, in Vector2Int pos, out Vector2Int result)
		{
			if (current)
			{
				if (ctx.posMapping.ExistsOriPos(pos))
				{
					result = ctx.posMapping.GetCurPosByOri(pos);
					return true;
				}
				else
				{
					result = Vector2Int.zero;
					return false;
				}
			}
			else
			{
				if (ctx.posMapping.ExistsCurPos(pos))
				{
					result = ctx.posMapping.GetOriPosByCur(pos);
					return true;
				}
				else
				{
					result = Vector2Int.zero;
					return false;
				}
			}
		}

		public bool PosHitInCurrentTilemap(GameLogicContext ctx, in Vector2Int hitPos, out Vector2Int realPos)
		{
			realPos = hitPos - ctx.mainGpTilemap.PosOffset;
			//return 0 <= hitPos.x && hitPos.x < ctx.mainGpTilemap.MapWidth
			//	&& 0 <= hitPos.y && hitPos.y < ctx.mainGpTilemap.MapHeight;
			return 0 <= realPos.x && realPos.x < ctx.mainGpTilemap.MapWidth
				&& 0 <= realPos.y && realPos.y < ctx.mainGpTilemap.MapHeight;
		}

		public bool PosHitInOriginalTilemap(GameLogicContext ctx, in Vector2Int hitPos, out Vector2Int realPos)
		{
			realPos = hitPos - ctx.originalGpTilemap.PosOffset;
			return 0 <= realPos.x && realPos.x < ctx.mainGpTilemap.MapWidth
				&& 0 <= realPos.y && realPos.y < ctx.mainGpTilemap.MapHeight;
		}

		public bool PosHitInTilemap(GameLogicContext ctx, in Vector2Int hitPos, out Vector2Int realPos)
		{
			/*if (PosHitInOriginalTilemap(ctx, hitPos, out realPos))
			{
				return ORIGINAL;
			}
			else
			{
				//realPos = hitPos;
				return CURRENT;
			}*/
			//return PosHitInOriginalTilemap(ctx, hitPos, out realPos);
			if (PosHitInOriginalTilemap(ctx, hitPos, out realPos))
			{
				return ORIGINAL;
			}
			else
			{
				PosHitInCurrentTilemap(ctx, hitPos, out realPos);
				return CURRENT;
			}
		}

		#endregion Cur/Ori Pos


		#region ArrowHud

		public void ClearArrowHud(GameLogicContext ctx)
		{
			ctx.arrowHudTilemap.ClearAllArrow();
		}

		public void SetArrowHudPoint(GameLogicContext ctx, in Vector2Int pos)
		{
			ctx.arrowHudTilemap.SetArrowPoint(pos);
		}

		public void SetArrowHudPlus(GameLogicContext ctx, List<Vector2Int> posList, in Vector2Int dir)
		{
			ctx.arrowHudTilemap.SetPlusArrow(posList, dir);
		}
		public void SetArrowHudDouble(GameLogicContext ctx, List<Vector2Int> posList, in Vector2Int dir)
		{
			ctx.arrowHudTilemap.SetDoubleArrow(posList, dir);
		}
		public void SetArrowHudMove(GameLogicContext ctx, List<Vector2Int> posList, in Vector2Int dir)
		{
			ctx.arrowHudTilemap.SetMoveArrow(posList, dir);
		}

		#endregion ArrowHud


		#region HighlightHud

		protected static readonly bool BLUECOLOR = false;
		protected static readonly bool REDCOLOR = true;

		public void ClearHighlightHud(GameLogicContext ctx)
		{
			ctx.highlightHud.ClearHighlight();
			ctx.oriHighlightHud.ClearHighlight();
			ctx.gpGrouping.PutDownGroup();
		}

		public void SetHighlightHudByGroup(GameLogicContext ctx, int groupId, bool isRed, bool showToOriginal = false)
		{
			foreach (var curPos in ctx.gpGrouping.EachPosByGroupId(groupId))
			{
				var data = GetGpTileData(ctx, CURRENT, curPos);
				ctx.highlightHud.AddHighlightPos(curPos, data, isRed);

				if (showToOriginal)
				{
					if (TryPosConvertTo(ctx, ORIGINAL, curPos, out var oriPos))
					{
						data = GetGpTileData(ctx, ORIGINAL, oriPos);
						ctx.oriHighlightHud.AddHighlightPos(oriPos, data, isRed);
					}
				}
			}
		}

		#endregion HighlightHud


		#region ColorCounter

		public void RefreshColorCounter(GameLogicContext ctx)
		{
			ctx.uiCounterBlue.SetNumber(ctx.colorCounter.BlueCount);
			ctx.uiCounterRed.SetNumber(ctx.colorCounter.RedCount);

			//var counter = ctx.colorCounter;
			//Debug.LogFormat("All : {0} | White : {1} | Blue : {2} | Red : {3}",
			//	counter.WhiteCount + counter.BlueCount + counter.RedCount,
			//	counter.WhiteCount, counter.BlueCount, counter.RedCount);
		}

		protected void CountForRemoveCurTile(GameLogicContext ctx, GpTileData data)
		{
			/*if (!data.grayColor)
			{
				if (data.IsColorBlue)
					ctx.colorCounter.DecBlue();
				else if (data.IsColorRed)
					ctx.colorCounter.DecRed();
				else if (data.IsColorWhite)
					ctx.colorCounter.DecWhite();
			}*/
			if (data.IsColorBlue)
				ctx.colorCounter.DecBlue();
			else if (data.IsColorRed)
				ctx.colorCounter.DecRed();
			else if (data.IsColorWhite)
				ctx.colorCounter.DecWhite();
		}

		protected void CountForOccupyCurTile(GameLogicContext ctx, bool isRed)
		{
			ctx.colorCounter.DecWhite();
			if (isRed)
				ctx.colorCounter.AddRed();
			else
				ctx.colorCounter.AddBlue();
		}

		#endregion ColorCounter


		#region Group

		public int GetGroupId(GameLogicContext ctx, in Vector2Int curPos)
		{
			return ctx.gpGrouping.GetGroupId(curPos);
		}

		public bool CanPickUpGroup(GameLogicContext ctx, in Vector2Int curPos)
		{
			return ctx.gpGrouping.CanPickUpGroup(curPos, false);
		}
		public bool CanPickUpGroup(GameLogicContext ctx, in Vector2Int curPos, bool checkSameId)
		{
			return ctx.gpGrouping.CanPickUpGroup(curPos, checkSameId);
		}

		public void PickUpGroup(GameLogicContext ctx, in Vector2Int curPos)
		{
			ctx.gpGrouping.PickUpGroup(curPos);
		}

		public bool IsPickedGroup(GameLogicContext ctx)
		{
			return ctx.gpGrouping.IsPickedGroup();
		}

		public void RefreshSplitGroup(GameLogicContext ctx)
		{
			ctx.gpGrouping.RefreshSplitGroup();
		}

		public void GetGroupBorder(GameLogicContext ctx, int groupId, in Vector2Int dir,
			in List<Vector2Int> grayList, in List<Vector2Int> whiteList, in List<Vector2Int> redList, in List<Vector2Int> wallList)
		{
			ctx.gpGrouping.GetGroupBorder(groupId, dir, grayList, whiteList, redList, wallList);
		}
		public void GetGroupBorder(GameLogicContext ctx, int groupId, in Vector2Int dir, BorderMoveContext bmCtx)
		{
			ctx.gpGrouping.GetGroupBorder(groupId, dir, bmCtx.grayList, bmCtx.whiteList, bmCtx.redList, bmCtx.wallList);
		}
		public void GetGroupBorder(GameLogicContext ctx, BorderMoveContext bmCtx)
		{
			ctx.gpGrouping.GetGroupBorder(bmCtx.groupId, bmCtx.dir, bmCtx.grayList, bmCtx.whiteList, bmCtx.redList, bmCtx.wallList);
		}

		#endregion Group


		#region CurrentTile

		public void SetCurrentTile(GameLogicContext ctx, in Vector2Int curPos, GpTileData data)
		{
			ctx.posMapping.OnAddCurPos(curPos, data.originalPos);
			ctx.mainGpTilemap.SetTile(curPos, data);
		}

		//public void RemoveCurrentTile(GameLogicContext ctx, in Vector2Int curPos)
		//{
		//	ctx.posMapping.OnRemoveCurPos(curPos);
		//	ctx.mainGpTilemap.RemoveTile(curPos);
		//}

		public void SetEmptyCurrentTile(GameLogicContext ctx, in Vector2Int curPos)
		{
			ctx.posMapping.OnRemoveCurPos(curPos);
			ctx.mainGpTilemap.SetEmptyTile(curPos);
		}

		public void RemoveCurTileGroup(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			var curPos = current ? pos : ctx.posMapping.GetCurPosByOri(pos);
			ctx.gpGrouping.RemoveFromGroup(curPos);
			ctx.posMapping.OnRemoveCurPos(curPos);
		}

		#endregion CurrentTile

		// TODO : SetEmptyTile 的 emptyTile 用对象池 (或用同一对象)


		#region GetTileData

		public bool IsValidPos(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			if (current)
				return ctx.mainGpTilemap.IsValidPos(pos);
			else
				return ctx.originalGpTilemap.IsValidPos(pos);
		}

		public GpTileData GetGpTileData(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			if (current)
				return ctx.mainGpTilemap.GetGpTileData(pos);
			else
				return ctx.originalGpTilemap.GetGpTileData(pos);
		}

		#endregion GetTileData


		#region SetDataValue

		protected delegate void SetTileDataFlagDelegate(in Vector2Int pos);
		protected delegate void SetTileDataBoolDelegate(in Vector2Int pos, bool value);
		protected delegate void SetTileDataIntDelegate(in Vector2Int pos, int value);

		protected void SetTileDataTemplate(GameLogicContext ctx, SetTileDataFlagDelegate curTmFunc, SetTileDataFlagDelegate oriTmFunc,
			bool current, in Vector2Int pos)
		{
			(current ? curTmFunc : oriTmFunc)(pos);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? curTmFunc : oriTmFunc)(mTempPosResult);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}
		protected void SetTileDataTemplate(GameLogicContext ctx, SetTileDataBoolDelegate curTmFunc, SetTileDataBoolDelegate oriTmFunc,
			bool current, in Vector2Int pos, bool value)
		{
			(current ? curTmFunc : oriTmFunc)(pos, value);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? curTmFunc : oriTmFunc)(mTempPosResult, value);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}
		protected void SetTileDataTemplate(GameLogicContext ctx, SetTileDataIntDelegate curTmFunc, SetTileDataIntDelegate oriTmFunc,
			bool current, in Vector2Int pos, int value)
		{
			(current ? curTmFunc : oriTmFunc)(pos, value);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? curTmFunc : oriTmFunc)(mTempPosResult, value);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}

		/*
		public void SetTileDataColorWhite(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			(current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataColorWhite(pos);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataColorWhite(mTempPosResult);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}
		public void SetTileDataColorRed(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			(current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataColorRed(pos);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataColorRed(mTempPosResult);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}
		public void SetTileDataColorBlue(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			(current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataColorBlue(pos);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataColorBlue(mTempPosResult);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}

		public void SetTileDataGrayColor(GameLogicContext ctx, bool current, in Vector2Int pos, bool value)
		{
			(current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataGrayColor(pos, value);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataGrayColor(mTempPosResult, value);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}

		public void SetTileDataMine(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			(current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataMine(pos);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataMine(mTempPosResult);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}
		public void SetTileDataNumber(GameLogicContext ctx, bool current, in Vector2Int pos, int value)
		{
			(current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataNumber(pos, value);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataNumber(mTempPosResult, value);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}

		public void SetTileDataFlagNone(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			(current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataFlagNone(pos);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataFlagNone(mTempPosResult);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}
		public void SetTileDataFlagMine(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			(current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataFlagMine(pos);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataFlagMine(mTempPosResult);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}
		public void SetTileDataFlagMark(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			(current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataFlagMark(pos);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataFlagMark(mTempPosResult);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}

		public void SetTileDataHolded(GameLogicContext ctx, bool current, in Vector2Int pos, bool value)
		{
			(current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataHolded(pos, value);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataHolded(mTempPosResult, value);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}
		public void SetTileDataOpened(GameLogicContext ctx, bool current, in Vector2Int pos, bool value)
		{
			(current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataOpened(pos, value);
			(current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(pos);
			if (TryPosConvertTo(ctx, !current, pos, out mTempPosResult))
			{
				(!current ? ctx.mainGpTilemap : ctx.originalGpTilemap).SetDataOpened(mTempPosResult, value);
				(!current ? ctx.highlightHud : ctx.oriHighlightHud).RefreshTile(mTempPosResult);
			}
		}
		*/

		public void SetTileDataColorWhite(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			SetTileDataTemplate(ctx, ctx.mainGpTilemap.SetDataColorWhite, ctx.originalGpTilemap.SetDataColorWhite, current, pos);
		}
		public void SetTileDataColorRed(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			SetTileDataTemplate(ctx, ctx.mainGpTilemap.SetDataColorRed, ctx.originalGpTilemap.SetDataColorRed, current, pos);
		}
		public void SetTileDataColorBlue(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			SetTileDataTemplate(ctx, ctx.mainGpTilemap.SetDataColorBlue, ctx.originalGpTilemap.SetDataColorBlue, current, pos);
		}

		public void SetTileDataGrayColor(GameLogicContext ctx, bool current, in Vector2Int pos, bool value)
		{
			SetTileDataTemplate(ctx, ctx.mainGpTilemap.SetDataGrayColor, ctx.originalGpTilemap.SetDataGrayColor, current, pos, value);
		}

		public void SetTileDataMine(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			SetTileDataTemplate(ctx, ctx.mainGpTilemap.SetDataMine, ctx.originalGpTilemap.SetDataMine, current, pos);
		}
		public void SetTileDataNumber(GameLogicContext ctx, bool current, in Vector2Int pos, int value)
		{
			SetTileDataTemplate(ctx, ctx.mainGpTilemap.SetDataNumber, ctx.originalGpTilemap.SetDataNumber, current, pos, value);
		}

		public void SetTileDataFlagNone(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			SetTileDataTemplate(ctx, ctx.mainGpTilemap.SetDataFlagNone, ctx.originalGpTilemap.SetDataFlagNone, current, pos);
		}
		public void SetTileDataFlagMine(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			SetTileDataTemplate(ctx, ctx.mainGpTilemap.SetDataFlagMine, ctx.originalGpTilemap.SetDataFlagMine, current, pos);
		}
		public void SetTileDataFlagMark(GameLogicContext ctx, bool current, in Vector2Int pos)
		{
			SetTileDataTemplate(ctx, ctx.mainGpTilemap.SetDataFlagMark, ctx.originalGpTilemap.SetDataFlagMark, current, pos);
		}

		public void SetTileDataHolded(GameLogicContext ctx, bool current, in Vector2Int pos, bool value)
		{
			SetTileDataTemplate(ctx, ctx.mainGpTilemap.SetDataHolded, ctx.originalGpTilemap.SetDataHolded, current, pos, value);
		}
		public void SetTileDataOpened(GameLogicContext ctx, bool current, in Vector2Int pos, bool value)
		{
			SetTileDataTemplate(ctx, ctx.mainGpTilemap.SetDataOpened, ctx.originalGpTilemap.SetDataOpened, current, pos, value);
		}

		#endregion SetDataValue


		// TODO : ？合并素材减少DrawCall


		// TODO : 抖动效果 配合爆炸和攻击等


		// TODO : 专用的 Setting 对象
	}
}
