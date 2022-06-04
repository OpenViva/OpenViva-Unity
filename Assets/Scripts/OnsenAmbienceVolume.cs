using UnityEngine;


namespace viva
{

    public class OnsenAmbienceVolume : MonoBehaviour
    {

        [Header("Must Be In Camera Layer")]
        private static int onsenCounter = 0;

        public void OnTriggerEnter(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            if (onsenCounter++ == 0)
            {
                GameDirector.instance.SetUserInOnsen(true);
            }
        }

        public void OnTriggerExit(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            if (--onsenCounter == 0)
            {
                GameDirector.instance.SetUserInOnsen(false);
            }
        }
    }

}