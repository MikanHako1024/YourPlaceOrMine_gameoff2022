using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay
{
	public class UITitle : MonoBehaviour, IFakeScene
	{
		public UIHelp uiHelp;

		public UIButton uiPlayButton;
		public UIButton uiHelpButton;

		public void Init()
		{
			uiPlayButton.InitButton();
			uiHelpButton.InitButton();
		}


		protected Vector3 CurrentMousePos()
		{
			return Camera.main.ScreenToWorldPoint(Input.mousePosition);
		}

		protected void Update()
		{
			var mousePos = CurrentMousePos();
			if (uiPlayButton.UpdateMouse(mousePos))
			{
				OnPlayButtonClick();
			}
			if (uiHelpButton.UpdateMouse(mousePos))
			{
				OnHelpButtonClick();
			}
		}


		protected void OnPlayButtonClick()
		{
			GameMainLogic.Inst.DoPlayGame();
		}

		protected void OnHelpButtonClick()
		{
			GameMainLogic.Inst.DoOpenHelpFromTitle();
		}
	}
}
