using UnityEngine;


namespace viva
{


    public abstract partial class Character : VivaSessionAsset
    {

        [SerializeField]
        private FootstepInfo m_footstepInfo;
        public FootstepInfo footstepInfo { get { return m_footstepInfo; } }

        private void InitFootstepSounds()
        {
            footstepInfo.lastFloorPos = floorPos;
        }

        public void UpdateFootstepCheck()
        {
            //Return if player is on horse
            if (GameDirector.player.controller as HorseControls != null)
            {
                return;
            }

            if (Vector3.SqrMagnitude(floorPos - footstepInfo.lastFloorPos) > 1.0f)
            {
                footstepInfo.lastFloorPos = floorPos;
                PlayFootstep();
            }
        }

        protected virtual void OnFootstep() { }


        public void PlayFootstep()
        {
            SoundManager.main.RequestHandle(floorPos).PlayOneShot(footstepInfo.sounds[(int)footstepInfo.currentType].GetRandomAudioClip());

            if (footstepInfo.currentType == FootstepInfo.Type.WATER)
            {
                Vector3 pos = floorPos + head.forward * 0.2f;
                Quaternion rot = Quaternion.LookRotation(Tools.FlatForward(head.forward), Vector3.up) * Quaternion.Euler(-60.0f, 0.0f, 0.0f);
                GameDirector.instance.SplashWaterFXAt(pos, rot, 0.7f, 1.8f, 15);
            }
            OnFootstep();
        }

        public T GetItemIfHeldByEitherHand<T>() where T : Item
        {
            T item = rightHandState.GetItemIfHeld<T>();
            if (item == null)
            {
                item = leftHandState.GetItemIfHeld<T>();
            }
            return item;
        }
    }

}