using UnityEngine;

namespace viva
{

    public class RigidBodyBlend : MonoBehaviour
    {

        public delegate void OnJointBreakCallback();

        private Joint joint;
        public OnJointBreakCallback onJointBreakCallback = null;
        private Quaternion startLocalRotation;
        private Quaternion jointOffset;
        private Rigidbody targetBody;


        private void OnJointBreak()
        {
            if (onJointBreakCallback != null)
            {
                onJointBreakCallback();
                onJointBreakCallback = null;
                if (joint != null)
                {
                    GameObject.Destroy(joint);
                    joint = null;
                    if (targetBody)
                    {
                        var pickupEventCallback = targetBody.gameObject.GetComponent<RigidBodyPickupEvent>();
                        if (pickupEventCallback)
                        {
                            pickupEventCallback.onDrop?.Invoke();
                        }
                    }
                }
                GameObject.Destroy(this);
            }
        }

        public void Begin(Rigidbody _targetBody, Rigidbody connectedBody, bool useFixedJoint, OnJointBreakCallback onBreakCallback, float _connectedMassScale)
        {
            if (_targetBody == null || connectedBody == null)
            {
                return;
            }
            targetBody = _targetBody;
            if (IsValid())
            {
                return;
            }

            var pickupEventCallback = targetBody.gameObject.GetComponent<RigidBodyPickupEvent>();
            if (pickupEventCallback)
            {
                pickupEventCallback.onPickup?.Invoke();
            }

            if (useFixedJoint)
            {
                var fixedJoint = targetBody.gameObject.AddComponent<FixedJoint>();
                fixedJoint.connectedBody = connectedBody;
                joint = fixedJoint;
            }
            else
            {
                var configJoint = targetBody.gameObject.AddComponent<ConfigurableJoint>();
                configJoint.autoConfigureConnectedAnchor = false;
                configJoint.connectedBody = connectedBody;
                configJoint.anchor = Vector3.zero;
                configJoint.connectedAnchor = Vector3.zero;
                configJoint.xMotion = ConfigurableJointMotion.Limited;
                configJoint.yMotion = ConfigurableJointMotion.Limited;
                configJoint.zMotion = ConfigurableJointMotion.Limited;
                var drive = configJoint.xDrive;
                drive.positionSpring = 100000.0f;
                drive.positionDamper = 10.0f;
                configJoint.xDrive = drive;
                configJoint.yDrive = drive;
                configJoint.zDrive = drive;
                var sjl = configJoint.linearLimit;
                sjl.limit = 10.0f;
                configJoint.linearLimit = sjl;

                configJoint.slerpDrive = drive;
                configJoint.rotationDriveMode = RotationDriveMode.Slerp;

                joint = configJoint;
            }

            // joint.connectedMassScale = 0.0001f;
            // joint.massScale = 0.0f;
            onJointBreakCallback = onBreakCallback;

            startLocalRotation = targetBody.gameObject.transform.rotation;
            jointOffset = connectedBody.gameObject.transform.rotation;
        }


        public bool IsValid()
        {
            return joint != null && joint.connectedBody != null;
        }

        public void Break()
        {
            OnJointBreak();
        }

        public void Blend(float blendValue, Vector3 targetLocalPos, Quaternion targetLocalRot, float additionalConnectedMassScale)
        {

            if (!IsValid())
            {
                OnJointBreak();
                return;
            }

            ConfigurableJoint configJoint = joint as ConfigurableJoint;
            if (configJoint)
            {
                configJoint.targetPosition = Quaternion.Inverse(targetLocalRot) * -targetLocalPos;
                configJoint.SetTargetRotationLocal(jointOffset * targetLocalRot, startLocalRotation);
            }
            // joint.connectedMassScale = blendValue;
            joint.massScale = 1.0f + blendValue * additionalConnectedMassScale;
            // const float safetyForce = 1000000.0f;
            // float force = safetyForce-(safetyForce-2500.0f)*blendValue;
            // joint.breakForce = force;
            // joint.breakTorque = force;
        }
    }


}