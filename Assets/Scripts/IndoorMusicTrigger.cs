using UnityEngine;


namespace viva
{

    public class IndoorMusicTrigger : MonoBehaviour
    {
        private static int indoorCounter = 0;

        public void OnTriggerEnter(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (!camera) return;

            if (indoorCounter == 0)
            {
                GameDirector.instance.SetUserIsIndoors(true);
            }
            indoorCounter++;
        }

        public void OnTriggerExit(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (!camera) return;

            indoorCounter--;
            if (indoorCounter == 0)
            {
                GameDirector.instance.SetUserIsIndoors(false);
            }
        }
    }

}