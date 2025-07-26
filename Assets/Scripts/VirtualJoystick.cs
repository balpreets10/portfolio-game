using System;

using UnityEngine;
using UnityEngine.UI;

public interface IJoystick
{
    void InitializeJoystick();

    void ShowJoystick(bool show);

    void SetJoystickPosition(Vector2 uiPosition);

    void UpdateJoystickVisual(Vector2 input);

    Canvas GetCanvas();

    Camera GetCamera();

    Action<Vector2> OnJoystickMoved { get; set; }
}

public class VirtualJoystick : MonoBehaviour, IJoystick
{
    [Header("References")]
    [SerializeField] private Canvas canvas;

    [SerializeField] private Camera uiCamera;
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;

    [Header("Settings")]
    [SerializeField] private float joystickRange = 100f;

    [SerializeField] private float handleReturnSpeed = 10f;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // Events
    public Action<Vector2> OnJoystickMoved { get; set; }

    // Internal state
    private bool isInitialized = false;

    private bool isActive = false;
    private Vector2 currentInput = Vector2.zero;

    // Components
    private Image backgroundImage;

    private Image handleImage;

    private void Awake()
    {
        ShowJoystick(false);
    }

    public void InitializeJoystick()
    {
        if (isInitialized) return;

        // Get canvas if not assigned
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("VirtualJoystick must be a child of a Canvas!");
            return;
        }

        // Set UI camera based on canvas render mode
        if (uiCamera == null)
        {
            uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        // Get image components
        backgroundImage = joystickBackground?.GetComponent<Image>();
        handleImage = joystickHandle?.GetComponent<Image>();

        // Ensure proper hierarchy setup
        if (joystickHandle != null && joystickBackground != null)
        {
            // Make sure handle is child of background for proper relative positioning
            if (joystickHandle.parent != joystickBackground)
            {
                joystickHandle.SetParent(joystickBackground, false);
            }
        }

        // Set initial visual state
        SetVisualState(false);

        // Hide initially
        ShowJoystick(false);

        isInitialized = true;

        if (enableDebugLogs)
            Debug.Log("VirtualJoystick initialized successfully");
    }

    private void Update()
    {
        if (!isInitialized || !isActive) return;

        // Smooth handle return to center when not being dragged
        if (joystickHandle != null && currentInput == Vector2.zero)
        {
            Vector2 targetPosition = Vector2.zero; // Handle centers at (0,0) relative to background
            if (Vector2.Distance(joystickHandle.anchoredPosition, targetPosition) > 0.1f)
            {
                joystickHandle.anchoredPosition = Vector2.Lerp(
                    joystickHandle.anchoredPosition,
                    targetPosition,
                    handleReturnSpeed * Time.deltaTime
                );
            }
        }
    }

    public void ShowJoystick(bool show)
    {
        if (joystickBackground != null)
        {
            joystickBackground.gameObject.SetActive(show);
            isActive = show;
            SetVisualState(show);

            if (enableDebugLogs)
                Debug.Log($"Joystick visibility: {show}");
        }
    }

    public void SetJoystickPosition(Vector2 uiPosition)
    {
        if (joystickBackground == null) return;

        // Set the background position directly
        joystickBackground.anchoredPosition = uiPosition;

        // Reset handle to center relative to background
        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition = Vector2.zero;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"Joystick positioned at UI coordinates: {uiPosition}");
            Debug.Log($"Background world position: {joystickBackground.position}");
        }
    }

    public void UpdateJoystickVisual(Vector2 input)
    {
        if (joystickHandle == null) return;

        currentInput = input;

        // Position handle relative to background center
        Vector2 handlePosition = input * joystickRange;

        // Clamp to joystick range (safety check)
        if (handlePosition.magnitude > joystickRange)
        {
            handlePosition = handlePosition.normalized * joystickRange;
        }

        joystickHandle.anchoredPosition = handlePosition;

        // Fire event
        OnJoystickMoved?.Invoke(input);

        if (enableDebugLogs && input != Vector2.zero)
        {
            Debug.Log($"Joystick input: {input}, Handle position: {handlePosition}");
        }
    }

    public Canvas GetCanvas()
    {
        return canvas;
    }

    public Camera GetCamera()
    {
        return uiCamera;
    }

    private void SetVisualState(bool active)
    {
        Color targetBackgroundColor = active ? inactiveColor : inactiveColor;
        Color targetHandleColor = active ? activeColor : activeColor;

        if (backgroundImage != null)
            backgroundImage.color = targetBackgroundColor;
        if (handleImage != null)
            handleImage.color = targetHandleColor;
    }

    // Helper method to convert screen position to UI position for debugging
    public Vector2 ConvertScreenToUIPosition(Vector2 screenPosition)
    {
        if (canvas == null) return screenPosition;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // For ScreenSpaceOverlay, use direct conversion
            Vector2 canvasPosition = new Vector2(
                screenPosition.x - Screen.width * 0.5f,
                screenPosition.y - Screen.height * 0.5f
            );

            if (enableDebugLogs)
            {
                Debug.Log($"Screen: {screenPosition} -> Canvas: {canvasPosition}");
            }

            return canvasPosition;
        }
        else
        {
            // For camera-based canvases
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, uiCamera, out Vector2 localPosition))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"Screen: {screenPosition} -> Local: {localPosition}");
                }
                return localPosition;
            }
        }

        return screenPosition;
    }

    // Validation method to check setup
    [ContextMenu("Validate Setup")]
    public void ValidateSetup()
    {
        if (canvas == null)
        {
            Debug.LogError("Canvas reference is missing!");
            return;
        }

        if (joystickBackground == null)
        {
            Debug.LogError("Joystick background reference is missing!");
            return;
        }

        if (joystickHandle == null)
        {
            Debug.LogError("Joystick handle reference is missing!");
            return;
        }

        if (joystickHandle.parent != joystickBackground)
        {
            Debug.LogWarning("Joystick handle should be a child of joystick background for proper positioning!");
        }

        Debug.Log("VirtualJoystick setup validation complete - all references are valid!");
    }
}