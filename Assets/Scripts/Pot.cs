using System.Collections;
using UnityEngine;

namespace viva
{


    public class Pot : Container
    {

        [SerializeField]
        private AudioClip waterFillingSound;
        [SerializeField]
        private AudioClip fruitDropSound;
        [SerializeField]
        private Vector3 m_colorCount = Vector3.zero;
        [VivaFileAttribute]
        public Vector3 colorCount { get { return m_colorCount; } protected set { m_colorCount = value; } }
        [SerializeField]
        private int m_fruitCount = 0;
        [VivaFileAttribute]
        public int fruitCount { get { return m_fruitCount; } protected set { m_fruitCount = value; } }
        [SerializeField]
        private Material waterMat;
        [SerializeField]
        private Material fillingMat;
        [SerializeField]
        private float m_timeHeated = 0.0f;
        [VivaFileAttribute]
        public float timeHeated { get { return m_timeHeated; } protected set { m_timeHeated = value; } }
        [SerializeField]
        private float timeToHeat = 10.0f;

        private static int waterFillBS;
        private Coroutine fillSoundCoroutine = null;
        private float lastFillSoundTime = 0.0f;

        private static readonly int waterColorID = Shader.PropertyToID("_WaterColor");
        private static readonly int boilID = Shader.PropertyToID("_Boil");


        protected override void OnItemAwake()
        {
            base.OnItemAwake();
            UpdateWaterMaterial();
        }

        private IEnumerator PlayFillSound()
        {

            var handle = SoundManager.main.RequestHandle(Vector3.zero, transform);
            handle.loop = true;
            handle.Play(waterFillingSound);

            while (Time.time - lastFillSoundTime < 0.25f)
            {
                yield return new WaitForSeconds(0.25f);
            }

            handle.Stop();
            fillSoundCoroutine = null;
        }

        public override void OnPostPickup()
        {
            base.OnPostPickup();
            if (timeToHeat == timeHeated)
            {
                TutorialManager.main.DisplayObjectHint(this, "hint_addFruit", HintWaitForFruit);
            }
        }

        private IEnumerator HintWaitForFruit(Item source)
        {
            Pot pot = source as Pot;
            if (pot == null)
            {
                yield break;
            }
            while (true)
            {
                if (!pot || pot.fruitCount > 0)
                {
                    TutorialManager.main.StopHint();
                    yield break;
                }
                yield return null;
            }
        }

        private IEnumerator HintWaitForHeat(Item source)
        {
            Pot pot = source as Pot;
            if (pot == null)
            {
                yield break;
            }
            float startHeat = timeHeated;
            while (true)
            {
                if (!pot || startHeat != pot.timeHeated)
                {
                    TutorialManager.main.StopHint();
                    yield break;
                }
                yield return null;
            }
        }

        protected override void OnCollisionEnter(Collision collision)
        {

            Item item = Tools.SearchTransformAncestors<Item>(collision.transform);
            if (item == null)
            {
                base.OnCollisionEnter(collision);
                return;
            }
            if (m_substanceAmount <= 0.0f)
            {
                return;
            }
            if (timeHeated != timeToHeat)
            {
                TutorialManager.main.DisplayObjectHint(this, "hint_potNeedsHeat", HintWaitForHeat);
                return;
            }
            switch (item.settings.itemType)
            {
                case Item.Type.BLUEBERRY:
                    m_colorCount += new Vector3(0, 0, 1);
                    break;
                case Item.Type.STRAWBERRY:
                    m_colorCount += new Vector3(1, 0, 0);
                    break;
                case Item.Type.PEACH:
                    m_colorCount += new Vector3(1, 0.6f, 0.5f);
                    break;
                case Item.Type.CANTALOUPE:
                    m_colorCount += new Vector3(0, 1, 0);
                    break;
                default:
                    base.OnCollisionEnter(collision);
                    return;
            }

            m_fruitCount++;

            SoundManager.main.RequestHandle(transform.position).PlayOneShot(fruitDropSound);
            GameDirector.Destroy(item.gameObject);

            UpdateWaterMaterial();
        }

        protected override void OnUpdateStatusBar()
        {
            if (timeHeated == 0.0f)
            {
                base.OnUpdateStatusBar();
                return;
            }
            float percent = timeHeated / timeToHeat;
            if (fruitCount == 0)
            {
                statusBar.SetInfoText(Mathf.CeilToInt(substanceAmount * 10) / 10 + " Water", percent);
            }
            else
            {
                statusBar.SetInfoText(Mathf.CeilToInt(substanceAmount * 10) / 10 + " Filling", percent);
            }
        }

        public void UpdateWaterMaterial()
        {
            if (m_fruitCount == 0)
            {
                substanceGrowSMR.material = waterMat;
                substanceGrowSMR.material.SetColor(waterColorID, new Color(0.8f, 0.8f, 0.8f, 0.3f));
            }
            else
            {
                substanceGrowSMR.material = fillingMat;
                Vector3 unit = m_colorCount / m_fruitCount;
                substanceGrowSMR.material.color = new Color(unit.x, unit.y, unit.z, 1.0f);
            }
        }

        public Color? AttemptScoopFilling()
        {
            if (m_fruitCount == 0)
            {
                return null;
            }
            //consume 1 scoop
            ChangeSubstanceAmount(-1.0f);
            if (substanceAmount == 0)
            {
                m_timeHeated = 0.0f;
                m_fruitCount = 0;
                UpdateHeatedMaterial();
            }

            return substanceGrowSMR.material.color;
        }

        public void UpdateOvenState(Oven oven, float time)
        {
            if (substanceAmount > 0)
            {
                timeHeated = Mathf.Min(timeHeated + time, timeToHeat);
                UpdateHeatedMaterial();
            }
            EnableItemLogicTemporarily(1.5f);
        }

        private void UpdateHeatedMaterial()
        {
            float percent = timeHeated / timeToHeat;
            if (fruitCount == 0)
            {
                substanceGrowSMR.material.SetFloat(boilID, percent);
            }
        }
    }

}