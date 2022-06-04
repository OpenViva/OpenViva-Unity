using UnityEngine;


namespace viva
{

    public class IndoorAmbienceVolume : MonoBehaviour
    {

        [Header("Must Be In Camera Layer")]
        private static int indoorCounter = 0;

        public void OnTriggerEnter(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            if (indoorCounter++ == 0)
            {
                GameDirector.instance.SetUserIsIndoors(true);
            }
        }

        public void OnTriggerExit(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            if (--indoorCounter == 0)
            {
                GameDirector.instance.SetUserIsIndoors(false);
            }
        }
    }

}