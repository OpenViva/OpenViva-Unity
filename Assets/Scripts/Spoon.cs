using System.Collections;
using UnityEngine;

namespace viva
{


    public class Spoon : Item
    {

        [Range(0.0f, 0.1f)]
        [SerializeField]
        private float bottomOffset = 0.01f;
        [SerializeField]
        private AudioClip grindSound;
        [SerializeField]
        private AudioClip jellyScoopSound;
        [SerializeField]
        private AudioClip jellyApplySound;
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float grindMinimum;
        [SerializeField]
        private MeshRenderer fillingMeshRenderer;
        [SerializeField]
        private bool m_hasFillingScoop = false;
        [VivaFileAttribute]
        public bool hasFillingScoop { get { return m_hasFillingScoop; } protected set { m_hasFillingScoop = value; } }
        [SerializeField]
        private Color m_fillingColor;
        [VivaFileAttribute]
        public Color fillingColor { get { return m_fillingColor; } protected set { m_fillingColor = value; } }

        private Vector3? lastGrindBottom = null;
        private float lastGrindSoundTime = 0;
        private Coroutine grindSoundCoroutine;


        protected override void OnItemAwake()
        {
            if (m_hasFillingScoop)
            {
                SetFillingScoop(m_fillingColor);
            }
        }

        public override void OnItemLateUpdate()
        {

            Player player = mainOwner as Player;
            if (player != null && mainOccupyState != null)
            {
                //allow mixing animation if other hand is not a mixing bowl

                HandState otherHandState = (mainOccupyState as HandState).otherHandState; ;
                if (mainOccupyState.rightSide)
                {
                    otherHandState = player.leftPlayerHandState;
                }
                else
                {
                    otherHandState = player.rightPlayerHandState;
                }
                if (otherHandState.heldItem != null && otherHandState.heldItem.settings.itemType == Item.Type.MIXING_BOWL)
                {
                    //Dont do anything
                }
                else
                {
                    PlayerHandState mainHandState = mainOccupyState as PlayerHandState;
                    if (mainHandState.actionState.isDown)
                    {
                        mainHandState.animSys.SetTargetAnimation(Player.Animation.MIXING_SPOON_SCOOP);
                    }
                }
            }
        }

        private bool CheckIfCanPaintNails(Collider collider)
        {
            if (!hasFillingScoop)
            {
                return false;
            }
            CharacterCollisionCallback ccc = collider.GetComponent<CharacterCollisionCallback>();
            if (ccc && ccc.owner.characterType == Character.Type.LOLI)
            {
                Loli loli = ccc.owner as Loli;
                switch (ccc.collisionPart)
                {
                    case CharacterCollisionCallback.Type.RIGHT_PALM:
                    case CharacterCollisionCallback.Type.LEFT_PALM:
                        if (loli.outfit.fingerNailColor != m_fillingColor)
                        {
                            loli.outfit.fingerNailColor = m_fillingColor;
                            SetFillingScoop(null);
                            loli.ApplyFingerNailColor(loli.outfit.fingerNailColor);
                            PlayJellyApplySound();
                            return true;
                        }
                        break;
                    case CharacterCollisionCallback.Type.RIGHT_FOOT:
                    case CharacterCollisionCallback.Type.LEFT_FOOT:
                        if (loli.outfit.toeNailColor != m_fillingColor)
                        {
                            loli.outfit.toeNailColor = m_fillingColor;
                            SetFillingScoop(null);
                            loli.ApplyToeNailColor(loli.outfit.toeNailColor);
                            PlayJellyApplySound();
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        public override void OnItemFixedUpdate()
        {
            base.OnItemFixedUpdate();

            //TODO: Move to TriggerEnter()
            Vector3 bottom = transform.position + transform.up * bottomOffset;
            Collider[] results = Physics.OverlapSphere(bottom, 0.04f, Instance.itemsMask, QueryTriggerInteraction.Collide);
            foreach (Collider collider in results)
            {
                if (CheckIfCanPaintNails(collider))
                {
                    break;
                }
                Item item = Tools.SearchTransformAncestors<Item>(collider.transform);
                if (item == null)
                {
                    continue;
                }
                switch (item.settings.itemType)
                {
                    case Item.Type.MIXING_BOWL:
                        UpdateMixingBowlInteraction(bottom, item as MixingBowl);
                        break;
                    case Item.Type.POT:
                        UpdatePotInteraction(item as Pot);
                        break;
                    case Item.Type.PASTRY:
                        UpdatePastryInteraction(item as Pastry);
                        break;
                }
            }
        }
        private void UpdatePotInteraction(Pot targetPot)
        {

            if (m_hasFillingScoop || targetPot.substanceAmount < 1.0f)
            {
                return;
            }
            Color? liquidColor = targetPot.AttemptScoopFilling();
            if (liquidColor.HasValue)
            {
                SetFillingScoop(liquidColor.Value);
                SoundManager.main.RequestHandle(transform.position).PlayOneShot(jellyScoopSound);
            }
        }

        private void UpdatePastryInteraction(Pastry targetPastry)
        {
            if (!m_hasFillingScoop)
            {
                return;
            }
            targetPastry.SetFilling(m_fillingColor);
            SetFillingScoop(null);  //consume filling
            PlayJellyApplySound();
            TutorialManager.main.DisplayObjectHint(targetPastry, "hint_lastBakeStep");
        }

        public void PlayJellyApplySound()
        {
            SoundManager.main.RequestHandle(transform.position).PlayOneShot(jellyApplySound);
        }

        public void SetFillingScoop(Color? newFillingColor)
        {
            if (newFillingColor.HasValue)
            {
                m_hasFillingScoop = true;
                fillingMeshRenderer.enabled = true;
                fillingMeshRenderer.material.color = newFillingColor.Value;
                m_fillingColor = newFillingColor.Value;
            }
            else
            {
                m_hasFillingScoop = false;
                fillingMeshRenderer.enabled = false;
            }
        }

        private void UpdateMixingBowlInteraction(Vector3 bottom, MixingBowl targetMixingBowl)
        {

            Vector3? currGrindBottom = null;
            //check if mxing
            if (!targetMixingBowl.IsPointInsideMixingHalfSphere(bottom))
            {
                return;
            }
            //if pointing against each other
            if (Vector3.Dot(transform.up, targetMixingBowl.transform.up) > -0.7f)
            {
                return;
            }
            targetMixingBowl.SetBatterPullPosition(bottom);
            currGrindBottom = targetMixingBowl.transform.InverseTransformPoint(bottom);
            if (lastGrindBottom.HasValue)
            {
                //calculate grind speed (distance/time)
                float grindSpeed = Vector3.Distance(lastGrindBottom.Value, currGrindBottom.Value) / Time.deltaTime;
                if (grindSpeed > grindMinimum)
                {
                    targetMixingBowl.Mix(grindSpeed * 0.015f);
                    if (grindSound != null)
                    {
                        lastGrindSoundTime = Time.time;
                        if (grindSoundCoroutine == null)
                        {
                            grindSoundCoroutine = GameDirector.instance.StartCoroutine(PlayGrindSoundCoroutine());
                        }
                    }
                }
            }
            lastGrindBottom = currGrindBottom;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0.0f, 0.5f, 1.0f, 0.5f);
            Gizmos.DrawSphere(transform.position + transform.up * bottomOffset, 0.04f);
        }

        private IEnumerator PlayGrindSoundCoroutine()
        {

            var handle = SoundManager.main.RequestHandle(Vector3.zero, transform);
            handle.Play(grindSound);
            while (Time.time - lastGrindSoundTime <= 0.1f)
            {
                yield return new WaitForSeconds(0.1f);
            }
            handle.Stop();
            grindSoundCoroutine = null;
        }
    }

}