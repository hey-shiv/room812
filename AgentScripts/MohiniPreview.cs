using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MohiniPreview
{
    public static string Main()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/812/Prefabs/Characters/PF_Mohini_Definitive.prefab");
        var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        m.transform.position = Vector3.zero;
        m.transform.rotation = Quaternion.identity;

        var camGo = new GameObject("PREVIEW_CAM");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.012f, 0.013f, 0.015f, 1);
        cam.fieldOfView = 42;
        camGo.transform.position = new Vector3(0f, 1.52f, 2.1f);
        camGo.transform.LookAt(new Vector3(0f, 1.50f, 0f));
        camGo.tag = "MainCamera";

        var lightGo = new GameObject("FLASHLIGHT");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Spot;
        light.intensity = 14f;
        light.range = 14f;
        light.spotAngle = 38f;
        light.color = new Color(1f, 0.96f, 0.9f);
        light.shadows = LightShadows.Soft;
        lightGo.transform.position = new Vector3(0.12f, 1.62f, 1.7f);
        lightGo.transform.LookAt(new Vector3(0f, 1.5f, 0f));

        var fill = new GameObject("FILL");
        var fl = fill.AddComponent<Light>();
        fl.type = LightType.Point; fl.intensity = 1.2f; fl.range = 8f;
        fl.color = new Color(0.35f, 0.42f, 0.6f);
        fill.transform.position = new Vector3(-1.4f, 1.8f, 1.2f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.025f, 0.028f, 0.035f);
        RenderSettings.fog = false;

        string path = "Assets/812/Scenes/Mohini_Preview.unity";
        EditorSceneManager.SaveScene(scene, path);
        return "saved " + path;
    }
}
