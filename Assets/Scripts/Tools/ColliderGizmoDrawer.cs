using UnityEngine;

/// <summary>
/// Draws gizmos for all colliders in the scene, even when not selected.
/// Attach this script to any GameObject in your scene to visualize all colliders.
/// </summary>
public class ColliderGizmoDrawer : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [SerializeField] private bool showColliderGizmos = true;

    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private bool showWireframe = true;
    [SerializeField] private bool showSolid = false;
    [SerializeField] private float solidAlpha = 0.2f;

    [Header("Collider Type Filters")]
    [SerializeField] private bool showBoxColliders = true;

    [SerializeField] private bool showSphereColliders = true;
    [SerializeField] private bool showCapsuleColliders = true;
    [SerializeField] private bool showMeshColliders = true;
    [SerializeField] private bool showTerrainColliders = true;
    [SerializeField] private bool showWheelColliders = true;

    [Header("Performance")]
    [SerializeField] private bool onlyShowInPlayMode = false;

    [SerializeField] private bool onlyShowInEditMode = false;
    [SerializeField] private int maxCollidersToShow = 200;

    private void OnDrawGizmos()
    {
        if (!showColliderGizmos) return;

        // Check play mode restrictions
        if (onlyShowInPlayMode && !Application.isPlaying) return;
        if (onlyShowInEditMode && Application.isPlaying) return;

        DrawAllColliders();
    }

    private void DrawAllColliders()
    {
        // Find all colliders in the scene
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);

        // Limit number of colliders for performance
        int collidersToShow = Mathf.Min(colliders.Length, maxCollidersToShow);

        for (int i = 0; i < collidersToShow; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled) continue;

            DrawColliderGizmo(col);
        }
    }

    private void DrawColliderGizmo(Collider collider)
    {
        // Set gizmo color
        Color wireColor = gizmoColor;
        Color solidColor = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, solidAlpha);

        // Store original gizmo matrix
        Matrix4x4 originalMatrix = Gizmos.matrix;

        // Set transform matrix for the collider
        Gizmos.matrix = Matrix4x4.TRS(collider.transform.position, collider.transform.rotation, collider.transform.lossyScale);

        // Draw based on collider type
        switch (collider)
        {
            case BoxCollider box when showBoxColliders:
                DrawBoxCollider(box, wireColor, solidColor);
                break;

            case SphereCollider sphere when showSphereColliders:
                DrawSphereCollider(sphere, wireColor, solidColor);
                break;

            case CapsuleCollider capsule when showCapsuleColliders:
                DrawCapsuleCollider(capsule, wireColor, solidColor);
                break;

            case MeshCollider mesh when showMeshColliders:
                DrawMeshCollider(mesh, wireColor, solidColor);
                break;

            case TerrainCollider terrain when showTerrainColliders:
                DrawTerrainCollider(terrain, wireColor, solidColor);
                break;

            case WheelCollider wheel when showWheelColliders:
                DrawWheelCollider(wheel, wireColor, solidColor);
                break;
        }

        // Restore original gizmo matrix
        Gizmos.matrix = originalMatrix;
    }

    private void DrawBoxCollider(BoxCollider box, Color wireColor, Color solidColor)
    {
        Vector3 size = box.size;
        Vector3 center = box.center;

        if (showWireframe)
        {
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(center, size);
        }

        if (showSolid)
        {
            Gizmos.color = solidColor;
            Gizmos.DrawCube(center, size);
        }
    }

    private void DrawSphereCollider(SphereCollider sphere, Color wireColor, Color solidColor)
    {
        Vector3 center = sphere.center;
        float radius = sphere.radius;

        if (showWireframe)
        {
            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(center, radius);
        }

        if (showSolid)
        {
            Gizmos.color = solidColor;
            Gizmos.DrawSphere(center, radius);
        }
    }

    private void DrawCapsuleCollider(CapsuleCollider capsule, Color wireColor, Color solidColor)
    {
        Vector3 center = capsule.center;
        float radius = capsule.radius;
        float height = capsule.height;

        // Draw capsule as combination of cylinder and spheres
        if (showWireframe)
        {
            Gizmos.color = wireColor;

            // Draw cylinder part
            float cylinderHeight = Mathf.Max(0, height - radius * 2);
            Vector3 topCenter = center + Vector3.up * (cylinderHeight * 0.5f);
            Vector3 bottomCenter = center - Vector3.up * (cylinderHeight * 0.5f);

            // Draw top and bottom circles
            DrawWireCircle(topCenter, radius, Vector3.up);
            DrawWireCircle(bottomCenter, radius, Vector3.up);

            // Draw connecting lines
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            foreach (Vector3 dir in directions)
            {
                Vector3 offset = dir * radius;
                Gizmos.DrawLine(topCenter + offset, bottomCenter + offset);
            }

            // Draw end spheres
            Gizmos.DrawWireSphere(topCenter, radius);
            Gizmos.DrawWireSphere(bottomCenter, radius);
        }

        if (showSolid)
        {
            Gizmos.color = solidColor;
            // Approximate solid capsule with spheres
            float cylinderHeight = Mathf.Max(0, height - radius * 2);
            Vector3 topCenter = center + Vector3.up * (cylinderHeight * 0.5f);
            Vector3 bottomCenter = center - Vector3.up * (cylinderHeight * 0.5f);

            Gizmos.DrawSphere(topCenter, radius);
            Gizmos.DrawSphere(bottomCenter, radius);
        }
    }

    private void DrawMeshCollider(MeshCollider mesh, Color wireColor, Color solidColor)
    {
        if (mesh.sharedMesh == null) return;

        if (showWireframe)
        {
            Gizmos.color = wireColor;
            Gizmos.DrawWireMesh(mesh.sharedMesh);
        }

        if (showSolid)
        {
            Gizmos.color = solidColor;
            Gizmos.DrawMesh(mesh.sharedMesh);
        }
    }

    private void DrawTerrainCollider(TerrainCollider terrain, Color wireColor, Color solidColor)
    {
        if (terrain.terrainData == null) return;

        TerrainData data = terrain.terrainData;
        Vector3 size = data.size;
        Vector3 center = size * 0.5f;

        if (showWireframe)
        {
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(center, size);
        }

        if (showSolid)
        {
            Gizmos.color = solidColor;
            Gizmos.DrawCube(center, size);
        }
    }

    private void DrawWheelCollider(WheelCollider wheel, Color wireColor, Color solidColor)
    {
        Vector3 center = wheel.center;
        float radius = wheel.radius;

        if (showWireframe)
        {
            Gizmos.color = wireColor;
            // Draw wheel as circle
            DrawWireCircle(center, radius, Vector3.right);
            DrawWireCircle(center, radius, Vector3.forward);
        }

        if (showSolid)
        {
            Gizmos.color = solidColor;
            Gizmos.DrawSphere(center, radius);
        }
    }

    private void DrawWireCircle(Vector3 center, float radius, Vector3 normal)
    {
        Vector3 forward = Vector3.Slerp(Vector3.forward, -normal, 0.5f).normalized;
        Vector3 right = Vector3.Cross(normal, forward).normalized;

        Vector3 prevPoint = center + forward * radius;

        for (int i = 1; i <= 32; i++)
        {
            float angle = i * Mathf.PI * 2f / 32f;
            Vector3 point = center + (forward * Mathf.Cos(angle) + right * Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }

#if UNITY_EDITOR

    [UnityEditor.MenuItem("Tools/Toggle Collider Gizmos")]
    private static void ToggleColliderGizmos()
    {
        ColliderGizmoDrawer drawer = FindFirstObjectByType<ColliderGizmoDrawer>();
        if (drawer != null)
        {
            drawer.showColliderGizmos = !drawer.showColliderGizmos;
            UnityEditor.EditorUtility.SetDirty(drawer);
        }
        else
        {
            GameObject go = new GameObject("ColliderGizmoDrawer");
            go.AddComponent<ColliderGizmoDrawer>();
            UnityEditor.Selection.activeGameObject = go;
        }
    }

#endif
}