using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
	public abstract class UIButtonBase : MonoBehaviour
	{
		protected bool mPressed = false;
		public bool IsPressed => mPressed;

		public virtual void SetPressState(bool pressed)
		{
			if (mPressed != pressed)
			{
				mPressed = pressed;
			}
		}


		public Rect hitRect = Rect.zero;

		protected virtual Vector2 CurrentMouseHitPos(in Vector3 mousePos)
		{
			//return Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
			return mousePos - transform.position;
		}

		protected virtual bool IsHitInRect(in Vector3 mousePos)
		{
			var pos = CurrentMouseHitPos(mousePos);
			return hitRect.xMin <= pos.x && pos.x < hitRect.xMax
				&& hitRect.yMin <= pos.y && pos.y < hitRect.yMax;
		}


		public virtual bool UpdateMouse(in Vector3 mousePos)
		{
			if (mPressed)
			{
				if (!IsHitInRect(mousePos))
				{
					SetPressState(false);
					return false;
				}
				else if (Input.GetMouseButtonUp(0))
				{
					SetPressState(false);
					return true;
				}
			}
			else
			{
				if (Input.GetMouseButtonDown(0) && IsHitInRect(mousePos))
				{
					SetPressState(true);
					return false;
				}
			}
			return false;
		}


		public virtual void InitButton()
		{
			SetPressState(false);
		}
	}
}
