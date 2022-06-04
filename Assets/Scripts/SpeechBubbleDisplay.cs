using UnityEngine;


namespace viva
{

    public class SpeechBubbleDisplay : MonoBehaviour
    {

        [SerializeField]
        private MeshRenderer meshRenderer;
        [Range(0, 1)]
        [SerializeField]
        private float growAnimLength = 1;
        [Range(1, 3)]
        [SerializeField]
        private float animLength = 3;
        [Range(0, 1)]
        [SerializeField]
        private float startSize = 1;
        [Range(0, 1)]
        [SerializeField]
        private float endSize = 1;
        private float startTime = 0;

        private static readonly int symTexID = Shader.PropertyToID("_SymTex");


        private void LateUpdate()
        {
            transform.rotation = Quaternion.LookRotation(GameDirector.instance.mainCamera.transform.position - transform.position, Vector3.up);

            float ratio = Mathf.Clamp01((Time.time - startTime) / growAnimLength);
            ratio = ratio + Mathf.Sin(ratio * Mathf.PI / 2) * 1.7f * (1.0f - ratio);

            transform.localScale = Vector3.one * Mathf.LerpUnclamped(startSize, endSize, ratio);

            float fade = Mathf.Clamp01(Time.time - startTime - animLength);
            meshRenderer.material.SetFloat(Instance.alphaID, 1.0f - fade);
            if (fade >= 1.0f)
            {
                gameObject.SetActive(false);
            }
        }

        public void DisplayBubble(Texture2D contentTexture)
        {
            if (contentTexture == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                meshRenderer.material.SetTexture(symTexID, contentTexture);
            }

        }

        private void OnEnable()
        {
            startTime = Time.time;
        }
    }

}