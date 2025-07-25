using System;

using UnityEngine;

public class RaycastManager : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float raycastDistance = 10f;

    [SerializeField] private LayerMask interactableMask = -1; // All layers by default
    [SerializeField] private Camera raycastCamera;

    [Header("Raycast Direction")]
    [SerializeField] private Vector3 raycastDirection = Vector3.forward;

    [SerializeField] private bool useWorldSpace = false; // If true, uses world space; if false, uses local space relative to camera
    [SerializeField] private bool normalizeDirection = true; // Automatically normalize the direction vector

    [Header("Input Settings")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [SerializeField] private bool enableKeyboardInput = true;
    [SerializeField] private bool enableJoystickInput = true;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugRays = true;

    [SerializeField] private bool showGizmos = true;

    // Events for different interactable types
    public static event Action<IInteractable> OnInteractableHit;

    public static event Action<IInteractableWithSection> OnInteractableWithSectionHit;

    public static event Action OnInteractableLost;

    // Current state
    private IInteractable currentInteractable;

    private IInteractableWithSection currentInteractableWithSection;
    private RaycastHit currentHit;
    private bool isHittingInteractable;

    // Properties
    public IInteractable CurrentInteractable => currentInteractable;

    public IInteractableWithSection CurrentInteractableWithSection => currentInteractableWithSection;
    public RaycastHit CurrentHit => currentHit;
    public bool IsHittingInteractable => isHittingInteractable;

    // Reference to character skin controller (for section-based interactions)
    public CharacterSkinController characterSkinController;

    private void Start()
    {
        InitializeCamera();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeCamera()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
            if (raycastCamera == null)
            {
                Debug.LogError("No camera found! Please assign a camera to GenericRaycastManager.");
            }
        }
    }

    private void SubscribeToEvents()
    {
        if (enableJoystickInput)
        {
            //VirtualJoystick.OnJoystickActionPressed += OnJoystickActionPressed;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (enableJoystickInput)
        {
            //VirtualJoystick.OnJoystickActionPressed -= OnJoystickActionPressed;
        }
    }

    private void Update()
    {
        PerformRaycast();
        HandleInput();
    }

    private void HandleInput()
    {
        if (enableKeyboardInput && Input.GetKeyDown(interactionKey))
        {
            TriggerInteraction();
        }
    }

    private void OnJoystickActionPressed()
    {
        if (enableJoystickInput)
        {
            TriggerInteraction();
        }
    }

    private void TriggerInteraction()
    {
        if (currentInteractable != null)
        {
            // Call the interaction method on the current interactable
            if (currentInteractable is IInteractableWithSection interactableWithSection)
            {
                // Handle section-specific interaction logic if needed
                HandleSectionInteraction(interactableWithSection);
            }
            else
            {
                // Handle regular interactable
                HandleRegularInteraction(currentInteractable);
            }

            if (showDebugRays)
            {
                Debug.Log($"Interaction triggered with: {currentInteractable.GetType().Name}");
            }
        }
    }

    private void HandleSectionInteraction(IInteractableWithSection interactableWithSection)
    {
        // You can add specific logic for section-based interactions here
        // For example, triggering resume board landing or other section-specific actions

        if (showDebugRays)
        {
            Debug.Log($"Section interaction: {interactableWithSection.GetResumeSection().title}");
        }
    }

    private void HandleRegularInteraction(IInteractable interactable)
    {
        // Handle regular interactable logic
        if (showDebugRays)
        {
            Debug.Log($"Regular interaction with: {interactable.GetInteractionText()}");
        }
    }

    private Vector3 GetRaycastDirection()
    {
        Vector3 direction;

        if (useWorldSpace)
        {
            direction = raycastDirection;
        }
        else
        {
            // Transform local direction to world space relative to camera
            direction = raycastCamera.transform.TransformDirection(raycastDirection);
        }

        if (normalizeDirection)
        {
            direction = direction.normalized;
        }

        return direction;
    }

    private void PerformRaycast()
    {
        if (raycastCamera == null) return;

        Vector3 rayDirection = GetRaycastDirection();
        Ray ray = new Ray(raycastCamera.transform.position, rayDirection);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, interactableMask))
        {
            currentHit = hit;
            //Debug.Log("Hit - " + hit.collider.name);
            // Try to get any interactable component
            IInteractable hitInteractable = hit.collider.GetComponent<IInteractable>();
            if (hitInteractable == null)
            {
                hitInteractable = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (hitInteractable != null)
            {
                isHittingInteractable = true;

                // Check if this is a new interactable
                if (currentInteractable != hitInteractable)
                {
                    // Handle transition from old to new interactable
                    HandleInteractableTransition(hitInteractable);
                }
            }
            else
            {
                // Hit something else, not an interactable
                HandleInteractableLost();
            }
        }
        else
        {
            // No hit at all
            HandleInteractableLost();
        }
    }

    private void HandleInteractableTransition(IInteractable newInteractable)
    {
        // Clean up old interactable
        if (currentInteractable != null)
        {
            currentInteractable.OnInteractionLost();
        }

        // Set new interactable
        currentInteractable = newInteractable;
        currentInteractableWithSection = newInteractable as IInteractableWithSection;

        // Trigger interaction
        currentInteractable.OnInteract();

        // Fire appropriate events
        OnInteractableHit?.Invoke(currentInteractable);

        if (currentInteractableWithSection != null)
        {
            OnInteractableWithSectionHit?.Invoke(currentInteractableWithSection);

            // Handle character skin controller for section-based interactions
            if (characterSkinController != null)
            {
                characterSkinController.ChangeMaterialSettings(currentInteractableWithSection.GetResumeSection().index);
            }
        }

        if (showDebugRays)
        {
            string interactionText = currentInteractable.GetInteractionText();
            string sectionInfo = currentInteractableWithSection != null ?
                $" | Section: {currentInteractableWithSection.GetResumeSection().title}" : "";
            Debug.Log($"Interactable Hit: {interactionText}{sectionInfo} | Distance: {currentHit.distance:F2}m");
        }
    }

    private void HandleInteractableLost()
    {
        if (isHittingInteractable)
        {
            currentInteractable?.OnInteractionLost();
            isHittingInteractable = false;
            currentInteractable = null;
            currentInteractableWithSection = null;
            OnInteractableLost?.Invoke();

            if (showDebugRays)
            {
                Debug.Log("Interactable Lost");
            }
        }
    }

    // Public methods for external control
    public void SetRaycastDistance(float distance)
    {
        raycastDistance = Mathf.Max(0.1f, distance);
    }

    public void SetInteractableMask(LayerMask mask)
    {
        interactableMask = mask;
    }

    public void SetCamera(Camera camera)
    {
        raycastCamera = camera;
    }

    public void SetInteractionKey(KeyCode key)
    {
        interactionKey = key;
    }

    public void EnableKeyboardInput(bool enable)
    {
        enableKeyboardInput = enable;
    }

    public void EnableJoystickInput(bool enable)
    {
        if (enableJoystickInput != enable)
        {
            enableJoystickInput = enable;
            if (enable)
            {
                //VirtualJoystick.OnJoystickActionPressed += OnJoystickActionPressed;
            }
            else
            {
                //VirtualJoystick.OnJoystickActionPressed -= OnJoystickActionPressed;
            }
        }
    }

    // New methods for raycast direction control
    public void SetRaycastDirection(Vector3 direction)
    {
        raycastDirection = direction;
    }

    public void SetUseWorldSpace(bool worldSpace)
    {
        useWorldSpace = worldSpace;
    }

    public void SetNormalizeDirection(bool normalize)
    {
        normalizeDirection = normalize;
    }

    public Vector3 GetCurrentRaycastDirection()
    {
        return GetRaycastDirection();
    }

    // Method to manually trigger interaction (can be called from UI buttons, etc.)
    public void ManualTriggerInteraction()
    {
        TriggerInteraction();
    }

    // Get current interaction text for UI display
    public string GetCurrentInteractionText()
    {
        return currentInteractable?.GetInteractionText() ?? "";
    }

    // Check if we can interact with current target
    public bool CanInteract()
    {
        return currentInteractable != null && isHittingInteractable;
    }

    // Gizmos for debugging
    private void OnDrawGizmos()
    {
        if (!showGizmos || raycastCamera == null) return;

        Vector3 rayStart = raycastCamera.transform.position;
        Vector3 rayDirection = GetRaycastDirection();
        Vector3 rayEnd = rayStart + rayDirection * raycastDistance;

        // Draw main ray
        Gizmos.color = isHittingInteractable ? Color.green : Color.white;
        Gizmos.DrawLine(rayStart, rayEnd);

        // Draw hit indicator
        if (Application.isPlaying && isHittingInteractable)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentHit.point, 0.3f);

            // Draw line to hit point
            Gizmos.DrawLine(rayStart, currentHit.point);

            // Draw normal at hit point
            Gizmos.color = Color.white;
            Gizmos.DrawLine(currentHit.point, currentHit.point + currentHit.normal * 0.5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || raycastCamera == null) return;

        // Draw raycast range sphere
        Gizmos.color = Color.cyan;
        Vector3 rayDirection = GetRaycastDirection();
        Vector3 rayEnd = raycastCamera.transform.position + rayDirection * raycastDistance;
        Gizmos.DrawWireSphere(rayEnd, 0.2f);
    }
}