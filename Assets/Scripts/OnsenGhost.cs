using UnityEngine;


namespace viva
{

    public class OnsenGhost : MonoBehaviour
    {

        [SerializeField]
        private SoundSet scarySounds;
        [SerializeField]
        private OnsenGhostMiniGame miniGame;

        private float waitForSound;
        public float currentSpeed;

        private void OnEnable()
        {
            waitForSound = 0.0f;
        }

        public void Kill()
        {
            miniGame.AlternateWin();
        }


        void Update()
        {
            Vector3 forward = Tools.FlatForward(GameDirector.instance.mainCamera.transform.position - transform.position);
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(90.0f, 0.0f, 0.0f);
            transform.position += forward.normalized * currentSpeed * Time.deltaTime;

            Vector3 pos = transform.position;
            pos.y += (GameDirector.player.transform.position.y + 1.0f - pos.y) * Time.deltaTime * 0.5f;
            transform.position = pos;

            waitForSound -= Time.deltaTime;
            if (waitForSound <= 0.0f)
            {
                waitForSound = 4.0f + Random.value * 4.0f;

                var handle = SoundManager.main.RequestHandle(Vector3.zero, transform);
                handle.PlayOneShot(scarySounds.GetRandomAudioClip());
                handle.pitch = 0.7f + Random.value * 0.4f;
                handle.maxDistance = 16.0f;
            }
        }
    }

}