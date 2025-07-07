using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class FenceBoundarySystem : MonoBehaviour
{
    [Header("Fence Settings")]
    public GameObject fencePrefab;

    public Transform referencePoint; // Objects will face this transform

    public float spacing = 2f;
    public bool alignToPath = true;

    [Header("Path Settings")]
    public List<Vector3> pathPoints = new List<Vector3>();

    public bool showGizmos = true;
    public Color gizmoColor = Color.yellow;
    public float gizmoSize = 0.5f;

    [Header("Generated Fences")]
    public List<GameObject> generatedFences = new List<GameObject>();

    private void OnDrawGizmos()
    {
        if (!showGizmos || pathPoints.Count < 2) return;

        Gizmos.color = gizmoColor;

        // Draw path lines
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
        }

        // Draw path points
        foreach (Vector3 point in pathPoints)
        {
            Gizmos.DrawWireSphere(point, gizmoSize);
        }

        // Draw fence preview positions
        if (spacing > 0)
        {
            Gizmos.color = Color.green;
            List<Vector3> fencePositions = CalculateFencePositions();
            foreach (Vector3 pos in fencePositions)
            {
                Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
            }
        }
    }

    public void AddPathPoint(Vector3 point)
    {
        pathPoints.Add(point);
    }

    public void ClearPath()
    {
        pathPoints.Clear();
    }

    public void GenerateFences()
    {
        if (fencePrefab == null)
        {
            Debug.LogError("Fence prefab is not assigned!");
            return;
        }

        ClearGeneratedFences();

        List<Vector3> fencePositions = CalculateFencePositions();
        List<Vector3> fenceRotations = CalculateFenceRotations();

        for (int i = 0; i < fencePositions.Count; i++)
        {
            GameObject fence = Instantiate(fencePrefab, fencePositions[i], Quaternion.identity, transform);

            if (alignToPath && i < fenceRotations.Count)
            {
                fence.transform.LookAt(fence.transform.position + fenceRotations[i]);
            }

            generatedFences.Add(fence);
        }

        Debug.Log($"Generated {generatedFences.Count} fence objects");
    }

    public void ClearGeneratedFences()
    {
        foreach (GameObject fence in generatedFences)
        {
            if (fence != null)
            {
                if (Application.isPlaying)
                    Destroy(fence);
                else
                    DestroyImmediate(fence);
            }
        }
        generatedFences.Clear();
    }

    private List<Vector3> CalculateFencePositions()
    {
        List<Vector3> positions = new List<Vector3>();

        if (pathPoints.Count < 2 || spacing <= 0)
            return positions;

        float totalDistance = 0f;

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 startPoint = pathPoints[i];
            Vector3 endPoint = pathPoints[i + 1];
            Vector3 direction = (endPoint - startPoint).normalized;
            float segmentLength = Vector3.Distance(startPoint, endPoint);

            float currentDistance = 0f;

            // Place fences along this segment
            while (currentDistance < segmentLength)
            {
                Vector3 position = startPoint + direction * currentDistance;
                positions.Add(position);
                currentDistance += spacing;
            }
        }

        return positions;
    }

    private List<Vector3> CalculateFenceRotations()
    {
        List<Vector3> rotations = new List<Vector3>();

        if (pathPoints.Count < 2)
            return rotations;

        List<Vector3> fencePositions = CalculateFencePositions();

        for (int i = 0; i < fencePositions.Count; i++)
        {
            Vector3 direction = Vector3.forward;

            // Find which path segment this fence belongs to
            for (int j = 0; j < pathPoints.Count - 1; j++)
            {
                Vector3 segmentStart = pathPoints[j];
                Vector3 segmentEnd = pathPoints[j + 1];

                // Check if fence position is on this segment
                Vector3 segmentDir = (segmentEnd - segmentStart).normalized;
                float dot = Vector3.Dot((fencePositions[i] - segmentStart).normalized, segmentDir);

                if (dot > 0.9f) // Close enough to be on this segment
                {
                    direction = segmentDir;
                    break;
                }
            }

            rotations.Add(direction);
        }

        return rotations;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(FenceBoundarySystem))]
public class FenceBoundarySystemEditor : Editor
{
    private FenceBoundarySystem fenceSystem;
    private bool isDrawingPath = false;

    private void OnEnable()
    {
        fenceSystem = (FenceBoundarySystem)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Path Tools", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Drawing Path"))
        {
            isDrawingPath = true;
            Tools.current = Tool.None;
        }

        if (GUILayout.Button("Stop Drawing"))
        {
            isDrawingPath = false;
            Tools.current = Tool.Move;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Path"))
        {
            fenceSystem.ClearPath();
            EditorUtility.SetDirty(fenceSystem);
        }

        if (GUILayout.Button("Generate Fences"))
        {
            fenceSystem.GenerateFences();
            EditorUtility.SetDirty(fenceSystem);
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Clear Generated Fences"))
        {
            fenceSystem.ClearGeneratedFences();
            EditorUtility.SetDirty(fenceSystem);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Path Points: {fenceSystem.pathPoints.Count}");
        EditorGUILayout.LabelField($"Generated Fences: {fenceSystem.generatedFences.Count}");

        if (isDrawingPath)
        {
            EditorGUILayout.HelpBox("Click in the Scene view to add path points. Press ESC to stop drawing.", MessageType.Info);
        }
    }

    private void OnSceneGUI()
    {
        if (!isDrawingPath) return;

        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Vector3 mousePos = e.mousePosition;
            mousePos.y = Camera.current.pixelHeight - mousePos.y;

            Ray ray = Camera.current.ScreenPointToRay(mousePos);
            RaycastHit hit;

            Vector3 worldPos;
            if (Physics.Raycast(ray, out hit))
            {
                worldPos = hit.point;
            }
            else
            {
                // Use a default Y position if no hit
                worldPos = ray.origin + ray.direction * 10f;
                worldPos.y = 0f;
            }

            fenceSystem.AddPathPoint(worldPos);
            EditorUtility.SetDirty(fenceSystem);
            e.Use();
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            isDrawingPath = false;
            Tools.current = Tool.Move;
            e.Use();
        }
    }
}

#endif