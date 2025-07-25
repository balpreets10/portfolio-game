using System;

using Reflex.Attributes;

using UnityEngine;
using UnityEngine.UI;

public class ActionButton : MonoBehaviour, IActionButton
{
    [Header("Button Settings")]
    [SerializeField] private Button actionButton;

    private Rect buttonArea;
    private bool isButtonEnabled = false;

    public static event Action OnActionPressed;

    [Inject] private IPlatformDetector platformDetector;

    private void Start()
    {
        if (platformDetector.CurrentPlatform == GamePlatform.Mobile)
        {
            InitializeActionButton();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void InitializeActionButton()
    {
        // Keep button interactable for visual feedback, but handle clicks manually
        actionButton.interactable = false;

        // Calculate button area in screen coordinates
        UpdateButtonArea();
        RaycastManager.OnInteractableHit += OnInteractableHit;
        RaycastManager.OnInteractableLost += OnInteractableLost;
    }

    private void Update()
    {
        if (!isButtonEnabled) return;

        // Handle manual input detection
#if UNITY_EDITOR
        HandleEditorInput();
#else
        HandleTouchInput();
#endif
    }

#if UNITY_EDITOR

    private void HandleEditorInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (IsInButtonArea(mousePos))
            {
                OnButtonClick();
            }
        }
    }

#endif

    private void HandleTouchInput()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.touches[i];
            if (touch.phase == TouchPhase.Began && IsInButtonArea(touch.position))
            {
                OnButtonClick();
                break; // Only process one touch
            }
        }
    }

    private void OnInteractableLost()
    {
        isButtonEnabled = false;
        actionButton.interactable = false;
    }

    private void OnInteractableHit(IInteractable interactable)
    {
        isButtonEnabled = true;
        actionButton.interactable = true; // Visual feedback only
    }

    public void OnButtonClick()
    {
        if (!isButtonEnabled) return;

        Debug.Log("Action button clicked!");
        OnActionPressed?.Invoke();
    }

    private void UpdateButtonArea()
    {
        Vector3[] corners = new Vector3[4];
        actionButton.GetComponent<RectTransform>().GetWorldCorners(corners);

        Vector2 min = corners[0];
        Vector2 max = corners[2];

        buttonArea = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }

    private void OnDestroy()
    {
        RaycastManager.OnInteractableHit -= OnInteractableHit;
        RaycastManager.OnInteractableLost -= OnInteractableLost;
    }

    public bool IsInButtonArea(Vector2 screenPosition)
    {
        return buttonArea.Contains(screenPosition);
    }
}

public interface IActionButton
{
    bool IsInButtonArea(Vector2 screenPosition);
}