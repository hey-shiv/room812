using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MohiniSceneSwap
{
    public static string Main()
    {
        var old = GameObject.Find("/Ghost_Mohini");
        if (old == null) return "Ghost_Mohini not found";
        string comps = string.Join(",", old.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name));

        // remove old visual children (everything except logic components on root)
        foreach (Transform child in old.transform.Cast<Transform>().ToArray())
            Object.DestroyImmediate(child.gameObject);
        foreach (var r in old.GetComponentsInChildren<Renderer>(true))
            Object.DestroyImmediate(r);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/812/Prefabs/Characters/PF_Mohini_Definitive.prefab");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        model.name = "Mohini_Model";
        model.transform.SetParent(old.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        // avoid double colliders / animators on the logic root
        var cap = model.GetComponent<CapsuleCollider>(); if (cap) Object.DestroyImmediate(cap);
        if (old.GetComponent<Animator>() == null)
        {
            var a = model.GetComponent<Animator>();
            if (a) old.AddComponent<Animator>().runtimeAnimatorController = a.runtimeAnimatorController;
        }

        EditorSceneManager.MarkSceneDirty(old.scene);
        EditorSceneManager.SaveScene(old.scene);
        return "swapped. old components: " + comps;
    }
}
