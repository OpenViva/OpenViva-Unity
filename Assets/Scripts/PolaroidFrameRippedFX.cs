using System.Collections;
using UnityEngine;

namespace viva
{


    public class PolaroidFrameRippedFX : MonoBehaviour
    {

        [SerializeField]
        private ParticleSystem fx;

        private float timer = 1.0f;

        private IEnumerator finish()
        {
            while (timer > 0.0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }
            Destroy(this.gameObject);
        }

        public void EmitFX(Vector3 position)
        {
            transform.position = position;
            fx.Emit(10);
            timer = 2.0f;
        }
        private void Start()
        {
            StartCoroutine(finish());
        }
    }


}