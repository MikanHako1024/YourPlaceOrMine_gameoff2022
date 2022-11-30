using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
	public class UIFaceButton : UIButtonBase
	{
		[SerializeField]
		protected SpriteRenderer faceSpriteRenderer;
		[SerializeField]
		protected SpriteRenderer bgSpriteRenderer;


		public override void SetPressState(bool pressed)
		{
			if (mPressed != pressed)
			{
				base.SetPressState(pressed);
				RefreshSprite();
			}
			else
			{
				base.SetPressState(pressed);
			}
		}


		public void RefreshSprite()
		{
			faceSpriteRenderer.sprite = GetFaceSprite(mFaceType, mPressed);
			bgSpriteRenderer.sprite = GetBgSprite(mFaceColor, mPressed);
		}


		#region GetSprite

		public List<Sprite> faceSpriteList = new(6);
		public List<Sprite> bgSpriteList = new(6);

		protected Sprite GetFaceSprite(FaceType faceType, bool holded)
		{
			if (faceType == FaceType.Normal)
				return faceSpriteList[holded ? 4 : 0];
			else if (faceType == FaceType.Press)
				return faceSpriteList[holded ? 5 : 1];
			else if (faceType == FaceType.Win)
				return faceSpriteList[holded ? 6 : 2];
			else if (faceType == FaceType.Lose)
				return faceSpriteList[holded ? 7 : 3];
			else
				return null;
		}

		protected Sprite GetBgSprite(FaceColor faceColor, bool holded)
		{
			if (faceColor == FaceColor.White)
				return bgSpriteList[holded ? 3 : 0];
			else if (faceColor == FaceColor.Blue)
				return bgSpriteList[holded ? 4 : 1];
			else if (faceColor == FaceColor.Red)
				return bgSpriteList[holded ? 5 : 2];
			else
				return null;
		}

		#endregion GetSprite


		#region Members

		protected enum FaceType
		{
			None,
			Normal,
			Press,
			Win,
			Lose,
		}

		protected FaceType mFaceType = FaceType.Normal;

		protected enum FaceColor
		{
			White,
			Blue,
			Red,
		}

		protected FaceColor mFaceColor = FaceColor.White;

		#endregion Members


		#region SetState

		public void SetVisible(bool value)
		{
			faceSpriteRenderer.enabled = value;
			bgSpriteRenderer.enabled = value;
		}


		public void SetFaceNormal()
		{
			mFaceType = FaceType.Normal;
			RefreshSprite();
		}
		public void SetFacePress()
		{
			mFaceType = FaceType.Press;
			RefreshSprite();
		}
		public void SetFaceWin()
		{
			mFaceType = FaceType.Win;
			RefreshSprite();
		}
		public void SetFaceLose()
		{
			mFaceType = FaceType.Lose;
			RefreshSprite();
		}

		public void SetColorWhite()
		{
			mFaceColor = FaceColor.White;
			RefreshSprite();
		}
		public void SetColorBlue()
		{
			mFaceColor = FaceColor.Blue;
			RefreshSprite();
		}
		public void SetColorRed()
		{
			mFaceColor = FaceColor.Red;
			RefreshSprite();
		}

		public void SetHolded(bool value)
		{
			mPressed = value;
			RefreshSprite();
		}

		#endregion SetState
	}
}
