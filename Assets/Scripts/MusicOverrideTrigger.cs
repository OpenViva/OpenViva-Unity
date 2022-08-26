using UnityEngine;


namespace viva
{

    public class MusicOverrideTrigger : MonoBehaviour
    {
        private int counter = 0;

        [SerializeField]
        private GameDirector.Music musicToOverride;

        public void OnTriggerEnter(Collider collider)
        {
            var camera = collider.GetComponent<Camera>();
            if (!camera) return;

            if (counter == 0)
            {
                GameDirector.instance.SetOverrideMusic(musicToOverride);
            }
            counter++;
        }

        public void OnTriggerExit(Collider collider)
        {
            var camera = collider.GetComponent<Camera>();
            if (!camera) return;

            counter--;
            if (counter == 0)
            {
                GameDirector.instance.SetOverrideMusic(null);
            }
        }
    }

}
