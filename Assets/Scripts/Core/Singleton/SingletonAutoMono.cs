using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
public class SingletonAutoMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T GetInstance()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject();
            obj.name = typeof(T).ToString();
            DontDestroyOnLoad(obj); // 过场不摧毁
            instance = obj.AddComponent<T>();
        }
        return instance;
    }

    public static T Instance => GetInstance();
}
*/
