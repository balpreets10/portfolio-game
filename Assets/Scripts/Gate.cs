using System;

using UnityEngine;

public class Gate : MonoBehaviour
{
    public static event Action OnGateExit;

    private bool entered = false;

    private void OnTriggerExit(Collider other)
    {
        OnGateExit?.Invoke();
    }
}