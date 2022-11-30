using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
	public class UICounter : MonoBehaviour
	{
		[SerializeField]
		protected List<Sprite> spriteList = new(12);

		protected Sprite GetEmptySprite()
		{
			return spriteList[11];
		}

		protected Sprite GetMinusSprite()
		{
			return spriteList[10];
		}

		protected Sprite GetNumberSprite(int number)
		{
			if (0 <= number && number <= 9)
				return spriteList[number];
			else
				return null;
		}

		protected Sprite GetEndSprite(int index)
		{
			if (0 <= index && index <= 2)
				return spriteList[12 + index];
			else
				return null;
		}


		[SerializeField]
		protected SpriteRenderer spriteRendererDigit1;
		[SerializeField]
		protected SpriteRenderer spriteRendererDigit2;
		[SerializeField]
		protected SpriteRenderer spriteRendererDigit3;

		public void RefreshSprite(int number, bool endFlag)
		{
			if (endFlag)
			{
				spriteRendererDigit1.sprite = GetEndSprite(0);
				spriteRendererDigit2.sprite = GetEndSprite(1);
				spriteRendererDigit3.sprite = GetEndSprite(2);
			}
			else
			{
				int num = number;
				//bool minus = num < 0;

				int digit3 = Mathf.Abs(num % 10);
				int digit2 = Mathf.Abs(num % 100) / 10;
				int digit1 = Mathf.Abs(num % 1000) / 100;

				spriteRendererDigit1.sprite = num < 0 ? GetMinusSprite() : GetNumberSprite(digit1);
				spriteRendererDigit2.sprite = GetNumberSprite(digit2);
				spriteRendererDigit3.sprite = GetNumberSprite(digit3);
			}
		}


		protected int mNumber = 0;
		protected bool mEndFlag = false;

		public void SetNumber(int number)
		{
			mNumber = number;
			mEndFlag = false;
			RefreshSprite(mNumber, mEndFlag);
		}

		public void SetEndFlag()
		{
			mEndFlag = true;
			RefreshSprite(mNumber, mEndFlag);
		}

		public void Clear()
		{
			SetNumber(0);
		}
	}
}
