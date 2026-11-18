using System;
using System.Collections.Generic;
using UnityEngine;

namespace Room812.FX
{
    /// <summary>
    /// Tracks injuries on an investigator. Each wound can bleed, feed the blood trail and
    /// lower sanity. Injury marks are spawned as transient decals on the hit surface.
    /// </summary>
    public class DamageSystem : MonoBehaviour
    {
        [Serializable]
        public class Injury
        {
            public string id;
            public float severity = 1f;
            public float bleedRate = 0.2f;
            public float age;
        }

        public float maxHealth = 100f;
        public float health = 100f;
        public Color bloodColor = new Color(0.35f, 0.02f, 0.02f, 0.85f);
        public List<Injury> injuries = new List<Injury>();

        public event Action<Injury> OnInjured;
        public bool IsBleeding { get { return injuries.Count > 0; } }

        public float BleedRate
        {
            get
            {
                float total = 0f;
                foreach (var injury in injuries) total += injury.bleedRate;
                return total;
            }
        }

        public void ApplyInjury(Vector3 worldPosition, Vector3 normal, float severity)
        {
            var injury = new Injury
            {
                id = "wound_" + injuries.Count,
                severity = Mathf.Clamp01(severity),
                bleedRate = 0.15f * Mathf.Clamp01(severity),
                age = 0f
            };
            injuries.Add(injury);
            health = Mathf.Max(0f, health - 18f * injury.severity);
            DecalSpawner.SpawnTextured("MAT_Decal_BloodSplatter", worldPosition, normal, 0.35f + 0.2f * injury.severity, 120f);
            var sanity = Room812.Core.SanityManager.Instance;
            if (sanity != null) sanity.Drain(0.18f * injury.severity);
            OnInjured?.Invoke(injury);
        }

        public void Heal(float amount)
        {
            health = Mathf.Min(maxHealth, health + amount);
            if (health >= maxHealth * 0.6f && injuries.Count > 0)
                injuries.RemoveAt(injuries.Count - 1);
        }

        void Update()
        {
            for (int i = 0; i < injuries.Count; i++) injuries[i].age += Time.deltaTime;
        }
    }
}
