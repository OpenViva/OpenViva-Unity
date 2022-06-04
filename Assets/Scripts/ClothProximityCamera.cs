using UnityEngine;


namespace viva
{


    public partial class ClothProximityCamera : MonoBehaviour
    {

        [SerializeField]
        private Cloth[] targets;


        private void OnTriggerEnter(Collider collider)
        {

            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            foreach (var target in targets)
            {
                if (target)
                {
                    target.enabled = true;
                }
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            foreach (var target in targets)
            {
                if (target)
                {
                    target.enabled = false;
                }
            }
        }
    }

}