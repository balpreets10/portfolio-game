using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Unity.VisualScripting;
using System;

public class BuildingLightEffect : MonoBehaviour
{
    [Header("LightSettings")]
    [SerializeField] private GameObject lightSource1;

    [SerializeField] private GameObject lightSource2;

    [SerializeField] private Transform lightParent;

    [Header("Emerge Settings")]
    [SerializeField] private Vector3 groundPosition = Vector3.zero;

    [SerializeField] private float targetHeight = 5f;
    [SerializeField] private float emergeDuration = 2f;
    [SerializeField] private Ease emergeEase = Ease.OutQuad;

    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [SerializeField] private float rotationSpeed = 90f; // degrees per second
    [SerializeField] private float rotationDuration = 1f;

    [Header("Events")]
    [SerializeField] private UnityEvent onEmergeStart;

    [SerializeField] private UnityEvent onTargetHeightReached;
    [SerializeField] private GameObject skillObject;

    private Vector3 targetPosition;
    private Tween emergeTween;
    private Tween rotationTween;
    private bool isEmerging = false;

    [SerializeField] private SectionHouse currentHouse = null;

    private Camera mainCamera;

    private void Awake()
    {
        ValidateSettings();
        CalculateTargetPosition();
    }

    private void Start()
    {
        // Set initial position to ground level
        lightParent.position = groundPosition;
        ManageLights(false);
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        //BuildingRaycastManager.OnBuildingHit += OnBuildingHit;
        //BuildingRaycastManager.OnBuildingLost += OnBuildingLost;
        RaycastManager.OnInteractableLost += OnInteractableLost;
        RaycastManager.OnInteractableHit += OnInteractableHit;
    }

    private void OnDisable()
    {
        // Clean up tweens when disabled
        emergeTween?.Kill();
        rotationTween?.Kill();
        isEmerging = false;

        //BuildingRaycastManager.OnBuildingHit -= OnBuildingHit;
        //BuildingRaycastManager.OnBuildingLost -= OnBuildingLost;
        RaycastManager.OnInteractableLost -= OnInteractableLost;
        RaycastManager.OnInteractableHit -= OnInteractableHit;
    }

    private void OnInteractableLost()
    {
        if (currentHouse != null)
        {
            OnSectionHouseLost();
        }
    }

    private void OnInteractableHit(IInteractable interactable)
    {
        if (interactable.GetType() == typeof(SectionHouse))
        {
            SectionHouse house = interactable as SectionHouse;
            if (house != null)
            {
                OnSectionHouseHit(house);
            }
        }
    }

    private void OnSectionHouseLost()
    {
        currentHouse = null;
        MoveToGround();
    }

    private void OnSectionHouseHit(SectionHouse house)
    {
        currentHouse = house;
        groundPosition = house.Ground.position;
        targetPosition = house.LightTarget.position;
        skillObject.transform.position = house.effectTextTarget.position;
        StartEmerge();
    }

    private void ValidateSettings()
    {
        if (emergeDuration <= 0f)
        {
            Debug.LogWarning($"[{name}] Emerge duration must be positive. Setting to 1f.");
            emergeDuration = 1f;
        }

        if (rotationDuration <= 0f)
        {
            Debug.LogWarning($"[{name}] Rotation duration must be positive. Setting to 1f.");
            rotationDuration = 1f;
        }

        if (rotationAxis == Vector3.zero)
        {
            Debug.LogWarning($"[{name}] Rotation axis cannot be zero. Setting to Vector3.up.");
            rotationAxis = Vector3.up;
        }
    }

    private void CalculateTargetPosition()
    {
        if (currentHouse == null)
            targetPosition = groundPosition + Vector3.up * targetHeight;
        else
            targetPosition = currentHouse.LightTarget.position;
    }

    [ContextMenu("Start Emerge")]
    public void StartEmerge()
    {
        // Kill any existing tweens first
        emergeTween?.Kill();
        rotationTween?.Kill();

        // Always activate lights when starting emerge
        ManageLights(true);

        // Set starting position
        lightParent.position = groundPosition;

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError($"[{name}] GameObject is not active. Cannot start emerge animation.");
            return;
        }

        isEmerging = true;

        // Calculate target position in case settings changed
        CalculateTargetPosition();

        // Invoke start event
        onEmergeStart?.Invoke();

        // Start emerge animation
        emergeTween = lightParent.DOMove(targetPosition, emergeDuration)
            .SetEase(emergeEase)
            .OnComplete(OnEmergeComplete)
            .OnKill(() => isEmerging = false);

        // Start infinite rotation
        StartInfiniteRotation();
    }

    private void ManageLights(bool activate)
    {
        lightSource1.SetActive(activate);
        lightSource2.SetActive(activate);
        skillObject.SetActive(activate);

        // Make skillObject look at camera when activated
        if (activate && mainCamera != null)
        {
            skillObject.transform.LookAt(mainCamera.transform);
        }
    }

    private void StartInfiniteRotation()
    {
        // Kill existing rotation tween if any
        rotationTween?.Kill();

        // Calculate rotation amount based on speed and duration
        float rotationAmount = rotationSpeed * rotationDuration;

        // Start infinite rotation
        rotationTween = lightParent.DORotate(
            rotationAxis.normalized * rotationAmount,
            rotationDuration,
            RotateMode.LocalAxisAdd
        )
        .SetLoops(-1, LoopType.Incremental)
        .SetEase(Ease.Linear);
    }

    private void OnEmergeComplete()
    {
        isEmerging = false;
        onTargetHeightReached?.Invoke();
    }

    [ContextMenu("Stop Emerge")]
    public void StopEmerge()
    {
        emergeTween?.Kill();
        isEmerging = false;
    }

    [ContextMenu("Stop Rotation")]
    public void StopRotation()
    {
        rotationTween?.Kill();
        ManageLights(false);
    }

    [ContextMenu("Stop All")]
    public void StopAll()
    {
        StopEmerge();
        StopRotation();
    }

    [ContextMenu("Move To Ground")]
    public void MoveToGround()
    {
        emergeTween = lightParent.DOMove(groundPosition, 0.2f)
            .SetEase(emergeEase).OnComplete(StopRotation);
    }

    [ContextMenu("Reset to Ground")]
    public void ResetToGround()
    {
        StopAll();
        lightParent.position = groundPosition;
    }

    public void SetGroundPosition(Vector3 position)
    {
        groundPosition = position;
        CalculateTargetPosition();
    }

    public void SetTargetHeight(float height)
    {
        targetHeight = height;
        CalculateTargetPosition();
    }

    public bool IsEmerging => isEmerging;
    public Vector3 GroundPosition => groundPosition;
    public float TargetHeight => targetHeight;
    public Vector3 TargetPosition => targetPosition;

    private void OnDestroy()
    {
        // Clean up tweens on destroy
        emergeTween?.Kill();
        rotationTween?.Kill();
    }

    private void OnDrawGizmosSelected()
    {
        // Draw ground position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundPosition, 0.2f);

        // Calculate and draw target position
        CalculateTargetPosition();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPosition, 0.2f);

        // Draw line between ground and target
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(groundPosition, targetPosition);

        // Draw rotation axis
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(lightParent.position, rotationAxis.normalized * 2f);
    }
}