using System.Collections;
using UnityEngine;

namespace viva
{


    public partial class Loli : Character
    {

        public delegate void OnLoadFinish();


        public static IEnumerator LoadLoliFromSerializedLoli(string cardFilename, Loli targetLoli, OnLoadFinish onFinish)
        {

            if (targetLoli == null)
            {
                Debug.LogError("[Loli] targetLoli is null!");
                yield break;
            }
            ModelCustomizer.LoadLoliFromCardRequest cardRequest = new ModelCustomizer.LoadLoliFromCardRequest(cardFilename, targetLoli);
            yield return GameDirector.instance.StartCoroutine(ModelCustomizer.main.LoadVivaModelCard(cardRequest));
            if (cardRequest.target == null)
            {
                Debug.LogError(cardRequest.error);
                if (onFinish != null)
                {
                    onFinish();
                }
                yield break;
            }

            Debug.Log("[Loli] Loaded " + cardFilename);
            if (onFinish != null)
            {
                onFinish();
            }
        }

        public void SetNameTagTexture(Texture2D texture, float yOffset)
        {
            if (texture == null)
            {
                nametagMR.gameObject.SetActive(false);
            }
            else
            {
                nametagMR.gameObject.SetActive(true);
                nametagMR.material.mainTexture = texture;
                nametagMR.material.SetTextureOffset("_MainTex", new Vector2(0, yOffset));
            }
        }

        public override sealed void Save(GameDirector.VivaFile vivaFile)
        {

            //serialize loli self
            var serializedAsset = new GameDirector.VivaFile.SerializedAsset(this);
            if (serializedAsset == null)
            {
                Debug.LogError("[PERSISTANCE] Could not save loli " + name);
                return;
            }
            var selfAsset = new GameDirector.VivaFile.SerializedLoli(headModel.sourceCardFilename, serializedAsset);
            serializedAsset.transform.position = floorPos;
            serializedAsset.transform.rotation = Quaternion.LookRotation(Tools.FlatForward(spine1RigidBody.transform.forward), Vector3.up);

            selfAsset.activeTaskSession = new GameDirector.VivaFile.SerializedLoli.SerializedTaskData(active.currentTask);
            selfAsset.serviceIndex = Service.GetServiceIndex(this); //-1 if null
            vivaFile.loliAssets.Add(selfAsset);
        }

        //applies all serialized properties and awakens loli scripts
        public IEnumerator InitializeLoli(GameDirector.VivaFile.SerializedLoli serializedLoli, OnGenericCallback onFinish)
        {

            if (serializedLoli == null)
            {
                Debug.LogError("[Loli] LoliAsset is null!");
                yield break;
            }

            //apply serialized properties
            var cdm = new CoroutineDeserializeManager();
            GameDirector.instance.StartCoroutine(SerializedVivaProperty.Deserialize(serializedLoli.propertiesAsset.properties, this, cdm));
            transform.position = serializedLoli.propertiesAsset.transform.position;
            transform.rotation = serializedLoli.propertiesAsset.transform.rotation;

            while (!cdm.finished)
            {
                yield return null;
            }

            gameObject.SetActive(true); //call character awake

            //employ into service if any
            if (serializedLoli.serviceIndex >= 0 && serializedLoli.serviceIndex < GameDirector.instance.town.services.Count)
            {
                var service = GameDirector.instance.town.services[serializedLoli.serviceIndex];
                if (service.Employ(this))
                {
                    //match task with service
                    serializedLoli.activeTaskSession.taskIndex = (int)service.targetBehavior;
                }
            }
            //apply active session if any
            if (serializedLoli.activeTaskSession != null)
            {
                var task = active.GetTask((ActiveBehaviors.Behavior)serializedLoli.activeTaskSession.taskIndex);
                GameDirector.instance.StartCoroutine(SerializedVivaProperty.Deserialize(serializedLoli.activeTaskSession.properties, task.session, cdm));
                active.SetTask(task, null);
            }

            while (!cdm.finished)
            {
                yield return null;
            }

            GameDirector.characters.Add(this);

            onFinish?.Invoke();
        }
    }

}