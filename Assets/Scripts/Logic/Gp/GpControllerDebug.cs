using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Gameplay
{
	[RequireComponent(typeof(GpMapController))]
	public class GpControllerDebug : MonoBehaviour
	{
		[SerializeField]
		protected GpMapController mainController;

		protected void Awake()
		{
			if (!mainController)
				mainController = GetComponent<GpMapController>();
		}


#if UNITY_EDITOR
		#region Debug

		public int randomSeed = 0;
		public bool fixedRandomSeed = false;

		protected void TestMakeMap(int width, int height, int mine)
		{
			GpMapGameLogic.Inst.ProcessClearAll(mainController.GameLogicContext);

			if (fixedRandomSeed)
				GpMapGenerator.Inst.ResetRandomSeed(randomSeed);
			else
				GpMapGenerator.Inst.ResetRandomSeed();
			randomSeed = GpMapGenerator.Inst.RandomSeed;
			Debug.LogFormat("RandomSeed : {0}", randomSeed);
			GpMapGameLogic.Inst.ProcessGenerateMap(mainController.GameLogicContext, width, height, mine);
		}

		[ContextMenu("Test Map 10x5 Mine 10")]
		public void Test2()
		{
			TestMakeMap(10, 5, 10);
		}

		[ContextMenu("Test Map 20x16 Mine 20")]
		public void Test3()
		{
			TestMakeMap(20, 16, 20);
		}

		[ContextMenu("Test Map 20x16 Mine 50")]
		public void Test4()
		{
			TestMakeMap(20, 16, 50);
		}

		[ContextMenu("Test Map 18x20 Mine 40")]
		public void Test5()
		{
			TestMakeMap(18, 20, 40);
		}


		[ContextMenu("Open All")]
		public void OpenAll()
		{
			var curTilemap = mainController.GameLogicContext.mainGpTilemap;
			foreach (var pos in curTilemap.EachMapPos())
				curTilemap.SetDataOpened(pos, true);

			var oriTilemap = mainController.GameLogicContext.originalGpTilemap;
			foreach (var pos in oriTilemap.EachMapPos())
				oriTilemap.SetDataOpened(pos, true);
		}


		[ContextMenu("Test Map 18x20 Mine 50 Open")]
		public void Test6()
		{
			TestMakeMap(18, 20, 50);
			OpenAll();
		}


		[ContextMenu("OpponentAI OneStep")]
		public void OpponentAIOneStep()
		{
			var random = GpMapGameLogic.Inst.MakeOpponentAIRandom(); // temp
			GpMapGameLogic.Inst.OpponentAIOneStep(mainController.GameLogicContext, mainController.BorderMoveContext, random);
		}

		#endregion Debug
#endif
	}
}
