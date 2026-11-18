using UnityEngine;

namespace Room812.FX
{
    /// <summary>
    /// Mobile-friendly shatter: hides the glass panes and spawns a handful of short-lived
    /// rigidbody shards. Cheap, no pre-fractured meshes required.
    /// </summary>
    public class GlassShatter : MonoBehaviour
    {
        public Renderer[] glassPanes;
        public int shardCount = 9;
        public float burstForce = 3f;
        public float shardLifetime = 6f;
        public bool shattered;

        public void Shatter()
        {
            if (shattered) return;
            shattered = true;
            if (glassPanes != null)
                foreach (var pane in glassPanes) if (pane != null) pane.enabled = false;

            for (int i = 0; i < shardCount; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "GlassShard";
                shard.transform.position = transform.position + Random.insideUnitSphere * 0.35f;
                shard.transform.localScale = new Vector3(0.07f, 0.13f, 0.012f);
                var rb = shard.AddComponent<Rigidbody>();
                rb.mass = 0.08f;
                rb.AddForce(Random.insideUnitSphere * burstForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.Impulse);
                Destroy(shard, shardLifetime);
            }
            Room812.Core.NoiseBus.Emit(transform.position, 8f, gameObject);
        }
    }
}
