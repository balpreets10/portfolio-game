using System;

using TMPro;

using UnityEngine;

public class Gate : MonoBehaviour
{
    public TypeWriterEffect typeWriterEffect;

    public static event Action OnGateExit;

    private void OnEnable()
    {
        SplashScreen.OnLoadingComplete += OnLoadingComplete;
    }

    private void OnDisable()
    {
        SplashScreen.OnLoadingComplete += OnLoadingComplete;
    }

    private void OnLoadingComplete()
    {
        typeWriterEffect.StartTypewriter();
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Invoking");
        OnGateExit?.Invoke();
    }
}