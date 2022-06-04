using UnityEngine;

namespace viva
{


    public partial class Pastry : Item
    {

        [SerializeField]
        private bool m_hasFilling = false;
        [VivaFileAttribute]
        public bool hasFilling { get { return m_hasFilling; } protected set { m_hasFilling = value; } }
        [SerializeField]
        private Color defaultFillingColor;
        [SerializeField]
        private Color m_fillingColor;
        [VivaFileAttribute]
        public Color fillingColor { get { return m_fillingColor; } protected set { m_fillingColor = value; } }
        [SerializeField]
        private MeshRenderer meshRenderer;
        [SerializeField]
        private float timeBaked = 0.0f;
        [SerializeField]
        private float timeToBakeFully = 10.0f;
        [SerializeField]
        private bool m_baked = false;
        [VivaFileAttribute]
        public bool isBaked { get { return m_baked; } protected set { m_baked = value; } }
        [SerializeField]
        private Material bakedMaterial;

        private static readonly int fillingColorID = Shader.PropertyToID("_FillingColor");


        protected override void OnItemAwake()
        {
            if (hasFilling)
            {
                SetFilling(m_fillingColor);
            }
            if (m_baked)
            {
                FinishBaking();
            }
        }

        private void FinishBaking()
        {
            m_baked = true;
            meshRenderer.material = bakedMaterial;
            SetFilling(m_fillingColor);
        }

        public void SetFilling(Color? newFillingColor)
        {
            if (newFillingColor.HasValue)
            {
                m_hasFilling = true;
                m_fillingColor = newFillingColor.Value;
                m_fillingColor.a = 1.0f; //ensure alpha set to 1 for shader
                meshRenderer.material.SetColor(fillingColorID, m_fillingColor);
            }
            else
            {
                m_hasFilling = false;
                meshRenderer.material.SetColor(fillingColorID, defaultFillingColor);
            }
        }

        public override void OnPostPickup()
        {
            if (!isBaked && !hasFilling)
            {
                TutorialManager.main.DisplayObjectHint(this, "hint_heatPot");
            }
        }

        protected override void OnUpdateStatusBar()
        {
            if (timeBaked == 0.0f)
            {
                statusBar.SetInfoText(null);
                return;
            }

            statusBar.SetInfoText(null, Mathf.Clamp01(timeBaked / timeToBakeFully));
        }

        public void UpdateOvenState(Oven oven, float time)
        {
            timeBaked += time;
            if (timeBaked >= timeToBakeFully)
            {
                if (!m_baked)
                {
                    FinishBaking();
                    oven.playBakedReadySound();
                    GameDirector.player.CompleteAchievement(Player.ObjectiveType.BAKE_A_PASTRY);
                    return;
                }
                else if (timeBaked >= timeToBakeFully + 12.0f)
                {
                    oven.BurnAndDestroy(gameObject);
                    return;
                }
            }
            EnableItemLogicTemporarily(1.5f);
        }
    }

}