using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MohiniFixController
{
    public static string Main()
    {
        var old = GameObject.Find("/Ghost_Mohini");
        if (old == null) return "not found";
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/812/Art/Characters/Ghost_Mohini/Definitive/Animations/AC_Mohini_Definitive.controller");
        var rootAnim = old.GetComponent<Animator>();
        rootAnim.runtimeAnimatorController = ctrl;
        rootAnim.applyRootMotion = false;
        // remove the child Animator so only the root (driven by GhostAnimatorBridge) animates
        var child = old.transform.Find("Mohini_Model");
        if (child != null)
        {
            var ca = child.GetComponent<Animator>();
            if (ca != null) Object.DestroyImmediate(ca);
        }
        EditorSceneManager.MarkSceneDirty(old.scene);
        EditorSceneManager.SaveScene(old.scene);
        return "controller=" + (ctrl != null ? ctrl.name : "NULL");
    }
}
