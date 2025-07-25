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

        // Set initial visual state
        SetVisualState(false);

        // Hide initially
        ShowJoystick(false);

        isInitialized = true;
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
        }
    }

    public void SetJoystickPosition(Vector2 uiPosition)
    {
        if (joystickBackground != null)
        {
            joystickBackground.anchoredPosition = uiPosition;
            // Reset handle to center (0,0) relative to background
            if (joystickHandle != null)
                joystickHandle.anchoredPosition = Vector2.zero;
        }
    }

    public void UpdateJoystickVisual(Vector2 input)
    {
        if (joystickHandle == null) return;

        currentInput = input;

        // Since handle is child of background, position it relative to background's center (0,0)
        Vector2 handlePosition = input * joystickRange;
        joystickHandle.anchoredPosition = handlePosition;

        // Fire event
        OnJoystickMoved?.Invoke(input);
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
        //Color targetColor = active ? activeColor : inactiveColor;

        if (backgroundImage != null)
            backgroundImage.color = inactiveColor;
        if (handleImage != null)
            handleImage.color = activeColor;
    }
}

/*
using System;
using UnityEngine;
using Reflex.Attributes;

public class MobileMovementInputHandler : IPlatformMovementInputHandler
{
    public event Action<Vector2> OnMovementInput;

    [Header("Testing")]
    [SerializeField] private bool enableMouseEmulation = true;

    // Core state
    private bool isEnabled = false;

    private bool isInitialized = false;
    private Vector2 currentInput = Vector2.zero;

    // Touch tracking
    private int activeTouchId = -1;

    private bool isInputActive = false;
    private Vector2 inputStartPosition;

    // Settings
    private const float JOYSTICK_RANGE = 100f;

    private const float SCREEN_SPLIT_RATIO = 0.5f;

    [Inject] private IJoystick virtualJoystick;

    public void Initialize()
    {
        if (isInitialized) return;

        virtualJoystick?.InitializeJoystick();
        isInitialized = true;
    }

    public void UpdateInput()
    {
        if (!isEnabled || !isInitialized) return;

        if (enableMouseEmulation && Application.isEditor)
            ProcessMouseInput();
        else
            ProcessTouchInput();
    }

    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        if (!enabled)
        {
            StopInput();
            virtualJoystick?.ShowJoystick(false);
        }
    }

    public bool IsInputActive()
    {
        return isEnabled && isInitialized && isInputActive;
    }

    public void Cleanup()
    {
        StopInput();
        OnMovementInput = null;
        isEnabled = false;
        isInitialized = false;
    }

    #region Touch Input Processing

    private void ProcessTouchInput()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.touches[i];
            bool isLeftSide = IsLeftSideOfScreen(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (isLeftSide && activeTouchId == -1)
                        StartInput(touch.fingerId, touch.position);
                    break;

                case TouchPhase.Moved:
                    if (touch.fingerId == activeTouchId)
                    {
                        if (isLeftSide)
                            UpdateInput(touch.position);
                        else
                            StopInput();
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == activeTouchId)
                        StopInput();
                    break;
            }
        }
    }

    #endregion Touch Input Processing

    #region Mouse Emulation (Editor Only)

    private void ProcessMouseInput()
    {
        Vector2 mousePos = Input.mousePosition;
        bool isLeftSide = IsLeftSideOfScreen(mousePos);

        if (Input.GetMouseButtonDown(0) && isLeftSide && activeTouchId == -1)
        {
            StartInput(0, mousePos);
        }
        else if (Input.GetMouseButton(0) && activeTouchId == 0)
        {
            if (isLeftSide)
                UpdateInput(mousePos);
            else
                StopInput();
        }
        else if (Input.GetMouseButtonUp(0) && activeTouchId == 0)
        {
            StopInput();
        }
    }

    #endregion Mouse Emulation (Editor Only)

    #region Input Management

    private void StartInput(int touchId, Vector2 position)
    {
        activeTouchId = touchId;
        isInputActive = true;
        inputStartPosition = position;

        // Show joystick at touch position
        Vector2 uiPosition = ScreenToUIPosition(position);
        virtualJoystick?.SetJoystickPosition(uiPosition);
        virtualJoystick?.ShowJoystick(true);
    }

    private void UpdateInput(Vector2 currentPosition)
    {
        if (!isInputActive) return;

        // Calculate movement vector
        Vector2 delta = currentPosition - inputStartPosition;
        float distance = delta.magnitude;

        // Clamp to joystick range
        if (distance > JOYSTICK_RANGE)
        {
            delta = delta.normalized * JOYSTICK_RANGE;
            distance = JOYSTICK_RANGE;
        }

        // Convert to normalized input (-1 to 1)
        currentInput = delta / JOYSTICK_RANGE;

        // Update visuals and fire event
        virtualJoystick?.UpdateJoystickVisual(currentInput);
        OnMovementInput?.Invoke(currentInput);
    }

    private void StopInput()
    {
        if (!isInputActive) return;

        activeTouchId = -1;
        isInputActive = false;
        currentInput = Vector2.zero;
        Debug.Log("Stop Input");
        virtualJoystick?.UpdateJoystickVisual(Vector2.zero);
        virtualJoystick?.ShowJoystick(false);
        OnMovementInput?.Invoke(Vector2.zero);
    }

    private bool IsLeftSideOfScreen(Vector2 screenPosition)
    {
        return screenPosition.x < Screen.width * SCREEN_SPLIT_RATIO;
    }

    private Vector2 ScreenToUIPosition(Vector2 screenPosition)
    {
        Canvas canvas = virtualJoystick?.GetCanvas();
        Camera uiCamera = virtualJoystick?.GetCamera();

        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, uiCamera, out Vector2 localPosition))
            {
                return localPosition;
            }
        }

        return screenPosition;
    }

    #endregion Input Management
}

/* Optimized

using System;
using UnityEngine;
using Reflex.Attributes;

namespace Portfolio.InputSystem
{
    // Enums for better state management
    public enum InitializationState
    {
        Uninitialized,
        Initialized
    }

    public enum EnabledState
    {
        Disabled,
        Enabled
    }

    public enum TouchState
    {
        None,
        Tracking,
        Invalid
    }

    public enum ScreenRegion
    {
        Left,
        Right,
        Invalid
    }

    public class MobileMovementInputHandler : IPlatformMovementInputHandler
    {
        public event Action<Vector2> OnMovementInput;

        [Header("Testing")]
        [SerializeField] private bool enableMouseEmulation = true;

        // Core state using enums
        private InitializationState initializationState = InitializationState.Uninitialized;

        private EnabledState enabledState = EnabledState.Disabled;
        private TouchState touchState = TouchState.None;

        private Vector2 currentInput = Vector2.zero;
        private int activeTouchId = -1;
        private Vector2 inputStartPosition;

        // Settings
        private const float JOYSTICK_RANGE = 100f;

        private const float SCREEN_SPLIT_RATIO = 0.5f;

        [Inject] private IJoystick virtualJoystick;

        public void Initialize()
        {
            if (initializationState == InitializationState.Initialized) return;

            virtualJoystick?.InitializeJoystick();
            initializationState = InitializationState.Initialized;
        }

        public void UpdateInput()
        {
            if (enabledState == EnabledState.Disabled || initializationState == InitializationState.Uninitialized)
                return;

            if (enableMouseEmulation && Application.isEditor)
                ProcessMouseInput();
            else
                ProcessTouchInput();
        }

        public void SetEnabled(bool enabled)
        {
            enabledState = enabled ? EnabledState.Enabled : EnabledState.Disabled;

            if (enabledState == EnabledState.Disabled)
            {
                StopInput();
                virtualJoystick?.ShowJoystick(false);
            }
        }

        public bool IsInputActive()
        {
            return enabledState == EnabledState.Enabled &&
                   initializationState == InitializationState.Initialized &&
                   touchState == TouchState.Tracking;
        }

        public void Cleanup()
        {
            StopInput();
            OnMovementInput = null;
            enabledState = EnabledState.Disabled;
            initializationState = InitializationState.Uninitialized;
        }

        #region Touch Input Processing

        private void ProcessTouchInput()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.touches[i];
                ScreenRegion region = GetScreenRegion(touch.position);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        if (region == ScreenRegion.Left && touchState == TouchState.None)
                            StartInput(touch.fingerId, touch.position);
                        break;

                    case TouchPhase.Moved:
                        if (touch.fingerId == activeTouchId)
                        {
                            if (region == ScreenRegion.Left)
                                UpdateInput(touch.position);
                            else
                                StopInput();
                        }
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (touch.fingerId == activeTouchId)
                            StopInput();
                        break;
                }
            }
        }

        #endregion Touch Input Processing

        #region Mouse Emulation (Editor Only)

        private void ProcessMouseInput()
        {
            Vector2 mousePos = Input.mousePosition;
            ScreenRegion region = GetScreenRegion(mousePos);

            if (Input.GetMouseButtonDown(0) && region == ScreenRegion.Left && touchState == TouchState.None)
            {
                StartInput(0, mousePos);
            }
            else if (Input.GetMouseButton(0) && touchState == TouchState.Tracking)
            {
                if (region == ScreenRegion.Left)
                    UpdateInput(mousePos);
                else
                    StopInput();
            }
            else if (Input.GetMouseButtonUp(0) && touchState == TouchState.Tracking)
            {
                StopInput();
            }
        }

        #endregion Mouse Emulation (Editor Only)

        #region Input Management

        private void StartInput(int touchId, Vector2 position)
        {
            activeTouchId = touchId;
            touchState = TouchState.Tracking;
            inputStartPosition = position;

            // Show joystick at touch position
            Vector2 uiPosition = ScreenToUIPosition(position);
            virtualJoystick?.SetJoystickPosition(uiPosition);
            virtualJoystick?.ShowJoystick(true);
        }

        private void UpdateInput(Vector2 currentPosition)
        {
            if (touchState != TouchState.Tracking) return;

            // Calculate movement vector
            Vector2 delta = currentPosition - inputStartPosition;
            float distance = delta.magnitude;

            // Clamp to joystick range
            if (distance > JOYSTICK_RANGE)
            {
                delta = delta.normalized * JOYSTICK_RANGE;
                distance = JOYSTICK_RANGE;
            }

            // Convert to normalized input (-1 to 1)
            currentInput = delta / JOYSTICK_RANGE;

            // Update visuals and fire event
            virtualJoystick?.UpdateJoystickVisual(currentInput);
            OnMovementInput?.Invoke(currentInput);
        }

        private void StopInput()
        {
            if (touchState != TouchState.Tracking) return;

            activeTouchId = -1;
            touchState = TouchState.None;
            currentInput = Vector2.zero;
            Debug.Log("Stop Input");
            virtualJoystick?.UpdateJoystickVisual(Vector2.zero);
            virtualJoystick?.ShowJoystick(false);
            OnMovementInput?.Invoke(Vector2.zero);
        }

        private ScreenRegion GetScreenRegion(Vector2 screenPosition)
        {
            if (screenPosition.x < 0 || screenPosition.x > Screen.width ||
                screenPosition.y < 0 || screenPosition.y > Screen.height)
                return ScreenRegion.Invalid;

            return screenPosition.x < Screen.width * SCREEN_SPLIT_RATIO ?
                   ScreenRegion.Left : ScreenRegion.Right;
        }

        private Vector2 ScreenToUIPosition(Vector2 screenPosition)
        {
            Canvas canvas = virtualJoystick?.GetCanvas();
            Camera uiCamera = virtualJoystick?.GetCamera();

            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPosition, uiCamera, out Vector2 localPosition))
                {
                    return localPosition;
                }
            }

            return screenPosition;
        }

        #endregion Input Management
    }
}

*/