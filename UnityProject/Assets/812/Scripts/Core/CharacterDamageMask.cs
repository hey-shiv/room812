using UnityEngine;
using Room812.FX;

namespace Room812.Core
{
    /// <summary>
    /// Visual injury state on the investigator: clothing/skin darken and redden as wounds
    /// accumulate. Uses MaterialPropertyBlocks so no shared material assets are mutated.
    /// </summary>
    public class CharacterDamageMask : MonoBehaviour
    {
        public Renderer[] bodyRenderers;
        public Color bloodTint = new Color(0.42f, 0.05f, 0.05f, 1f);
        public float maxTint = 0.65f;

        DamageSystem damage;
        MaterialPropertyBlock block;
        Color[] baseColors;

        void Start()
        {
            damage = GetComponent<DamageSystem>();
            if (bodyRenderers == null || bodyRenderers.Length == 0)
            {
                var model = transform.Find("PlayerModel");
                if (model != null) bodyRenderers = model.GetComponentsInChildren<Renderer>(true);
            }
            baseColors = new Color[bodyRenderers == null ? 0 : bodyRenderers.Length];
            if (bodyRenderers != null)
                for (int i = 0; i < bodyRenderers.Length; i++)
                {
                    var m = bodyRenderers[i].sharedMaterial;
                    baseColors[i] = m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                }
            block = new MaterialPropertyBlock();
            if (damage != null) damage.OnInjured += OnInjured;
            Apply();
        }

        void OnDestroy()
        {
            if (damage != null) damage.OnInjured -= OnInjured;
        }

        void OnInjured(DamageSystem.Injury injury) { Apply(); }

        void Apply()
        {
            if (damage == null || bodyRenderers == null) return;
            float t = Mathf.Clamp01(damage.injuries.Count * 0.3f) * maxTint;
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] == null) continue;
                bodyRenderers[i].GetPropertyBlock(block);
                block.SetColor("_BaseColor", Color.Lerp(baseColors[i], bloodTint, t));
                bodyRenderers[i].SetPropertyBlock(block);
            }
        }
    }
}
