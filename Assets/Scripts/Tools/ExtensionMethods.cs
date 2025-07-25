using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using UnityEngine;

public static class ExtensionMethods
{
    public static void DestroyChildren(this Transform t)
    {
        bool isPlaying = Application.isPlaying;

        while (t.childCount != 0)
        {
            Transform child = t.GetChild(0);

            if (isPlaying)
            {
                child.SetParent(null);
                UnityEngine.Object.Destroy(child.gameObject);
            }
            else UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    public static void DeactivateChildren(this Transform t)
    {
        bool isPlaying = Application.isPlaying;

        for (int i = 0; i < t.childCount; i++)
        {
            GameObject go = t.GetChild(i).gameObject;

            if (isPlaying)
            {
                go.SetActive(false);
            }
        }
    }

    public static List<Transform> GetChildren(this Transform t)
    {
        List<Transform> children = new List<Transform>();
        bool isPlaying = Application.isPlaying;

        for (int i = 0; i < t.childCount; i++)
        {
            children.Add(t.GetChild(i));
        }
        return children;
    }

    public static void SetActive(this Transform obj, bool value)
    {
        obj.gameObject.SetActive(value);
    }

    public static void SetActive(this MonoBehaviour obj, bool value)
    {
        obj.gameObject.SetActive(value);
    }

    public static void SetParent(this Transform transform, Transform parent, Vector3 localPosition, Vector3 localScale, bool worldPositionStays)
    {
        transform.SetParent(parent, worldPositionStays);
        transform.localPosition = localPosition;
        transform.localScale = localScale;
    }

    //
    public static bool IsNullOrEmpty<T>(this T[] array) where T : class
    {
        if (array == null || array.Length == 0)
            return true;
        else
            return array.All(item => item == null);
    }

    public static bool Contains<T>(this T[] array, T element)
    {
        for (int i = array.Length - 1; i != -1; --i)
        {
            if (element.Equals(array[i]))
                return true;
        }
        return false;
    }

    public static Transform GetSiblingAtIndex(this Transform t, int index)
    {
        return t.parent.GetChild(index);
    }

    //Task Extensions
    public static System.Runtime.CompilerServices.TaskAwaiter GetAwaiter(this System.TimeSpan timeSpan)
    {
        return System.Threading.Tasks.Task.Delay(timeSpan).GetAwaiter();
    }

    public static async void WrapErrors(this Task task)
    {
        await task;
    }

    public static IEnumerator AsIEnumerator(this Task task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted)
        {
            throw task.Exception;
        }
    }

    public static bool IsTouchSupported()
    {
        // Check if we're on a touch platform AND have touch support
        RuntimePlatform platform = Application.platform;
        // If it's WebGL, be more restrictive
        if (platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.Log("platform: " + Application.platform + ", Touch supported: " + Input.touchSupported);
            // Only return true if we can detect actual touch input
            return Input.touchCount > 0 || HasRecentTouchInput() || IsTouchSupportedJS();
        }

        bool isTouchPlatform = platform == RuntimePlatform.Android ||
                              platform == RuntimePlatform.IPhonePlayer ||
                              Application.isMobilePlatform;
        Debug.Log("platform: " + Application.platform + ", Touch supported: " + Input.touchSupported);
        return isTouchPlatform || Input.touchSupported;
    }

    private static bool HasRecentTouchInput()
    {
        // Check if there has been any touch input recently
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase != TouchPhase.Canceled)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the current platform is desktop
    /// </summary>
    /// <returns>True if desktop platform, false otherwise</returns>
    public static bool IsDesktopPlatform()
    {
        return Application.platform == RuntimePlatform.WindowsPlayer ||
               Application.platform == RuntimePlatform.OSXPlayer ||
               Application.platform == RuntimePlatform.LinuxPlayer ||
               Application.platform == RuntimePlatform.WindowsEditor ||
               Application.platform == RuntimePlatform.OSXEditor ||
               Application.platform == RuntimePlatform.LinuxEditor;
    }

    /// <summary>
    /// Gets the appropriate input sensitivity based on platform
    /// </summary>
    /// <param name="mouseSensitivity">Mouse sensitivity for desktop</param>
    /// <param name="touchSensitivity">Touch sensitivity for mobile</param>
    /// <returns>Appropriate sensitivity value</returns>
    public static float GetPlatformSensitivity(float mouseSensitivity, float touchSensitivity)
    {
        return IsTouchSupported() ? touchSensitivity : mouseSensitivity;
    }

    /// <summary>
    /// Converts screen position to normalized viewport position
    /// </summary>
    /// <param name="screenPosition">Screen position in pixels</param>
    /// <returns>Normalized viewport position (0-1)</returns>
    public static Vector2 ScreenToViewportPoint(Vector2 screenPosition)
    {
        return new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
    }

    /// <summary>
    /// Checks if a screen position is within a specific area of the screen
    /// </summary>
    /// <param name="screenPosition">Screen position to check</param>
    /// <param name="areaMin">Minimum normalized viewport coordinates</param>
    /// <param name="areaMax">Maximum normalized viewport coordinates</param>
    /// <returns>True if position is within area</returns>
    public static bool IsInScreenArea(Vector2 screenPosition, Vector2 areaMin, Vector2 areaMax)
    {
        Vector2 viewportPos = ScreenToViewportPoint(screenPosition);
        return viewportPos.x >= areaMin.x && viewportPos.x <= areaMax.x &&
               viewportPos.y >= areaMin.y && viewportPos.y <= areaMax.y;
    }

    // Helper method to check if we're in editor and simulating touch
    public static bool IsEditorTouchSimulation()
    {
        return Application.isEditor && Input.touchSupported;
    }

    [DllImport("__Internal")]
    private static extern bool IsTouchSupportedJS();
}