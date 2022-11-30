using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
	public class GpMapController : MonoBehaviour, IFakeScene
	{
		#region Members

		[SerializeField]
		protected GpTilemapData mainGpTilemap;

		[SerializeField]
		protected GpGroupingMap mainGrouping;

		[SerializeField]
		protected HudArrowTilemap arrowHudTilemap;

		[SerializeField]
		protected HudGroupHighlight highlightHud;
		[SerializeField]
		protected HudGroupHighlight originalHighlightHud;

		[SerializeField]
		protected GpTilemapData originalGpTilemap;

		[SerializeField]
		protected UIFaceButton uiFaceBlue;
		[SerializeField]
		protected UIFaceButton uiFaceRed;

		[SerializeField]
		protected UICounter uiCounterBlue;
		[SerializeField]
		protected UICounter uiCounterRed;

		[SerializeField]
		protected UIButton uiHelpBtn;


		protected BorderMoveContext mBorderMoveContext;
		public BorderMoveContext BorderMoveContext => mBorderMoveContext;


		protected GameLogicContext mGameLogicContext;
		public GameLogicContext GameLogicContext => mGameLogicContext;

		#endregion Members


		#region UnityMessage

		public void Init()
		{
			mBorderMoveContext = new(new(16), new(16), new(16), new(16));

			mGameLogicContext = new()
			{
				mapController = this,
				gpGrouping = mainGrouping,
				arrowHudTilemap = arrowHudTilemap,
				highlightHud = highlightHud,
				oriHighlightHud = originalHighlightHud,
				mainGpTilemap = mainGrouping.main,
				originalGpTilemap = originalGpTilemap,
				posMapping = new GpPosMappingData(),
				uiFaceBlue = uiFaceBlue,
				uiFaceRed = uiFaceRed,
				colorCounter = new GpColorCounter(),
				uiCounterBlue = uiCounterBlue,
				uiCounterRed = uiCounterRed,
				borderMoveContext = mBorderMoveContext,
				redActionCoroutine = null,
				gameState = GameState.None,
			};

			GpMapGameLogic.Inst.ProcessClearAll(mGameLogicContext);
		}

		protected void Update()
		{
			UpdateMain();
		}

		#endregion UnityMessage


		#region UpdateMain

		protected bool mLastPosInFocus = false;
		protected Vector2Int mLastMousePos = Vector2Int.zero;

		protected void UpdateMain()
		{
			if (mControllable)
			{
				// 区分移动和点击 分成两部分
				var currentMousePos = CurrentMouseHitGridPos();
				if (!mLastPosInFocus || mLastMousePos != currentMousePos)
				{
					mLastPosInFocus = true;
					mLastMousePos = currentMousePos;
					UpdateMouseMove();
				}
				UpdateMouseButton();
			}
			UpdateUIButtons();
		}

		protected void UpdateMouseMove()
		{
			UpdateClickMove();
			UpdateDragMove();
		}

		protected void UpdateMouseButton()
		{
			if (mDragingState)
				UpdateDragButton();
			else
			{
				UpdateDragButton();
				if (!mDragingState)
					UpdateClickButton();
			}
		}

		#endregion UpdateMain


		#region PressState

		// 只要处于任意一种非None的鼠标模式 就必定有一个 hitPos
		// 只要处于None鼠标模式 就必定没有 hitPos

		protected enum PressState
		{
			None,
			PressL,
			PressR,
			PressLR,
		}

		protected PressState mPressState = PressState.None;

		protected Vector2Int mPressHitPos = Vector2Int.zero;

		protected Vector2Int CurrentMouseHitGridPos()
		{
			var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			return new Vector2Int((int)pos.x - (pos.x < 0 ? 1 : 0), (int)pos.y - (pos.y < 0 ? 1 : 0));
		}
		protected Vector3 CurrentMouseHitPos3()
		{
			return Camera.main.ScreenToWorldPoint(Input.mousePosition);
		}

		protected void ChangePressState(PressState newState)
		{
			if (mPressState != newState)
			{
				if (mPressState == PressState.PressL)
					ShowPressLHitState(false);
				else if (mPressState == PressState.PressR)
					ShowPressRHitState(false);
				else if (mPressState == PressState.PressLR)
					ShowPressLRHitState(false);

				// 退出状态时 清除选中坐标状态

				mPressState = newState;

				if (mPressState == PressState.PressL)
					ShowPressLHitState(true);
				else if (mPressState == PressState.PressR)
					ShowPressRHitState(true);
				else if (mPressState == PressState.PressLR)
					ShowPressLRHitState(true);

				// 进入状态时 不自动更新选中坐标状态
				// 需要额外调用更新方法
			}
		}

		protected void RefreshPressHitPos(in Vector2Int hitPos)
		{
			if (mPressHitPos != hitPos)
			{
				if (mPressState == PressState.PressL)
					ShowPressLHitState(false);
				else if (mPressState == PressState.PressR)
					ShowPressRHitState(false);
				else if (mPressState == PressState.PressLR)
					ShowPressLRHitState(false);

				mPressHitPos = hitPos;

				if (mPressState == PressState.PressL)
					ShowPressLHitState(true);
				else if (mPressState == PressState.PressR)
					ShowPressRHitState(true);
				else if (mPressState == PressState.PressLR)
					ShowPressLRHitState(true);
			}
		}

		#endregion PressState


		#region InputKey

		protected bool LMouseDown => Input.GetMouseButtonDown(0);
		protected bool RMouseDown => Input.GetMouseButtonDown(1);
		protected bool LMouseUp => Input.GetMouseButtonUp(0);
		protected bool RMouseUp => Input.GetMouseButtonUp(1);
		protected bool LMouseHold => Input.GetMouseButton(0);
		protected bool RMouseHold => Input.GetMouseButton(1);

		protected bool SpaceKeyDown => Input.GetKeyDown(KeyCode.Space);

		protected bool TabKeyDown => Input.GetKeyDown(KeyCode.Tab);
		protected bool TabKeyUp => Input.GetKeyUp(KeyCode.Tab);
		protected bool TabKeyHold => Input.GetKey(KeyCode.Tab);

		#endregion InputKey


		#region ClickControl

		// 立即触发右键
		[SerializeField]
		protected bool pressRImmediately = false;

		protected void UpdateClickMove()
		{
			if (mPressState != PressState.None)
				RefreshPressHitPos(CurrentMouseHitGridPos());
		}

		protected void UpdateClickButton()
		{
			RefreshPressHitPos(CurrentMouseHitGridPos());

			if (mPressState == PressState.PressLR && LMouseUp)
			{
				OnLRMouseUpL();
				ChangePressState(PressState.None);
			}
			else if (mPressState == PressState.PressL && LMouseUp)
			{
				OnLMouseUpL();
				ChangePressState(PressState.None);
			}
			else if (mPressState == PressState.PressLR && RMouseUp)
			{
				OnLRMouseUpR();
				ChangePressState(PressState.None);
			}
			else if (mPressState == PressState.PressR && RMouseUp)
			{
				OnRMouseUpR();
				ChangePressState(PressState.None);
			}
			else
			{
				if ((mPressState == PressState.PressR || RMouseHold) && LMouseDown)
				{
					// OnRMouseDownL
					ChangePressState(PressState.PressLR);
				}
				else if (mPressState == PressState.None && LMouseDown)
				{
					// OnMouseDownL
					ChangePressState(PressState.PressL);
				}

				if ((mPressState == PressState.PressL || LMouseHold) && RMouseDown)
				{
					// OnLMouseDownR
					ChangePressState(PressState.PressLR);
				}
				else if (mPressState == PressState.None && RMouseDown)
				{
					// OnMouseDownR
					if (pressRImmediately)
					{
						// 立即触发右键
						RefreshPressHitPos(CurrentMouseHitGridPos());
						OnRMouseUpR();
					}
					else
					{
						ChangePressState(PressState.PressR);
					}
				}
			}
		}

		protected void ShowPressLHitState(bool value)
		{
			GpMapGameLogic.Inst.ProcessPressLHit(mGameLogicContext, mPressHitPos, value);
		}
		protected void ShowPressRHitState(bool value)
		{
			GpMapGameLogic.Inst.ProcessPressRHit(mGameLogicContext, mPressHitPos, value);
		}
		protected void ShowPressLRHitState(bool value)
		{
			GpMapGameLogic.Inst.ProcessPressLRHit(mGameLogicContext, mPressHitPos, value);
		}

		protected void OnLRMouseUpL()
		{
			GpMapGameLogic.Inst.ProcessClickLR(mGameLogicContext, mPressHitPos);
		}
		protected void OnLMouseUpL()
		{
			GpMapGameLogic.Inst.ProcessClickL(mGameLogicContext, mPressHitPos);
		}
		protected void OnLRMouseUpR()
		{
			GpMapGameLogic.Inst.ProcessClickLR(mGameLogicContext, mPressHitPos);
		}
		protected void OnRMouseUpR()
		{
			GpMapGameLogic.Inst.ProcessClickR(mGameLogicContext, mPressHitPos);
		}

		#endregion ClickControl


		#region DragControl

		protected bool mDragingState = false;

		protected Vector2Int mDragStartPos = Vector2Int.zero - Vector2Int.one;
		protected Vector2Int mDragHitPos = Vector2Int.zero - Vector2Int.one;

		protected void UpdateDragMove()
		{
			if (mDragingState)
			{
				UpdateDragR();
			}
			else
			{
				UpdateNoDragMouseMove();
			}
		}

		protected void UpdateDragButton()
		{
			if (mDragingState)
			{
				// 按住MouseR时按下并抬起MouseL触发
				if (!RMouseHold || RMouseUp)
					EndDragR();
				if (mDragingState && LMouseDown)
					UpdateDragRMouseDownL();
			}
			else
			{
				if (RMouseDown)
					StartDragR();
			}
		}

		protected void StartDragR()
		{
			ChangePressState(PressState.None);

			if (GpMapGameLogic.Inst.CanDragRStart(mGameLogicContext, CurrentMouseHitGridPos()))
			{
				mDragingState = true;
				mDragStartPos = CurrentMouseHitGridPos();
				mDragHitPos = mDragStartPos;

				GpMapGameLogic.Inst.ProcessDragRStart(mGameLogicContext, mDragStartPos);
			}
		}

		protected void UpdateDragR()
		{
			var hitPos = CurrentMouseHitGridPos();
			if (mDragHitPos != hitPos)
			{
				mDragHitPos = hitPos;
				GpMapGameLogic.Inst.ProcessDragRUpdate(mGameLogicContext, mDragStartPos, mDragHitPos);
			}
		}

		protected void UpdateDragRMouseDownL()
		{
			var bmCtx = mBorderMoveContext;
			bmCtx.Clear();
			GpMapGameLogic.Inst.ProcessDragRMouseDownL(mGameLogicContext, mDragStartPos, mDragHitPos, bmCtx, out var borderMoveType);
			if (bmCtx.move != Vector2Int.zero)
				mDragStartPos += bmCtx.move;
			if (borderMoveType == BorderMoveType.AttackAndOccupy)
			{
				EndDragR();
				GpMapGameLogic.Inst.ProcessWaitBlueActionEnd(mGameLogicContext);
			}
			else if (borderMoveType == BorderMoveType.Move)
			{
				if (GpMapGameLogic.Inst.CanDragRContinue(mGameLogicContext, mDragStartPos))
					GpMapGameLogic.Inst.ProcessDragRContinue(mGameLogicContext, mDragStartPos, mDragHitPos);
				else
					EndDragR();
			}
			else
			{
				if (GpMapGameLogic.Inst.CanDragRContinue(mGameLogicContext, mDragStartPos))
					GpMapGameLogic.Inst.ProcessDragRContinue(mGameLogicContext, mDragStartPos, mDragHitPos);
				else
					EndDragR();
			}
		}

		// TODO : 移动次数限制

		protected void EndDragR()
		{
			GpMapGameLogic.Inst.ProcessDragREnd(mGameLogicContext, mDragStartPos, mDragHitPos);

			mDragingState = false;
			mDragStartPos = Vector2Int.zero;
			mDragHitPos = Vector2Int.zero;
		}

		protected void UpdateNoDragMouseMove()
		{
			var hitPos = CurrentMouseHitGridPos();
			if (mDragHitPos != hitPos)
			{
				mDragHitPos = hitPos;
				GpMapGameLogic.Inst.ProcessNoDragUpdate(mGameLogicContext, mDragHitPos);
			}
		}

		#endregion DragControl


		#region Button

		protected void UpdateUIButtons()
		{
			var pos = CurrentMouseHitPos3();
			if (uiFaceBlue.UpdateMouse(pos))
			{
				GpMapGameLogic.Inst.ProcessStartNewGame(GameLogicContext);
			}
			if (uiFaceRed.UpdateMouse(pos))
			{
			}
			if (uiHelpBtn.UpdateMouse(pos))
			{
				GameMainLogic.Inst.DoOpenHelpFromGame();
			}
		}

		#endregion Button


		#region OpponentAction

		[SerializeField, Range(0, 2)]
		protected float opponentSeqWaitTime = 1f;
		public float RedActionSeqDuration => opponentSeqWaitTime;

		#endregion OpponentAction


		#region Controllable

		protected bool mControllable = true;

		public bool IsControllable => mControllable;

		public void SetControllable(bool value)
		{
			mControllable = value;
		}

		#endregion Controllable


		#region GenerateMapSize

		[SerializeField]
		protected Vector3Int normalMapSize = new(18, 18, 50);
		public Vector3Int NormalMapSize => normalMapSize;

		#endregion GenerateMapSize
	}
}
