using UnityEngine;

namespace Room812.Audio
{
    /// <summary>Guarantees all looping ambient sources start in Play Mode.</summary>
    public class AmbientAudioStarter : MonoBehaviour
    {
        void Start()
        {
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
                if (source != null && source.loop && source.clip != null && !source.isPlaying)
                    source.Play();
        }
    }
}
