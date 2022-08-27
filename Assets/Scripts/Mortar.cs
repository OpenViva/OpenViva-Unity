using System.Collections;
using UnityEngine;

namespace viva
{


    public class Mortar : Container
    {

        [SerializeField]
        private AudioClip grainPrepareClip;
        [SerializeField]
        private ParticleSystem grainSpillFX;
        [SerializeField]
        private SubstanceSpill grainSpillSubstance;

        private int[] grainsBS = null;
        [SerializeField]
        private float m_grainCount = 0;
        [VivaFileAttribute]
        public float grainCount { get { return m_grainCount; } protected set { m_grainCount = value; } }

        public bool hasGrain { get { return m_grainCount > 0; } }
        public bool hasMaxGrain { get { return m_grainCount == 3.0f; } }
        private int grainToAdd = 0;


        protected override void OnItemAwake()
        {
            base.OnItemAwake();
            grainsBS = new int[]{
            substanceGrowSMR.sharedMesh.GetBlendShapeIndex("grains1"),
            substanceGrowSMR.sharedMesh.GetBlendShapeIndex("grains2"),
            substanceGrowSMR.sharedMesh.GetBlendShapeIndex("grains3")
        };
            UpdateGrainBlendShapes();
        }

        public override void OnPostPickup()
        {
            base.OnPostPickup();
            TutorialManager.main.DisplayObjectHint(this, "hint_wheatIntoMortar");
        }

        public override void OnItemLateUpdate()
        {
            Player player = mainOwner as Player;
            if (player != null)
            {

                if (player.controls == Player.ControlType.KEYBOARD)
                {
                    PlayerHandState mortarHand = mainOccupyState as PlayerHandState;
                    PlayerHandState mixingBowlHand;
                    if (mainOccupyState.rightSide)
                    {
                        mixingBowlHand = player.leftPlayerHandState;
                    }
                    else
                    {
                        mixingBowlHand = player.rightPlayerHandState;
                    }
                    MixingBowl mixingBowl = mixingBowlHand.GetItemIfHeld<MixingBowl>();
                    if (mixingBowl != null && substanceAmount >= 1.0f)
                    {
                        if (mortarHand.actionState.isDown)
                        {
                            Player.Animation emptyContentsAnim;
                            if (mixingBowlHand.rightSide)
                            {
                                emptyContentsAnim = Player.Animation.MORTAR_EMPTY_INTO_MIXING_BOWL_RIGHT;
                            }
                            else
                            {
                                emptyContentsAnim = Player.Animation.MORTAR_EMPTY_INTO_MIXING_BOWL_LEFT;
                            }
                            mortarHand.animSys.SetTargetAnimation(emptyContentsAnim);
                            mixingBowlHand.animSys.SetTargetAnimation(emptyContentsAnim);
                        }
                    }
                    else
                    {
                        UpdatePlayerKeyboardMixingInteraction<Pestle>(player, hasGrain, grainCount == 0);
                    }
                }
            }
        }

        public void Grind(float grindAmount)
        {
            float grindedCount = m_grainCount - Mathf.Max(0.0f, m_grainCount - grindAmount);
            m_grainCount -= grindedCount;
            m_substanceAmount += grindedCount;
            UpdateGrainBlendShapes();
            OnUpdateStatusBar();

            if (m_grainCount == 0)
            {
                TutorialManager.main.DisplayObjectHint(this, "hint_flourIntoBowl");
            }
        }

        protected override void OnUpdateStatusBar()
        {
            float fillPercent = m_substanceAmount / (m_substanceAmount + m_grainCount);
            if (m_substanceAmount <= 0.0f)
            {
                if (m_grainCount <= 0.0f)
                {
                    statusBar.SetInfoText(null, fillPercent);
                }
                else
                {
                    statusBar.SetInfoText(Mathf.Floor(m_grainCount * 10.0f) / 10.0f + " Grain", fillPercent);
                }
            }
            else
            {
                statusBar.SetInfoText(Mathf.Floor(m_substanceAmount * 10.0f) / 10.0f + " Flour", fillPercent);
            }
        }

        protected override void OnSpillContents()
        {
            base.OnSpillContents();

            if (m_grainCount > 0)
            {
                for (int i = 0; i < grainsBS.Length; i++)
                {
                    substanceGrowSMR.SetBlendShapeWeight(grainsBS[i], 0.0f);
                }
                grainSpillFX.Emit(Mathf.CeilToInt(m_grainCount * 10));
                grainSpillSubstance.BeginInstantSpill((int)m_grainCount);
                m_grainCount = 0;
            }
        }

        private void UpdateGrainBlendShapes()
        {
            float grainCountBS = m_grainCount;
            for (int i = 0; i < grainsBS.Length; i++)
            {
                float visGrainBS = grainCountBS - Mathf.Max(0.0f, grainCountBS - 1.0f);
                grainCountBS -= visGrainBS;
                substanceGrowSMR.SetBlendShapeWeight(grainsBS[i], visGrainBS * 100.0f);
            }
            ChangeSubstanceAmount(0.0f);
        }

        private void AddWheatGrain()
        {
            if (m_grainCount >= 3.0f)
            {
                return;
            }
            m_grainCount = Mathf.Min(m_grainCount + 1.0f, 3.0f);
            UpdateGrainBlendShapes();
            SoundManager.main.RequestHandle(transform.position).PlayOneShot(grainPrepareClip);
            OnUpdateStatusBar();
            TutorialManager.main.DisplayObjectHint(this, "hint_mortarAndPestle");
        }

        protected void OnCollisionEnter(Collision collision)
        {
            if (m_grainCount + grainToAdd >= 3.0f)
            {
                return;
            }
            //must be upright to add grain
            if (IsTippingOver())
            {
                return;
            }

            //Find wheat spikes
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint point = collision.GetContact(i);
                Item item = point.otherCollider.GetComponent<Item>();
                if (item == null)
                {
                    continue;
                }
                if (item.settings.itemType != Item.Type.WHEAT_SPIKE)
                {
                    continue;
                }
                if (item.mainOccupyState != null)
                {
                    continue;
                }
                if (item.enabled == false)
                {
                    continue;
                }
                item.enabled = false;
                GameDirector.instance.StartCoroutine(AddWheatGrainAnimation(item));
            }
        }

        private IEnumerator AddWheatGrainAnimation(Item wheatSpike)
        {
            if (wheatSpike == null)
            {
                yield break;
            }
            if (wheatSpike.mainOccupyState != null)
            {
                wheatSpike.mainOccupyState.AttemptDrop();
            }
            grainToAdd++;
            wheatSpike.gameObject.layer = WorldUtil.noneLayer;
            wheatSpike.SetAttribute(Item.Attributes.DISABLE_PICKUP);
            const float animDuration = 0.3f;    //seconds
            TransformBlend animBlend = new TransformBlend();
            animBlend.SetTarget(true, wheatSpike.transform, false, false, 0.0f, 1.0f, animDuration);
            while (!animBlend.blend.finished)
            {
                animBlend.Blend(transform.position, Quaternion.Euler(90.0f, 0.0f, 0.0f));
                if (wheatSpike == null)
                {
                    yield break;
                }
                yield return null;
            }
            Destroy(wheatSpike.gameObject);
            AddWheatGrain();
            grainToAdd--;
        }
    }

}