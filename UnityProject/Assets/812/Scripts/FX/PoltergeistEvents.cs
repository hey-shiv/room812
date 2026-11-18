using System.Collections;
using UnityEngine;
using Room812.AI;

namespace Room812.FX
{
    /// <summary>
    /// Environmental poltergeist reactions triggered by ghost state: doors slam, the breaker
    /// trips and lights die, and handprints appear on nearby walls. All local/offline.
    /// </summary>
    public class PoltergeistEvents : MonoBehaviour
    {
        public GhostBrain brain;
        public float eventInterval = 9f;
        public float breakerDuration = 4f;
        public bool doorsSlam = true;
        public bool breakerTrips = true;
        public bool handprints = true;

        GhostBrain.GhostState lastState;
        float timer;
        TubeLightFlicker[] flickers;
        Transform[] doors;

        void Start()
        {
            if (brain == null) brain = FindAnyObjectByType<GhostBrain>();
            flickers = FindObjectsByType<TubeLightFlicker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var candidates = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var list = new System.Collections.Generic.List<Transform>();
            foreach (var t in candidates) if (t.name.Contains("Door")) list.Add(t);
            doors = list.ToArray();
        }

        void Update()
        {
            if (brain == null) return;
            if (brain.state != lastState)
            {
                lastState = brain.state;
                if (lastState == GhostBrain.GhostState.Hunt || lastState == GhostBrain.GhostState.Manifest)
                    TriggerEvent();
            }
            timer += Time.deltaTime;
            if (timer >= eventInterval && brain.blackboard != null && brain.blackboard.aggression > 0.55f)
            {
                timer = 0f;
                TriggerEvent();
            }
        }

        void TriggerEvent()
        {
            int pick = Random.Range(0, 3);
            if (pick == 0 && breakerTrips) StartCoroutine(TripBreaker());
            else if (pick == 1 && doorsSlam && doors.Length > 0) StartCoroutine(SlamDoor(doors[Random.Range(0, doors.Length)]));
            else
            {
                var shatter = FindAnyObjectByType<GlassShatter>();
                if (shatter != null) shatter.Shatter();
                else if (handprints) SpawnHandprint();
            }
        }

        IEnumerator TripBreaker()
        {
            foreach (var f in flickers) if (f != null) f.broken = true;
            yield return new WaitForSeconds(breakerDuration);
            foreach (var f in flickers) if (f != null) f.broken = false;
        }

        IEnumerator SlamDoor(Transform door)
        {
            if (door == null) yield break;
            Quaternion original = door.localRotation;
            door.localRotation = Quaternion.Euler(0f, 82f, 0f);
            yield return new WaitForSeconds(0.15f);
            door.localRotation = original;
        }

        void SpawnHandprint()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            Vector3 pos = player.transform.position + Vector3.up * Random.Range(1.1f, 1.9f);
            Vector3 normal = Random.value > 0.5f ? Vector3.right : Vector3.left;
            DecalSpawner.SpawnTextured("MAT_Decal_Handprint", pos, normal, 0.45f, 45f);
        }
    }
}
