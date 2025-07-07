using System;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform player;

    [SerializeField] private float followDistance = 5f;
    [SerializeField] private float followHeight = 2f;
    [SerializeField] private Vector3 offset = Vector3.zero;

    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 2f;

    [SerializeField] private bool invertYAxis = false;
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private bool enableSmoothing = true;

    [Header("Rotation Constraints")]
    [SerializeField] private float minVerticalAngle = -30f;

    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("Collision Detection")]
    [SerializeField] private bool enableCollisionDetection = true;

    [SerializeField] private LayerMask collisionLayers = -1;
    [SerializeField] private float collisionRadius = 0.3f;

    [Header("Interaction Settings")]
    public float interactionRange = 3f;

    public LayerMask interactableMask = 1;
    public KeyCode interactionKey = KeyCode.E;

    [Header("UI References")]
    public GameObject interactionPrompt;

    public TextMeshProUGUI interactionText;
    public GameObject resumePanel;
    public TextMeshProUGUI resumeContentText;
    public TextMeshProUGUI resumeTitleText;
    public Button closeButton;

    [Header("Audio")]
    public AudioClip interactionSound;

    [Header("Debug Settings")]
    [SerializeField] private bool showCameraGizmos = true;

    [SerializeField] private bool showInteractionGizmos = true;

    // Private variables
    private float rotationY = 0f;

    private float rotationX = 0f;
    private Vector3 currentVelocity;
    private Camera cam;
    private bool enableMouseLook = false;
    private bool isInteracting;
    private bool interactPressed;
    private IInteractable currentInteractable;

    private bool enabled = false;

    private void Start()
    {
        cam = GetComponent<Camera>();
        SetupUI();

        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("No camera found! Please attach this script to a camera or ensure Camera.main exists.");
                return;
            }
        }

        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogError("Player not found! Please assign a player transform or tag your player GameObject with 'Player' tag.");
                return;
            }
        }

        // Initialize rotation values
        Vector3 currentRotation = transform.eulerAngles;
        rotationX = currentRotation.x;
        rotationY = currentRotation.y;
    }

    private void Update()
    {
        if (player == null) return;
        if (!enabled && Input.GetMouseButtonUp(0))
        {
            enabled = true;
        }
        if (enabled)
        {
            HandleInput();
            HandleMouseLook();
            HandleInteraction();
            HandleCameraMovement();
            HandlePlayerRotation();
        }
    }

    private void SetupUI()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (resumePanel != null)
        {
            resumePanel.SetActive(false);
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseResumePanel);
        }
    }

    private void ShowResumePanel(Section section)
    {
        if (resumePanel != null)
        {
            isInteracting = true;
            resumePanel.SetActive(true);

            if (resumeTitleText != null)
                resumeTitleText.text = section.title;

            if (resumeContentText != null)
                resumeContentText.text = section.content;

            // Show cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void CloseResumePanel()
    {
        if (resumePanel != null)
        {
            isInteracting = false;
            resumePanel.SetActive(false);

            // Hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleInput()
    {
        // Toggle mouse look with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            enableMouseLook = !enableMouseLook;
            Cursor.lockState = enableMouseLook ? CursorLockMode.Locked : CursorLockMode.None;
        }

        if (Input.GetMouseButtonUp(0) && !enableMouseLook)
        {
            enableMouseLook = true;
            Cursor.lockState = CursorLockMode.Locked;
        }

        interactPressed = Input.GetKeyDown(interactionKey);
    }

    private void HandleMouseLook()
    {
        if (!enableMouseLook) return;
        if (isInteracting) return;

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Invert Y axis if enabled
        if (invertYAxis)
            mouseY = -mouseY;

        // Update rotation values
        rotationY += mouseX;
        rotationX -= mouseY;

        // Clamp vertical rotation
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
    }

    private void HandleCameraMovement()
    {
        // Calculate desired position based on player position and camera rotation
        Vector3 direction = new Vector3(0, 0, -followDistance);
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);

        // Calculate the desired position
        Vector3 desiredPosition = player.position + offset + (rotation * direction);
        desiredPosition.y += followHeight;

        // Handle collision detection
        if (enableCollisionDetection)
        {
            desiredPosition = HandleCollision(player.position + offset, desiredPosition);
        }

        // Apply position (with or without smoothing)
        if (enableSmoothing)
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
        }
        else
        {
            transform.position = desiredPosition;
        }

        // Make camera look at player
        Vector3 lookDirection = (player.position + offset) - transform.position;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    private void HandlePlayerRotation()
    {
        // Rotate player to match camera's horizontal rotation (Y-axis only)
        if (player != null)
        {
            Vector3 playerRotation = player.eulerAngles;
            playerRotation.y = rotationY;
            player.rotation = Quaternion.Euler(playerRotation);
        }
    }

    private Vector3 HandleCollision(Vector3 targetPosition, Vector3 desiredPosition)
    {
        Vector3 direction = (desiredPosition - targetPosition).normalized;
        float distance = Vector3.Distance(targetPosition, desiredPosition);

        RaycastHit hit;
        if (Physics.SphereCast(targetPosition, collisionRadius, direction, out hit, distance, collisionLayers))
        {
            // Position camera just before the collision point
            return targetPosition + direction * (hit.distance - collisionRadius);
        }

        return desiredPosition;
    }

    private void HandleInteraction()
    {
        // Raycast for interactables
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableMask))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Show interaction prompt
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    ShowInteractionPrompt(interactable.GetInteractionText());
                }

                // Handle interaction input
                if (interactPressed)
                {
                    StartInteraction(interactable);
                }
            }
        }
        else
        {
            // Hide interaction prompt
            if (currentInteractable != null)
            {
                currentInteractable = null;
                HideInteractionPrompt();
            }
        }
    }

    private void ShowInteractionPrompt(string text)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
            if (interactionText != null)
            {
                interactionText.text = $"Press {interactionKey} to {text}";
            }
        }
    }

    private void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    private void StartInteraction(IInteractable interactable)
    {
        // Get resume section data
        Section section = interactable.GetResumeSection();
        if (section != null)
        {
            ShowResumePanel(section);
        }

        interactable.OnInteract();
    }

    // Public methods for external control
    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
    }

    public void SetFollowDistance(float distance)
    {
        followDistance = Mathf.Max(0.1f, distance);
    }

    public void SetFollowHeight(float height)
    {
        followHeight = height;
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Max(0f, sensitivity);
    }

    public void SetMouseLook(bool enabled)
    {
        enableMouseLook = enabled;
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
    }

    public void ResetCamera()
    {
        rotationX = 0f;
        rotationY = 0f;
        if (player != null)
        {
            transform.position = player.position + Vector3.back * followDistance + Vector3.up * followHeight;
            transform.LookAt(player.position);
        }
    }

    public void SetInteractionRange(float range)
    {
        interactionRange = range;
    }

    // Handle cursor lock/unlock when application focus changes
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && enableMouseLook)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // Draw gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        // Camera follow gizmos
        if (showCameraGizmos)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(player.position + offset, 0.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(player.position + offset, transform.position);

            if (enableCollisionDetection)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, collisionRadius);
            }
        }

        // Interaction raycast gizmos
        if (showInteractionGizmos && cam != null)
        {
            Vector3 rayStart = cam.transform.position;
            Vector3 rayEnd = rayStart + cam.transform.forward * interactionRange;

            // Draw interaction range ray
            Gizmos.color = Color.green;
            Gizmos.DrawLine(rayStart, rayEnd);

            // Draw interaction range sphere at end
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rayEnd, 0.2f);
        }
    }
}

public interface IInteractable
{
    string GetInteractionText();

    void OnInteract();

    Section GetResumeSection();
}