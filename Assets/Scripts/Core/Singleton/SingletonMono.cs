using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T GetInstance()
    {
        if (instance == null)
            Debug.LogWarningFormat("No found instance of class \"{0}\"", typeof(T));
        return instance;
    }

    public static T Instance => GetInstance();

    private void Awake()
    {
        instance = this as T;
    }
}
*/
