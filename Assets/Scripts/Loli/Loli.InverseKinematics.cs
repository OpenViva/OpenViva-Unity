using UnityEngine;


namespace viva
{


    public partial class Loli : Character
    {

        public enum IKAnimation
        {
            HORSE_MOUNT_HOLD_RIGHT,
            HORSE_MOUNT_HOLD_LEFT,
        }

        [SerializeField]
        public IKAnimationTarget[] ikAnimationTargets = new IKAnimationTarget[System.Enum.GetValues(typeof(IKAnimation)).Length];
        [SerializeField]
        private ItemSettings handItemSettings;
        // [SerializeField]
        // private GameObject handRigidBodyPrefab;

        public LoliHandState rightLoliHandState { get { return rightHandState as LoliHandState; } }
        public LoliHandState leftLoliHandState { get { return leftHandState as LoliHandState; } }


        public class ArmIK
        {

            public class RetargetingInfo
            {
                public Vector3 target;
                public Vector3 pole;
                public Quaternion? handRotation = Quaternion.identity;
            }

            public TwoBoneIK ik = null;
            public Transform spine2 = null;
            public Transform shoulder = null;
            public Transform upperArm = null;
            public Transform arm = null;
            public Transform wrist = null;
            public Transform hand = null;
            private float shoulderIKUpRange = 75.0f;
            private float shoulderIKForwardRange = 35.0f;
            public Quaternion shoulderLocalBaseRotation;
            public readonly float sign;


            public ArmIK(bool rightSide)
            {
                if (rightSide)
                {
                    shoulderIKForwardRange *= -1.0f;
                    sign = 1.0f;
                }
                else
                {
                    sign = -1.0f;
                }
            }

            public ArmIK(ArmIK copy)
            {
                ik = new TwoBoneIK(copy.ik);
                spine2 = copy.spine2;
                shoulder = copy.shoulder;
                upperArm = copy.upperArm;
                arm = copy.arm;
                wrist = copy.wrist;
                hand = copy.hand;
                shoulderLocalBaseRotation = copy.shoulderLocalBaseRotation;
                sign = copy.sign;
            }

            public void OverrideWorldRetargetingTransform(RetargetingInfo retargeting, Vector3 worldTarget, Vector3 worldPole, Quaternion? handRotation)
            {
                retargeting.target = WorldToHoldSpace(worldTarget);
                retargeting.pole = WorldToHoldSpace(worldPole);
                retargeting.handRotation = handRotation;
            }

            public Vector3 HoldSpaceToWorld(Vector3 holdSpacePos)
            {
                holdSpacePos.x *= sign;
                return spine2.TransformPoint(holdSpacePos);
            }

            public Vector3 WorldToHoldSpace(Vector3 worldPos)
            {
                Vector3 result = spine2.InverseTransformPoint(worldPos);
                result.x *= sign;
                return result;
            }

            public void Apply(RetargetingInfo retargeting, float blend)
            {

                if (blend <= 0.0f)
                {
                    return;
                }

                //transform points to holdSpace (spine2)
                Vector3 holdSpaceTarget = HoldSpaceToWorld(retargeting.target);
                Vector3 holdSpacePole = HoldSpaceToWorld(retargeting.pole);

                Debug.DrawLine(holdSpaceTarget, holdSpacePole, Color.grey, 1.0f);

                Quaternion oldP0Rotation = ik.p0.rotation;
                Quaternion oldP1Rotation = ik.p1.rotation;
                Quaternion oldArmLocalRotation = arm.localRotation;
                ik.Solve(holdSpaceTarget, holdSpacePole);
                Quaternion newP1Rotation = ik.p1.rotation;
                ik.p0.rotation = Quaternion.LerpUnclamped(oldP0Rotation, ik.p0.rotation, blend);

                Vector3 localShoulderDir = this.spine2.InverseTransformDirection(ik.p0.up);
                Quaternion targetLocalShoulderRotation = shoulderLocalBaseRotation;
                float shoulderUpBlendRatio = Mathf.Clamp01((localShoulderDir.y + 0.6f) / 1.6f);
                float shoulderUpBlend = Tools.EaseInOutQuad(shoulderUpBlendRatio * shoulderUpBlendRatio) * blend;
                targetLocalShoulderRotation *= Quaternion.Euler(shoulderIKUpRange * shoulderUpBlend, 0.0f, 0.0f);
                float shoulderForwardBlendRatio = Mathf.Clamp((localShoulderDir.z), -.5f, 1.0f);
                float shoulderForwardBlend = (shoulderForwardBlendRatio * shoulderForwardBlendRatio) * blend;
                targetLocalShoulderRotation *= Quaternion.Euler(0.0f, 0.0f, shoulderIKForwardRange * shoulderForwardBlend);
                shoulder.transform.localRotation = Quaternion.LerpUnclamped(shoulder.transform.localRotation, targetLocalShoulderRotation, blend);

                ik.Solve(holdSpaceTarget, holdSpacePole);

                newP1Rotation = ik.p1.rotation;
                ik.p0.rotation = Quaternion.LerpUnclamped(oldP0Rotation, ik.p0.rotation, blend);
                ik.p1.rotation = Quaternion.LerpUnclamped(oldP1Rotation, newP1Rotation, blend);

                if (retargeting.handRotation.HasValue)
                {
                    hand.rotation = Quaternion.LerpUnclamped(hand.rotation, retargeting.handRotation.Value, blend);
                }

                FixUpperArm();
                FixWrist();
            }

            public void FixUpperArm()
            {
                Vector3 armLocal = Vector3.zero;
                armLocal.y = Mathf.DeltaAngle(ik.p0.localEulerAngles.y, 0.0f) * 0.5f;
                upperArm.localEulerAngles = armLocal;
            }

            public void FixWrist()
            {
                Vector3 wristLocal = Vector3.zero;
                wristLocal.y = Mathf.DeltaAngle(0.0f, hand.localEulerAngles.y) * 0.5f;
                wrist.localEulerAngles = wristLocal;
            }
        }

        // private void LateUpdatePostIKHoldStateLogic(){

        // holdStates[(int)Occupation.HEAD] = null;
        // holdStates[(int)Occupation.HAND_RIGHT] = ReloadHandState(
        // 	ref m_rightLoliHandState,
        // 	hand_r,
        // 	Occupation.HAND_RIGHT,
        // 	Quaternion.Euler(-14.0f,115.0f,50.0f),
        // 	CharacterCollisionCallback.CollisionPart.RIGHT_PALM
        // );
        // holdStates[(int)Occupation.HAND_LEFT] = ReloadHandState(
        // 	ref m_leftLoliHandState,
        // 	hand_l,
        // 	Occupation.HAND_LEFT,
        // 	Quaternion.Euler(-14.0f,-115.0f,-50.0f),
        // 	CharacterCollisionCallback.CollisionPart.LEFT_PALM
        // );
        // holdStates[(int)Occupation.SHOULDER_RIGHT] = ReloadShoulderState( shoulder_r, Occupation.SHOULDER_RIGHT );
        // holdStates[(int)Occupation.SHOULDER_LEFT] = ReloadShoulderState( shoulder_l, Occupation.SHOULDER_LEFT );


        private void ApplyAnimationIK()
        {

            rightLoliHandState.ApplyIK();
            leftLoliHandState.ApplyIK();
        }

        public void SetupArmIKForLateUpdate(LoliHandState targetHandstate, IKAnimation ikAnim)
        {

            if (transform.parent != null)
            {
                targetHandstate.holdArmIK.OverrideWorldRetargetingTransform(
                    targetHandstate.holdRetargeting,
                    transform.parent.TransformPoint(ikAnimationTargets[(int)ikAnim].target),
                    transform.parent.TransformPoint(ikAnimationTargets[(int)ikAnim].pole),
                    transform.parent.rotation * Quaternion.Euler(ikAnimationTargets[(int)ikAnim].eulerRotation)
                );
            }
            else
            {
                targetHandstate.holdArmIK.OverrideWorldRetargetingTransform(
                    targetHandstate.holdRetargeting,
                    spine1.TransformPoint(ikAnimationTargets[(int)ikAnim].target),
                    spine1.TransformPoint(ikAnimationTargets[(int)ikAnim].pole),
                    spine1.rotation * Quaternion.Euler(ikAnimationTargets[(int)ikAnim].eulerRotation)
                );
            }
        }
    }

}
