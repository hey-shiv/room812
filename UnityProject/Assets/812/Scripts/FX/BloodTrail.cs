using UnityEngine;

namespace Room812.FX
{
    /// <summary>
    /// Leaves intermittent blood droplets while the investigator is injured, and dark wet
    /// footprints when walking through water. The ghost AI can later read these decals.
    /// </summary>
    [RequireComponent(typeof(DamageSystem))]
    public class BloodTrail : MonoBehaviour
    {
        public float dropInterval = 0.65f;
        public float footprintInterval = 0.45f;
        public Color bloodColor = new Color(0.35f, 0.02f, 0.02f, 0.8f);
        public Color wetColor = new Color(0.08f, 0.09f, 0.12f, 0.55f);
        public float decalLife = 90f;

        DamageSystem damage;
        float dropTimer;
        float footTimer;

        void Awake() { damage = GetComponent<DamageSystem>(); }

        void Update()
        {
            Vector3 origin = transform.position + Vector3.up * 0.4f;
            RaycastHit hit;
            if (!Physics.Raycast(origin, Vector3.down, out hit, 1.2f)) return;

            if (damage != null && damage.IsBleeding)
            {
                dropTimer += Time.deltaTime;
                if (dropTimer >= dropInterval / Mathf.Max(0.1f, damage.BleedRate * 5f))
                {
                    dropTimer = 0f;
                    DecalSpawner.SpawnTextured("MAT_Decal_BloodDroplet", hit.point, hit.normal, 0.22f, decalLife);
                }
            }

            var wet = hit.collider.GetComponent<WetSurface>();
            if (wet != null)
            {
                footTimer += Time.deltaTime;
                if (footTimer >= footprintInterval)
                {
                    footTimer = 0f;
                    DecalSpawner.SpawnTextured("MAT_Decal_WetFootprint", hit.point, hit.normal, 0.3f, decalLife * 0.6f);
                }
            }
        }
    }
}
