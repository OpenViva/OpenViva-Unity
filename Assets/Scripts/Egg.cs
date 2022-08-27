using System.Collections;
using UnityEngine;


namespace viva
{

    public class Egg : Harvest
    {

        [SerializeField]
        private AudioClip eggSplatSound;
        [SerializeField]
        private AudioClip eggCrackSound;
        [SerializeField]
        private MeshRenderer eggMR;
        [SerializeField]
        private GameObject eggSplatGameObject;
        [SerializeField]
        private float breakStrength = 3.0f;
        [SerializeField]
        private float crackStrength = 1.0f;
        [SerializeField]
        private GameObject eggYolkPrefab;
        [SerializeField]
        private VivaSessionAsset m_sourceChickenItem = null;
        [VivaFileAttribute]
        public VivaSessionAsset sourceChickenItem { get { return m_sourceChickenItem; } set { SetSourceChicken(value); } }

        private bool splat = false;
        private EggHolder currentEggHolder = null;
        private int currentEggHolderSlot = -1;


        protected override void OnItemAwake()
        {
            base.OnItemAwake();
            SetSourceChicken(sourceChickenItem);
        }

        private void SetSourceChicken(VivaSessionAsset newSourceChicken)
        {
            ChickenItem chickenItem = newSourceChicken as ChickenItem;
            if (chickenItem == null)
            {
                return;
            }
            chickenItem.OnEggSourceSet();
            m_sourceChickenItem = newSourceChicken;
        }

        public override void OnPostPickup()
        {
            base.OnPostPickup();
            if (currentEggHolder != null)
            {
                currentEggHolder.RemoveFromSlot(currentEggHolderSlot);
                currentEggHolder = null;
            }
            TutorialManager.main.DisplayObjectHint(this, "hint_crackEggs", HintCrackEgg);
        }

        private IEnumerator HintCrackEgg(Item source)
        {
            Egg egg = source as Egg;
            if (egg == null)
            {
                yield break;
            }
            while (true)
            {
                if (egg == null || egg.splat)
                {
                    TutorialManager.main.StopHint();
                    yield break;
                }
                yield return null;
            }
        }

        private void CheckIfCollisionIsAnEggHolder(Collision collision)
        {
            EggHolder eggHolder = collision.gameObject.GetComponent<EggHolder>();
            if (eggHolder == null)
            {
                return;
            }
            if (mainOccupyState != null)
            {
                return;
            }
            if (currentEggHolder != null)
            {
                return;
            }
            currentEggHolderSlot = -1;
            Vector3 localSlotPos = eggHolder.FindNearestFreeSlotLocalPos(eggHolder.transform.InverseTransformPoint(transform.position), ref currentEggHolderSlot);
            if (currentEggHolderSlot == -1)
            {
                return;
            }
            currentEggHolder = eggHolder;
            eggHolder.AddToSlot(localSlotPos, currentEggHolderSlot, this);
        }

        protected void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.sqrMagnitude < breakStrength * breakStrength)
            {
                CheckIfCollisionIsAnEggHolder(collision);
                return;
            }
            if (splat)
            {
                return;
            }

            //Find lowest contact point
            Vector3 splatNorm = Vector3.zero;
            Vector3 splatPos = Vector3.zero;
            Transform splatTarget = null;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint point = collision.GetContact(i);

                if (point.normal.y > splatNorm.y)
                {
                    splatNorm = point.normal;
                    splatPos = point.point;
                    splatTarget = point.otherCollider.transform;
                }
            }
            if (splatTarget == null)
            {
                return;
            }

            if (mainOccupyState != null)
            {
                mainOccupyState.AttemptDrop();
            }
            splat = true;
            eggMR.enabled = false;
            this.gameObject.layer = WorldUtil.noneLayer;

            rigidBody.isKinematic = true;
            this.SetAttribute(Attributes.DISABLE_PICKUP);
            SoundManager.main.RequestHandle(transform.position).PlayOneShot(eggSplatSound);
            eggSplatGameObject.SetActive(true);
            eggSplatGameObject.transform.position = splatPos + splatNorm * 0.005f;
            eggSplatGameObject.transform.rotation = Quaternion.LookRotation(splatNorm, UnityEngine.Random.onUnitSphere) * Quaternion.Euler(90.0f, 0.0f, 0.0f);
            // ParentToTransform( splatTarget );
            Destroy(this.gameObject, 8.0f);
        }

        public void Crack()
        {
            if (splat)
            {
                return;
            }
            GameObject.Instantiate(eggYolkPrefab, transform.position, transform.rotation, transform.parent);
            splat = true;

            gameObject.GetComponent<MeshRenderer>().enabled = false;
            gameObject.layer = WorldUtil.noneLayer;

            mainOccupyState.AttemptDrop();
            SetAttribute(Attributes.DISABLE_PICKUP);
            SoundManager.main.RequestHandle(transform.position).PlayOneShot(eggCrackSound);
            Destroy(gameObject, 2.0f);
        }

        public override void OnItemLateUpdate()
        {

            Player player = mainOwner as Player;
            if (player != null)
            {
                PlayerHandState handState = mainOccupyState as PlayerHandState;
                if (handState.actionState.isDown)
                {
                    handState.gripState.Consume();
                    handState.actionState.Consume();

                    Crack();

                    handState.animSys.SetTargetAnimation(Player.Animation.EGG_CRACK);
                }
            }
        }

        protected override void OnItemDestroy()
        {
            ChickenItem chickenItem = sourceChickenItem as ChickenItem;
            if (chickenItem == null)
            {
                return;
            }
            chickenItem.OnEggDestroyed(transform.position);
        }
    }

}