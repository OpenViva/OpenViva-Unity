using UnityEngine;


namespace viva
{

    public class SoundManager : MonoBehaviour
    {

        [SerializeField]
        private AudioSource[] audioSources;
        [SerializeField]
        private GameObject audioSourcePrefab;

        private AudioSourceHandle[] handles;
        private int handleIndex = 0;

        public static SoundManager main;


        public class AudioSourceHandle
        {
            private AudioSource source;
            public bool exists { get { return source != null; } }
            public float volume { get { if (source) { return source.volume; } else { return 0.0f; } } set { if (source) { source.volume = value; } } }
            public float pitch { set { if (source) { source.pitch = value; } } }
            public bool loop { set { if (source) { source.loop = value; } } }
            public float maxDistance { set { if (source) { source.maxDistance = value; } } }
            public bool playing { get { return source ? source.isPlaying : false; } }

            public AudioSourceHandle(AudioSource _source, Vector3 localPosition, Transform parent)
            {
                source = _source;
                Setup(localPosition, parent);
            }

            public void Setup(Vector3 localPosition, Transform parent)
            {
                source.transform.SetParent(parent, false);
                source.transform.localPosition = localPosition;
                source.volume = 1.0f;
                source.pitch = 1.0f;
                source.loop = false;
                source.maxDistance = 16.0f;
            }

            public void PlayOneShot(AudioClip clip)
            {
                if (source && clip)
                {
                    source.PlayOneShot(clip);
                }
            }

            public void Play(AudioClip clip)
            {
                if (source)
                {
                    source.clip = clip;
                    source.Play();
                }
            }

            public void PlayDelayed(AudioClip clip, float delay)
            {
                if (source && clip)
                {
                    source.clip = clip;
                    source.PlayDelayed(delay);
                }
            }

            public void Stop()
            {
                if (source)
                {
                    source.Stop();
                }
            }
        }

        private void Awake()
        {
            main = this;
            handles = new AudioSourceHandle[audioSources.Length];
            for (int i = 0; i < handles.Length; i++)
            {
                handles[i] = new AudioSourceHandle(audioSources[i], Vector3.zero, null);
            }
        }

        public AudioSourceHandle RequestHandle(Vector3 localPosition, Transform parent = null)
        {

            for (int i = 0; i < audioSources.Length; i++)
            {
                handleIndex = (handleIndex + 1) % audioSources.Length;
                var candidate = handles[handleIndex];
                //keep iterating until finding one that isn't occupied
                if (!candidate.exists || !candidate.playing)
                {
                    break;
                }
            }

            var handle = handles[handleIndex];
            if (!handle.exists)
            {
                var audioSourcePrefabInstance = GameObject.Instantiate(audioSourcePrefab);
                var newAudioSource = audioSourcePrefabInstance.GetComponent<AudioSource>();
                audioSources[handleIndex] = newAudioSource;
                handle = new AudioSourceHandle(newAudioSource, localPosition, parent);
                handles[handleIndex] = handle;
            }
            else
            {
                handle.Setup(localPosition, parent);
            }
            return handle;
        }
    }

}