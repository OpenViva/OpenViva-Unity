using UnityEngine;

namespace viva
{


    public class SliceTarget : MonoBehaviour
    {

        [SerializeField]
        private Mesh m_sliceMesh;
        public Mesh sliceMesh { get { return m_sliceMesh; } }
        [SerializeField]
        private GameObject slicedPrefab;
        [SerializeField]
        private Material[] materials;

        [Header("Spawn Variables")]
        [SerializeField]
        public ItemSettings spawnSettings;
        [SerializeField]
        public MeshFilter meshFilter;
        [SerializeField]
        public MeshRenderer meshRenderer;
        [SerializeField]
        public MeshCollider meshCollider;
        [SerializeField]
        protected Mesh volumeMesh;
        [SerializeField]
        public int slicesAllowed = 5;

        public Bounds? GetVolumeBounds()
        {
            if (volumeMesh != null)
            {
                return volumeMesh.bounds;
            }
            else
            {
                return null;
            }
        }

        public void SetSlicedMeshes(Mesh[] meshes)
        {
            if (meshes == null)
            {
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                GameObject part = Instantiate<GameObject>(slicedPrefab, transform.position, transform.rotation);
                SliceTarget sliceTarget = part.GetComponent<SliceTarget>();
                sliceTarget.spawnSettings = spawnSettings;
                sliceTarget.materials = materials;
                sliceTarget.slicedPrefab = slicedPrefab;

                //disable cutting if mesh is too small
                float boxVol = meshes[i].bounds.extents.magnitude;
                if (boxVol < 0.025f)
                {
                    sliceTarget.slicesAllowed = 0;
                }
                else
                {
                    sliceTarget.slicesAllowed = slicesAllowed - 1;
                }

                sliceTarget.meshFilter.mesh = meshes[i];
                sliceTarget.meshRenderer.materials = materials;
                sliceTarget.meshCollider.sharedMesh = meshes[i];
                sliceTarget.volumeMesh = volumeMesh;
                Item.AddAndAwakeItemComponent<Item>(part, spawnSettings, null);
            }
            GameObject.Destroy(this.gameObject);
        }
    }

}