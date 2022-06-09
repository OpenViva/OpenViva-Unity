using UnityEngine;


namespace viva
{

    public class SubstanceCollisionCallback : MonoBehaviour
    {

        [SerializeField]
        public Rigidbody rigidBody;
        [SerializeField]
        private bool disableSelfDestroy = false;

        public SubstanceSpill sourceSubstanceSpill;

        private void Awake()
        {
            if (!disableSelfDestroy)
            {
                Destroy(this.gameObject, 3.0f);
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (sourceSubstanceSpill == null)
            {
                Destroy(this.gameObject);
                return;
            }
            if (sourceSubstanceSpill.activeSpillAmount == 0.0f)
            {
                return;
            }
            Container container = Tools.SearchTransformAncestors<Container>(collider.transform);
            if (container != null)
            {
                CheckContainerContact(container);
            }
        }

        private void CheckContainerContact(Container container)
        {
            if (container == sourceSubstanceSpill.sourceContainer)
            {
                return;
            }
            if (container.isLidClosed)
            {
                return;
            }
            if (container.AttemptReceiveSubstanceSpill(sourceSubstanceSpill.substance, sourceSubstanceSpill.activeSpillAmount))
            {
                //consume spill
                sourceSubstanceSpill.ConsumeSpill(container);
                if (!disableSelfDestroy)
                {
                    Destroy(this.gameObject);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {

            if (sourceSubstanceSpill == null)
            {
                Destroy(this.gameObject);
                return;
            }
            if (sourceSubstanceSpill.activeSpillAmount == 0.0f)
            {
                return;
            }
            if (rigidBody.velocity.sqrMagnitude > 0.1f)
            {
                sourceSubstanceSpill.PlaySpillContactSound(transform.position);
            }
            Character character = Tools.SearchTransformAncestors<Character>(collision.gameObject.transform);
            if (character)
            {
                Loli loli = character as Loli;
                if (loli)
                {
                    loli.passive.environment.AttemptReactToSubstanceSpill(sourceSubstanceSpill.substance, transform.position);
                }
            }
        }
    }

}