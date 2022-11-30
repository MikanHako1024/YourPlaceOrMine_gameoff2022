using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay
{
	public interface IFakeScene
	{
		public void Init();
	}

	public class GameMainLogic : BaseManagerMono<GameMainLogic>
	{
		protected void Awake()
		{
			OnInitMain();
		}

		protected void Start()
		{
			OnStartMain();
		}


		protected void OnInitMain()
		{
			InitInstManager(this);
		}

		protected override void InitMgrSuccess()
		{
			InitAllManager();
		}

		protected void InitAllManager()
		{
		}


		[SerializeField]
		protected GpMapController gpMapController;
		[SerializeField]
		protected UITitle uiTitle;
		[SerializeField]
		protected UIHelp uiHelp;

		[SerializeField]
		protected Vector3 gameCameraPos;

		protected Vector3 titleCameraPos;
		protected Vector3 helpCameraPos;

		protected void InitFakeScenePos()
		{
			titleCameraPos = uiTitle.transform.position;
			helpCameraPos = uiHelp.transform.position;
		}

		protected void InitAllScenes()
		{
			gpMapController.enabled = false;
			uiTitle.enabled = false;
			uiHelp.enabled = false;

			gpMapController.Init();
			uiTitle.Init();
			uiHelp.Init();
		}


		protected void OnStartMain()
		{
			InitFakeScenePos();
			InitAllScenes();

			GotoTitleScene();
		}


		protected void MoveCamera(Vector3 pos)
		{
			pos.z = Camera.main.transform.position.z;
			Camera.main.transform.position = pos;
		}


		#region SceneType

		protected enum SceneType
		{
			None,
			Title,
			Help,
			Map,
		}

		protected SceneType mCurScene = SceneType.None;

		protected void ExitCurScene()
		{
			if (mCurScene == SceneType.Help)
				OnHelpSceneExit();
			else if (mCurScene == SceneType.Title)
				OnTitleSceneExit();
			else if (mCurScene == SceneType.Map)
				OnMapSceneExit();
			mCurScene = SceneType.None;
		}

		public void GotoTitleScene()
		{
			ExitCurScene();
			mCurScene = SceneType.Title;
			OnTitleSceneEnter();
		}
		public void GotoHelpScene()
		{
			ExitCurScene();
			mCurScene = SceneType.Help;
			OnHelpSceneEnter();
		}
		public void GotoMapScene()
		{
			ExitCurScene();
			mCurScene = SceneType.Map;
			OnMapSceneEnter();
		}

		#endregion SceneType


		#region Scene Enter/Exit

		protected void OnTitleSceneEnter()
		{
			uiTitle.enabled = true;
			MoveCamera(titleCameraPos);
		}
		protected void OnTitleSceneExit()
		{
			uiTitle.enabled = false;
		}

		protected void OnHelpSceneEnter()
		{
			uiHelp.enabled = true;
			MoveCamera(helpCameraPos);
		}
		protected void OnHelpSceneExit()
		{
			uiHelp.enabled = false;
		}

		protected void OnMapSceneEnter()
		{
			gpMapController.enabled = true;
			MoveCamera(gameCameraPos);
		}
		protected void OnMapSceneExit()
		{
			gpMapController.enabled = false;
		}

		#endregion Scene Enter/Exit


		public void DoPlayGame()
		{
			GotoMapScene();
			GpMapGameLogic.Inst.ProcessStartNewGame(gpMapController.GameLogicContext);
		}

		public void DoOpenHelpFromTitle()
		{
			GotoHelpScene();
			uiHelp.OpenHelpFromTitle();
		}
		public void DoOpenHelpFromGame()
		{
			GotoHelpScene();
			uiHelp.OpenHelpFromMap();
		}

		public void DoBackTitle()
		{
			GotoTitleScene();
		}
		public void DoBackGame()
		{
			GotoMapScene();
		}
	}
}
