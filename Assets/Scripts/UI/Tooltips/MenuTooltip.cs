using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace viva
{
    public class MenuTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private GameObject toolTipGO;
        [Tooltip("Specify pointer enter delay amount.")]
        public float delayedEnterTime = 0f;
        [Tooltip("Specify pointer exit delay amount.")]
        public float delayedExitTime = 0f;

        void Start()
        {
            if (toolTipGO != null)
            {
                toolTipGO.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (toolTipGO != null && GameSettings.main.toggleTooltips)
            {
                LeanTween.delayedCall(delayedEnterTime, () =>
                {
                    toolTipGO.SetActive(true);
                });
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (toolTipGO != null)
            {
                LeanTween.delayedCall(delayedExitTime, () =>
                {
                    toolTipGO.SetActive(false);
                });
            }
        }
    }
}