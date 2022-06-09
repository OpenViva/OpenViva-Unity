using UnityEngine;


namespace viva
{

    public enum Occupation
    {
        HEAD,
        HAND_RIGHT,
        HAND_LEFT,
        SHOULDER_RIGHT,
        SHOULDER_LEFT
    }

    public enum HoldType
    {
        NULL,
        GHOST,
        OBJECT
    }


    public abstract class OccupyState : MonoBehaviour
    {

        public delegate void OnNewItemCallback(OccupyState source, Item oldItem, Item newItem);

        [SerializeField]
        protected Character m_owner;
        public Character owner { get { return m_owner; } }
        [SerializeField]
        protected Occupation m_occupation;
        public Occupation occupation { get { return m_occupation; } }
        [SerializeField]
        private Rigidbody rigidBody;
        [SerializeField]
        private Collider[] colliders;
        [SerializeField]
        private float rigidBodyBlendMassScale = 0.00001f;

        public Item heldItem { get; private set; }
        public HoldType holdType { get; private set; } = HoldType.NULL;
        public bool occupied { get { return holdType != HoldType.NULL; } }
        public bool rightSide { get { return occupation == Occupation.HAND_RIGHT || occupation == Occupation.SHOULDER_RIGHT; } }
        private bool lockState;
        public RigidBodyBlend rigidBodyGrab { get; private set; } = null;
        public float blendProgress { get { return holdBlend.value; } }
        public bool finishedBlending { get { return holdBlend.finished; } }
        private Tools.EaseBlend holdBlend = new Tools.EaseBlend();
        protected float blendTargetMassScale = 0.0f;
        public OnNewItemCallback onItemChange;


        protected virtual void OnPreDropItem() { }
        protected virtual void OnPostPickupItem() { }
        protected virtual void OnBeginRigidBodyGrab() { }
        protected virtual void OnStopRigidBodyGrab() { }
        protected abstract void GetRigidBodyBlendConnectedAnchor(out Vector3 targetLocalPos, out Quaternion targetLocalRot);


        private void UpdateHoldBlend()
        {
            holdBlend.Update(Time.fixedDeltaTime);
            if (blendProgress == 0.0f)
            {
                owner.RemoveModifyAnimationCallback(UpdateHoldBlend);
            }
            else
            {
                if (rigidBodyGrab)
                {
                    GetRigidBodyBlendConnectedAnchor(out Vector3 targetLocalPos, out Quaternion targetLocalRot);
                    if (owner.characterType == Character.Type.PLAYER)
                    {
                        if (heldItem != null)
                        {
                            rigidBodyGrab.Blend(blendProgress, targetLocalPos, targetLocalRot, heldItem.settings.heldMassScale);
                        }
                    }
                    else
                    {
                        rigidBodyGrab.Blend(blendProgress, targetLocalPos, targetLocalRot, 2.0f);
                    }
                }
            }
        }
        protected bool Pickup(Item item)
        {

            if (item == null)
            {
                return false;
            }
            if (item.mainOccupyState == this)
            {
                Debug.LogError("[OccupyState] Item already occupied!");
                return false;
            }
            OccupyState oldMainOccupyState = item.mainOccupyState;

            if (item.mainOccupyState != null && !item.settings.allowMultipleOwners)
            {
                item.mainOccupyState.AttemptDropOptionalCallbacks(true, false);
            }
            Item oldItem = heldItem;
            DropHeldItem(false, false);

            Debug.Log("[" + owner.name + "] picked up " + item.name);
            heldItem = item;

            item.AddOccupyState(this);
            item.OnPostPickup();

            onItemChange?.Invoke(this, oldItem, item);

            OnPostPickupItem();

            owner.AddModifyAnimationCallback(UpdateHoldBlend);

            item.onMainOccupyStateChanged?.Invoke(oldMainOccupyState, item.mainOccupyState);
            return true;
        }

        protected void BeginRigidBodyGrab(Rigidbody targetBody, Rigidbody sourceBody, bool useFixedJoint, HoldType _holdType, float blendDuration)
        {
            DropRigidBodyGrab();
            rigidBodyGrab = targetBody.gameObject.AddComponent<RigidBodyBlend>();

            if (rigidBodyGrab)
            {
                rigidBodyGrab.Begin(
                    targetBody,
                    sourceBody,
                    useFixedJoint,
                    OnJointBreak,
                    rigidBodyBlendMassScale
                );
            }
            holdType = _holdType;

            holdBlend.reset(0.0f);
            holdBlend.StartBlend(1.0f, blendDuration);

            OnBeginRigidBodyGrab();

            owner.AddModifyAnimationCallback(UpdateHoldBlend);
        }

        private void OnJointBreak()
        {
            AttemptDrop();
        }

        private void DropRigidBodyGrab()
        {
            if (rigidBodyGrab)
            {
                OnStopRigidBodyGrab();
                rigidBodyGrab.Break();
                rigidBodyGrab = null;
            }
            holdType = HoldType.NULL;
            holdBlend.StartBlend(0.0f, 0.4f);
        }

        private void DropHeldItem(bool generateOccupyStateCallback, bool generateItemCallbacks)
        {
            if (heldItem == null)
            {
                return;
            }
            // Debug.Log("["+owner.name+"] dropped "+heldItem.name);
            OnPreDropItem();

            if (generateOccupyStateCallback)
            {
                onItemChange?.Invoke(this, heldItem, null);
            }
            if (generateItemCallbacks)
            {
                heldItem.onMainOccupyStateChanged?.Invoke(this, null);
            }

            heldItem.OnPreDrop();
            heldItem.RemoveOccupyState(this);

            Item oldItem = heldItem;
            heldItem = null;
            oldItem.OnPostDrop();

            holdType = HoldType.NULL;
            holdBlend.StartBlend(0.0f, 0.4f);
        }

        private bool AttemptDropOptionalCallbacks(bool generateOccupyStateCallback, bool generateItemCallbacks)
        {
            //prevent infinite looping in case callbacks call Drop as well
            if (lockState)
            {
                return false;
            }
            lockState = true;
            DropHeldItem(generateOccupyStateCallback, generateItemCallbacks);
            DropRigidBodyGrab();
            lockState = false;
            return true;
        }

        public bool AttemptDrop()
        {
            return AttemptDropOptionalCallbacks(true, true);
        }
    }

}