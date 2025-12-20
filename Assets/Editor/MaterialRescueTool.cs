using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class MaterialRescueTool : EditorWindow
{
    [MenuItem("🚑 Rescue Tools/RESET PROJECT TO STANDARD (Fix Purple)")]
    public static void ResetToStandard()
    {
        if (!EditorUtility.DisplayDialog("Rescue Project?", 
            "This will FORCE your graphics settings to Built-in and reset ALL materials to Standard shader. \n\nUse this if your project is purple/broken.\n\nContinue?", "Yes, Fix It", "Cancel"))
        {
            return;
        }

        Debug.Log("🚑 STARTING RESCUE OPERATION...");

        // 1. Force Graphics Settings to Built-in (Null)
        GraphicsSettings.renderPipelineAsset = null;
        QualitySettings.renderPipeline = null;
        Debug.Log("✅ Render Pipeline turned OFF (Set to Built-in).");

        // 2. Find ALL Materials in the project
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null)
            {
                // Only reset if it's using a URP or Error shader
                if (mat.shader.name.Contains("Universal Render Pipeline") || 
                    mat.shader.name.Contains("Error") || 
                    mat.shader.name.Contains("Internal"))
                {
                    // Reset to Standard
                    mat.shader = Shader.Find("Standard");
                    count++;
                }
            }
        }

        Debug.Log($"✅ Reset {count} broken materials to 'Standard' shader.");
        Debug.Log("🚑 RESCUE COMPLETE! Objects should be visible (White/Grey or Textured).");
        
        // Force refresh
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
