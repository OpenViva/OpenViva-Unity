using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SpatialTracking;

namespace viva
{

    public partial class PlayerHandState : HandState
    {

        private class HoverItem
        {
            public readonly Item item;
            public int characterTriggerMask = 0;

            public HoverItem(Item _item)
            {
                item = _item;
            }
        }

        public enum PlayerButton
        {
            GRIP,
            ACTION,
            TRACKPAD_BUTTON
        }

        [SerializeField]
        private MeshRenderer m_indicatorMR;
        public MeshRenderer indicatorMR { get { return m_indicatorMR; } }
        [SerializeField]
        private Texture2D defaultPickupTexture;
        [SerializeField]
        private TrackedPoseDriver m_behaviourPose;
        public TrackedPoseDriver behaviourPose { get { return m_behaviourPose; } }
        [SerializeField]
        private Transform m_absoluteHandTransform;
        public Transform absoluteHandTransform { get { return m_absoluteHandTransform; } }
        [SerializeField]
        private Transform directionPointer;
        public Vector3 directionPointing { get { return directionPointer.forward; } }
        public Vector3 directionPoint { get { return directionPointer.position; } }
        public GameObject directionPointerContainer { get { return directionPointer.gameObject; } }

        private Player.HandAnimationSystem m_animSys;
        public Player.HandAnimationSystem animSys { get { return m_animSys; } }
        public readonly ButtonState[] buttonStates = new ButtonState[]{
            new ButtonState(),
            new ButtonState(),
            new ButtonState()
        };
        public ButtonState gripState { get { return buttonStates[(int)PlayerButton.GRIP]; } }
        public ButtonState actionState { get { return buttonStates[(int)PlayerButton.ACTION]; } }
        public ButtonState trackpadButtonState { get { return buttonStates[(int)PlayerButton.TRACKPAD_BUTTON]; } }
        public Vector2 trackpadPos;
        public Vector3 trackedPosition;
        public Quaternion trackedRotation;

        private bool overrideAlt = false;
        private Vector3 rawAnimationLocalPos = Vector3.zero;
        private Quaternion rawAnimationLocalRot = Quaternion.identity;
        public Player player { get { return owner as Player; } }
        public PlayerHandState otherPlayerHandState { get { return rightSide ? player.leftPlayerHandState : player.rightPlayerHandState; } }

        private Item m_nearestItem = null;
        public Item nearestItem { get { return m_nearestItem; } }
        private Coroutine generateBubblesCoroutine = null;
        private GameObject bubbles = null;
        private Texture defaultIndicatorTexture;
        private Set<HoverItem> hoverItems = new Set<HoverItem>();
        private float nearbyItemTimer = 0.0f;
        private bool updateVisualTransform = false;
        private bool vivaControlsBinded = false;
        private bool vrTrackpadBinded = false;

        private Vector3? freezeKeyboardLocalPosition = null;
        private Quaternion freezeKeyboardLocalRotation = Quaternion.identity;


        public void StartDeprecatedXRInput()
        {
            actionState.Consume();
        }

        private int buggySteamVRMultipleFiringFix = 0;
        // private void SteamVRTogglePauseMenu( SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource ){
        //     if( Time.frameCount != buggySteamVRMultipleFiringFix ){
        //         buggySteamVRMultipleFiringFix = Time.frameCount;
        //         if( fromSource == SteamVR_Input_Sources.RightHand && !GameDirector.settings.trackpadMovementUseRight ){
        //             player.TogglePauseMenu(); 
        //         }else if( fromSource == SteamVR_Input_Sources.LeftHand && GameDirector.settings.trackpadMovementUseRight ){
        //             player.TogglePauseMenu();
        //         }
        //     }
        // }

        public void UpdateSteamVRInput()
        {
            // if( SteamVR.active ){

            //     // SteamVR_Actions.player_Grab.onStateDown += delegate{ gripState.UpdateState( true ); };
            //     // SteamVR_Actions.player_Grab.onStateUp += delegate{ gripState.UpdateState( false ); };
            //     // SteamVR_Actions.player_Action.onStateDown += delegate{ actionState.UpdateState( true ); };
            //     // SteamVR_Actions.player_Action.onStateUp += delegate{ actionState.UpdateState( false ); };
            //     // SteamVR_Actions.player_TrackpadPress.onStateDown += delegate{ trackpadButtonState.UpdateState( true ); };
            //     // SteamVR_Actions.player_TrackpadPress.onStateUp += delegate{ trackpadButtonState.UpdateState( false ); };
            //     // SteamVR_Actions.player_Trackpad.onUpdate += delegate{ trackpadPos = SteamVR_Actions.player_Trackpad.GetAxis( behaviourPose.inputSource ); };

            //     if( SteamVR_Actions.player_grab.GetStateDown( behaviourPose.inputSource ) ){
            //         gripState.UpdateState( true );
            //     }else if( SteamVR_Actions.player_grab.GetStateUp( behaviourPose.inputSource ) ){
            //         gripState.UpdateState( false );
            //     }
            //     if( SteamVR_Actions.player_action.GetStateDown( behaviourPose.inputSource ) ){
            //         actionState.UpdateState( true );
            //     }else if( SteamVR_Actions.player_action.GetStateUp( behaviourPose.inputSource ) ){
            //         actionState.UpdateState( false );
            //     }
            //     if( SteamVR_Actions.player_trackpadpress.GetStateDown( behaviourPose.inputSource ) ){
            //         trackpadButtonState.UpdateState( true );
            //     }else if( SteamVR_Actions.player_trackpadpress.GetStateUp( behaviourPose.inputSource ) ){
            //         trackpadButtonState.UpdateState( false );
            //     }
            //     trackpadPos = SteamVR_Actions.player_trackpad.GetAxis( behaviourPose.inputSource );
            // }
        }

        public void UnbindSteamVRInput()
        {

        }

        // public void InitializeDeprecatedMKBInput(InputActions_viva vivaControls)
        // {
        //     if (vivaControlsBinded)
        //     {
        //         return;
        //     }
        //     vivaControlsBinded = true;
        //     if (rightSide)
        //     {
        //         vivaControls.Keyboard.extendRight.performed += ctx => player.OnInputTogglePresentHand(this);
        //         vivaControls.Keyboard.rightInteract.performed += ctx => UpdateKeyboardGripAndAction(ctx.ReadValueAsButton(), player.keyboardAlt);
        //         vivaControls.Keyboard.rightInteract.canceled += ctx => UpdateKeyboardGripAndAction(ctx.ReadValueAsButton(), player.keyboardAlt);
        //     }
        //     else
        //     {
        //         vivaControls.Keyboard.extendLeft.performed += ctx => player.OnInputTogglePresentHand(this);
        //         vivaControls.Keyboard.leftInteract.performed += ctx => UpdateKeyboardGripAndAction(ctx.ReadValueAsButton(), player.keyboardAlt);
        //         vivaControls.Keyboard.leftInteract.canceled += ctx => UpdateKeyboardGripAndAction(ctx.ReadValueAsButton(), player.keyboardAlt);
        //     }
        // }

        public void InitializeUnityInputControls( InputActions_viva vivaControls ){
            Player player = owner as Player;

            Debug.Log("initializing " + rightSide);
            
            if( rightSide ) {
                vivaControls.VRRightHand.Position.performed += ctx => trackedPosition = ctx.ReadValue<Vector3>();
                vivaControls.VRRightHand.Rotation.performed += ctx => trackedRotation = ctx.ReadValue<Quaternion>();
                vivaControls.VRRightHand.Move.performed += ctx => trackpadPos = ctx.ReadValue<Vector2>();
                vivaControls.VRRightHand.Move.canceled += ctx => trackpadPos = ctx.ReadValue<Vector2>();
                vivaControls.VRRightHand.Select.performed += ctx => trackpadButtonState.UpdateState( ctx.ReadValueAsButton() );
                vivaControls.VRRightHand.Select.canceled += ctx => trackpadButtonState.UpdateState( ctx.ReadValueAsButton() );
                vivaControls.VRRightHand.Interact.performed += ctx => actionState.UpdateState( ctx.ReadValueAsButton() );
                vivaControls.VRRightHand.Interact.canceled += ctx => actionState.UpdateState( ctx.ReadValueAsButton() );
                vivaControls.VRRightHand.Grab.performed += ctx => gripState.UpdateState( ctx.ReadValue<float>()>0.5f );
                vivaControls.VRRightHand.Pause.performed += ctx => player.TogglePauseMenu();

                vivaControls.Keyboard.extendRight.performed += ctx => player.OnInputTogglePresentHand( this );
                vivaControls.Keyboard.rightInteract.performed += ctx => UpdateKeyboardGripAndAction( ctx.ReadValueAsButton(), player.keyboardAlt );
                vivaControls.Keyboard.rightInteract.canceled += ctx => UpdateKeyboardGripAndAction( ctx.ReadValueAsButton(), player.keyboardAlt );
                vivaControls.Keyboard.rightInteract.performed += ctx => actionState.UpdateState( ctx.ReadValueAsButton() );
                vivaControls.Keyboard.rightInteract.canceled += ctx => actionState.UpdateState( ctx.ReadValueAsButton() );
            } else {
                vivaControls.VRLeftHand.Position.performed += ctx => trackedPosition = ctx.ReadValue<Vector3>();
                vivaControls.VRLeftHand.Rotation.performed += ctx => trackedRotation = ctx.ReadValue<Quaternion>();
                vivaControls.VRLeftHand.Move.performed += ctx => trackpadPos = ctx.ReadValue<Vector2>();
                vivaControls.VRLeftHand.Move.canceled += ctx => trackpadPos = ctx.ReadValue<Vector2>();
                vivaControls.VRLeftHand.Select.performed += ctx => trackpadButtonState.UpdateState( ctx.ReadValueAsButton() );
                vivaControls.VRLeftHand.Select.canceled += ctx => trackpadButtonState.UpdateState( ctx.ReadValueAsButton() );
                vivaControls.VRLeftHand.Interact.performed += ctx => actionState.UpdateState( ctx.ReadValueAsButton() );
                vivaControls.VRLeftHand.Interact.canceled += ctx => actionState.UpdateState( ctx.ReadValueAsButton() );
                vivaControls.VRLeftHand.Grab.performed += ctx => gripState.UpdateState( ctx.ReadValue<float>()>0.5f );
                vivaControls.VRLeftHand.Pause.performed += ctx => player.TogglePauseMenu();

                vivaControls.Keyboard.extendLeft.performed += ctx => player.OnInputTogglePresentHand( this );
                vivaControls.Keyboard.leftInteract.performed += ctx => UpdateKeyboardGripAndAction( ctx.ReadValueAsButton(), player.keyboardAlt );
                vivaControls.Keyboard.leftInteract.canceled += ctx => UpdateKeyboardGripAndAction( ctx.ReadValueAsButton(), player.keyboardAlt );
                vivaControls.Keyboard.leftInteract.performed += ctx => actionState.UpdateState( ctx.ReadValueAsButton() );
                vivaControls.Keyboard.leftInteract.canceled += ctx => actionState.UpdateState( ctx.ReadValueAsButton() );
            }
        }    

        public void Initialize(Dictionary<Player.Animation, Player.PlayerAnimationInfo> animationInfos)
        {

            //right side uses layer 0, left is 1
            m_animSys = new Player.HandAnimationSystem(owner as Player, System.Convert.ToInt32(!rightSide), animationInfos);
            defaultIndicatorTexture = m_indicatorMR.material.mainTexture;

            //place into the world
            // selfItem.transform.SetParent( null, true );
            selfItem.rigidBody.maxAngularVelocity = 64.0f;
            selfItem.rigidBody.maxDepenetrationVelocity = 0.001f;

            behaviourPose.transform.SetParent(player.transform, true);

            // SteamVR_Actions.player_pause.AddOnStateDownListener( SteamVRTogglePauseMenu, behaviourPose.inputSource );
        }

        protected override void GetRigidBodyBlendConnectedAnchor(out Vector3 targetLocalPos, out Quaternion targetLocalRot)
        {
            targetLocalPos = fingerAnimator.wrist.InverseTransformPoint(fingerAnimator.targetBone.position) * 1.375f;
            targetLocalRot = Quaternion.Inverse(fingerAnimator.wrist.rotation) * fingerAnimator.targetBone.rotation;
        }

        protected override void OnBeginRigidBodyGrab()
        {
            Player player = owner as Player;
            onItemChange += player.OnPlayerPickupEnd;
            m_indicatorMR.gameObject.SetActive(false);
        }

        protected override void OnStopRigidBodyGrab()
        {
            Player player = owner as Player;
            onItemChange -= player.OnPlayerPickupEnd;
            m_animSys.SetTargetAndIdleAnimation(Player.Animation.IDLE);
            m_nearestItem = null;

            freezeKeyboardLocalPosition = null;
        }

        public void SetAbsoluteVROffsets(Vector3 localPos, Vector3 localRot, bool relativeToRight)
        {
            if (relativeToRight)
            {
                if (!rightSide)
                {
                    localPos.x *= -1;
                    localRot *= -1;
                }
            }
            else
            {
                if (rightSide)
                {
                    localPos.x *= -1;
                    localRot *= -1;
                }
            }

            absoluteHandTransform.localPosition = localPos;
            absoluteHandTransform.localRotation = Quaternion.Euler(localRot);
        }

        public void CleanHand()
        {

            if (HasAttribute(HandState.Attribute.SOAPY))
            {
                if (bubbles == null)
                {
                    Debug.LogError("ERROR SOAPY bubbles is null, cannot clean!");
                }
                else
                {
                    Destroy(bubbles);
                    bubbles = null;
                    SetAttribute(Attribute.NONE);
                }
            }
        }

        public void GenerateBubbles(GameObject prefab, bool flipBubbleScaleX)
        {
            if (bubbles != null)
            {  //do not generate bubbles twice
                return;
            }
            if (generateBubblesCoroutine != null)
            {
                return;
            }
            generateBubblesCoroutine = owner.StartCoroutine(GenerateBubblesAnimation(prefab, flipBubbleScaleX));
        }

        private IEnumerator GenerateBubblesAnimation(GameObject prefab, bool flipBubbleScaleX)
        {

            bubbles = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            bubbles.transform.SetParent(fingerAnimator.hand, false);
            if (flipBubbleScaleX)
            {
                bubbles.transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
            }

            MeshRenderer bubblesMR = bubbles.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
            if (bubblesMR == null)
            {
                Debug.LogError("generateBubblesObj doesn't have a Mesh Renderer!");
                Debug.Break();
            }
            Material bubblesMaterial = bubblesMR.material;

            const float growTime = 2.0f;
            float timer = growTime;
            while (timer > 0.0f)
            {
                timer = Mathf.Max(0.0f, timer - Time.deltaTime);
                bubblesMaterial.SetFloat(Instance.bubbleSizeID, (1.0f - (timer / growTime)) * 0.025f);
                yield return null;
            }
            SetAttribute(HandState.Attribute.SOAPY);

            generateBubblesCoroutine = null;
        }

        protected override void OnPostPickupItem()
        {
            base.OnPostPickupItem();
        }

        public void AddToNearestItems(Item item, CharacterTriggerCallback.Type sourceCharacterTriggerType)
        {
            HoverItem hoverItem = null;
            foreach (HoverItem candidate in hoverItems.objects)
            {
                if (candidate.item == item)
                {
                    hoverItem = candidate;
                    break;
                }
            }
            if (hoverItem == null)
            {
                hoverItem = new HoverItem(item);
                hoverItems.objects.Add(hoverItem);
            }
            hoverItem.characterTriggerMask |= 1 << (int)sourceCharacterTriggerType;
        }

        public void RemoveFromNearestItems(Item item, CharacterTriggerCallback.Type sourceCharacterTriggerType)
        {
            HoverItem hoverItem = null;
            foreach (HoverItem candidate in hoverItems.objects)
            {
                if (candidate.item == item)
                {
                    hoverItem = candidate;
                    break;
                }
            }
            if (hoverItem == null)
            {
                return;
            }
            hoverItem.characterTriggerMask = ~((~hoverItem.characterTriggerMask) | (1 << (int)sourceCharacterTriggerType));

            if (hoverItem.characterTriggerMask == 0)
            {
                hoverItems.Remove(hoverItem);
            }
        }

        private void UpdateNearestItemIndicator()
        {

            if (m_nearestItem != null)
            {
                m_nearestItem.SetEnableStatusBar(true);
                indicatorMR.gameObject.SetActive(true);
                indicatorMR.gameObject.transform.position = m_nearestItem.transform.TransformPoint(m_nearestItem.indicatorOffset);
                Texture2D itemPickupTexture = m_nearestItem.settings.pickupTexture;
                if (itemPickupTexture == null)
                {
                    itemPickupTexture = defaultPickupTexture;
                }
                indicatorMR.material.mainTexture = itemPickupTexture;
            }
        }

        public void FixedUpdateNearestItemIndicatorTimer()
        {
            nearbyItemTimer += Time.deltaTime;
            if (nearbyItemTimer < 0.1f)
            {
                UpdateNearestItemIndicator();
                return;
            }
            nearbyItemTimer %= 0.1f;
            //find nearest item
            Item newNearestItem = null;
            float leastDist = Mathf.Infinity;
            for (int i = hoverItems.objects.Count; i-- > 0;)
            {
                Item item = hoverItems.objects[i].item;
                if (item == null)
                {
                    //remove from nearestItems
                    hoverItems.objects.RemoveAt(i);
                    continue;
                }
                if (!item.CanBePickedUp(this))
                {
                    continue;
                }
                if (item.mainOwner == owner)
                {
                    continue;
                }
                float sqDist = Vector3.SqrMagnitude(item.transform.position - fingerAnimator.hand.position);
                if (sqDist < leastDist)
                {
                    leastDist = sqDist;
                    newNearestItem = item;
                }
            }
            indicatorMR.gameObject.SetActive(false);
            if (m_nearestItem != null)
            {
                m_nearestItem.SetEnableStatusBar(false);
            }
            m_nearestItem = newNearestItem;
            UpdateNearestItemIndicator();
        }

        private void SetIndicatorPickupTexture(Texture texture)
        {
            if (texture == null)
            {
                texture = defaultIndicatorTexture;
            }
            m_indicatorMR.material.mainTexture = texture;
        }

        public void CacheRawAnimationTransform()
        {
            rawAnimationLocalPos = fingerAnimator.hand.parent.localPosition;
            rawAnimationLocalRot = fingerAnimator.hand.parent.localRotation;
        }

        public void UpdateKeyboardGripAndAction(bool keyboardMain, bool keyboardAlt)
        {
            if (holdType == HoldType.OBJECT)
            {
                if (keyboardAlt || overrideAlt)
                {
                    if (!keyboardMain)
                    {
                        overrideAlt = false;
                    }
                    gripState.UpdateState(keyboardMain);
                    actionState.UpdateState(false);
                }
                else
                {
                    gripState.UpdateState(false);
                    actionState.UpdateState(keyboardMain);
                }
                //if not holding an object
            }
            else
            {
                gripState.UpdateState(keyboardMain);
                actionState.UpdateState(false);
            }
        }

        Collider CalculateNearbyGrabCollider(Vector3 dir, float length, ref Vector3 colliderPos, ref Vector3 colliderNormal)
        {
            Vector3 handCenter = selfItem.rigidBody.worldCenterOfMass;
            if (!GamePhysics.GetRaycastInfo(handCenter, dir, length, Instance.itemsMask, QueryTriggerInteraction.Ignore, 0.0f))
            {
                return null;
            }
            colliderPos = GamePhysics.result().point;
            colliderNormal = GamePhysics.result().normal;
            return GamePhysics.result().collider;
        }

        public bool AttemptGrabNearby()
        {

            Vector3 colliderPos = Vector3.zero;
            Vector3 colliderNormal = Vector3.zero;
            Collider collider = CalculateNearbyGrabCollider(-selfItem.transform.forward + selfItem.transform.up, 0.15f, ref colliderPos, ref colliderNormal);
            if (collider == null)
            {
                if (nearestItem)
                {
                    if (!nearestItem.settings.usePickupAnimation)
                    {
                        collider = nearestItem.GetComponentInChildren<Collider>();
                        if (collider)
                        {
                            colliderPos = collider.ClosestPoint(selfItem.rigidBody.worldCenterOfMass);
                            colliderNormal = (selfItem.rigidBody.worldCenterOfMass - colliderPos).normalized;
                            GrabGenericRigidBody(collider, colliderPos, colliderNormal);
                            return true;
                        }
                    }
                    GrabItemRigidBody(nearestItem);
                    return true;
                }
                return false;
            }
            GrabGenericRigidBody(collider, colliderPos, colliderNormal);
            return true;
        }

        protected override void OnPreApplyHoldingTransform(Item targetItem)
        {
            Player.Animation grabAnimation;
            if (targetItem == null)
            {
                grabAnimation = Player.Animation.GENERIC;
            }
            else
            {
                grabAnimation = targetItem.GetPreferredPlayerHeldAnimation(this);
            }
            animSys.SetAnimationImmediate(player.GetAnimator(), rightSide ? 0 : 1, grabAnimation);
            animSys.SetTargetAndIdleAnimation(grabAnimation);
            player.rightPlayerHandState.ForceApplyVisualTransform();
            player.leftPlayerHandState.ForceApplyVisualTransform();
        }

        protected override void OnPostApplyHoldingTransform(Transform grabTransform)
        {
            base.OnPostApplyHoldingTransform(grabTransform);

            fingerAnimator.wrist.position = selfItem.transform.position;
            fingerAnimator.wrist.rotation = selfItem.transform.rotation;

            if (player.controls == Player.ControlType.KEYBOARD)
            {
                if (heldItem == null || !heldItem.settings.usePickupAnimation)
                {
                    freezeKeyboardLocalPosition = owner.head.InverseTransformPoint(fingerAnimator.wrist.position);
                    freezeKeyboardLocalRotation = Quaternion.Inverse(owner.head.rotation) * fingerAnimator.wrist.rotation;
                }
            }
        }

        protected void ForceApplyVisualTransform()
        {
            updateVisualTransform = true;
            ApplyPhysicsTransform();
        }

        public void OverrideKeyboardAltUntilGripUp()
        {
            overrideAlt = true;
        }

        public void ApplyCachedRawTransform()
        {
            Transform wrist = fingerAnimator.hand.parent;
            wrist.localPosition = rawAnimationLocalPos;
            wrist.localRotation = rawAnimationLocalRot;
        }

        public void ApplyRigidBodyTransform()
        {

            float forceMultiplier;
            if (heldItem != null)
            {
                forceMultiplier = blendProgress;
            }
            else
            {
                forceMultiplier = 1.0f;
            }

            if (freezeKeyboardLocalPosition.HasValue)
            {
                fingerAnimator.wrist.position = owner.head.TransformPoint(freezeKeyboardLocalPosition.Value);
                fingerAnimator.wrist.rotation = owner.head.rotation * freezeKeyboardLocalRotation;
            }

            Vector3 targetRigidBodyPos = fingerAnimator.wrist.position;
            Quaternion targetRigidBodyRot = fingerAnimator.wrist.rotation;
            Vector3 posDelta = targetRigidBodyPos - selfItem.transform.position;
            if (posDelta.sqrMagnitude > 0.75f)
            {   //ensure it cannot get stuck
                selfItem.transform.position = targetRigidBodyPos;
            }
            else
            {
                selfItem.rigidBody.AddForce(posDelta * 64.0f * forceMultiplier, ForceMode.VelocityChange);
            }
            Quaternion rotForce = targetRigidBodyRot * Quaternion.Inverse(selfItem.transform.rotation);
            float rotForceScalar = 128.0f * rotForce.w * forceMultiplier;
            selfItem.rigidBody.AddTorque(rotForce.x * rotForceScalar, rotForce.y * rotForceScalar, rotForce.z * rotForceScalar, ForceMode.VelocityChange);

            updateVisualTransform = true;
        }

        public void ApplyPhysicsTransform()
        {
            if (updateVisualTransform)
            {
                updateVisualTransform = false;

                fingerAnimator.wrist.position = selfItem.rigidBody.position;
                fingerAnimator.wrist.rotation = selfItem.rigidBody.rotation;
            }
        }
    }

}