using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
	public class UIHelp : MonoBehaviour, IFakeScene
	{
		#region Members

		[SerializeField]
		protected Sprite centerSprite;
		[SerializeField]
		protected Sprite leftSprite;
		[SerializeField]
		protected Sprite rightSprite;

		[SerializeField]
		protected SpriteRenderer spriteRenderer;

		[SerializeField]
		protected UIButton closeButton;

		[SerializeField]
		protected UIButton leftButton;
		[SerializeField]
		protected UIButton rightButton;

		[SerializeField]
		protected UIButton leftCloseButton;
		[SerializeField]
		protected UIButton rightCloseButton;

		#endregion Members


		public void Init()
		{
			closeButton.InitButton();
			leftButton.InitButton();
			rightButton.InitButton();
			leftCloseButton.InitButton();
			rightCloseButton.InitButton();

			CloseHelp();
		}


		#region ShowState

		public enum ShowState
		{
			None,
			Center,
			Left,
			Right,
		}

		protected ShowState mShowState = ShowState.None;

		public void SetShowState(ShowState showState)
		{
			mShowState = showState;
		}

		#endregion ShowState


		#region Update

		protected void Update()
		{
			if (mShowState == ShowState.Center)
				UpdateCenterState();
			else if (mShowState == ShowState.Left)
				UpdateLeftState();
			else if (mShowState == ShowState.Right)
				UpdateRightState();
		}

		protected Vector3 CurrentMousePos()
		{
			return Camera.main.ScreenToWorldPoint(Input.mousePosition);
		}

		protected void UpdateCenterState()
		{
			var pos = CurrentMousePos();
			if (closeButton.UpdateMouse(pos))
				CloseHelp();
			else if (leftButton.UpdateMouse(pos))
				OnOpenLeftHelp();
			else if (rightButton.UpdateMouse(pos))
				OnOpenRightHelp();
		}

		protected void UpdateLeftState()
		{
			var pos = CurrentMousePos();
			if (closeButton.UpdateMouse(pos))
				CloseHelp();
			else if (leftCloseButton.UpdateMouse(pos))
				OnCloseLeftHelp();
		}

		protected void UpdateRightState()
		{
			var pos = CurrentMousePos();
			if (closeButton.UpdateMouse(pos))
				CloseHelp();
			else if (rightCloseButton.UpdateMouse(pos))
				OnCloseRightHelp();
		}

		#endregion Update


		protected void SetSprite(Sprite sprite)
		{
			spriteRenderer.sprite = sprite;
		}

		protected void SetButtonActive(UIButton button, bool value)
		{
			button.gameObject.SetActive(value);
		}


		#region Events

		protected void OnCloseHelp()
		{
			gameObject.SetActive(false);
		}

		protected void OnOpenHelp()
		{
			gameObject.SetActive(true);
			OnOpenCenterHelp();
		}

		protected void OnOpenCenterHelp()
		{
			SetShowState(ShowState.Center);
			SetSprite(centerSprite);
			SetButtonActive(closeButton, true);
			SetButtonActive(leftButton, true);
			SetButtonActive(rightButton, true);
			SetButtonActive(leftCloseButton, false);
			SetButtonActive(rightCloseButton, false);
		}

		protected void OnOpenLeftHelp()
		{
			SetShowState(ShowState.Left);
			SetSprite(leftSprite);
			SetButtonActive(closeButton, true);
			SetButtonActive(leftButton, false);
			SetButtonActive(rightButton, false);
			SetButtonActive(leftCloseButton, true);
			SetButtonActive(rightCloseButton, false);
		}

		protected void OnOpenRightHelp()
		{
			SetShowState(ShowState.Right);
			SetSprite(rightSprite);
			SetButtonActive(closeButton, true);
			SetButtonActive(leftButton, false);
			SetButtonActive(rightButton, false);
			SetButtonActive(leftCloseButton, false);
			SetButtonActive(rightCloseButton, true);
		}

		protected void OnCloseLeftHelp()
		{
			OnOpenCenterHelp();
		}

		protected void OnCloseRightHelp()
		{
			OnOpenCenterHelp();
		}

		#endregion Events


		public void OpenHelpFromTitle()
		{
			SetCloseHelpMethod(GameMainLogic.Inst.DoBackTitle);
			OnOpenHelp();
		}
		public void OpenHelpFromMap()
		{
			SetCloseHelpMethod(GameMainLogic.Inst.DoBackGame);
			OnOpenHelp();
		}

		public void CloseHelp()
		{
			OnCloseHelp();
			DoCloseHelpMethod();
		}


		#region CloseHelpMethod

		protected delegate void CloseHelpDelegate();

		protected CloseHelpDelegate mCloseHelpMethod;

		protected void SetCloseHelpMethod(CloseHelpDelegate method)
		{
			mCloseHelpMethod = method;
		}

		protected void DoCloseHelpMethod()
		{
			if (mCloseHelpMethod != null)
				mCloseHelpMethod.Invoke();
		}

		#endregion CloseHelpMethod
	}
}
