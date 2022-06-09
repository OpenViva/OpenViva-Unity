using UnityEngine;


namespace viva
{


    public class ParticleSystemProximityCamera : MonoBehaviour
    {

        [SerializeField]
        private ParticleSystem[] pSystems;
        [SerializeField]
        private bool followCamera = true;
        [SerializeField]
        private Vector3 endpointA;
        [SerializeField]
        private Vector3 endpointB;
        [SerializeField]
        private float padding = 4.0f;

        private Transform target = null;


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(endpointA, 0.5f);
            Gizmos.DrawSphere(endpointB, 0.5f);
        }

        private void OnTriggerEnter(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            foreach (var pSys in pSystems)
            {
                var main = pSys.emission;
                main.enabled = true;
            }
            if (followCamera)
            {
                enabled = true;
                target = collider.transform;
            }
        }
        private void OnTriggerExit(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            foreach (var pSys in pSystems)
            {
                var main = pSys.emission;
                main.enabled = false;
            }
            enabled = false;
            target = null;
        }

        private void Update()
        {
            if (target == null)
            {
                enabled = false;
            }

            Vector3 diff = endpointB - endpointA;
            transform.position = endpointA + diff * Mathf.Clamp01(Tools.PointOnRayRatio(endpointA, endpointB, target.position + target.forward * padding));
        }
    }

}