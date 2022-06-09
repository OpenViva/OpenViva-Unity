using UnityEngine;

namespace viva
{


    public abstract class Mechanism : VivaSessionAsset
    {

        [SerializeField]
        private Mesh m_highlightMesh;
        public Mesh highlightMesh { get { return m_highlightMesh; } }
        [SerializeField]
        private Transform m_highlightTransformOverride;
        public Transform highlightTransformOverride { get { return m_highlightTransformOverride; } }

        protected override void OnAwake()
        {
            enabled = false;
            OnMechanismAwake();
        }
        //prevent usage of Monobehavior functions
        public override sealed void FixedUpdate()
        {
        }
        public override sealed void Update()
        {
        }
        public override sealed void LateUpdate()
        {
        }
        public virtual void OnMechanismFixedUpdate()
        {
        }
        public virtual void OnMechanismUpdate()
        {
        }
        public virtual void OnMechanismAwake()
        {
        }
        public virtual void OnMechanismLateUpdate()
        {
        }
        public virtual void OnItemGrabbed(Item item)
        {
        }
        public virtual void OnItemReleased(Item item)
        {
        }
        public virtual void OnItemRotationChange(Item item, float newPercentRotated)
        {
        }
        public virtual void OnMechanismTriggerEnter(MechanismCollisionCallback self, Collider collider)
        {
        }
        public virtual void OnMechanismTriggerExit(MechanismCollisionCallback self, Collider collider)
        {
        }
        public abstract bool AttemptCommandUse(Loli targetLoli, Character commandSource);

        public abstract void EndUse(Character targetCharacter);

        public virtual void OnDrawGizmosSelected()
        {
            if (highlightMesh == null)
            {
                return;
            }
            Gizmos.color = Color.cyan;
            if (highlightTransformOverride)
            {
                Gizmos.matrix = highlightTransformOverride.localToWorldMatrix;
            }
            else
            {
                Gizmos.matrix = transform.localToWorldMatrix;
            }
            Gizmos.DrawWireMesh(highlightMesh, 0);
        }
    }

}