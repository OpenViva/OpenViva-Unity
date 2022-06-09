using System.Collections;
using UnityEngine;

namespace viva
{


    public class Container : Item
    {

        protected delegate void FillAmountChangeCallback(float amountChange);

        [SerializeField]
        private bool m_allowLid = false;
        public bool allowLid { get { return m_allowLid; } }
        [Range(0.01f, 0.3f)]
        [SerializeField]
        private float m_lidHeight = 0.1f;
        public float lidHeight { get { return m_lidHeight; } }
        [Range(-0.05f, 0.05f)]
        [SerializeField]
        private float m_lidPlacementHeightOffset = 0.0f;
        public float lidPlacementHeightOffset { get { return m_lidPlacementHeightOffset; } }
        [Range(0.01f, 0.3f)]
        [SerializeField]
        private float lidRadius = 0.1f;
        [Range(0.005f, 0.05f)]
        [SerializeField]
        private float lidThickness = 0.1f;
        [SerializeField]
        protected SkinnedMeshRenderer substanceGrowSMR;
        [Range(1.0f, 10.0f)]
        [SerializeField]
        protected float maxSubstanceAmount = 10.0f;
        [Range(0.0f, 10.0f)]
        [SerializeField]
        protected float m_substanceAmount = 0.0f;
        [VivaFileAttribute]
        public float substanceAmount { get { return m_substanceAmount; } protected set { m_substanceAmount = value; } }
        [SerializeField]
        private bool instantSpill = true;
        [SerializeField]
        private AudioClip fillSound;
        [SerializeField]
        private AudioClip spillSound;
        [SerializeField]
        protected SubstanceSpill.Substance storageSubstance;
        [SerializeField]
        private string substanceGrowBSName = "flourGrow";
        [SerializeField]
        private string substanceName = "Flour";
        [SerializeField]
        private bool hideSMRIfEmpty = true;
        [SerializeField]
        protected ParticleSystem substanceSpillFX;
        [SerializeField]
        protected SubstanceSpill substanceSpill;
        [SerializeField]
        private SubstanceSurfaceSim substanceSurfaceSim;
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float liquidPitchBSEnd = 0.3f;
        private static readonly int mixLoopID = Animator.StringToHash("mixLoop");
        [Range(0.0f, 0.2f)]
        [SerializeField]
        private float mixerSphereHeight;
        [Range(0.0f, 0.4f)]
        [SerializeField]
        private float mixerSphereRadius;
        [HideInInspector]
        [SerializeField]
        private Player.Animation[] playerKeyboardMixAnimations;
        [SerializeField]
        private Player.Animation emptyContentsAnim = Player.Animation.IDLE;

        public enum PlayerKeyboardMixAnimType
        {
            MIX_IN_RIGHT,
            MIX_IN_LEFT,
            MIX_RIGHT_LOOP,
            MIX_LEFT_LOOP
        }

        private ContainerLid lid = null;
        public bool isLidClosed { get { return lid != null; } }
        protected int substanceGrowBS;
        private int substancePitchPosBS;
        private int substancePitchNegBS;
        private bool tipReset = true;
        private Coroutine liquidPitchCoroutine;
        private float liquidPitchVel = 0.0f;
        private bool isMixing = false;
        private float lastPickupTime = 0.0f;


        protected override void OnItemAwake()
        {
            substanceGrowBS = substanceGrowSMR.sharedMesh.GetBlendShapeIndex(substanceGrowBSName);
            substancePitchPosBS = substanceGrowSMR.sharedMesh.GetBlendShapeIndex("pitch+");
            substancePitchNegBS = substanceGrowSMR.sharedMesh.GetBlendShapeIndex("pitch-");
            ChangeSubstanceAmount(0.0f);
            CheckLiquidMove();
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            CheckLiquidMove();
        }

        private void CheckLiquidMove()
        {
            if (substanceSurfaceSim == null)
            {
                return;
            }
            //start sim only if it is tipping over or liquid is not balanced
            float liquidPitchRatio = GetLiquidPitchRatio();
            if (liquidPitchRatio < 0.05f && substanceGrowSMR.GetBlendShapeWeight(substancePitchPosBS) < 4.0f)
            {
                return;
            }
            if (liquidPitchCoroutine != null)
            {
                return;
            }
            substanceGrowSMR.SetBlendShapeWeight(substancePitchPosBS, liquidPitchRatio * 100.0f);
            liquidPitchCoroutine = GameDirector.instance.StartCoroutine(LiquidPitchAnimate());
        }

        private float GetLiquidPitchRatio()
        {
            float forceUp = (transform.up - rigidBody.velocity * substanceSurfaceSim.parentVelocityInfluence).normalized.y;
            return 1.0f - Tools.GetClampedRatio(liquidPitchBSEnd, 0.98f, forceUp);
        }

        private IEnumerator LiquidPitchAnimate()
        {

            float sleepTimeout = 0.0f;
            do
            {
                float currPitchRatio = substanceGrowSMR.GetBlendShapeWeight(substancePitchPosBS) / 100.0f;
                float newPitchRatio = GetLiquidPitchRatio();
                liquidPitchVel *= substanceSurfaceSim.friction;
                liquidPitchVel += (newPitchRatio - currPitchRatio) * Time.fixedDeltaTime * substanceSurfaceSim.acceleration;
                float newBSVal = (currPitchRatio + liquidPitchVel) * 100.0f;
                substanceGrowSMR.SetBlendShapeWeight(substancePitchPosBS, newBSVal);
                substanceGrowSMR.SetBlendShapeWeight(substancePitchNegBS, -newBSVal);

                Vector3 up = transform.up;
                if (up.y < 0.0f)
                {
                    up.y = 0.0f;
                }
                Vector3 forceUp = (transform.up - rigidBody.velocity * substanceSurfaceSim.parentVelocityInfluence).normalized;
                forceUp = new Vector3(-forceUp.x, 0.0f, -forceUp.z);
                if (forceUp != Vector3.zero && up != Vector3.zero)
                {
                    substanceGrowSMR.transform.rotation = Quaternion.LerpUnclamped(
                        substanceGrowSMR.transform.rotation,
                        Quaternion.LookRotation(forceUp, up),
                        newPitchRatio * Time.fixedDeltaTime * substanceSurfaceSim.yawRotateSpeed
                    );
                }
                Vector3 localEuler = substanceGrowSMR.transform.localEulerAngles;
                localEuler.x = 0.0f;
                localEuler.z = 0.0f;
                substanceGrowSMR.transform.localEulerAngles = localEuler;
                if (Mathf.Abs(liquidPitchVel) < 0.001f)
                {
                    sleepTimeout += Time.fixedDeltaTime;
                }
                else
                {
                    sleepTimeout = 0.0f;
                }
                yield return new WaitForFixedUpdate();
            } while (sleepTimeout < 0.2f);

            liquidPitchCoroutine = null;
        }

        public void ChangeSubstanceAmount(float amountChange)
        {
            ChangeSpecificSubstanceAmount(substanceGrowSMR, maxSubstanceAmount, substanceGrowBS, amountChange, ref m_substanceAmount);
        }

        public bool SetLid(ContainerLid newLid)
        {
            if (!allowLid)
            {
                return false;
            }
            lid = newLid;
            return true;
        }

        protected virtual bool OnReceiveSubstanceSpill(SubstanceSpill.Substance substance, float spillAmount)
        {
            if (substance != storageSubstance)
            {
                return false;
            }
            if (isLidClosed)
            {
                return false;
            }
            if (fillSound != null)
            {
                SoundManager.main.RequestHandle(transform.position).PlayOneShot(fillSound);
            }
            GameDirector.instance.StartCoroutine(AnimationChangeFill(spillAmount, ChangeSubstanceAmount));
            return true;
        }

        protected virtual void OnSpillContents()
        {
            if (isLidClosed)
            {
                return;
            }
            if (m_substanceAmount > 0 && substanceSpill != null && substanceSpillFX != null)
            {

                if (tipReset && spillSound != null)
                {
                    SoundManager.main.RequestHandle(transform.position).PlayOneShot(spillSound);
                }
                if (tipReset && instantSpill)
                {
                    substanceGrowSMR.SetBlendShapeWeight(substanceGrowBS, 0.0f);
                    int spillAmount = Tools.SafeFloorToInt(m_substanceAmount);
                    substanceSpillFX.Emit(Mathf.Min(substanceSpillFX.main.maxParticles, spillAmount * 10));
                    substanceSpill.BeginInstantSpill(spillAmount);
                    substanceAmount = 0;
                }
                else
                {
                    substanceGrowSMR.SetBlendShapeWeight(substanceGrowBS, 0.0f);
                    SetSubstanceSpillFXEmission(true);
                    substanceSpill.BeginContinuousSpill(0.5f);
                }
                ChangeSubstanceAmount(0.0f);
                OnUpdateStatusBar();
            }
        }

        private void SetSubstanceSpillFXEmission(bool enable)
        {
            var emissionModule = substanceSpillFX.emission;
            emissionModule.enabled = enable;
        }

        protected override void OnUpdateStatusBar()
        {
            if (statusBar == null)
            {
                return;
            }
            string info = "";
            if (m_substanceAmount > 0.0f)
            {
                info = Mathf.CeilToInt(m_substanceAmount * 10) / 10 + " " + substanceName;
            }
            statusBar.SetInfoText(info);
        }

        public bool AttemptReceiveSubstanceSpill(SubstanceSpill.Substance substance, float spillAmount)
        {
            if (spillAmount <= 0.0f)
            {
                return false;
            }
            return OnReceiveSubstanceSpill(substance, spillAmount);
        }

        public override void OnItemLateUpdatePostIK()
        {
            base.OnItemLateUpdatePostIK();

            CheckLiquidMove();

            if (IsTippingOver())
            {
                OnSpillContents();
                tipReset = false;
            }
            else
            {
                if (!tipReset && !instantSpill)
                {
                    SetSubstanceSpillFXEmission(false);
                }
                tipReset = true;
            }
        }


        public override void OnItemLateUpdate()
        {
            if (mainOwner != null)
            {
                switch (mainOwner.characterType)
                {
                    case Character.Type.PLAYER:
                        Player player = mainOwner as Player;
                        UpdatePlayerKeyboardMixingInteraction<Pestle>(mainOwner as Player, false, substanceAmount > 0);
                        break;
                }
            }
        }

        public bool IsTippingOver()
        {
            //do not tip until one second after picking up
            if (Time.time - lastPickupTime < 0.5f)
            {
                return false;
            }
            return transform.up.y < liquidPitchBSEnd;
        }

        public bool IsPointWithinLidArea(Vector3 point)
        {

            Vector3 local = transform.InverseTransformPoint(point);
            float xzSqDist = local.x * local.x + local.z * local.z;
            if (xzSqDist > lidRadius * lidRadius)
            {
                return false;
            }
            return Mathf.Abs(local.y - m_lidHeight) < lidThickness;
        }

        protected IEnumerator AnimationChangeFill(float changeAmount, FillAmountChangeCallback animateCallback)
        {

            if (fillSound != null)
            {
                SoundManager.main.RequestHandle(transform.position).PlayOneShot(fillSound);
            }
            float timer = 0.0f;
            while (timer < 0.5f)
            {
                float lastTimer = timer;
                timer = Mathf.Clamp01(timer + Time.deltaTime);
                float deltaTimer = timer - lastTimer;

                //add percent to amount
                animateCallback(deltaTimer * 2.0f * changeAmount);
                yield return null;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(1.0f, 0.0f, 0.5f, 0.5f);
            float step = Mathf.PI * 2.0f / 32.0f;
            Gizmos.matrix = transform.localToWorldMatrix;
            if (allowLid)
            {
                Vector3? last = null;
                for (float radian = 0.0f; radian <= Mathf.PI * 2.0f; radian += step)
                {

                    Vector3 curr = Vector3.up * m_lidHeight;
                    curr.x += Mathf.Cos(radian) * lidRadius;
                    curr.z += Mathf.Sin(radian) * lidRadius;
                    if (last.HasValue)
                    {
                        Gizmos.DrawLine(last.Value - Vector3.up * lidThickness, curr - Vector3.up * lidThickness);
                        Gizmos.DrawLine(last.Value - Vector3.up * lidThickness, last.Value + Vector3.up * lidThickness);
                        Gizmos.DrawLine(last.Value + Vector3.up * lidThickness, curr + Vector3.up * lidThickness);
                    }
                    last = curr;
                }
                Gizmos.DrawSphere(Vector3.up * (lidPlacementHeightOffset + lidHeight), 0.01f);
            }

            if (playerKeyboardMixAnimations != null && playerKeyboardMixAnimations.Length == System.Enum.GetValues(typeof(PlayerKeyboardMixAnimType)).Length)
            {
                Gizmos.color = new Color(0.0f, 0.5f, 1.0f, 0.5f);
                Gizmos.DrawSphere(Vector3.up * mixerSphereHeight, -mixerSphereRadius);
            }
        }

        public bool IsPointInsideMixingHalfSphere(Vector3 point)
        {

            Vector3 local = transform.InverseTransformPoint(point);
            float sqDist = Vector3.SqrMagnitude(local - Vector3.up * mixerSphereHeight);
            if (sqDist > mixerSphereRadius * mixerSphereRadius)
            {
                return false;
            }
            return local.y < mixerSphereHeight;
        }

        protected void ChangeSpecificSubstanceAmount(SkinnedMeshRenderer targetSMR, float maxAmount, int substanceGrowBS, float amountChange, ref float amount)
        {
            if (targetSMR == null)
            {
                return;
            }
            if (maxAmount == 0.0f)
            {
                return;
            }
            amount = Mathf.Clamp(amount + amountChange, 0.0f, maxAmount);
            float bsValue = (amount / maxAmount) * 100.0f;
            targetSMR.SetBlendShapeWeight(substanceGrowBS, bsValue);
            if (hideSMRIfEmpty)
            {
                targetSMR.enabled = bsValue > 0.0f;
            }
            OnUpdateStatusBar();
        }

        public override bool CanBePlacedInRestParent()
        {
            return false;
        }

        protected void UpdatePlayerKeyboardMixingInteraction<T>(Player player, bool allowMixing, bool allowDumping) where T : Item
        {

            if (player == null)
            {
                return;
            }
            if (player.controls == Player.ControlType.KEYBOARD)
            {
                PlayerHandState containerHand = mainOccupyState as PlayerHandState;
                PlayerHandState mixerHand;
                if (mainOccupyState.rightSide)
                {
                    mixerHand = player.leftPlayerHandState;
                }
                else
                {
                    mixerHand = player.rightPlayerHandState;
                }
                T mixer = mixerHand.GetItemIfHeld<T>();
                if (containerHand.actionState.isDown)
                {
                    //empty contents
                    containerHand.actionState.Consume();
                    if (isMixing)
                    {
                        //restore both hands animation
                        containerHand.animSys.SetTargetAnimation(Player.Animation.IDLE);
                        mixerHand.animSys.SetTargetAnimation(Player.Animation.IDLE);
                    }
                    else if (allowDumping)
                    {
                        containerHand.animSys.SetTargetAnimation(emptyContentsAnim);
                    }
                }

                //find which state player is in
                if (isMixing)
                {
                    if (mixerHand.actionState.isHeldDown)
                    {
                        //play grind loop
                        if (allowMixing)
                        {
                            player.GetAnimator().SetFloat(mixLoopID, player.GetAnimator().GetFloat(mixLoopID) + Time.deltaTime * 1.7f);
                        }
                        else
                        {
                            mixerHand.animSys.SetTargetAnimation(Player.Animation.IDLE);
                        }
                    }

                    if (containerHand.actionState.isDown)
                    {
                        containerHand.actionState.Consume();
                        //return to idle
                        containerHand.animSys.SetTargetAnimation(Player.Animation.IDLE);
                        mixerHand.animSys.SetTargetAnimation(Player.Animation.IDLE);
                    }
                }
                else if (mixerHand.actionState.isDown)
                {
                    if (allowMixing)
                    {
                        //start grind animation
                        Player.Animation inAnim;
                        if (mainOccupyState.rightSide)
                        {
                            inAnim = playerKeyboardMixAnimations[(int)PlayerKeyboardMixAnimType.MIX_IN_LEFT];
                        }
                        else
                        {
                            inAnim = playerKeyboardMixAnimations[(int)PlayerKeyboardMixAnimType.MIX_IN_RIGHT];
                        }
                        containerHand.animSys.SetTargetAnimation(inAnim);
                        mixerHand.animSys.SetTargetAnimation(inAnim);
                    }
                }
            }
        }

        protected override void OnItemDestroy()
        {
            ExitMixing();
        }

        public override void OnPreDrop()
        {
            ExitMixing();
        }

        public override void OnPostPickup()
        {
            AddStateListeners(mainOwner as Player);
            OnUpdateStatusBar();
            lastPickupTime = Time.time;
        }

        private void ExitMixing()
        {
            RemoveStateListeners(mainOwner as Player);
            RestorePlayerHandAnimations(mainOwner as Player);
        }

        private void AddStateListeners(Player player)
        {
            if (player == null)
            {
                return;
            }
            player.AddOnAnimationChangeListener(OnPlayerHandAnimationChange);
            if (mainOccupyState.rightSide)
            {
                player.leftHandState.onItemChange += OnCharacterMixerHandItemChange;
            }
            else
            {
                player.rightHandState.onItemChange += OnCharacterMixerHandItemChange;
            }
        }

        private void RemoveStateListeners(Player player)
        {
            if (player == null)
            {
                return;
            }
            player.RemoveOnAnimationChangeListener(OnPlayerHandAnimationChange);
            if (mainOccupyState.rightSide)
            {
                player.leftHandState.onItemChange -= OnCharacterMixerHandItemChange;
            }
            else
            {
                player.rightHandState.onItemChange -= OnCharacterMixerHandItemChange;
            }
        }

        //Exit mixing if mixer item was removed from hand
        private void OnCharacterMixerHandItemChange(OccupyState source, Item oldItem, Item newItem)
        {
            if (oldItem as Pestle)
            {
                RestorePlayerHandAnimations(source.owner as Player);
            }
        }

        private void RestorePlayerHandAnimations(Player player)
        {
            if (player == null)
            {
                return;
            }
            if (isMixing)
            {
                isMixing = false;
                player.rightPlayerHandState.animSys.SetTargetAnimation(Player.Animation.IDLE);
                player.leftPlayerHandState.animSys.SetTargetAnimation(Player.Animation.IDLE);
            }
        }

        //Exit mixing if player animation leaves any of the mixing animations
        private void OnPlayerHandAnimationChange(bool rightHand, Player.Animation oldAnim, Player.Animation newAnim)
        {

            if (playerKeyboardMixAnimations.Length != System.Enum.GetValues(typeof(PlayerKeyboardMixAnimType)).Length)
            {
                return;
            }
            if (newAnim == playerKeyboardMixAnimations[(int)PlayerKeyboardMixAnimType.MIX_IN_RIGHT] ||
                newAnim == playerKeyboardMixAnimations[(int)PlayerKeyboardMixAnimType.MIX_IN_LEFT] ||
                newAnim == playerKeyboardMixAnimations[(int)PlayerKeyboardMixAnimType.MIX_RIGHT_LOOP] ||
                newAnim == playerKeyboardMixAnimations[(int)PlayerKeyboardMixAnimType.MIX_LEFT_LOOP])
            {
                isMixing = true;
            }
            else
            {
                RestorePlayerHandAnimations(mainOwner as Player);
            }
        }
    }

}