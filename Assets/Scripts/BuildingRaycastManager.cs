using System;

using UnityEngine;

public class BuildingRaycastManager : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float raycastDistance = 10f;

    [SerializeField] private LayerMask buildingMask = 1;
    [SerializeField] private Camera raycastCamera;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugRays = true;

    [SerializeField] private bool showGizmos = true;

    // Events
    public static event Action<SectionHouse> OnBuildingHit;

    public static event Action OnBuildingLost;

    // Current state
    private SectionHouse currentBuilding;

    private RaycastHit currentHit;
    private bool isHittingBuilding;

    // Properties
    public SectionHouse CurrentBuilding => currentBuilding;

    public RaycastHit CurrentHit => currentHit;
    public bool IsHittingBuilding => isHittingBuilding;

    private void Start()
    {
        // Get camera if not assigned
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
            if (raycastCamera == null)
            {
                Debug.LogError("No camera found! Please assign a camera to BuildingRaycastManager.");
            }
        }
    }

    private void Update()
    {
        PerformRaycast();
    }

    private void PerformRaycast()
    {
        if (raycastCamera == null) return;

        // Cast ray from camera forward
        Ray ray = new Ray(raycastCamera.transform.position, raycastCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, buildingMask))
        {
            // Store hit info
            currentHit = hit;

            // Check if we hit a building with SectionHouse component
            SectionHouse hitBuilding = hit.collider.GetComponent<SectionHouse>();

            if (hitBuilding != null)
            {
                // We're hitting a building
                isHittingBuilding = true;

                // Check if this is a new building
                if (currentBuilding != hitBuilding)
                {
                    currentBuilding = hitBuilding;
                    OnBuildingHit?.Invoke(hitBuilding);

                    if (showDebugRays)
                    {
                        Debug.Log($"Building Hit: {hitBuilding.houseName} | Distance: {hit.distance:F2}m");
                    }
                }
            }
            else
            {
                // Hit something else, not a building
                HandleBuildingLost();
            }
        }
        else
        {
            // No hit at all
            HandleBuildingLost();
        }
    }

    private void HandleBuildingLost()
    {
        if (isHittingBuilding)
        {
            isHittingBuilding = false;
            currentBuilding = null;
            OnBuildingLost?.Invoke();

            if (showDebugRays)
            {
                Debug.Log("Building Lost");
            }
        }
    }

    // Public methods for external control
    public void SetRaycastDistance(float distance)
    {
        raycastDistance = Mathf.Max(0.1f, distance);
    }

    public void SetBuildingMask(LayerMask mask)
    {
        buildingMask = mask;
    }

    public void SetCamera(Camera camera)
    {
        raycastCamera = camera;
    }

    // Gizmos for debugging
    private void OnDrawGizmos()
    {
        if (!showGizmos || raycastCamera == null) return;

        Vector3 rayStart = raycastCamera.transform.position;
        Vector3 rayEnd = rayStart + raycastCamera.transform.forward * raycastDistance;

        // Draw main ray
        Gizmos.color = Color.green;
        Gizmos.DrawLine(rayStart, rayEnd);

        // Draw hit indicator
        if (Application.isPlaying && isHittingBuilding)
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
        Vector3 rayEnd = raycastCamera.transform.position + raycastCamera.transform.forward * raycastDistance;
        Gizmos.DrawWireSphere(rayEnd, 0.2f);
    }
}