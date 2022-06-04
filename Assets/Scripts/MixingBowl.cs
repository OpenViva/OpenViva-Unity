using UnityEngine;

namespace viva
{


    public class MixingBowl : Container
    {

        [SerializeField]
        private Material[] eggMaterialLayers;
        [SerializeField]
        private Material batterLayer;
        [SerializeField]
        private Transform mixerCenterBone;
        [Range(0, 10)]
        [SerializeField]
        private int m_eggCount = 0;
        [VivaFileAttribute]
        public int eggCount { get { return m_eggCount; } protected set { m_eggCount = value; } }
        [Range(0, 1)]
        [SerializeField]
        private float mixSpinSpeed = 0.2f;
        [SerializeField]
        private GameObject rawDoughPrefab;
        [SerializeField]
        private Transform[] rawDoughPrefabSpawnPoints;
        [SerializeField]
        private AudioClip pastryCreatedSound;
        [SerializeField]
        private ParticleSystem pastryCreatedFX;


        private static int moundGrowBS;

        private float eggsToMix = 0.0f;
        private float eggsMixed = 0.0f;


        protected override void OnItemAwake()
        {
            base.OnItemAwake();
            eggsToMix = m_eggCount;
            UpdateLayerMaterial();
        }

        public void IncreaseEggCount()
        {
            int allowedEggIncrease = Mathf.Min(m_eggCount + 1, 4) - m_eggCount;
            m_eggCount += allowedEggIncrease;
            eggsToMix += allowedEggIncrease;
            UpdateLayerMaterial();
            if (allowedEggIncrease > 0 && m_eggCount == 4)
            {
                TutorialManager.main.DisplayObjectHint(this, "hint_mixBatter");
            }
        }

        protected override bool OnReceiveSubstanceSpill(SubstanceSpill.Substance substance, float spillAmount)
        {
            if (substance == SubstanceSpill.Substance.FLOUR)
            {
                GameDirector.instance.StartCoroutine(AnimationChangeFill(spillAmount, ChangeSubstanceAmount));
                TutorialManager.main.DisplayObjectHint(this, "hint_addEggs");
            }
            else if (substance == SubstanceSpill.Substance.EGG)
            {
                IncreaseEggCount();
            }
            return true;
        }

        private void UpdateLayerMaterial()
        {

            if (m_eggCount < 4)
            {
                substanceGrowSMR.material = eggMaterialLayers[(int)Mathf.Clamp(m_eggCount, 0.0f, eggMaterialLayers.Length - 1)];
            }
            else
            {
                substanceGrowSMR.material = batterLayer;
            }
            OnUpdateStatusBar();
        }

        public void Mix(float amount)
        {
            //cannot mix if not enough flour
            if (m_substanceAmount < 6.0f)
            {
                return;
            }

            var newEggsMixed = eggsToMix - Mathf.Max(0, eggsToMix - amount);
            eggsToMix -= newEggsMixed;
            eggsMixed += newEggsMixed;
            if (eggsToMix <= 0.0f)
            {
                //consume ingredients and make raw dough items
                m_eggCount = 0;
                UpdateLayerMaterial();
                m_substanceAmount = 0.0f;
                ChangeSubstanceAmount(0.0f);
                //spawn raw dough
                int doughCount = Mathf.Min(rawDoughPrefabSpawnPoints.Length, 4);
                for (int i = 0; i < doughCount; i++)
                {
                    Transform spawnPoint = rawDoughPrefabSpawnPoints[i];
                    GameObject.Instantiate(rawDoughPrefab, spawnPoint.position, spawnPoint.rotation);
                }
                eggsMixed = 0;
                SoundManager.main.RequestHandle(transform.position).PlayOneShot(pastryCreatedSound);
                pastryCreatedFX.Emit(30);
            }
            OnUpdateStatusBar();
        }

        protected override void OnUpdateStatusBar()
        {
            string info = "";
            if (m_substanceAmount != 0.0f)
            {
                info += (int)m_substanceAmount + " Flour";
            }
            float fillPercent = 0.0f;
            if (m_eggCount != 0)
            {
                if (info != "")
                {
                    info += "\n";
                }
                info += Tools.SafeFloorToInt(m_eggCount) + " Egg";
                if (m_eggCount > 1)
                {
                    info += "s";
                }
                if (eggsMixed > 0.0f && eggsToMix > 0.0f)
                {
                    fillPercent = eggsMixed / (eggsToMix + eggsMixed);
                }
            }
            statusBar.SetInfoText(info, fillPercent);
        }

        public override void OnItemLateUpdate()
        {
            Player player = mainOwner as Player;
            if (player != null)
            {
                bool validToMix = eggsToMix > 0.0f && substanceAmount >= 3.0f;
                UpdatePlayerKeyboardMixingInteraction<Spoon>(player, validToMix, substanceAmount > 0);
            }
        }

        public override void OnItemFixedUpdate()
        {
            base.OnItemFixedUpdate();
            mixerCenterBone.transform.localPosition = Vector3.MoveTowards(
                mixerCenterBone.transform.localPosition,
                Vector3.zero,
                Time.fixedDeltaTime * 0.001f
            );
        }

        private float? GetCurrentPullRadian()
        {
            if (mixerCenterBone.localPosition.x == 0 && mixerCenterBone.localPosition.y == 0)
            {
                return null;
            }
            return Mathf.Atan2(mixerCenterBone.localPosition.x, mixerCenterBone.localPosition.y);
        }

        public void SetBatterPullPosition(Vector3 pos)
        {
            Vector3 localPos = mixerCenterBone.parent.InverseTransformPoint(pos);
            localPos.z = 0.0f;

            float? oldRadian = GetCurrentPullRadian();
            mixerCenterBone.localPosition = Vector3.MoveTowards(
                mixerCenterBone.localPosition,
                localPos,
                Time.deltaTime * 0.0035f
            );

            float? newRadian = GetCurrentPullRadian();
            if (oldRadian.HasValue && newRadian.HasValue)
            {
                float radianDiff = Mathf.DeltaAngle(newRadian.Value * Mathf.Rad2Deg, oldRadian.Value * Mathf.Rad2Deg);
                radianDiff = Mathf.Clamp(radianDiff, -30.0f, 30.0f);
                mixerCenterBone.parent.localRotation *= Quaternion.Euler(0, 0, radianDiff * mixSpinSpeed);
            }
        }
    }

}