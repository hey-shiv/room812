using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MohiniHideEyes
{
    public static string Main()
    {
        string prefabPath = "Assets/812/Prefabs/Characters/PF_Mohini_Definitive.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        int n = 0;
        foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
            if (r.name.Contains("Eye")) { r.gameObject.SetActive(false); n++; }
        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);

        var inScene = GameObject.Find("/Ghost_Mohini/Mohini_Model");
        if (inScene != null)
        {
            foreach (var r in inScene.GetComponentsInChildren<Renderer>(true))
                if (r.name.Contains("Eye")) r.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(inScene.scene);
            EditorSceneManager.SaveScene(inScene.scene);
        }
        AssetDatabase.SaveAssets();
        return "disabled " + n + " eye renderers";
    }
}
