using UnityEngine;

namespace viva
{


    public class Knife : Item
    {

        [Range(0.0f, 0.001f)]
        [SerializeField]
        private float minBladeSpeed = 0.0001f;
        [SerializeField]
        private AudioClip knifeSliceSound;

        private float lastSliceTime = 0.0f;


        protected void OnCollisionEnter(Collision collision)
        {
            if (Time.time - lastSliceTime < 0.4f)
            {
                return;
            }
            if (rigidBody.velocity.sqrMagnitude < minBladeSpeed)
            {
                return;
            }
            Item item = collision.gameObject.GetComponent<Item>();
            if (item == null)
            {
                return;
            }
            Vector3? averagePos = GamePhysics.AverageContactPosition(collision, 2);
            if (averagePos.HasValue && transform.InverseTransformPoint(averagePos.Value).z > 0.0f)
            {
                Cut(item.gameObject);
            }
        }

        private void Cut(GameObject target)
        {
            SliceTarget sliceTarget = target.GetComponent<SliceTarget>();
            if (sliceTarget == null)
            {
                return;
            }
            Item item = target.GetComponent<Item>();
            if (item && item.mainOccupyState)
            {
                item.mainOccupyState.AttemptDrop();
            }

            if (sliceTarget.slicesAllowed <= 0)
            {
                return;
            }

            Mesh sliceMesh;
            if (sliceTarget.sliceMesh == null)
            {
                MeshFilter mf = target.GetComponent<MeshFilter>();
                sliceMesh = mf.sharedMesh;
            }
            else
            {
                sliceMesh = sliceTarget.sliceMesh;
            }
            Mesh[] meshes = MeshSlicer.Slice(
                sliceMesh,
                target.transform.InverseTransformPoint(transform.position),
                target.transform.InverseTransformDirection(transform.right),
                target.transform.InverseTransformDirection(transform.forward),
                sliceTarget.GetVolumeBounds(),
                12
            );
            if (meshes == null)
            {
                return;
            }
            SoundManager.main.RequestHandle(transform.position).PlayOneShot(knifeSliceSound);
            sliceTarget.SetSlicedMeshes(meshes);
            lastSliceTime = Time.time;
        }
    }

}