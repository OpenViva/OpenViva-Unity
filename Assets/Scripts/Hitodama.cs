using System.Collections;
using UnityEngine;


namespace viva
{

    public class Hitodama : MonoBehaviour
    {

        [SerializeField]
        private Rigidbody rigidBody;
        [Range(0.0001f, 0.1f)]
        [SerializeField]
        private float bobStrength = 0.01f;
        [Range(0.01f, 6.0f)]
        [SerializeField]
        private float bobFrequency = 1.0f;
        [SerializeField]
        private SoundSet ghostSoundSet;
        [SerializeField]
        private ParticleSystem mainFlamePSys;
        [SerializeField]
        private ParticleSystem spikesPSys;
        [SerializeField]
        private new Light light;
        [SerializeField]
        private Color normalColor;
        [SerializeField]
        private Color touchedColor;
        [SerializeField]
        private Coroutine touchCoroutine = null;


        private void OnCollisionEnter(Collision collision)
        {
            Item item = collision.collider.GetComponent<Item>();
            if (item == null)
            {
                return;
            }
            BeginTouch();
        }

        private void BeginTouch()
        {
            if (touchCoroutine != null)
            {
                return;
            }

            touchCoroutine = GameDirector.instance.StartCoroutine(TouchEffect());
        }

        private IEnumerator TouchEffect()
        {

            var sound = SoundManager.main.RequestHandle(Vector3.zero, transform);
            sound.Play(ghostSoundSet.GetRandomAudioClip());
            sound.pitch = 0.7f + Random.value * 0.3f;
            sound.maxDistance = 6.0f;

            float starTime = Time.time;
            float duration = 0.7f;
            while (Time.time - starTime < duration)
            {
                float ratio = (Time.time - starTime) / duration;

                Color newColor = Color.LerpUnclamped(normalColor, touchedColor, ratio);
                light.color = newColor;

                var mainFlameMain = mainFlamePSys.main;
                mainFlameMain.startColor = newColor;

                var spikesMain = spikesPSys.main;
                spikesMain.startColor = newColor;
                yield return null;
            }
            yield return new WaitForSeconds(1.5f);

            starTime = Time.time;
            duration = 1.5f;
            while (Time.time - starTime < duration)
            {
                float ratio = (Time.time - starTime) / duration;

                Color newColor = Color.LerpUnclamped(touchedColor, normalColor, ratio);
                light.color = newColor;

                var mainFlameMain = mainFlamePSys.main;
                mainFlameMain.startColor = newColor;

                var spikesMain = spikesPSys.main;
                spikesMain.startColor = newColor;
                yield return null;
            }
            yield return new WaitForSeconds(3.0f);

            touchCoroutine = null;
        }

        void FixedUpdate()
        {
            float force = Mathf.Sin(transform.position.x + transform.position.z + Time.time * bobFrequency) * bobStrength;
            if (!Physics.Raycast(transform.position, Vector3.down, 1.5f, Instance.wallsMask, QueryTriggerInteraction.Ignore))
            {
                force = bobStrength;
            }
            rigidBody.AddForce(Vector3.down * force, ForceMode.VelocityChange);
        }
    }

}