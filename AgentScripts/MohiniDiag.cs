using System.Linq;
using UnityEngine;
using UnityEditor;

public static class MohiniDiag
{
    public static string Main()
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/812/Prefabs/Characters/PF_Mohini_Definitive.prefab");
        var s = "";
        foreach (var r in p.GetComponentsInChildren<Renderer>(true))
            s += r.name + " enabled=" + r.enabled + " mat=" + (r.sharedMaterial ? r.sharedMaterial.name : "NULL") + "\n";
        var lg = p.GetComponent<LODGroup>();
        if (lg != null)
        {
            int i = 0;
            foreach (var lod in lg.GetLODs())
            {
                s += "LOD" + i + " screen=" + lod.screenRelativeTransitionHeight + " renderers=" + string.Join(",", lod.renderers.Select(x => x ? x.name : "NULL")) + "\n";
                i++;
            }
        }
        return s;
    }
}
