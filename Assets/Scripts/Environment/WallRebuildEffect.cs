using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class WallRebuildEffect : MonoBehaviour
{
    [Header("Wall Pieces")]
    public List<Transform> wallPieces = new List<Transform>();

    [Header("Animation Settings")]
    public float animationDuration = 2f;
    public float delayBetweenPieces = 0.1f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scatter Settings")]
    public float scatterRadius = 10f;
    public float scatterHeight = 5f;

    [Header("Y Position Settings")]
    public ScatterYMode scatterYMode = ScatterYMode.FixedY;
    public float fixedYPosition = 0f;
    public LayerMask groundLayerMask = 1; // Default layer
    public float groundOffset = 0.5f; // Offset above ground

    public bool randomizeRotation = true;

    [Header("Physics")]
    public bool usePhysics = false;
    public float physicsForce = 5f;

    public enum ScatterYMode
    {
        FixedY,         // Use fixedYPosition exactly
        OnGround,       // Raycast to find ground
        RandomHeight    // Random Y within scatterHeight range
    }

    // Store original positions and rotations
    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Quaternion> originalRotations = new List<Quaternion>();
    private List<Vector3> originalScales = new List<Vector3>();

    // Store scattered positions
    private List<Vector3> scatteredPositions = new List<Vector3>();
    private List<Quaternion> scatteredRotations = new List<Quaternion>();

    private void Start()
    {
        // Auto-populate wall pieces if not set
        if (wallPieces.Count == 0)
        {
            PopulateWallPieces();
        }

        // Store original transforms
        StoreOriginalTransforms();

        // Generate scattered positions
        GenerateScatteredPositions();

        // Start in scattered state
        ScatterWallImmediate();
    }

    private void PopulateWallPieces()
    {
        // Get all child objects as wall pieces
        for (int i = 0; i < transform.childCount; i++)
        {
            wallPieces.Add(transform.GetChild(i));
        }
    }

    private void StoreOriginalTransforms()
    {
        originalPositions.Clear();
        originalRotations.Clear();
        originalScales.Clear();

        foreach (Transform piece in wallPieces)
        {
            originalPositions.Add(piece.position);
            originalRotations.Add(piece.rotation);
            originalScales.Add(piece.localScale);
        }
    }

    private void GenerateScatteredPositions()
    {
        scatteredPositions.Clear();
        scatteredRotations.Clear();

        foreach (Transform piece in wallPieces)
        {
            // Generate random X and Z within scatter radius
            float randomX = Random.Range(-scatterRadius, scatterRadius);
            float randomZ = Random.Range(-scatterRadius, scatterRadius);

            Vector3 scatterPosition = new Vector3(
                transform.position.x + randomX,
                0f, // Will be set below based on mode
                transform.position.z + randomZ
            );

            // Set Y position based on selected mode
            float finalY = GetScatterYPosition(scatterPosition, piece);
            scatterPosition.y = finalY;

            scatteredPositions.Add(scatterPosition);

            // Generate random rotation if enabled
            if (randomizeRotation)
            {
                scatteredRotations.Add(Random.rotation);
            }
            else
            {
                scatteredRotations.Add(piece.rotation);
            }
        }
    }

    private float GetScatterYPosition(Vector3 scatterPosition, Transform piece)
    {
        switch (scatterYMode)
        {
            case ScatterYMode.FixedY:
                return fixedYPosition;

            case ScatterYMode.OnGround:
                return GetGroundY(scatterPosition);

            case ScatterYMode.RandomHeight:
                return transform.position.y + Random.Range(-scatterHeight, scatterHeight);

            default:
                return fixedYPosition;
        }
    }

    private float GetGroundY(Vector3 position)
    {
        // Cast a ray downward from a high point to find the ground
        Vector3 rayStart = new Vector3(position.x, transform.position.y + 100f, position.z);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayerMask))
        {
            return hit.point.y + groundOffset;
        }

        // If no ground found, use fixed Y as fallback
        Debug.LogWarning($"No ground found at position {position}, using fixed Y position as fallback");
        return fixedYPosition;
    }

    private void ScatterWallImmediate()
    {
        for (int i = 0; i < wallPieces.Count; i++)
        {
            wallPieces[i].position = scatteredPositions[i];
            wallPieces[i].rotation = scatteredRotations[i];

            // Add physics if enabled
            if (usePhysics)
            {
                Rigidbody rb = wallPieces[i].GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.AddForce(Random.insideUnitSphere * physicsForce, ForceMode.Impulse);
                }
            }
        }
    }

    [ContextMenu("Rebuild Wall")]
    public void RebuildWall()
    {
        StartCoroutine(RebuildWallCoroutine());
    }

    [ContextMenu("Scatter Wall")]
    public void ScatterWall()
    {
        StartCoroutine(ScatterWallCoroutine());
    }

    private IEnumerator RebuildWallCoroutine()
    {
        // Disable physics during rebuild
        if (usePhysics)
        {
            foreach (Transform piece in wallPieces)
            {
                Rigidbody rb = piece.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }
            }
        }

        // Create a sequence for all pieces
        Sequence rebuildSequence = DOTween.Sequence();

        for (int i = 0; i < wallPieces.Count; i++)
        {
            int index = i; // Capture for closure

            // Calculate delay for this piece
            float delay = i * delayBetweenPieces;

            // Add movement tween
            Tween moveTween = wallPieces[i].DOMove(originalPositions[i], animationDuration)
                .SetEase(easeCurve)
                .SetDelay(delay);

            // Add rotation tween
            Tween rotateTween = wallPieces[i].DORotateQuaternion(originalRotations[i], animationDuration)
                .SetEase(easeCurve)
                .SetDelay(delay);

            // Add scale tween (in case pieces were scaled during scatter)
            Tween scaleTween = wallPieces[i].DOScale(originalScales[i], animationDuration)
                .SetEase(easeCurve)
                .SetDelay(delay);

            // Add some juice - slight anticipation
            wallPieces[i].DOPunchScale(Vector3.one * 0.1f, 0.3f, 1, 0.5f)
                .SetDelay(delay + animationDuration * 0.8f);

            rebuildSequence.Join(moveTween);
            rebuildSequence.Join(rotateTween);
            rebuildSequence.Join(scaleTween);
        }

        // Wait for sequence to complete
        yield return rebuildSequence.WaitForCompletion();

        // Optional: Add final effect
        transform.DOPunchScale(Vector3.one * 0.05f, 0.5f, 1, 0.3f);
    }

    private IEnumerator ScatterWallCoroutine()
    {
        // Generate new scattered positions
        GenerateScatteredPositions();

        // Create a sequence for scattering
        Sequence scatterSequence = DOTween.Sequence();

        for (int i = 0; i < wallPieces.Count; i++)
        {
            float delay = i * delayBetweenPieces * 0.5f; // Faster scattering

            // Add movement tween
            Tween moveTween = wallPieces[i].DOMove(scatteredPositions[i], animationDuration * 0.7f)
                .SetEase(Ease.OutCubic)
                .SetDelay(delay);

            // Add rotation tween
            if (randomizeRotation)
            {
                Tween rotateTween = wallPieces[i].DORotateQuaternion(scatteredRotations[i], animationDuration * 0.7f)
                    .SetEase(Ease.OutCubic)
                    .SetDelay(delay);

                scatterSequence.Join(rotateTween);
            }

            scatterSequence.Join(moveTween);
        }

        yield return scatterSequence.WaitForCompletion();

        // Enable physics after scattering if needed
        if (usePhysics)
        {
            foreach (Transform piece in wallPieces)
            {
                Rigidbody rb = piece.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.AddForce(Random.insideUnitSphere * physicsForce, ForceMode.Impulse);
                }
            }
        }
    }

    // Public methods for external triggering
    public void TriggerRebuild()
    {
        RebuildWall();
    }

    public void TriggerScatter()
    {
        ScatterWall();
    }

    // Method to set custom scattered positions (useful for specific destruction patterns)
    public void SetCustomScatteredPositions(List<Vector3> positions, List<Quaternion> rotations = null)
    {
        if (positions.Count != wallPieces.Count)
        {
            Debug.LogWarning("Position count doesn't match wall pieces count");
            return;
        }

        scatteredPositions = new List<Vector3>(positions);

        if (rotations != null && rotations.Count == wallPieces.Count)
        {
            scatteredRotations = new List<Quaternion>(rotations);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw scatter radius in scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, scatterRadius);

        // Draw Y position indicators based on mode
        if (scatterYMode == ScatterYMode.FixedY)
        {
            Gizmos.color = Color.green;
            Vector3 center = new Vector3(transform.position.x, fixedYPosition, transform.position.z);
            Gizmos.DrawWireCube(center, new Vector3(scatterRadius * 2, 0.1f, scatterRadius * 2));
        }
        else if (scatterYMode == ScatterYMode.RandomHeight)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(scatterRadius * 2, scatterHeight * 2, scatterRadius * 2));
        }
    }
}