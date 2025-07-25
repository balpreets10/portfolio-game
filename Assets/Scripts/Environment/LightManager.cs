using System;

using DG.Tweening;

using UnityEngine;

public class LightManager : MonoBehaviour
{
    public Light mainLight;

    public float defaultIntensity = 0.5f;

    public float highlightIntensity = 1.2f;

    private bool enteredGate = false;

    private void Start()
    {
        if (mainLight == null) mainLight = GetComponent<Light>();
        mainLight.intensity = defaultIntensity;
    }

    private void OnEnable()
    {
        Gate.OnGateExit += OnGateExit;
    }

    private void OnDisable()
    {
        Gate.OnGateExit -= OnGateExit;
    }

    private void OnGateExit()
    {
        if (!enteredGate)
        {
            enteredGate = true;
            mainLight.DOIntensity(highlightIntensity, 1.5f);
        }
    }
}