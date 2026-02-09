using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TMPFontSelectiveSceneChanger : EditorWindow
{
    private TMP_FontAsset newFont;
    private bool includeInactive = true;
    private bool applyToPrefabs = true;

    private Vector2 scrollPos;
    private List<string> scenePaths = new();
    private List<bool> sceneSelections = new();

    [MenuItem("Tools/TextMeshPro/Selective Font Changer")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontSelectiveSceneChanger>("TMP Font Scene Selector");
    }

    void OnEnable()
    {
        RefreshSceneList();
    }

    void OnGUI()
    {
        GUILayout.Label("Change TextMeshPro Fonts (Selective Scenes)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New TMP Font Asset", newFont, typeof(TMP_FontAsset), false);
        includeInactive = EditorGUILayout.Toggle("Include Inactive Objects", includeInactive);
        applyToPrefabs = EditorGUILayout.Toggle("Also Apply to All Prefabs", applyToPrefabs);

        EditorGUILayout.Space();
        GUILayout.Label("Select Scenes to Modify:", EditorStyles.boldLabel);

        if (GUILayout.Button("🔄 Refresh Scene List"))
        {
            RefreshSceneList();
        }

        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
        for (int i = 0; i < scenePaths.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            sceneSelections[i] = EditorGUILayout.Toggle(sceneSelections[i], GUILayout.Width(20));
            EditorGUILayout.LabelField(scenePaths[i]);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("✅ Apply Font to Selected Scenes"))
        {
            if (newFont == null)
            {
                EditorUtility.DisplayDialog("Missing Font", "Please assign a TMP Font Asset before applying.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog(
                "Confirm Font Change",
                "This will modify the selected scenes (and optionally all prefabs) in your project.\n\nMake sure you have backups or version control.",
                "Proceed", "Cancel"))
            {
                ApplyFontChange();
            }
        }
    }

    void RefreshSceneList()
    {
        scenePaths.Clear();
        sceneSelections.Clear();

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            scenePaths.Add(path);
            sceneSelections.Add(false);
        }
    }

    void ApplyFontChange()
    {
        int totalCount = 0;

        // Apply to selected scenes
        for (int i = 0; i < scenePaths.Count; i++)
        {
            if (!sceneSelections[i]) continue;

            string path = scenePaths[i];
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int changed = ChangeFontsInScene(scene);
            totalCount += changed;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Changed {changed} TMP components in scene: {path}");
        }

        // Optionally apply to prefabs
        if (applyToPrefabs)
        {
            totalCount += ChangeFontsInAllPrefabs();
        }

        EditorUtility.DisplayDialog("Font Update Complete", $"Updated {totalCount} TextMeshPro objects.", "OK");
        Debug.Log($"TMPFontSelectiveSceneChanger: Updated {totalCount} TMP components to '{newFont.name}'.");
    }
    int ChangeFontsInScene(Scene scene)
    {
        int count = 0;
        TMP_Text[] allTextObjects = Object.FindObjectsOfType<TMP_Text>(includeInactive);

        foreach (TMP_Text tmp in allTextObjects)
        {
            Undo.RecordObject(tmp, "Change TMP Font");
            tmp.font = newFont;
            tmp.ForceMeshUpdate();
            EditorUtility.SetDirty(tmp);
            count++;
        }
        return count;
    }


    int ChangeFontsInAllPrefabs()
    {
        int count = 0;
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            TMP_Text[] textObjects = prefab.GetComponentsInChildren<TMP_Text>(true);
            bool modified = false;

            foreach (TMP_Text tmp in textObjects)
            {
                Undo.RecordObject(tmp, "Change TMP Font");
                tmp.font = newFont;
                EditorUtility.SetDirty(tmp);
                modified = true;
                count++;
            }

            if (modified)
                PrefabUtility.SavePrefabAsset(prefab);
        }

        return count;
    }
}
