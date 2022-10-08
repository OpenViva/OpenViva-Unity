using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva
{


    public partial class PhysicsSound : MonoBehaviour
    {

        [SerializeField]
        private Item sourceItem;
        [SerializeField]
        private SoundSet collisionSoundsSoft;
        [SerializeField]
        private SoundSet collisionSoundsHard;
        [SerializeField]
        private List<Rigidbody> ignoreSoundsFrom = new List<Rigidbody>();
        [SerializeField]
        private AudioClip dragSoundLoop;
        [SerializeField]
        private Rigidbody rigidBody;
        [SerializeField]
        private PhysicsSoundSettings settings;

        private float lastCollisionSoundTime;
        private Coroutine dragCoroutine = null;
        private bool isDragging = false;
        public bool playSounds = true;


        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (!playSounds) return;
            if (settings == null)
            {
                Debug.LogError("[PhysicsSound] Missing settings " + name);
                return;
            }
            //ignore collisions near start of game
            if (Time.time < 2.0f)
            {
                return;
            }
            if (sourceItem != null)
            {
                //Dont play any physics sounds if picked up
                if (sourceItem.mainOccupyState != null)
                {
                    return;
                }
            }
            if (ignoreSoundsFrom.Contains(collision.rigidbody))
            {
                return;
            }
            if (Time.time - lastCollisionSoundTime < 0.1f)
            {
                return;
            }
            var avgPos = GamePhysics.AverageContactPosition(collision, 2);
            if (!avgPos.HasValue)
            {
                return;
            }
            float sqVel = collision.relativeVelocity.sqrMagnitude;
            if (sqVel > settings.hardMinVel * settings.hardMinVel && collisionSoundsHard != null)
            {
                var handle = SoundManager.main.RequestHandle(avgPos.Value);
                handle.PlayOneShot(collisionSoundsHard.GetRandomAudioClip());

                lastCollisionSoundTime = Time.time;
            }
            else if (sqVel > settings.softMinVel * settings.softMinVel && collisionSoundsSoft != null)
            {
                var handle = SoundManager.main.RequestHandle(avgPos.Value);
                handle.PlayOneShot(collisionSoundsSoft.GetRandomAudioClip());

                float vel = Mathf.Sqrt(sqVel);
                float t = (vel - settings.softMinVel) / (settings.hardMinVel - settings.softMinVel);
                handle.volume = Mathf.Min(1.0f, t);
                handle.pitch = Mathf.LerpUnclamped(settings.softMinPitch, settings.softMaxPitch, t);

                lastCollisionSoundTime = Time.time;
            }
        }

        protected void OnCollisionStay(Collision collision)
        {

            if (settings == null || dragSoundLoop == null)
            {
                return;
            }
            //ignore collisions near start of game
            if (Time.time < 2.0f)
            {
                return;
            }
            if (rigidBody == null)
            {
                Debug.LogError("[PhysicsSound] missing rigidBody reference for " + name);
                return;
            }
            var relativeVel = collision.rigidbody ? collision.rigidbody.velocity : Vector3.zero;
            if ((rigidBody.velocity-relativeVel).sqrMagnitude > settings.dragMinVel * settings.dragMinVel)
            {
                isDragging = true;
                if (dragCoroutine == null)
                {
                    dragCoroutine = GameDirector.instance.StartCoroutine(Drag());
                }
            }
        }

        private IEnumerator Drag()
        {

            var handle = SoundManager.main.RequestHandle(Vector3.zero, transform);
            handle.Play(dragSoundLoop);
            handle.loop = true;

            while (isDragging)
            {
                float vel = rigidBody.velocity.magnitude;
                handle.pitch = Mathf.LerpUnclamped(settings.dragMinPitch, settings.dragMaxPitch, Tools.GetClampedRatio(settings.dragMinVel, settings.dragMaxVel, vel));
                handle.volume = Mathf.Clamp01(vel / (settings.dragMaxVolumeVel));
                isDragging = false;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
            }

            handle.Stop();

            dragCoroutine = null;
        }
    }

}