using UnityEngine;


namespace viva
{


    public class HeadState : OccupyState
    {

        [SerializeField]
        private Rigidbody headRigidBody;


        public void WearOnHead(
                    Item item,
                    Vector4 localPosAndPitch,
                    HoldType _holdType,
                    float blendDuration)
        {

            if (item.rigidBody == null)
            {
                return;
            }
            item.transform.localPosition = localPosAndPitch;
            BeginRigidBodyGrab(item.rigidBody, headRigidBody, false, HoldType.OBJECT, blendDuration);
            Pickup(item);
        }

        protected override void GetRigidBodyBlendConnectedAnchor(out Vector3 targetLocalPos, out Quaternion targetLocalRot)
        {
            targetLocalPos = Vector3.zero;
            targetLocalRot = Quaternion.identity;
        }

        protected override void OnPostPickupItem()
        {
        }

        protected override void OnPreDropItem()
        {
        }
    }

}