using System.Collections.Generic;
using UnityEngine;


namespace viva
{

    public class Oven : Mechanism
    {


        public static readonly int WATER_LAYERS = 4;

        [SerializeField]
        private bool startOn = true;
        [SerializeField]
        private AudioSource soundSource = null;
        [SerializeField]
        private ParticleSystem fireFX;
        [SerializeField]
        private Light fireLight;
        [SerializeField]
        private ParticleSystem burntFX;
        [SerializeField]
        private AudioClip bakedburnedSound;
        [SerializeField]
        private AudioClip bakedReadySound;

        private Collider[] colliders = new Collider[16];
        private float lastBakeUpdateTime = 0.0f;
        private ParticleSystem.EmitParams emitParams;
        private List<Item> itemsBeingBaked = new List<Item>();


        private void SetFlowEmission(bool enable)
        {
            var emissionModule = fireFX.emission;
            emissionModule.enabled = enable;

            emitParams = new ParticleSystem.EmitParams();
        }

        public void SetOn(bool on)
        {

            fireFX.gameObject.SetActive(on);
            fireLight.enabled = on;
            SetFlowEmission(on);
            if (on)
            {
                soundSource.Play();
            }
            else
            {
                soundSource.Stop();
            }
        }

        public override void OnMechanismAwake()
        {
            base.OnMechanismAwake();
            SetOn(startOn);
        }
        public override bool AttemptCommandUse(Loli targetLoli, Character commandSource)
        {
            return false;
        }

        public override void EndUse(Character targetCharacter)
        {
        }
        public void playBakedReadySound()
        {
            soundSource.PlayOneShot(bakedReadySound);
        }

        public override void OnMechanismFixedUpdate()
        {

            if (Time.time - lastBakeUpdateTime < 1.0f)
            {
                return;
            }
            lastBakeUpdateTime = Time.time;

            for (int i = itemsBeingBaked.Count; i-- > 0;)
            {
                Item item = itemsBeingBaked[i];
                if (item == null)
                {
                    itemsBeingBaked.RemoveAt(i);
                    continue;
                }
                switch (item.settings.itemType)
                {
                    case Item.Type.PASTRY:
                        (item as Pastry).UpdateOvenState(this, 1.0f);
                        break;
                    case Item.Type.POT:
                        (item as Pot).UpdateOvenState(this, 1.0f);
                        break;
                    case Item.Type.PEACH:
                    case Item.Type.STRAWBERRY:
                    case Item.Type.BLUEBERRY:
                    case Item.Type.CANTALOUPE:
                    case Item.Type.CANDLE:
                    case Item.Type.DONUT:
                    case Item.Type.WHEAT_SPIKE:
                    case Item.Type.EGG:
                    case Item.Type.SOAP:
                    case Item.Type.WATER_REED:
                        BurnAndDestroy(item.gameObject);
                        break;
                    case Item.Type.CHICKEN:
                        Chicken chicken = item.GetComponent<Chicken>();
                        soundSource.PlayOneShot(chicken.chickenSettings.bukku.GetRandomAudioClip());
                        BurnAndDestroy(item.gameObject);
                        break;
                }
            }
        }

        private void OnTriggerEnter(Collider collider)
        {

            Item item = collider.gameObject.GetComponent(typeof(Item)) as Item;
            if (item)
            {
                itemsBeingBaked.Add(item);
                GameDirector.mechanisms.Add(this);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            Item item = collider.gameObject.GetComponent(typeof(Item)) as Item;
            if (item)
            {
                itemsBeingBaked.Remove(item);
                if (itemsBeingBaked.Count == 0)
                {
                    GameDirector.mechanisms.Remove(this);
                }
            }
        }

        public void BurnAndDestroy(GameObject target)
        {
            burntFX.transform.position = target.transform.position;
            burntFX.Emit(10);
            GameDirector.Destroy(target);
            soundSource.PlayOneShot(bakedburnedSound);
        }
    }

}