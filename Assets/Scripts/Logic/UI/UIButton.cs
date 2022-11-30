using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
	[RequireComponent(typeof(SpriteRenderer))]
	public class UIButton : UIButtonBase
	{
		protected SpriteRenderer mSpriteRenderer;

		protected SpriteRenderer TouchSpriteRenderer()
		{
			if (!mSpriteRenderer)
				mSpriteRenderer = GetComponent<SpriteRenderer>();
			return mSpriteRenderer;
		}


		public Sprite normalSprite;
		public Sprite pressedSprite;

		protected virtual Sprite GetSprite(bool pressed)
		{
			return pressed ? pressedSprite : normalSprite;
		}


		public override void SetPressState(bool pressed)
		{
			if (mPressed != pressed)
			{
				TouchSpriteRenderer().sprite = GetSprite(pressed);
			}
			base.SetPressState(pressed);
		}

		public override void InitButton()
		{
			TouchSpriteRenderer();
			base.InitButton();
		}
	}
}
