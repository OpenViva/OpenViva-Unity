using UnityEngine;

namespace viva
{


    public class Cattail : Item
    {

        [SerializeField]
        private SoundSet smackFX;

        public void PlaySmackSound()
        {
            SoundManager.main.RequestHandle(transform.position).PlayOneShot(smackFX.GetRandomAudioClip());
        }
    }

}