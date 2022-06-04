using UnityEngine;

namespace viva
{


    public class MechanismCollisionCallback : MonoBehaviour
    {

        [SerializeField]
        private Mechanism parent;

        private void OnTriggerEnter(Collider collider)
        {
            parent.OnMechanismTriggerEnter(this, collider);
        }

        private void OnTriggerExit(Collider collider)
        {
            parent.OnMechanismTriggerExit(this, collider);
        }
    }

}