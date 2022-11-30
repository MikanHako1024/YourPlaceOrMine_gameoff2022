using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mono单例基类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseManagerMono<T> : MonoBehaviour where T : MonoBehaviour, new()
{
	private static T instance;

	private static T CreateInstance()
	{
		GameObject obj = new GameObject();
		obj.name = typeof(T).ToString();
		DontDestroyOnLoad(obj); // 过场不摧毁
		return obj.AddComponent<T>();
	}

	private static T GetInstance()
	{
		if (instance == null)
			instance = CreateInstance();
		return instance;
	}

	public static T Inst => GetInstance();
	//public static T Instance => GetInstance();


	private static bool HasInst()
	{
		return instance != null;
	}

	//private void RegisterInst(T obj)
	private static void RegisterInst(T obj)
	{
		instance = obj;
		DontDestroyOnLoad(obj); // 过场不摧毁
	}

	//private void RegisterInst(string name)
	//private static bool RegisterInst(string name)
	private static T RegisterInst(string name)
	{
		var obj = GameObject.Find(name)?.GetComponent<T>();
		if (obj)
			RegisterInst(obj);
		//return obj != null;
		return obj;
	}

	/*public bool TryRegisterThis(T obj)
	{
		if (HasInst())
		{
			gameObject.SetActive(false);
			return false;
		}
		else
		{
			RegisterInst(obj);
			return true;
		}
	}*/
	//public bool TryRegisterThis()
	private bool TryRegisterThis()
	{
		if (HasInst())
		{
			//gameObject.SetActive(false);
			return false;
		}
		else if (this is T)
		{
			RegisterInst(this as T);
			return true;
		}
		else
		{
			//gameObject.SetActive(false);
			return false;
		}
	}


	/*public virtual void InitManager()
	{
	}*/

	/*protected void Awake()
	{
		if (TryRegisterThis())
			AwakeSuccess();
		else
			AwakeFailed();
	}*/

	/*public virtual void InitManager()
	{
		if (TryRegisterThis())
			AwakeSuccess();
		else
			AwakeFailed();
	}*/

	//public static void InitManager()
	/*public static void InitInstManager()
	{
		if (!HasInst())
		{
			var obj = CreateInstance();
			//(obj as BaseManagerMono<T>).InitManager();
			(obj as BaseManagerMono<T>).InitManagerThis();
		}
	}*/

	public static void InitInstManager(string name)
	{
		if (!HasInst())
		{
			var obj = (name?.Length > 0)
				? (RegisterInst(name) ?? CreateInstance())
				: CreateInstance();
			//(obj as BaseManagerMono<T>).InitManager();
			(obj as BaseManagerMono<T>).InitManagerThis();
			Debug.Log("Init BaseManagerMono : " + typeof(T).ToString());
		}
	}
	public static void InitInstManager(T obj)
	{
		if (!HasInst())
		{
			(obj as BaseManagerMono<T>).InitManagerThis();
			Debug.Log("Init BaseManagerMono : " + typeof(T).ToString());
		}
	}

	//public void InitManagerThis()
	private void InitManagerThis()
	{
		if (TryRegisterThis())
			//AwakeSuccess();
			InitMgrSuccess();
		else
			//AwakeFailed();
			InitMgrFailed();
	}


	/*public abstract void AwakeSuccess();

	public virtual void AwakeFailed()
	{
		gameObject.SetActive(false);
	}*/
	protected abstract void InitMgrSuccess();

	protected virtual void InitMgrFailed()
	{
		gameObject.SetActive(false);
	}
}
