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

        // State management - simplified
        private bool isEnabled = false;

        private bool isInitialized = false;
        private bool isRotationActive = false;

        private MobileCameraSettings settings;
        private Vector2 lastTouchPosition;
        private int rotationTouchId = -1; // Track which touch is handling rotation

        // Editor simulation variables
        private Vector2 lastMousePosition;

        private bool isMouseDown = false;
        private bool isPinchMode = false;

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
            if (isInitialized) return;

            ResetState();
            isInitialized = true;
        }

        public void UpdateInput()
        {
            if (!isEnabled || !isInitialized || settings == null) return;

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
                lastMousePosition = Input.mousePosition;
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
                isRotationActive = true;
            }
            else if (Input.GetMouseButton(0) && isMouseDown && isRightSide)
            {
                Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
                lastMousePosition = Input.mousePosition;
                ProcessRotationInput(mouseDelta);
            }
            else if (Input.GetMouseButtonUp(0) || !isRightSide)
            {
                ResetRotationState();
            }
        }

        private void HandleEditorPinchZoom()
        {
            if (Input.GetMouseButton(0) && settings.EnablePinchZoom)
            {
                float scrollDelta = Input.GetAxis("Mouse ScrollWheel");

                if (Mathf.Abs(scrollDelta) > SCROLL_THRESHOLD)
                {
                    float pinchDelta = scrollDelta * settings.PinchSensitivity * SCROLL_SCALE_FACTOR;
                    Vector2 zoomInput = new Vector2(0, pinchDelta);
                    OnRotationInput?.Invoke(zoomInput);
                }
                else
                {
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
            // Handle existing rotation touch first
            if (rotationTouchId != -1)
            {
                bool foundRotationTouch = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.touches[i];
                    if (touch.fingerId == rotationTouchId)
                    {
                        foundRotationTouch = true;
                        HandleExistingRotationTouch(touch);
                        break;
                    }
                }

                // If rotation touch not found, reset
                if (!foundRotationTouch)
                {
                    ResetRotationState();
                }
                return; // Don't process new touches while one is active
            }

            // Look for new touches on the right side
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.touches[i];
                if (touch.phase == TouchPhase.Began &&
                    IsRightSideOfScreen(touch.position) &&
                    !actionButton.IsInButtonArea(touch.position))
                {
                    StartRotation(touch);
                    break; // Only handle one new touch at a time
                }
            }
        }

        private void StartRotation(Touch touch)
        {
            rotationTouchId = touch.fingerId;
            lastTouchPosition = touch.position;
            isRotationActive = true;
        }

        private void HandleExistingRotationTouch(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (IsRightSideOfScreen(touch.position))
                    {
                        Vector2 touchDelta = touch.position - lastTouchPosition;
                        lastTouchPosition = touch.position;
                        ProcessRotationInput(touchDelta);
                    }
                    else
                    {
                        ResetRotationState();
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    ResetRotationState();
                    break;
            }
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
                isRotationActive = true;
            }
            else
            {
                isRotationActive = false;
            }
        }

        private void HandlePinchZoom()
        {
            if (Input.touchCount != 2 || !settings.EnablePinchZoom) return;

            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            // Check if at least one touch is on the right side
            if (!(IsRightSideOfScreen(touch1.position) || IsRightSideOfScreen(touch2.position)))
                return;

            // Calculate pinch delta
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
            isEnabled = enabled;
            if (!enabled)
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
            return isEnabled && isInitialized && isRotationActive;
        }

        public void SetSettings(MobileCameraSettings settings)
        {
            this.settings = settings;
        }

        #region State Management

        private void ResetState()
        {
            isRotationActive = false;
            rotationTouchId = -1;
            isMouseDown = false;
            isPinchMode = false;
        }

        private void ResetRotationState()
        {
            isRotationActive = false;
            rotationTouchId = -1;
            isMouseDown = false;
        }

        #endregion State Management
    }
}