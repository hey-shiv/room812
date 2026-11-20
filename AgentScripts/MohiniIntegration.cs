using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class MohiniIntegration
{
    const string SRC_TEX = "/Users/shivashant/projects/room812/assets/mohini/textures/";
    const string SRC_FBX = "/Users/shivashant/projects/room812/assets/mohini/SK_Mohini.fbx";
    const string BASE = "Assets/812/Art/Characters/Ghost_Mohini/Definitive";
    const string TEXDIR = BASE + "/Textures";
    const string MATDIR = BASE + "/Materials";
    const string ANIMDIR = BASE + "/Animations";

    public static string Main()
    {
        var log = new List<string>();
        Ensure("Assets/812/Art/Characters/Ghost_Mohini");
        Ensure(BASE); Ensure(TEXDIR); Ensure(MATDIR); Ensure(ANIMDIR);

        // ---- import textures ----
        string[] maps = {
            "Face_Albedo","Face_Normal","Face_ORM",
            "Skin_Albedo","Skin_Normal","Skin_ORM",
            "Shroud_Albedo","Shroud_Normal","Shroud_ORM",
            "Hair_Albedo","Hair_Normal","Hair_ORM"
        };
        var tex = new Dictionary<string, Texture2D>();
        foreach (var m in maps)
        {
            string src = SRC_TEX + "TEX_Mohini_" + m + ".png";
            string dst = TEXDIR + "/TEX_Mohini_" + m + ".png";
            if (!File.Exists(src)) { log.Add("MISSING " + src); continue; }
            File.Copy(src, dst, true);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            var ti = AssetImporter.GetAtPath(dst) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = m.Contains("Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !m.Contains("Normal") && !m.Contains("ORM");
                ti.alphaIsTransparency = m.Contains("Face_Albedo");
                ti.mipmapEnabled = true;
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.maxTextureSize = m.Contains("Face") ? 1024 : 1024;
                ti.SaveAndReimport();
            }
            tex[m] = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
        }
        log.Add("textures=" + tex.Count);

        // ---- import FBX ----
        string fbxDst = BASE + "/SK_Mohini_Definitive.fbx";
        File.Copy(SRC_FBX, fbxDst, true);
        AssetDatabase.ImportAsset(fbxDst, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        var mi = AssetImporter.GetAtPath(fbxDst) as ModelImporter;
        if (mi != null)
        {
            mi.globalScale = 1f;
            mi.importAnimation = true;
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importCameras = false; mi.importLights = false;
            mi.importBlendShapes = false;
            mi.SaveAndReimport();
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxDst);
        log.Add("model=" + (model != null));

        // ---- materials ----
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");
        Material Mat(string name, string set, Color tint, float smooth, float bump, bool alpha=false)
        {
            string p = MATDIR + "/MAT_Mohini_" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, p); }
            mat.shader = lit;
            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Smoothness", smooth);
            mat.SetFloat("_BumpScale", bump);
            if (tex.ContainsKey(set + "_Albedo")) mat.SetTexture("_BaseMap", tex[set + "_Albedo"]);
            if (tex.ContainsKey(set + "_Normal")) { mat.SetTexture("_BumpMap", tex[set + "_Normal"]); mat.EnableKeyword("_NORMALMAP"); }
            if (tex.ContainsKey(set + "_ORM"))
            {
                mat.SetTexture("_OcclusionMap", tex[set + "_ORM"]); mat.EnableKeyword("_OCCLUSIONMAP");
                mat.SetFloat("_OcclusionStrength", 0.7f);
            }
            // procedural ring meshes are wound inward -> render both sides
            mat.SetFloat("_Cull", 0);
            if (alpha)
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.42f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = 2450;
                mat.SetOverrideTag("RenderType", "TransparentCutout");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }
        var faceMat = Mat("Face",  "Face",  Color.white, 0.30f, 0.55f, true);
        var skinMat = Mat("Skin",  "Skin",  new Color(0.72f,0.70f,0.68f), 0.26f, 0.45f);
        var shroudMat = Mat("Shroud","Shroud",new Color(0.78f,0.78f,0.76f), 0.12f, 0.55f);
        var hairMat = Mat("Hair",  "Hair",  new Color(0.12f,0.12f,0.13f), 0.08f, 0.25f);
        var eyeMat = new Material(lit) { name = "MAT_Mohini_Eye" };
        eyeMat.SetColor("_BaseColor", new Color(0.005f,0.005f,0.006f,1));
        eyeMat.SetFloat("_Smoothness", 0.35f);
        string eyePath = MATDIR + "/MAT_Mohini_Eye.mat";
        var ex = AssetDatabase.LoadAssetAtPath<Material>(eyePath);
        if (ex != null) AssetDatabase.DeleteAsset(eyePath);
        AssetDatabase.CreateAsset(eyeMat, eyePath);
        log.Add("materials=5");

        // ---- instantiate & assign ----
        var root = (GameObject)PrefabUtility.InstantiatePrefab(model);
        root.name = "PF_Mohini_Definitive";
        int assigned = 0;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            string n = r.gameObject.name;
            Material m = null;
            if (n.Contains("Face")) m = faceMat;
            else if (n.Contains("Dress") || n.Contains("Shroud")) m = shroudMat;
            else if (n.Contains("Hair")) m = hairMat;
            else if (n.Contains("Eye")) m = eyeMat;
            else m = skinMat;
            r.sharedMaterials = new[] { m };
            assigned++;
        }
        log.Add("renderers=" + assigned);

        // ---- LODGroup ----
        var lodGroup = root.AddComponent<LODGroup>();
        var all = root.GetComponentsInChildren<Renderer>(true);
        LOD[] lods = new LOD[3];
        lods[0] = new LOD(0.55f, all.Where(x => !x.name.Contains("_LOD")).ToArray());
        lods[1] = new LOD(0.28f, all.Where(x => x.name.Contains("_LOD1")).ToArray());
        lods[2] = new LOD(0.06f, all.Where(x => x.name.Contains("_LOD2")).ToArray());
        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();

        // ---- collider ----
        var cap = root.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0, 0.85f, 0);
        cap.radius = 0.28f; cap.height = 1.7f;

        // ---- animator controller ----
        var clips = AssetDatabase.LoadAllAssetsAtPath(fbxDst).OfType<AnimationClip>()
            .Where(c => c.name.Contains("ANIM_Mohini")).ToList();
        log.Add("clips=" + clips.Count);
        string ctrlPath = ANIMDIR + "/AC_Mohini_Definitive.controller";
        var old = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (old != null) AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("GhostState", AnimatorControllerParameterType.Int);
        var sm = ctrl.layers[0].stateMachine;
        // map GhostState enum -> clips
        var map = new (int state, string clip)[] {
            (0,"Idle"),(1,"Walk"),(2,"Search"),(3,"Stalk"),(4,"Manifest"),(5,"Disappear"),
            (6,"Observe"),(7,"HeadTurn"),(8,"Sudden"),(9,"Attack")
        };
        AnimatorState defaultState = null;
        foreach (var kv in map)
        {
            var clip = clips.FirstOrDefault(c => c.name.Contains("ANIM_Mohini_" + kv.clip));
            if (clip == null) { log.Add("NO CLIP " + kv.clip); continue; }
            var st = sm.AddState(kv.clip);
            st.motion = clip;
            st.speed = 1f;
            var tr = (kv.state == 0) ? null : sm.AddAnyStateTransition(st);
            if (tr != null)
            {
                tr.hasExitTime = false; tr.duration = (kv.clip=="Sudden"||kv.clip=="Attack") ? 0.06f : 0.35f;
                tr.AddCondition(AnimatorConditionMode.Equals, kv.state, "GhostState");
            }
            else defaultState = st;
        }
        if (defaultState != null) sm.defaultState = defaultState;
        var animator = root.GetComponent<Animator>();
        if (animator == null) animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = ctrl;
        animator.applyRootMotion = false;

        // ---- save prefab ----
        string prefabPath = "Assets/812/Prefabs/Characters/PF_Mohini_Definitive.prefab";
        var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        log.Add("prefab=" + (saved != null ? prefabPath : "FAILED"));
        return string.Join("\n", log);
    }

    static void Ensure(string p)
    {
        if (AssetDatabase.IsValidFolder(p)) return;
        string parent = p.Substring(0, p.LastIndexOf('/'));
        string name = p.Substring(p.LastIndexOf('/') + 1);
        Ensure(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
