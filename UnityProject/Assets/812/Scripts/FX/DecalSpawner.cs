using System.Collections.Generic;
using UnityEngine;

namespace Room812.FX
{
    /// <summary>
    /// Lightweight transient decal pool for blood, wet footprints, frost and handprints.
    /// Uses URP/Unlit quads with per-instance materials and fades by shrinking + alpha.
    /// </summary>
    public class DecalSpawner : MonoBehaviour
    {
        class Decal
        {
            public GameObject go;
            public Material mat;
            public float life;
            public float maxLife;
            public Vector3 baseScale;
        }

        public struct DecalMark
        {
            public Vector3 position;
            public float time;
            public string type;
        }

        static DecalSpawner instance;
        public static readonly List<DecalMark> RecentMarks = new List<DecalMark>();
        readonly List<Decal> active = new List<Decal>();

        void Awake() { instance = this; RecentMarks.Clear(); }

        public static GameObject Spawn(Vector3 position, Vector3 normal, Color color, float size, float life)
        {
            if (instance == null)
            {
                var host = new GameObject("DecalSpawner");
                instance = host.AddComponent<DecalSpawner>();
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return null;
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Decal";
            go.transform.position = position + normal.normalized * 0.02f;
            go.transform.rotation = Quaternion.LookRotation(normal.normalized);
            go.transform.localScale = Vector3.one * size;
            go.GetComponent<Renderer>().material = mat;

            instance.active.Add(new Decal { go = go, mat = mat, life = life, maxLife = life, baseScale = Vector3.one * size });
            return go;
        }

        public static GameObject SpawnTextured(string materialName, Vector3 position, Vector3 normal, float size, float life)
        {
            if (instance == null)
            {
                var host = new GameObject("DecalSpawner");
                instance = host.AddComponent<DecalSpawner>();
            }
            var source = Resources.Load<Material>("Decals/" + materialName);
            if (source == null) return Spawn(position, normal, Color.white, size, life);
            var mat = new Material(source);
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Decal_" + materialName;
            go.transform.position = position + normal.normalized * 0.02f;
            go.transform.rotation = Quaternion.LookRotation(normal.normalized);
            go.transform.localScale = Vector3.one * size;
            go.GetComponent<Renderer>().material = mat;
            instance.active.Add(new Decal { go = go, mat = mat, life = life, maxLife = life, baseScale = Vector3.one * size });
            if (materialName.Contains("Blood") || materialName.Contains("Wet"))
                RecentMarks.Add(new DecalMark { position = position, time = Time.time, type = materialName });
            return go;
        }

        void Update()
        {
            for (int i = RecentMarks.Count - 1; i >= 0; i--)
                if (Time.time - RecentMarks[i].time > 120f) RecentMarks.RemoveAt(i);
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var d = active[i];
                d.life -= Time.deltaTime;
                if (d.go == null || d.life <= 0f)
                {
                    if (d.go != null) Object.Destroy(d.go);
                    if (d.mat != null) Object.Destroy(d.mat);
                    active.RemoveAt(i);
                    continue;
                }
                float t = d.life / d.maxLife;
                Color c = d.mat.GetColor("_BaseColor");
                c.a = Mathf.Clamp01(t * 1.5f);
                d.mat.SetColor("_BaseColor", c);
                d.go.transform.localScale = d.baseScale * (0.7f + 0.3f * t);
            }
        }
    }
}
