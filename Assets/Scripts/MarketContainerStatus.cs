using System.Collections;
using UnityEngine;

namespace viva
{


    public class MarketContainerStatus : MonoBehaviour
    {

        [SerializeField]
        private MarketContainer marketContainer;
        [SerializeField]
        private MeshRenderer iconMeshRenderer;
        [SerializeField]
        private AudioClip readySound;

        private Coroutine animationCoroutine = null;
        private static readonly int scaleID = Shader.PropertyToID("_Scale");
        private static readonly int alphaID = Shader.PropertyToID("_Alpha");
        private static readonly int additiveID = Shader.PropertyToID("_Additive");


        private IEnumerator AnimateIcon()
        {
            while (true)
            {
                var joint = marketContainer.gameObject.GetComponent<Joint>();
                if (!joint)
                {
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
            var rb = marketContainer.GetComponent<Rigidbody>();
            if (rb)
            {
                while (true)
                {
                    if (rb.IsSleeping())
                    {
                        break;
                    }
                    yield return new WaitForFixedUpdate();
                }
            }

            bool withinRange = (GameDirector.player.head.position - transform.position).sqrMagnitude < 4.0f;
            if (marketContainer.IsValidMarket())
            {
                marketContainer.MarkAsReady();
                if (withinRange)
                {
                    iconMeshRenderer.enabled = true;
                    SoundManager.main.RequestHandle(transform.position).PlayOneShot(readySound);

                    float timer = 0.0f;
                    float duration = 0.35f;
                    while (timer < duration)
                    {
                        timer = Mathf.Min(timer + Time.deltaTime, duration);
                        float ratio = timer / duration;
                        float easeIn = 1.0f - Mathf.Pow(1.0f - ratio, 2);
                        iconMeshRenderer.material.SetFloat(scaleID, Mathf.Lerp(0.5f, 2.5f, easeIn));
                        iconMeshRenderer.material.SetFloat(alphaID, easeIn);
                        iconMeshRenderer.material.SetFloat(additiveID, 1.0f - easeIn);
                        yield return null;
                    }
                    yield return new WaitForSeconds(1.0f);

                    timer = 0.0f;
                    duration = 0.35f;
                    while (timer < duration)
                    {
                        timer = Mathf.Min(timer + Time.deltaTime, duration);
                        float ratio = timer / duration;
                        float easeOut = 1.0f - Mathf.Pow(1.0f - ratio, 2);
                        iconMeshRenderer.material.SetFloat(scaleID, Mathf.Lerp(2.5f, 3.5f, easeOut));
                        iconMeshRenderer.material.SetFloat(alphaID, 1.0f - easeOut);
                        yield return null;
                    }
                    iconMeshRenderer.enabled = false;
                }
            }
            animationCoroutine = null;
        }

        public void StopValidateReadyContainer()
        {
            if (animationCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(animationCoroutine);
                animationCoroutine = null;
                iconMeshRenderer.enabled = false;
            }
        }

        public void BeginValidateReadyContainer()
        {
            StopValidateReadyContainer();
            animationCoroutine = GameDirector.instance.StartCoroutine(AnimateIcon());
        }
    }

}