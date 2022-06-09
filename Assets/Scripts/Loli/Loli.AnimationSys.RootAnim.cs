using UnityEngine;


namespace viva
{


    public partial class Loli : Character
    {

        public enum TransformOffsetAnimation
        {
            HORSE_MOUNT_RIGHT,
            HORSE_MOUNT_LEFT,
            HORSE_IDLE
        }

        [SerializeField]
        public RootAnimationOffset[] rootAnimationOffsets = new RootAnimationOffset[System.Enum.GetValues(typeof(TransformOffsetAnimation)).Length];


        public delegate void AnchorFinishCallback();

        public int faceYawDisableSum { get; private set; } = 0;
        private bool currentAnimFaceYawToggle = false;
        private bool currentAnimAwarenessModeBinded = false;
        private Quaternion cachedAnchorRotation;
        private Vector3 cachedAnchorPosition;
        private RootAnimationOffset rootAnchorOffset = null;
        private Tools.EaseBlend anchorBlend = new Tools.EaseBlend();
        private AnchorFinishCallback onAnchorFinish;
        public bool anchorActive { get; private set; } = false;
        private bool stopRootAnchorAfterFinished = false;
        private Quaternion spineAnchorRotation;
        private Vector3 spineAnchorPosition;
        private Transform spineAnchorReference;
        private bool? enteredTransition = null;
        private bool canExitSpineAnchor = false;


        private void ModifyAnchorAnimation()
        {
            if (!hasBalance)
            {
                return;
            }

            anchorBlend.Update(Time.deltaTime);
            anchor.localRotation = Quaternion.LerpUnclamped(
                cachedAnchorRotation,
                Quaternion.Euler(rootAnchorOffset.eulerRotation),
                anchorBlend.value
            );
            anchor.localPosition = Vector3.LerpUnclamped(
                cachedAnchorPosition,
                rootAnchorOffset.position,
                anchorBlend.value
            );
        }

        public void ApplyDisableFaceYaw(ref bool source)
        {
            if (!source)
            {
                source = true;
                faceYawDisableSum++;
            }
        }
        public void RemoveDisableFaceYaw(ref bool source)
        {
            if (source)
            {
                source = false;
                faceYawDisableSum--;
            }
        }
        public bool IsFaceYawAnimationEnabled()
        {
            return faceYawDisableSum <= 0;
        }

        public void AnchorSpineUntilTransitionEnds(Transform reference)
        {

            if (spineAnchorReference == null)
            {
                enteredTransition = null;
                canExitSpineAnchor = false;
                onModifyAnimations += AnchorSpineTransition;

                spineAnchorReference = reference;

                spineAnchorPosition = spineAnchorReference.InverseTransformPoint(spine1.position);
                spineAnchorRotation = Quaternion.Inverse(spineAnchorReference.rotation) * spine1.rotation;
            }
        }

        public void StopAnchorSpineTransition()
        {
            spineAnchorReference = null;
        }

        private void AnchorSpineTransition()
        {
            bool inTransition = animator.IsInTransition(1);
            if (inTransition)
            {
                if (enteredTransition == null)
                {
                    enteredTransition = true;
                }
            }
            else if (enteredTransition.HasValue && enteredTransition.Value == true)
            {
                canExitSpineAnchor = true;
            }
            if (canExitSpineAnchor || spineAnchorReference == null)
            {
                onModifyAnimations -= AnchorSpineTransition;
                // onModifyAnimations += ResetAnchorRotationAfter;
            }
            else
            {
                Vector3 oldSpine1Pos = spine1.position;
                Quaternion oldSpine1Rot = spine1.rotation;

                spine1.position = spineAnchorReference.TransformPoint(spineAnchorPosition);
                spine1.rotation = spineAnchorReference.rotation * spineAnchorRotation;

                Vector3 finalSpine1Pos = spine1.position;
                Quaternion finalSpine1Rot = spine1.rotation;

                Quaternion diff = finalSpine1Rot * Quaternion.Inverse(oldSpine1Rot);
                anchor.rotation = diff * spineAnchorReference.rotation;
                anchor.position += finalSpine1Pos - oldSpine1Pos;


                spine1.position = finalSpine1Pos;
                spine1.rotation = finalSpine1Rot;
            }
        }

        public void BeginAnchorTransformAnimation(
                RootAnimationOffset _rootAnchorOffset,
                float transitionLength,
                AnchorFinishCallback _onAnchorFinish = null,
                bool _stopRootAnchorAfterFinished = true
                )
        {
            cachedAnchorPosition = anchor.localPosition;
            cachedAnchorRotation = anchor.localRotation;
            rootAnchorOffset = _rootAnchorOffset;
            onAnchorFinish = _onAnchorFinish;
            stopRootAnchorAfterFinished = _stopRootAnchorAfterFinished;

            if (!anchorActive)
            {
                AddModifyAnimationCallback(ModifyAnchorAnimation);
            }

            anchorActive = true;
            anchorBlend.reset(0.0f);
            anchorBlend.StartBlend(1.0f, transitionLength);
        }

        public void StopActiveAnchor()
        {
            if (anchorActive)
            {
                RemoveModifyAnimationCallback(ModifyAnchorAnimation);
                anchorActive = false;
                onModifyAnimations += ResetAnchorRotationAfter;
            }
        }
    }

}