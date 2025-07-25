using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class MaterialShaderConverter : EditorWindow
{
    private Shader targetShader;
    private List<Material> selectedMaterials = new List<Material>();
    private Vector2 scrollPosition;
    private bool showPreview = false;

    // Common property mappings from legacy to URP
    private Dictionary<string, string> propertyMappings = new Dictionary<string, string>
    {
        // Texture mappings
        {"_MainTex", "_BaseMap"},
        {"_BumpMap", "_NormalMap"},
        {"_MetallicGlossMap", "_MetallicGlossMap"},
        {"_OcclusionMap", "_OcclusionMap"},
        {"_EmissionMap", "_EmissionMap"},
        {"_DetailAlbedoMap", "_DetailAlbedoMap"},
        {"_DetailNormalMap", "_DetailNormalMap"},
        {"_ParallaxMap", "_ParallaxMap"},

        // Color mappings
        {"_Color", "_BaseColor"},
        {"_TintColor", "_BaseColor"},
        {"_EmissionColor", "_EmissionColor"},

        // Float mappings
        {"_Metallic", "_Metallic"},
        {"_Glossiness", "_Smoothness"},
        {"_BumpScale", "_BumpScale"},
        {"_OcclusionStrength", "_OcclusionStrength"},
        {"_Cutoff", "_Cutoff"},
        {"_DetailNormalMapScale", "_DetailNormalMapScale"},
        {"_Parallax", "_Parallax"}
    };

    [MenuItem("Tools/Material Shader Converter")]
    public static void ShowWindow()
    {
        GetWindow<MaterialShaderConverter>("Material Shader Converter");
    }

    private void OnEnable()
    {
        RefreshSelectedMaterials();
    }

    private void OnGUI()
    {
        GUILayout.Label("Material Shader Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Target shader selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target Shader:", GUILayout.Width(100));
        targetShader = (Shader)EditorGUILayout.ObjectField(targetShader, typeof(Shader), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Material selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Selected Materials ({selectedMaterials.Count}):", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh Selection", GUILayout.Width(120)))
        {
            RefreshSelectedMaterials();
        }
        EditorGUILayout.EndHorizontal();

        // Materials list
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        foreach (var material in selectedMaterials)
        {
            if (material != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(material, typeof(Material), false);
                EditorGUILayout.LabelField($"Current: {material.shader.name}", GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Preview toggle
        showPreview = EditorGUILayout.Toggle("Show Property Preview", showPreview);

        if (showPreview && selectedMaterials.Count > 0 && targetShader != null)
        {
            ShowPropertyPreview();
        }

        EditorGUILayout.Space();

        // Convert button
        GUI.enabled = targetShader != null && selectedMaterials.Count > 0;
        if (GUILayout.Button("Convert Materials", GUILayout.Height(30)))
        {
            ConvertMaterials();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Instructions
        EditorGUILayout.HelpBox(
            "Instructions:\n" +
            "1. Select materials in Project view\n" +
            "2. Choose target shader (e.g., Universal Render Pipeline/Lit)\n" +
            "3. Click 'Convert Materials' to apply changes\n\n" +
            "The tool will automatically map common properties between shaders.",
            MessageType.Info
        );
    }

    private void RefreshSelectedMaterials()
    {
        selectedMaterials.Clear();

        foreach (var obj in Selection.objects)
        {
            if (obj is Material material)
            {
                selectedMaterials.Add(material);
            }
        }

        Repaint();
    }

    private void ShowPropertyPreview()
    {
        if (selectedMaterials.Count == 0) return;

        var firstMaterial = selectedMaterials[0];
        if (firstMaterial == null) return;

        EditorGUILayout.LabelField("Property Preview (First Material):", EditorStyles.boldLabel);

        var oldShader = firstMaterial.shader;
        var newShader = targetShader;

        EditorGUILayout.BeginVertical("box");

        // Show texture mappings
        EditorGUILayout.LabelField("Texture Mappings:", EditorStyles.miniBoldLabel);
        foreach (var mapping in propertyMappings)
        {
            if (firstMaterial.HasProperty(mapping.Key) && newShader.FindPropertyIndex(mapping.Value) != -1)
            {
                var texture = firstMaterial.GetTexture(mapping.Key);
                if (texture != null)
                {
                    EditorGUILayout.LabelField($"{mapping.Key} → {mapping.Value}: {texture.name}");
                }
            }
        }

        // Show color mappings
        EditorGUILayout.LabelField("Color Mappings:", EditorStyles.miniBoldLabel);
        foreach (var mapping in propertyMappings)
        {
            if (firstMaterial.HasProperty(mapping.Key) && newShader.FindPropertyIndex(mapping.Value) != -1)
            {
                var prop = MaterialEditor.GetMaterialProperty(new Material[] { firstMaterial }, mapping.Key);
                if (prop != null && prop.type == MaterialProperty.PropType.Color)
                {
                    var color = firstMaterial.GetColor(mapping.Key);
                    EditorGUILayout.LabelField($"{mapping.Key} → {mapping.Value}: {color}");
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void ConvertMaterials()
    {
        if (targetShader == null || selectedMaterials.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please select a target shader and materials to convert.", "OK");
            return;
        }

        int convertedCount = 0;

        // Record undo for all materials
        Undo.RecordObjects(selectedMaterials.ToArray(), "Convert Material Shaders");

        foreach (var material in selectedMaterials)
        {
            if (material == null) continue;

            try
            {
                ConvertMaterial(material);
                convertedCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to convert material {material.name}: {e.Message}");
            }
        }

        // Save assets
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Conversion Complete",
            $"Successfully converted {convertedCount} out of {selectedMaterials.Count} materials.", "OK");
    }

    private void ConvertMaterial(Material material)
    {
        var oldShader = material.shader;
        var newShader = targetShader;

        // Store old properties
        var oldProperties = new Dictionary<string, object>();
        var oldTextureOffsets = new Dictionary<string, Vector2>();
        var oldTextureScales = new Dictionary<string, Vector2>();

        // Collect existing properties
        for (int i = 0; i < oldShader.GetPropertyCount(); i++)
        {
            var propName = oldShader.GetPropertyName(i);
            var propType = oldShader.GetPropertyType(i);

            try
            {
                switch (propType)
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        if (material.HasProperty(propName))
                            oldProperties[propName] = material.GetColor(propName);
                        break;

                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        if (material.HasProperty(propName))
                            oldProperties[propName] = material.GetFloat(propName);
                        break;

                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        if (material.HasProperty(propName))
                        {
                            var texture = material.GetTexture(propName);
                            if (texture != null)
                            {
                                oldProperties[propName] = texture;
                                oldTextureOffsets[propName] = material.GetTextureOffset(propName);
                                oldTextureScales[propName] = material.GetTextureScale(propName);
                            }
                        }
                        break;

                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        if (material.HasProperty(propName))
                            oldProperties[propName] = material.GetVector(propName);
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not read property {propName} from material {material.name}: {e.Message}");
            }
        }

        // Change shader
        material.shader = newShader;

        // Apply mapped properties
        foreach (var mapping in propertyMappings)
        {
            var oldProp = mapping.Key;
            var newProp = mapping.Value;

            if (oldProperties.ContainsKey(oldProp) && material.HasProperty(newProp))
            {
                try
                {
                    var value = oldProperties[oldProp];

                    if (value is Color color)
                    {
                        material.SetColor(newProp, color);
                    }
                    else if (value is float floatValue)
                    {
                        material.SetFloat(newProp, floatValue);
                    }
                    else if (value is Texture texture)
                    {
                        material.SetTexture(newProp, texture);

                        // Apply tiling and offset
                        if (oldTextureOffsets.ContainsKey(oldProp))
                            material.SetTextureOffset(newProp, oldTextureOffsets[oldProp]);
                        if (oldTextureScales.ContainsKey(oldProp))
                            material.SetTextureScale(newProp, oldTextureScales[oldProp]);
                    }
                    else if (value is Vector4 vector)
                    {
                        material.SetVector(newProp, vector);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not apply property {newProp} to material {material.name}: {e.Message}");
                }
            }
        }

        // Apply direct matches (same property names)
        foreach (var oldProp in oldProperties)
        {
            if (material.HasProperty(oldProp.Key) && !propertyMappings.ContainsKey(oldProp.Key))
            {
                try
                {
                    var value = oldProp.Value;

                    if (value is Color color)
                    {
                        material.SetColor(oldProp.Key, color);
                    }
                    else if (value is float floatValue)
                    {
                        material.SetFloat(oldProp.Key, floatValue);
                    }
                    else if (value is Texture texture)
                    {
                        material.SetTexture(oldProp.Key, texture);

                        if (oldTextureOffsets.ContainsKey(oldProp.Key))
                            material.SetTextureOffset(oldProp.Key, oldTextureOffsets[oldProp.Key]);
                        if (oldTextureScales.ContainsKey(oldProp.Key))
                            material.SetTextureScale(oldProp.Key, oldTextureScales[oldProp.Key]);
                    }
                    else if (value is Vector4 vector)
                    {
                        material.SetVector(oldProp.Key, vector);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not apply direct property {oldProp.Key} to material {material.name}: {e.Message}");
                }
            }
        }

        EditorUtility.SetDirty(material);
    }

    private void OnSelectionChange()
    {
        RefreshSelectedMaterials();
    }
}