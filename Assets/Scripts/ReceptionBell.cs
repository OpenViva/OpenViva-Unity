using UnityEngine;


namespace viva
{


    public class ReceptionBell : MonoBehaviour
    {

        [SerializeField]
        private ParticleSystem bellFX;
        [SerializeField]
        private AudioClip bellSound;
        [SerializeField]
        private OnsenReception targetReception;

        public FilterUse filterUse { get; private set; } = new FilterUse();


        private void OnCollisionEnter(Collision collision)
        {

            if (collision.rigidbody && collision.relativeVelocity.magnitude > 0.5f)
            {

                var handle = SoundManager.main.RequestHandle(transform.position);
                handle.volume = Mathf.Clamp01(collision.relativeVelocity.magnitude - 0.5f);
                handle.PlayOneShot(bellSound);

                bellFX.Emit(1);

                Player source = Tools.SearchTransformAncestors<Player>(collision.transform);
                targetReception.CreateClerkSession(source, null);
            }
        }
    }

}