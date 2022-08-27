using System.Collections;
using UnityEngine;

namespace viva
{

    public class Rein : Item
    {

        [SerializeField]
        private Horse horse;

        private Vector3 restPos;
        private Quaternion restRot;
        private Coroutine animateToRestPoseCoroutine = null;

        public override void OnPreDrop()
        {

            horse.RemoveDriverHand();
            PlayRestPoseAnimation();
        }

        public override bool OnPrePickupInterrupt(HandState handState)
        {
            if (horse.sleeping)
            {
                horse.WakeUp();
                return true;
            }
            return false;
        }

        public override void OnPostPickup()
        {
            restPos = transform.localPosition;
            restRot = transform.localRotation;
            StopRestPoseAnimation();
            mainOwner.gameObject.layer = WorldUtil.noneLayer;

            horse.AddDriverHand(mainOwner as Player);
        }

        private void StopRestPoseAnimation()
        {
            if (animateToRestPoseCoroutine == null)
            {
                return;
            }
            GameDirector.instance.StopCoroutine(animateToRestPoseCoroutine);
            animateToRestPoseCoroutine = null;
        }

        private void PlayRestPoseAnimation()
        {
            if (animateToRestPoseCoroutine != null)
            {
                return;
            }
            animateToRestPoseCoroutine = GameDirector.instance.StartCoroutine(RestPoseAnimation());
        }

        public override void OnItemLateUpdatePostIK()
        {
            HandState handState = mainOccupyState as HandState;
            if (handState != null)
            {
                transform.position = handState.fingerAnimator.targetBone.position;
                transform.rotation = handState.fingerAnimator.targetBone.rotation;
            }
        }


        private IEnumerator RestPoseAnimation()
        {

            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;

            float timer = 0.0f;
            while (timer < 0.25f)
            {
                float ratio = Mathf.Clamp01(timer / 0.25f);
                transform.localPosition = Vector3.Lerp(startPos, restPos, ratio);
                transform.localRotation = Quaternion.Lerp(startRot, restRot, ratio);
                timer += Time.deltaTime;
                yield return null;
            }
            animateToRestPoseCoroutine = null;
        }
    }

}