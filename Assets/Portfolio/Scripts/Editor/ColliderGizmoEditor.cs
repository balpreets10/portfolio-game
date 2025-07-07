using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool that draws gizmos for all colliders in the scene without requiring a scene GameObject.
/// Enable/disable via Tools > Collider Gizmos > Toggle Display
/// </summary>
[InitializeOnLoad]
public static class ColliderGizmoEditor
{
    public static bool showColliderGizmos = true;
    public static Color gizmoColor = Color.green;
    public static bool showWireframe = true;
    public static bool showSolid = false;
    public static float solidAlpha = 0.2f;

    // Collider type filters
    public static bool showBoxColliders = true;

    public static bool showSphereColliders = true;
    public static bool showCapsuleColliders = true;
    public static bool showMeshColliders = true;
    public static bool showTerrainColliders = true;
    public static bool showWheelColliders = true;

    // Performance settings
    public static int maxCollidersToShow = 1000;

    // EditorPrefs keys
    public const string PREF_SHOW_GIZMOS = "ColliderGizmo_ShowGizmos";

    private const string PREF_GIZMO_COLOR = "ColliderGizmo_Color";
    private const string PREF_SHOW_WIREFRAME = "ColliderGizmo_ShowWireframe";
    private const string PREF_SHOW_SOLID = "ColliderGizmo_ShowSolid";
    private const string PREF_SOLID_ALPHA = "ColliderGizmo_SolidAlpha";
    private const string PREF_SHOW_BOX = "ColliderGizmo_ShowBox";
    private const string PREF_SHOW_SPHERE = "ColliderGizmo_ShowSphere";
    private const string PREF_SHOW_CAPSULE = "ColliderGizmo_ShowCapsule";
    private const string PREF_SHOW_MESH = "ColliderGizmo_ShowMesh";
    private const string PREF_SHOW_TERRAIN = "ColliderGizmo_ShowTerrain";
    private const string PREF_SHOW_WHEEL = "ColliderGizmo_ShowWheel";
    private const string PREF_MAX_COLLIDERS = "ColliderGizmo_MaxColliders";

    static ColliderGizmoEditor()
    {
        LoadPreferences();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void LoadPreferences()
    {
        showColliderGizmos = EditorPrefs.GetBool(PREF_SHOW_GIZMOS, true);
        string colorString = EditorPrefs.GetString(PREF_GIZMO_COLOR, ColorUtility.ToHtmlStringRGB(Color.green));
        ColorUtility.TryParseHtmlString("#" + colorString, out gizmoColor);
        showWireframe = EditorPrefs.GetBool(PREF_SHOW_WIREFRAME, true);
        showSolid = EditorPrefs.GetBool(PREF_SHOW_SOLID, false);
        solidAlpha = EditorPrefs.GetFloat(PREF_SOLID_ALPHA, 0.2f);
        showBoxColliders = EditorPrefs.GetBool(PREF_SHOW_BOX, true);
        showSphereColliders = EditorPrefs.GetBool(PREF_SHOW_SPHERE, true);
        showCapsuleColliders = EditorPrefs.GetBool(PREF_SHOW_CAPSULE, true);
        showMeshColliders = EditorPrefs.GetBool(PREF_SHOW_MESH, true);
        showTerrainColliders = EditorPrefs.GetBool(PREF_SHOW_TERRAIN, true);
        showWheelColliders = EditorPrefs.GetBool(PREF_SHOW_WHEEL, true);
        maxCollidersToShow = EditorPrefs.GetInt(PREF_MAX_COLLIDERS, 1000);
    }

    public static void SavePreferences()
    {
        EditorPrefs.SetBool(PREF_SHOW_GIZMOS, showColliderGizmos);
        EditorPrefs.SetString(PREF_GIZMO_COLOR, ColorUtility.ToHtmlStringRGB(gizmoColor));
        EditorPrefs.SetBool(PREF_SHOW_WIREFRAME, showWireframe);
        EditorPrefs.SetBool(PREF_SHOW_SOLID, showSolid);
        EditorPrefs.SetFloat(PREF_SOLID_ALPHA, solidAlpha);
        EditorPrefs.SetBool(PREF_SHOW_BOX, showBoxColliders);
        EditorPrefs.SetBool(PREF_SHOW_SPHERE, showSphereColliders);
        EditorPrefs.SetBool(PREF_SHOW_CAPSULE, showCapsuleColliders);
        EditorPrefs.SetBool(PREF_SHOW_MESH, showMeshColliders);
        EditorPrefs.SetBool(PREF_SHOW_TERRAIN, showTerrainColliders);
        EditorPrefs.SetBool(PREF_SHOW_WHEEL, showWheelColliders);
        EditorPrefs.SetInt(PREF_MAX_COLLIDERS, maxCollidersToShow);
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!showColliderGizmos) return;

        DrawAllColliders();
    }

    private static void DrawAllColliders()
    {
        Collider[] colliders = Object.FindObjectsByType<Collider>(sortMode: FindObjectsSortMode.None);
        int collidersToShow = Mathf.Min(colliders.Length, maxCollidersToShow);

        for (int i = 0; i < collidersToShow; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled) continue;

            DrawColliderGizmo(col);
        }
    }

    private static void DrawColliderGizmo(Collider collider)
    {
        Color wireColor = gizmoColor;
        Color solidColor = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, solidAlpha);

        Handles.matrix = Matrix4x4.TRS(collider.transform.position, collider.transform.rotation, collider.transform.lossyScale);

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

        Handles.matrix = Matrix4x4.identity;
    }

    private static void DrawBoxCollider(BoxCollider box, Color wireColor, Color solidColor)
    {
        Vector3 size = box.size;
        Vector3 center = box.center;

        if (showWireframe)
        {
            Handles.color = wireColor;
            Handles.DrawWireCube(center, size);
        }

        if (showSolid)
        {
            // Draw solid box using multiple quads
            Handles.color = solidColor;
            DrawSolidBox(center, size);
        }
    }

    private static void DrawSphereCollider(SphereCollider sphere, Color wireColor, Color solidColor)
    {
        Vector3 center = sphere.center;
        float radius = sphere.radius;

        if (showWireframe)
        {
            Handles.color = wireColor;
            Handles.DrawWireDisc(center, Vector3.up, radius);
            Handles.DrawWireDisc(center, Vector3.right, radius);
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }

        if (showSolid)
        {
            Handles.color = solidColor;
            Handles.SphereHandleCap(0, center, Quaternion.identity, radius * 2, EventType.Repaint);
        }
    }

    private static void DrawCapsuleCollider(CapsuleCollider capsule, Color wireColor, Color solidColor)
    {
        Vector3 center = capsule.center;
        float radius = capsule.radius;
        float height = capsule.height;

        if (showWireframe)
        {
            Handles.color = wireColor;

            float cylinderHeight = Mathf.Max(0, height - radius * 2);
            Vector3 topCenter = center + Vector3.up * (cylinderHeight * 0.5f);
            Vector3 bottomCenter = center - Vector3.up * (cylinderHeight * 0.5f);

            // Draw circles
            Handles.DrawWireDisc(topCenter, Vector3.up, radius);
            Handles.DrawWireDisc(bottomCenter, Vector3.up, radius);

            // Draw connecting lines
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            foreach (Vector3 dir in directions)
            {
                Vector3 offset = dir * radius;
                Handles.DrawLine(topCenter + offset, bottomCenter + offset);
            }

            // Draw end hemispheres
            Handles.DrawWireArc(topCenter, Vector3.right, Vector3.forward, 180, radius);
            Handles.DrawWireArc(topCenter, Vector3.forward, Vector3.right, -180, radius);
            Handles.DrawWireArc(bottomCenter, Vector3.right, Vector3.forward, -180, radius);
            Handles.DrawWireArc(bottomCenter, Vector3.forward, Vector3.right, 180, radius);
        }

        if (showSolid)
        {
            Handles.color = solidColor;
            float cylinderHeight = Mathf.Max(0, height - radius * 2);
            Vector3 topCenter = center + Vector3.up * (cylinderHeight * 0.5f);
            Vector3 bottomCenter = center - Vector3.up * (cylinderHeight * 0.5f);

            // Draw cylinder body
            if (cylinderHeight > 0)
            {
                DrawSolidCylinder(center, radius, cylinderHeight);
            }

            // Draw sphere caps
            Handles.SphereHandleCap(0, topCenter, Quaternion.identity, radius * 2, EventType.Repaint);
            Handles.SphereHandleCap(0, bottomCenter, Quaternion.identity, radius * 2, EventType.Repaint);
        }
    }

    private static void DrawMeshCollider(MeshCollider mesh, Color wireColor, Color solidColor)
    {
        if (mesh.sharedMesh == null) return;

        if (showWireframe)
        {
            Handles.color = wireColor;
            DrawSolidMesh(mesh.sharedMesh);
        }

        if (showSolid)
        {
            // For mesh colliders, we'll draw using Graphics.DrawMesh in OnDrawGizmos
            // or use a simpler approach with GL calls
            Handles.color = solidColor;
            DrawSolidMesh(mesh.sharedMesh);
        }
    }

    private static void DrawTerrainCollider(TerrainCollider terrain, Color wireColor, Color solidColor)
    {
        if (terrain.terrainData == null) return;

        TerrainData data = terrain.terrainData;
        Vector3 size = data.size;
        Vector3 center = size * 0.5f;

        if (showWireframe)
        {
            Handles.color = wireColor;
            Handles.DrawWireCube(center, size);
        }

        if (showSolid)
        {
            Handles.color = solidColor;
            DrawSolidBox(center, size);
        }
    }

    private static void DrawWheelCollider(WheelCollider wheel, Color wireColor, Color solidColor)
    {
        Vector3 center = wheel.center;
        float radius = wheel.radius;

        if (showWireframe)
        {
            Handles.color = wireColor;
            Handles.DrawWireDisc(center, Vector3.right, radius);
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }

        if (showSolid)
        {
            Handles.color = solidColor;
            // Draw solid discs for wheel
            Handles.DrawSolidDisc(center, Vector3.right, radius);
            Handles.DrawSolidDisc(center, Vector3.forward, radius);
        }
    }

    // Helper method to draw solid box using quads
    private static void DrawSolidBox(Vector3 center, Vector3 size)
    {
        Vector3 halfSize = size * 0.5f;
        Vector3[] vertices = new Vector3[8];

        // Calculate all 8 vertices of the box
        vertices[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        vertices[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        vertices[2] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        vertices[3] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
        vertices[4] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
        vertices[5] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        vertices[6] = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
        vertices[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);

        // Draw all 6 faces using quads
        int[,] faces = new int[6, 4] {
            {0, 1, 2, 3}, // Front
            {5, 4, 7, 6}, // Back
            {4, 0, 3, 7}, // Left
            {1, 5, 6, 2}, // Right
            {3, 2, 6, 7}, // Top
            {4, 5, 1, 0}  // Bottom
        };

        for (int i = 0; i < 6; i++)
        {
            Handles.DrawSolidRectangleWithOutline(new Vector3[] {
                vertices[faces[i, 0]],
                vertices[faces[i, 1]],
                vertices[faces[i, 2]],
                vertices[faces[i, 3]]
            }, Handles.color, Color.clear);
        }
    }

    // Helper method to draw solid cylinder
    private static void DrawSolidCylinder(Vector3 center, float radius, float height)
    {
        int segments = 16;
        float halfHeight = height * 0.5f;

        // Draw cylinder sides
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * 2 * Mathf.PI;
            float angle2 = (float)(i + 1) / segments * 2 * Mathf.PI;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, -halfHeight, Mathf.Sin(angle1) * radius);
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, -halfHeight, Mathf.Sin(angle2) * radius);
            Vector3 p3 = center + new Vector3(Mathf.Cos(angle2) * radius, halfHeight, Mathf.Sin(angle2) * radius);
            Vector3 p4 = center + new Vector3(Mathf.Cos(angle1) * radius, halfHeight, Mathf.Sin(angle1) * radius);

            Handles.DrawSolidRectangleWithOutline(new Vector3[] { p1, p2, p3, p4 }, Handles.color, Color.clear);
        }

        // Draw top and bottom caps
        Handles.DrawSolidDisc(center + Vector3.up * halfHeight, Vector3.up, radius);
        Handles.DrawSolidDisc(center - Vector3.up * halfHeight, Vector3.down, radius);
    }

    // Helper method to draw solid mesh (simplified)
    private static void DrawSolidMesh(Mesh mesh)
    {
        // For performance reasons, we'll just draw a simplified version
        // You could implement full mesh rendering here if needed
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Draw only a subset of triangles to avoid performance issues
        int maxTriangles = Mathf.Min(triangles.Length / 3, 100);

        for (int i = 0; i < maxTriangles * 3; i += 3)
        {
            if (i + 2 < triangles.Length)
            {
                Vector3 v1 = vertices[triangles[i]];
                Vector3 v2 = vertices[triangles[i + 1]];
                Vector3 v3 = vertices[triangles[i + 2]];

                Handles.DrawSolidRectangleWithOutline(new Vector3[] { v1, v2, v3, v1 }, Handles.color, Color.clear);
            }
        }
    }

    [MenuItem("Tools/Collider Gizmos/Toggle Display")]
    private static void ToggleDisplay()
    {
        showColliderGizmos = !showColliderGizmos;
        SavePreferences();
        SceneView.RepaintAll();
    }

    [MenuItem("Tools/Collider Gizmos/Settings")]
    private static void OpenSettings()
    {
        ColliderGizmoSettingsWindow.ShowWindow();
    }
}

public class ColliderGizmoSettingsWindow : EditorWindow
{
    private static ColliderGizmoSettingsWindow window;

    public static void ShowWindow()
    {
        window = GetWindow<ColliderGizmoSettingsWindow>("Collider Gizmo Settings");
        window.minSize = new Vector2(300, 400);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Collider Gizmo Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        // Main settings
        ColliderGizmoEditor.showColliderGizmos = EditorGUILayout.Toggle("Show Collider Gizmos", ColliderGizmoEditor.showColliderGizmos);
        ColliderGizmoEditor.gizmoColor = EditorGUILayout.ColorField("Gizmo Color", ColliderGizmoEditor.gizmoColor);

        GUILayout.Space(10);
        GUILayout.Label("Rendering Options", EditorStyles.boldLabel);
        ColliderGizmoEditor.showWireframe = EditorGUILayout.Toggle("Show Wireframe", ColliderGizmoEditor.showWireframe);
        ColliderGizmoEditor.showSolid = EditorGUILayout.Toggle("Show Solid", ColliderGizmoEditor.showSolid);
        ColliderGizmoEditor.solidAlpha = EditorGUILayout.Slider("Solid Alpha", ColliderGizmoEditor.solidAlpha, 0f, 1f);

        GUILayout.Space(10);
        GUILayout.Label("Collider Types", EditorStyles.boldLabel);
        ColliderGizmoEditor.showBoxColliders = EditorGUILayout.Toggle("Box Colliders", ColliderGizmoEditor.showBoxColliders);
        ColliderGizmoEditor.showSphereColliders = EditorGUILayout.Toggle("Sphere Colliders", ColliderGizmoEditor.showSphereColliders);
        ColliderGizmoEditor.showCapsuleColliders = EditorGUILayout.Toggle("Capsule Colliders", ColliderGizmoEditor.showCapsuleColliders);
        ColliderGizmoEditor.showMeshColliders = EditorGUILayout.Toggle("Mesh Colliders", ColliderGizmoEditor.showMeshColliders);
        ColliderGizmoEditor.showTerrainColliders = EditorGUILayout.Toggle("Terrain Colliders", ColliderGizmoEditor.showTerrainColliders);
        ColliderGizmoEditor.showWheelColliders = EditorGUILayout.Toggle("Wheel Colliders", ColliderGizmoEditor.showWheelColliders);

        GUILayout.Space(10);
        GUILayout.Label("Performance", EditorStyles.boldLabel);
        ColliderGizmoEditor.maxCollidersToShow = EditorGUILayout.IntSlider("Max Colliders", ColliderGizmoEditor.maxCollidersToShow, 100, 5000);

        if (EditorGUI.EndChangeCheck())
        {
            ColliderGizmoEditor.SavePreferences();
            SceneView.RepaintAll();
        }
    }
}