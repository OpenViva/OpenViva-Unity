using UnityEngine;

namespace viva
{


    public class HideLOD : MonoBehaviour
    {

        [SerializeField]
        private GameObject showOnTrigger;
        [SerializeField]
        private GameObject showOnExit;

        private int inside = 0;


        private void OnTriggerEnter(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            if (++inside == 1)
            {
                showOnTrigger.SetActive(true);
                showOnExit.SetActive(false);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            Camera camera = collider.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }
            if (--inside == 0)
            {
                showOnTrigger.SetActive(false);
                showOnExit.SetActive(true);
            }
        }

        private void OnDrawGizmosSelected()
        {

            Collider[] colliders = this.GetComponents<Collider>();
            Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 0.5f);
            foreach (Collider c in colliders)
            {
                var box = c as BoxCollider;
                if (box)
                {
                    var size = box.size;
                    size.x = -size.x;
                    Gizmos.DrawCube(transform.TransformPoint(box.center), size);
                }
            }
            if (showOnTrigger)
            {
                Gizmos.color = Color.blue;
                MeshFilter mf = showOnTrigger.GetComponentInChildren<MeshFilter>();
                if (mf)
                {
                    Gizmos.DrawMesh(mf.sharedMesh, mf.transform.position, mf.transform.rotation);
                }
            }
            // if( showOnExit ){
            // 	Gizmos.color = Color.cyan;
            // 	MeshFilter mf = showOnExit.GetComponentInChildren<MeshFilter>();
            // 	if( mf ){
            // 		Gizmos.DrawMesh( mf.sharedMesh );
            // 	}
            // }
        }

    }

}