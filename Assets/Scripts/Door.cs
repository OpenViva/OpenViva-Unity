using System.Collections;
using UnityEngine;

namespace viva
{

    public class Door : Mechanism
    {

        private float restYaw;
        private bool locked = false;
        private Coroutine openCloseCoroutine = null;

        [SerializeField]
        private SoundSet openSounds;
        [SerializeField]
        private SoundSet closeSounds;
        [SerializeField]
        private AudioSource audioSource;


        private void Start()
        {
            restYaw = transform.localEulerAngles.y;
        }

        public bool IsClosed()
        {
            float yawDiff = transform.localEulerAngles.y - restYaw;
            return Mathf.Abs(yawDiff) < 0.5f;
        }

        public IEnumerator OpenCloseSoundDetector()
        {

            bool startsClosed = IsClosed();
            while (true)
            {
                if (startsClosed)
                {
                    if (!IsClosed())
                    {
                        audioSource.PlayOneShot(openSounds.GetRandomAudioClip());
                        startsClosed = false;
                    }
                }
                else if (IsClosed())
                {
                    audioSource.PlayOneShot(closeSounds.GetRandomAudioClip());
                    startsClosed = true;
                }
                yield return new WaitForFixedUpdate();
            }
        }

        public override bool AttemptCommandUse(Loli targetLoli, Character commandSource)
        {
            return false;
        }

        public override void EndUse(Character targetCharacter)
        {
        }

        public override void OnItemGrabbed(Item item)
        {

            //ensure it does not already exist
            OnItemReleased(item);

            HandState parentHoldState = item.mainOwner.FindOccupyStateByHeldItem(item) as HandState;
            if (parentHoldState != null)
            {
                Valve valve = item as Valve;

                if (openCloseCoroutine == null && audioSource != null)
                {
                    openCloseCoroutine = GameDirector.instance.StartCoroutine(OpenCloseSoundDetector());
                }
            }
        }

        public override void OnItemReleased(Item item)
        {

            if (openCloseCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(openCloseCoroutine);
                openCloseCoroutine = null;
            }
        }
    }

}