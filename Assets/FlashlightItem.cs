using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva {

    public class FlashlightItem : Item
    {
        [SerializeField]
        private GameObject lightContainer;

        [SerializeField]
        private SoundSet flashlightOnSound;

        [SerializeField]
        private SoundSet flashlightOffSound;

        private Coroutine toggleCoroutine = null;

        [SerializeField]
        private MeshRenderer flashlightRenderer;

        private static int emissionColorID = Shader.PropertyToID("_EmissionColor");

        protected override void OnItemAwake()
        {
            base.OnItemAwake();
        }
        private void Toggle()
        {
            StopLightCoroutine();
            toggleCoroutine = GameDirector.instance.StartCoroutine(SetLightOn(!lightContainer.activeSelf));
        }

        private void StopLightCoroutine()
        {
            if (toggleCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(toggleCoroutine);
                toggleCoroutine = null;
            }
        }

        public override void OnItemLateUpdate()
        {
            if(mainOwner == null)
            {
                Debug.Log("mainOwner null");
                return;
            }
            PlayerHandState handState = mainOwner.FindOccupyStateByHeldItem(this) as PlayerHandState;
            if (handState == null)
            {
                Debug.Log("Handstate null");
                return;
            }
            if (handState.actionState.isDown)
            {
                Toggle();
            }
        }

        private IEnumerator SetLightOn(bool enable)
        {
            yield return new WaitForSeconds(0.3f);
            lightContainer.SetActive(enable);

            if (enable)
            {
                SoundManager.main.RequestHandle(transform.position).PlayOneShot(flashlightOnSound.GetRandomAudioClip());
                flashlightRenderer.material.SetColor(emissionColorID, Color.white);
       
            }
            else
            {
                SoundManager.main.RequestHandle(transform.position).PlayOneShot(flashlightOffSound.GetRandomAudioClip());
                flashlightRenderer.material.SetColor(emissionColorID, Color.black);
            }
        }

        public override void OnPreDrop()
        {
            StopLightCoroutine();
        }

    }
}