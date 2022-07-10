using UnityEngine;


namespace viva
{

    public class ExploreAmbienceVolume : MonoBehaviour
    {

        [Header("Must Be In Camera Layer")]
        private static int exploreCounter = 0;

        public void OnTriggerEnter(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            if (exploreCounter++ == 0)
            {
                GameDirector.instance.SetUserIsExploring(true);
            }
        }

        public void OnTriggerExit(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            if (--exploreCounter == 0)
            {
                GameDirector.instance.SetUserIsExploring(false);
            }
        }
    }

}