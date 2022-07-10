using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace viva
{

    public class MapToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Text tooltip;
        [Tooltip("Specify Enter Fade time.")]
        public float fadetime = 0f;
        [Tooltip("Specify Exit Fade time.")]
        public float Unfadetime = 0f;

        public void OnPointerEnter(PointerEventData eventData)
        {            
            StartCoroutine(FadeText(fadetime, tooltip));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StartCoroutine(UnFadeText(Unfadetime, tooltip));          
        }
        public IEnumerator FadeText(float time, Text text)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, 0);
            while (text.color.a < 1.0f)
            {
                text.color = new Color(text.color.r, text.color.g, text.color.b, text.color.a + (Time.deltaTime / time));
                yield return null;
            }
        }

        public IEnumerator UnFadeText(float time, Text text)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, 1);
            while (text.color.a > 0.0f)
            {
                text.color = new Color(text.color.r, text.color.g, text.color.b, text.color.a - (Time.deltaTime / time));
                yield return null;
            }
        }
    }
}