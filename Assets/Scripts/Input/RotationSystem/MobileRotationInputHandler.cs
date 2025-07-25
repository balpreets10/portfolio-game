/*
using System;

using Portfolio.CameraSystem;

using Reflex.Attributes;

using UnityEngine;

namespace Portfolio.InputSystem
{
    /// <summary>
    /// Mobile platform input handler for touch-based camera rotation
    /// Right half of screen handles camera rotation, left half is reserved for movement
    /// In editor: Left mouse button simulates touch, Right mouse button + Left mouse simulates pinch zoom
    /// </summary>
    public class MobileRotationInputHandler : IPlatformRotationInputHandler
    {
        public event Action<Vector2> OnRotationInput;

        public bool IsRotationActive { get; private set; }

        private bool isEnabled = false;
        private MobileCameraSettings settings;
        private Vector2 lastTouchPosition;
        private bool isTouchActive = false;
        private int rotationTouchId = -1; // Track which touch is handling rotation

        // Editor simulation variables
        private bool isMouseDown = false;

        private Vector2 lastMousePosition;
        private float lastPinchDistance = 0f;
        private bool isPinchMode = false;

        private const float SCREEN_SPLIT_RATIO = 0.5f;

        [Inject] private IActionButton actionButton;

        public void Initialize()
        {
            isTouchActive = false;
            IsRotationActive = false;
            rotationTouchId = -1;

            // Editor simulation initialization
            isMouseDown = false;
            isPinchMode = false;
        }

        public void UpdateInput()
        {
            if (!isEnabled || settings == null) return;

#if UNITY_EDITOR
            HandleEditorInput();
#else
            HandleTouchRotation();
            HandlePinchZoom();
#endif
        }

#if UNITY_EDITOR

        private void HandleEditorInput()
        {
            // Right mouse button enables pinch zoom mode
            if (Input.GetMouseButtonDown(1))
            {
                isPinchMode = true;
                lastPinchDistance = 100f; // Default distance for simulation
            }
            else if (Input.GetMouseButtonUp(1))
            {
                isPinchMode = false;
            }

            if (isPinchMode)
            {
                HandleEditorPinchZoom();
            }
            else
            {
                HandleEditorTouchRotation();
            }
        }

        private void HandleEditorTouchRotation()
        {
            Vector2 mousePos = Input.mousePosition;
            bool isRightSide = IsRightSideOfScreen(mousePos);

            if (Input.GetMouseButtonDown(0) && isRightSide && !actionButton.IsInButtonArea(mousePos))
            {
                lastMousePosition = mousePos;
                isMouseDown = true;
                isTouchActive = true;
            }
            else if (Input.GetMouseButton(0) && isMouseDown)
            {
                Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
                lastMousePosition = Input.mousePosition;

                // Apply deadzone
                if (mouseDelta.magnitude > settings.TouchDeadzone)
                {
                    Vector2 rotationInput = mouseDelta * settings.Sensitivity * 0.01f;

                    // Invert Y axis if needed
                    if (settings.InvertYAxis)
                        rotationInput.y = -rotationInput.y;

                    OnRotationInput?.Invoke(rotationInput);
                    IsRotationActive = true;
                }
                else
                {
                    IsRotationActive = false;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isMouseDown = false;
                isTouchActive = false;
                IsRotationActive = false;
            }
            else
            {
                isMouseDown = false;
                isTouchActive = false;
                IsRotationActive = false;
            }
        }

        private void HandleEditorPinchZoom()
        {
            if (Input.GetMouseButton(0) && settings.EnablePinchZoom)
            {
                // Use mouse wheel or vertical mouse movement for pinch simulation
                float scrollDelta = Input.GetAxis("Mouse ScrollWheel");

                if (Mathf.Abs(scrollDelta) > 0.01f)
                {
                    float pinchDelta = scrollDelta * settings.PinchSensitivity * 10f; // Scale for mouse wheel
                    Vector2 zoomInput = new Vector2(0, pinchDelta);
                    OnRotationInput?.Invoke(zoomInput);
                }
                else
                {
                    // Alternative: use mouse Y movement while in pinch mode
                    Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
                    lastMousePosition = Input.mousePosition;

                    if (Mathf.Abs(mouseDelta.y) > 1f)
                    {
                        float pinchDelta = mouseDelta.y * settings.PinchSensitivity * 0.01f;
                        Vector2 zoomInput = new Vector2(0, pinchDelta);
                        OnRotationInput?.Invoke(zoomInput);
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                lastMousePosition = Input.mousePosition;
            }
        }

#endif

        private void HandleTouchRotation()
        {
            // Process all touches to find rotation touch
            for (int i = 0; i < Input.touchCount; i++)
            {
                UnityEngine.Touch touch = Input.touches[i];
                bool isRightSide = IsRightSideOfScreen(touch.position);

                if (touch.phase == TouchPhase.Began && isRightSide && rotationTouchId == -1 && !actionButton.IsInButtonArea(touch.position))
                {
                    // Start rotation with this touch
                    rotationTouchId = touch.fingerId;
                    lastTouchPosition = touch.position;
                    isTouchActive = true;
                }
                else if (touch.fingerId == rotationTouchId)
                {
                    // Handle existing rotation touch
                    if (touch.phase == TouchPhase.Moved && isTouchActive)
                    {
                        Vector2 touchDelta = touch.position - lastTouchPosition;
                        lastTouchPosition = touch.position;

                        // Apply deadzone
                        if (touchDelta.magnitude > settings.TouchDeadzone)
                        {
                            Vector2 rotationInput = touchDelta * settings.Sensitivity * 0.01f; // Scale for touch

                            // Invert Y axis if needed
                            if (settings.InvertYAxis)
                                rotationInput.y = -rotationInput.y;

                            OnRotationInput?.Invoke(rotationInput);
                            IsRotationActive = true;
                        }
                        else
                        {
                            IsRotationActive = false;
                        }
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        // End rotation
                        rotationTouchId = -1;
                        isTouchActive = false;
                        IsRotationActive = false;
                    }
                }
            }

            // If rotation touch is no longer present, reset
            if (rotationTouchId != -1)
            {
                bool touchStillExists = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.touches[i].fingerId == rotationTouchId)
                    {
                        touchStillExists = true;
                        break;
                    }
                }

                if (!touchStillExists)
                {
                    rotationTouchId = -1;
                    isTouchActive = false;
                    IsRotationActive = false;
                }
            }
        }

        private void HandlePinchZoom()
        {
            if (Input.touchCount == 2 && settings.EnablePinchZoom)
            {
                UnityEngine.Touch touch1 = Input.GetTouch(0);
                UnityEngine.Touch touch2 = Input.GetTouch(1);

                // Check if at least one touch is on the right side
                bool hasRightSideTouch = IsRightSideOfScreen(touch1.position) || IsRightSideOfScreen(touch2.position);

                if (!hasRightSideTouch) return;

                // Get current distance
                float currentDistance = Vector2.Distance(touch1.position, touch2.position);

                // Get previous distance
                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;
                float prevDistance = Vector2.Distance(touch1PrevPos, touch2PrevPos);

                // Calculate pinch delta
                float pinchDelta = (currentDistance - prevDistance) * settings.PinchSensitivity * 0.01f;

                if (Mathf.Abs(pinchDelta) > 0.1f)
                {
                    Vector2 zoomInput = new Vector2(0, pinchDelta);
                    OnRotationInput?.Invoke(zoomInput);
                }
            }
        }

        private bool IsRightSideOfScreen(Vector2 screenPosition)
        {
            return screenPosition.x >= Screen.width * SCREEN_SPLIT_RATIO;
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            if (!enabled)
            {
                IsRotationActive = false;
                isTouchActive = false;
                rotationTouchId = -1;

#if UNITY_EDITOR
                isMouseDown = false;
                isPinchMode = false;
#endif
            }
        }

        public void Cleanup()
        {
            OnRotationInput = null;
        }

        public bool IsInputActive()
        {
            return (isEnabled && IsRotationActive);
        }

        public void SetSettings(MobileCameraSettings settings)
        {
            this.settings = settings;
        }
    }
}
*/

using System;

using Portfolio.CameraSystem;

using Reflex.Attributes;

using UnityEngine;

namespace Portfolio.InputSystem.Mobile
{
    /// <summary>
    /// Mobile platform input handler for touch-based camera rotation
    /// Right half of screen handles camera rotation, left half is reserved for movement
    /// In editor: Left mouse button simulates touch, Right mouse button + Left mouse simulates pinch zoom
    /// </summary>
    public class MobileRotationInputHandler : IPlatformRotationInputHandler
    {
        public event Action<Vector2> OnRotationInput;

        public bool IsRotationActive => HasRotationFlag(RotationInputState.RotationActive);

        // State management
        private CommonInputState commonState = CommonInputState.None;

        private RotationInputState rotationState = RotationInputState.None;

        private MobileCameraSettings settings;
        private Vector2 lastTouchPosition;
        private int rotationTouchId = -1; // Track which touch is handling rotation

        // Editor simulation variables - pooled to reduce allocations
        private Vector2 lastMousePosition;

        private float lastPinchDistance = 0f;

        private const float SCREEN_SPLIT_RATIO = 0.5f;
        private const float TOUCH_SCALE_FACTOR = 0.01f;
        private const float PINCH_SCALE_FACTOR = 0.01f;
        private const float PINCH_THRESHOLD = 0.1f;
        private const float SCROLL_SCALE_FACTOR = 10f;
        private const float MOUSE_Y_THRESHOLD = 1f;
        private const float SCROLL_THRESHOLD = 0.01f;
        private const float DEFAULT_PINCH_DISTANCE = 100f;

        [Inject] private IActionButton actionButton;

        public void Initialize()
        {
            if (HasCommonFlag(CommonInputState.Initialized)) return;

            ResetState();
            SetCommonFlag(CommonInputState.Initialized, true);
        }

        public void UpdateInput()
        {
            if (!HasCommonFlag(CommonInputState.Enabled | CommonInputState.Initialized) || settings == null) return;

#if UNITY_EDITOR
            HandleEditorInput();
#else
            HandleTouchRotation();
            HandlePinchZoom();
#endif
        }

#if UNITY_EDITOR

        private void HandleEditorInput()
        {
            // Right mouse button enables pinch zoom mode
            if (Input.GetMouseButtonDown(1))
            {
                SetRotationFlag(RotationInputState.PinchMode, true);
                lastPinchDistance = DEFAULT_PINCH_DISTANCE;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                SetRotationFlag(RotationInputState.PinchMode, false);
            }

            if (HasRotationFlag(RotationInputState.PinchMode))
            {
                HandleEditorPinchZoom();
            }
            else
            {
                HandleEditorTouchRotation();
            }
        }

        private void HandleEditorTouchRotation()
        {
            Vector2 mousePos = Input.mousePosition;
            bool isRightSide = IsRightSideOfScreen(mousePos);

            if (Input.GetMouseButtonDown(0) && isRightSide && !actionButton.IsInButtonArea(mousePos))
            {
                lastMousePosition = mousePos;
                SetRotationFlag(RotationInputState.MouseDown | RotationInputState.TouchActive, true);
                SetCommonFlag(CommonInputState.InputActive, true);
            }
            else if (Input.GetMouseButton(0) && HasRotationFlag(RotationInputState.MouseDown))
            {
                Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
                lastMousePosition = Input.mousePosition;

                ProcessRotationInput(mouseDelta);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                ResetRotationState();
            }
            else
            {
                ResetRotationState();
            }
        }

        private void HandleEditorPinchZoom()
        {
            if (Input.GetMouseButton(0) && settings.EnablePinchZoom)
            {
                // Use mouse wheel or vertical mouse movement for pinch simulation
                float scrollDelta = Input.GetAxis("Mouse ScrollWheel");

                if (Mathf.Abs(scrollDelta) > SCROLL_THRESHOLD)
                {
                    float pinchDelta = scrollDelta * settings.PinchSensitivity * SCROLL_SCALE_FACTOR;
                    Vector2 zoomInput = new Vector2(0, pinchDelta);
                    OnRotationInput?.Invoke(zoomInput);
                }
                else
                {
                    // Alternative: use mouse Y movement while in pinch mode
                    Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
                    lastMousePosition = Input.mousePosition;

                    if (Mathf.Abs(mouseDelta.y) > MOUSE_Y_THRESHOLD)
                    {
                        float pinchDelta = mouseDelta.y * settings.PinchSensitivity * TOUCH_SCALE_FACTOR;
                        Vector2 zoomInput = new Vector2(0, pinchDelta);
                        OnRotationInput?.Invoke(zoomInput);
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                lastMousePosition = Input.mousePosition;
            }
        }

#endif

        private void HandleTouchRotation()
        {
            // Early exit if no touches
            if (Input.touchCount == 0)
            {
                if (rotationTouchId != -1)
                {
                    ResetRotationState();
                }
                return;
            }

            // Process all touches to find rotation touch
            for (int i = 0; i < Input.touchCount; i++)
            {
                UnityEngine.Touch touch = Input.touches[i];

                if (touch.fingerId == rotationTouchId)
                {
                    HandleExistingRotationTouch(touch);
                    return; // Found our touch, no need to continue
                }
                else if (rotationTouchId == -1 && touch.phase == TouchPhase.Began)
                {
                    TryStartRotation(touch);
                }
            }

            // Verify rotation touch still exists
            if (rotationTouchId != -1)
            {
                VerifyRotationTouchExists();
            }
        }

        private void TryStartRotation(UnityEngine.Touch touch)
        {
            if (IsRightSideOfScreen(touch.position) && !actionButton.IsInButtonArea(touch.position))
            {
                rotationTouchId = touch.fingerId;
                lastTouchPosition = touch.position;
                SetRotationFlag(RotationInputState.TouchActive, true);
                SetCommonFlag(CommonInputState.InputActive, true);
            }
        }

        private void HandleExistingRotationTouch(UnityEngine.Touch touch)
        {
            if (touch.phase == TouchPhase.Moved && HasRotationFlag(RotationInputState.TouchActive))
            {
                Vector2 touchDelta = touch.position - lastTouchPosition;
                lastTouchPosition = touch.position;
                ProcessRotationInput(touchDelta);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                ResetRotationState();
            }
        }

        private void VerifyRotationTouchExists()
        {
            // Optimized touch verification using span-like iteration
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.touches[i].fingerId == rotationTouchId)
                {
                    return; // Touch still exists
                }
            }

            // Touch no longer exists
            ResetRotationState();
        }

        private void ProcessRotationInput(Vector2 delta)
        {
            // Apply deadzone
            if (delta.magnitude > settings.TouchDeadzone)
            {
                Vector2 rotationInput = delta * settings.Sensitivity * TOUCH_SCALE_FACTOR;

                // Invert Y axis if needed
                if (settings.InvertYAxis)
                    rotationInput.y = -rotationInput.y;

                OnRotationInput?.Invoke(rotationInput);
                SetRotationFlag(RotationInputState.RotationActive, true);
            }
            else
            {
                SetRotationFlag(RotationInputState.RotationActive, false);
            }
        }

        private void HandlePinchZoom()
        {
            if (Input.touchCount != 2 || !settings.EnablePinchZoom) return;

            UnityEngine.Touch touch1 = Input.GetTouch(0);
            UnityEngine.Touch touch2 = Input.GetTouch(1);

            // Check if at least one touch is on the right side
            if (!(IsRightSideOfScreen(touch1.position) || IsRightSideOfScreen(touch2.position)))
                return;

            // Calculate pinch delta using cached previous positions
            float currentDistance = Vector2.Distance(touch1.position, touch2.position);
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
            Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;
            float prevDistance = Vector2.Distance(touch1PrevPos, touch2PrevPos);

            float pinchDelta = (currentDistance - prevDistance) * settings.PinchSensitivity * PINCH_SCALE_FACTOR;

            if (Mathf.Abs(pinchDelta) > PINCH_THRESHOLD)
            {
                Vector2 zoomInput = new Vector2(0, pinchDelta);
                OnRotationInput?.Invoke(zoomInput);
            }
        }

        private bool IsRightSideOfScreen(Vector2 screenPosition)
        {
            return screenPosition.x >= Screen.width * SCREEN_SPLIT_RATIO;
        }

        public void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                SetCommonFlag(CommonInputState.Enabled, true);
            }
            else
            {
                ResetState();
            }
        }

        public void Cleanup()
        {
            OnRotationInput = null;
            ResetState();
        }

        public bool IsInputActive()
        {
            return HasCommonFlag(CommonInputState.Enabled | CommonInputState.InputActive);
        }

        public void SetSettings(MobileCameraSettings settings)
        {
            this.settings = settings;
        }

        #region State Management

        private bool HasCommonFlag(CommonInputState flag)
        {
            return (commonState & flag) == flag;
        }

        private void SetCommonFlag(CommonInputState flag, bool value)
        {
            if (value)
                commonState |= flag;
            else
                commonState &= ~flag;
        }

        private bool HasRotationFlag(RotationInputState flag)
        {
            return (rotationState & flag) == flag;
        }

        private void SetRotationFlag(RotationInputState flag, bool value)
        {
            if (value)
                rotationState |= flag;
            else
                rotationState &= ~flag;
        }

        private void ResetState()
        {
            commonState = CommonInputState.None;
            rotationState = RotationInputState.None;
            rotationTouchId = -1;
        }

        private void ResetRotationState()
        {
            SetRotationFlag(RotationInputState.TouchActive | RotationInputState.RotationActive | RotationInputState.MouseDown, false);
            SetCommonFlag(CommonInputState.InputActive, false);
            rotationTouchId = -1;
        }

        #endregion State Management
    }
}