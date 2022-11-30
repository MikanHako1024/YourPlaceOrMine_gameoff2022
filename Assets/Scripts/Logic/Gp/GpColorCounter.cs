using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
	public class GpColorCounter
	{
		protected int mWhiteCount = 0;
		protected int mBlueCount = 0;
		protected int mRedCount = 0;

		public int WhiteCount => mWhiteCount;
		public int BlueCount => mBlueCount;
		public int RedCount => mRedCount;

		public void Clear()
		{
			mWhiteCount = 0;
			mBlueCount = 0;
			mRedCount = 0;
		}

		public void Init(int white, int blue, int red)
		{
			mWhiteCount = white;
			mBlueCount = blue;
			mRedCount = red;
		}

		public void SetWhite(int white)
		{
			mWhiteCount = white;
		}
		public void SetBlue(int blue)
		{
			mBlueCount = blue;
		}
		public void SetRed(int red)
		{
			mRedCount = red;
		}

		public void AddWhite()
		{
			++mWhiteCount;
		}
		public void AddBlue()
		{
			++mBlueCount;
		}
		public void AddRed()
		{
			++mRedCount;
		}

		public void DecWhite()
		{
			--mWhiteCount;
			if (mWhiteCount < 0)
				mWhiteCount = 0;
		}
		public void DecBlue()
		{
			--mBlueCount;
			if (mBlueCount < 0)
				mBlueCount = 0;
		}
		public void DecRed()
		{
			--mRedCount;
			if (mRedCount < 0)
				mRedCount = 0;
		}
	}
}
