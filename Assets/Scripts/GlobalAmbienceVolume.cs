using UnityEngine;


namespace viva
{

    public class GlobalAmbienceVolume : MonoBehaviour
    {

        [SerializeField]
        private Ambience ambience;

        public void OnTriggerEnter(Collider collider)
        {
            Player player = collider.GetComponent<Player>();
            if (player != null)
            {
                GameDirector.instance.ambienceDirector.EnterAmbience(ambience);
            }
        }

        public void OnTriggerExit(Collider collider)
        {
            Player player = collider.GetComponent<Player>();
            if (player != null)
            {
                GameDirector.instance.ambienceDirector.ExitAmbience(ambience);
            }
        }
    }

}