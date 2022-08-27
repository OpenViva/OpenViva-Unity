using UnityEngine;

namespace viva
{


    public partial class PolaroidFrame : Item
    {

        public enum PhotoSummary
        {
            GENERIC,
            PANTY
        }

        [SerializeField]
        private GameObject rippedPrefab;

        [SerializeField]
        private PolaroidFrameRippedFX rippedFX;

        [SerializeField]
        private MeshRenderer baseMeshRenderer;

        [SerializeField]
        public PhotoSummary photoSummary = PhotoSummary.GENERIC;

        [SerializeField]
        private SoundSet ripSounds;

        private GameObject rippedInstance;
        private Transform rightRipPiece = null;
        private Transform leftRipPiece = null;

        private bool HasRipped()
        {
            float pieceDist = Vector3.SqrMagnitude(rightRipPiece.position - leftRipPiece.position);
            if (pieceDist > 0.04f)
            {
                return true;
            }
            return false;
        }

        public override void OnItemLateUpdatePostIK()
        {
            if (mainOwner == null)
            {
                return;
            }
            switch (mainOwner.characterType)
            {
                case Character.Type.PLAYER:
                    Player player = mainOwner as Player;
                    if (player.controls == Player.ControlType.KEYBOARD)
                    {
                        UpdatePlayerKeyboardPolaroidFrameInteraction(player, mainOccupyState as PlayerHandState);
                    }
                    else
                    {
                        UpdatePlayerVRPolaroidFrameInteraction(player, (PlayerHandState)mainOccupyState);
                    }
                    break;
            }
        }

        public override void OnPreDrop()
        {

            switch (mainOwner.characterType)
            {
                case Character.Type.PLAYER:
                    Player player = mainOwner as Player;
                    player.EndVRAnimatorBlend();
                    PlayerHandState otherHoldState = mainOccupyState == player.rightHandState ?
                        player.leftHandState as PlayerHandState :
                        player.rightHandState as PlayerHandState;
                    if (otherHoldState.animSys.currentAnim == Player.Animation.POLAROID_RIP_IN_LEFT ||
                        otherHoldState.animSys.currentAnim == Player.Animation.POLAROID_RIP_IN_RIGHT)
                    {

                        otherHoldState.animSys.SetTargetAndIdleAnimation(Player.Animation.IDLE);
                    }
                    break;
            }
            //if object was ripped, destroy it entirely when dropped
            if (rippedInstance != null)
            {
                Destroy(rippedInstance);
                Destroy(this.gameObject);
            }
        }

        private void UpdatePlayerVRPolaroidFrameInteraction(Player player, PlayerHandState mainHoldState)
        {
            HandState otherHoldState = mainHoldState == player.rightHandState ? player.leftHandState : player.rightHandState;
            //this animation is tied to both hands only, so just check one hand

            Player.HandAnimationSystem rightHandAnimSys = (player.rightHandState as PlayerHandState).animSys;
            bool ripPrepareAnimActive = rightHandAnimSys.currentAnim == Player.Animation.POLAROID_RIP_IN_RIGHT ||
                                        rightHandAnimSys.currentAnim == Player.Animation.POLAROID_RIP_IN_LEFT;
            if (otherHoldState.holdType != HoldType.NULL)
            {
                //if otherHoldState becomes busy during a ripPrepare, end the VRAnimatorBlend
                if (ripPrepareAnimActive)
                {
                    player.EndVRAnimatorBlend();
                }
                return;
            }

            PolaroidFrame polaroidFrame = (mainHoldState.heldItem as PolaroidFrame);
            //if currently ripping
            if (rippedInstance != null)
            {
                //maintain targetbone in the middle so ripping deformation comes from the center
                Vector3 midPoint = (mainHoldState.fingerAnimator.targetBone.position + otherHoldState.fingerAnimator.targetBone.position) * 0.5f;
                midPoint -= (mainHoldState.fingerAnimator.targetBone.up + otherHoldState.fingerAnimator.targetBone.up) * 0.027f;

                mainHoldState.fingerAnimator.targetBone.position = midPoint;
                mainHoldState.fingerAnimator.targetBone.rotation = player.head.rotation;
                otherHoldState.fingerAnimator.targetBone.position = midPoint;
                otherHoldState.fingerAnimator.targetBone.rotation = player.head.rotation;

                if (polaroidFrame.HasRipped())
                {
                    mainHoldState.AttemptDrop();
                }
                return;
            }
            //if hands come close enough
            Transform rightHandAbsolute = player.rightHandState.transform;
            Transform leftHandAbsolute = player.leftHandState.transform;
            if (Vector3.SqrMagnitude(rightHandAbsolute.position - leftHandAbsolute.position) < 0.06f)
            {

                if (!ripPrepareAnimActive)
                {
                    if (mainHoldState == player.rightHandState)
                    {
                        player.AttemptBeginVRAnimatorBlend(Player.Animation.POLAROID_RIP_IN_RIGHT, new Vector3(0.0f, -0.2f, 0.0f));
                    }
                    else
                    {
                        player.AttemptBeginVRAnimatorBlend(Player.Animation.POLAROID_RIP_IN_LEFT, new Vector3(0.0f, -0.2f, -0.0f));
                    }
                }
                else if (player.rightPlayerHandState.actionState.isHeldDown && player.leftPlayerHandState.actionState.isHeldDown)
                {
                    polaroidFrame.SpawnRippedInstance(player.rightHandState.fingerAnimator.targetBone, player.leftHandState.fingerAnimator.targetBone);
                    player.AllowVRAnimatorBlendHandPositions();
                }
                //if moves out of animator blend distance
            }
            else if (ripPrepareAnimActive && !(player.rightPlayerHandState.actionState.isHeldDown && player.leftPlayerHandState.actionState.isHeldDown))
            {
                player.EndVRAnimatorBlend();

                //hands are distant
            }
            else if (mainHoldState.actionState.isDown)
            {
                AttemptPlacePolaroidFrame(mainHoldState);
                mainHoldState.actionState.Consume();
            }
        }

        private void UpdatePlayerKeyboardPolaroidFrameInteraction(Player player, PlayerHandState mainHoldState)
        {
            PlayerHandState otherHoldState = mainHoldState.otherPlayerHandState;

            if (otherHoldState.holdType != HoldType.NULL)
            {
                if (mainHoldState.actionState.isDown)
                {
                    if (AttemptPlacePolaroidFrame(mainHoldState))
                    {
                        mainHoldState.actionState.Consume();
                        mainHoldState.gripState.Consume();
                    }
                }
                return;
            }
            if (player.GetAnimator().IsInTransition(0))
            {
                return;
            }

            Player.HandAnimationSystem rightHandAnimSys = (player.rightHandState as PlayerHandState).animSys;
            Player.HandAnimationSystem leftHandAnimSys = (player.leftHandState as PlayerHandState).animSys;

            if (mainHoldState.actionState.isDown)
            {
                if (rightHandAnimSys.currentAnim == Player.Animation.POLAROID || leftHandAnimSys.currentAnim == Player.Animation.POLAROID)
                {
                    //if idling with polaroid, attempt placement or begin rip animation
                    if (AttemptPlacePolaroidFrame(mainHoldState))
                    {
                        mainHoldState.actionState.Consume();
                        mainHoldState.gripState.Consume();
                    }
                    else
                    {
                        if (mainHoldState == player.rightHandState)
                        {
                            rightHandAnimSys.SetTargetAnimation(Player.Animation.POLAROID_RIP_IN_RIGHT);
                            leftHandAnimSys.SetTargetAnimation(Player.Animation.POLAROID_RIP_IN_RIGHT);
                        }
                        else
                        {
                            rightHandAnimSys.SetTargetAnimation(Player.Animation.POLAROID_RIP_IN_LEFT);
                            leftHandAnimSys.SetTargetAnimation(Player.Animation.POLAROID_RIP_IN_LEFT);
                        }
                    }
                }
                else if (rightHandAnimSys.currentAnim == Player.Animation.POLAROID_RIP_IN_RIGHT || rightHandAnimSys.currentAnim == Player.Animation.POLAROID_RIP_IN_LEFT)
                {
                    bool hand1 = mainHoldState.actionState.isDown;
                    bool hand2 = otherHoldState.gripState.isDown;
                    if (hand1 || hand2)
                    {
                        if (hand1 && hand2)
                        {
                            //begin rip
                            rightHandAnimSys.SetTargetAnimation(Player.Animation.POLAROID_RIP);
                            leftHandAnimSys.SetTargetAnimation(Player.Animation.POLAROID_RIP);
                            SpawnRippedInstance(player.rightHandState.fingerAnimator.targetBone, player.leftHandState.fingerAnimator.targetBone);
                        }
                        else
                        {
                            //cancel rip
                            if (mainHoldState == player.rightHandState)
                            {
                                rightHandAnimSys.SetTargetAnimation(Player.Animation.POLAROID);
                                leftHandAnimSys.SetTargetAnimation(Player.Animation.IDLE);
                            }
                            else
                            {
                                rightHandAnimSys.SetTargetAnimation(Player.Animation.IDLE);
                                leftHandAnimSys.SetTargetAnimation(Player.Animation.POLAROID);
                            }
                        }
                    }
                }
            }
            if (rippedInstance != null)
            {
                if (this.HasRipped())
                {
                    mainHoldState.AttemptDrop();
                }
            }
        }

        private bool AttemptPlacePolaroidFrame(PlayerHandState mainHoldState)
        {

            if (GamePhysics.GetRaycastInfo(transform.position, transform.forward, 0.3f, WorldUtil.wallsMask))
            {
                mainHoldState.AttemptDrop();
                rigidBody.Sleep();
                transform.position = GamePhysics.result().point + GamePhysics.result().normal * 0.01f;
                transform.rotation = Quaternion.LookRotation(-GamePhysics.result().normal, transform.up);
                return true;
            }

            return false;
        }

        public PolaroidFrameRippedFX SpawnRipFXParticleEmitter()
        {
            return Instantiate(rippedFX, transform.position, Quaternion.identity);
        }

        public void SpawnRippedInstance(Transform rightHandTarget, Transform leftHandTarget)
        {

            if (rippedInstance != null)
            {
                return;
            }
            baseMeshRenderer.enabled = false;
            rippedInstance = Instantiate(rippedPrefab, transform.position, transform.rotation);
            SkinnedMeshRenderer smr = rippedInstance.transform.GetChild(0).GetComponent(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
            smr.materials[1].mainTexture = baseMeshRenderer.materials[1].mainTexture;

            Transform root_l = rippedInstance.transform.GetChild(1).GetChild(0);
            root_l.SetParent(leftHandTarget, true);
            Transform root_r = rippedInstance.transform.GetChild(1).GetChild(0);
            root_r.SetParent(rightHandTarget, true);

            rightRipPiece = root_r.GetChild(1);
            leftRipPiece = root_l.GetChild(1);

            leftRipPiece.SetParent(leftHandTarget.parent, true);
            rightRipPiece.SetParent(rightHandTarget.parent, true);

            PlayRipSound();
        }

        public void PlayRipSound()
        {
            SoundManager.main.RequestHandle(transform.position).PlayOneShot(ripSounds.GetRandomAudioClip());
        }
    }

}