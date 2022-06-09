using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace viva
{


    public partial class GameDirector : MonoBehaviour
    {

        [Header("Persistence")]
        [SerializeField]
        private GameObject[] itemPrefabManifest;
        [SerializeField]
        private GameSettings m_settings;
        public FileLoadStatus fileLoadStatus;


        public GameObject FindItemPrefabByName(string name)
        {
            foreach (var item in itemPrefabManifest)
            {
                if (item.name == name)
                {
                    return item;
                }
            }
            Debug.LogError("Could not find item in manifest: " + name);
            return null;
        }

        [System.Serializable]
        public class TransformSave
        {
            public Vector3 position;
            public Quaternion rotation;

            public TransformSave(Transform target)
            {
                position = target.position;
                rotation = target.rotation;
            }
            public TransformSave()
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
            }
        }

        [System.Serializable]
        public class VivaFile
        {

            public string languageName;

            [System.Serializable]
            public class SerializedLoli
            {

                //defines assigned active task session data
                [System.Serializable]
                public class SerializedTaskData
                {
                    public int taskIndex;
                    public List<SerializedVivaProperty> properties;

                    public SerializedTaskData(ActiveBehaviors.ActiveTask task)
                    {
                        taskIndex = (int)task.type;
                        if (task.session != null)
                        {
                            properties = SerializedVivaProperty.Serialize(task.session);
                        }
                    }
                    public SerializedTaskData(ActiveBehaviors.Behavior taskType)
                    {
                        taskIndex = (int)taskType;
                    }
                }

                public string sourceCardFilename;
                public SerializedAsset propertiesAsset;
                public SerializedTaskData activeTaskSession;
                public int serviceIndex = -1;


                public SerializedLoli(string _sourceCardFilename, SerializedAsset _propertiesAsset)
                {
                    sourceCardFilename = _sourceCardFilename;
                    propertiesAsset = _propertiesAsset;
                    activeTaskSession = new SerializedTaskData(ActiveBehaviors.Behavior.IDLE);
                }
            }

            //defines a prefab gameObject and a single component
            [System.Serializable]
            public class SerializedAsset
            {
                public bool targetsSceneAsset;
                public string assetName;
                public string sessionReferenceName;
                public List<SerializedVivaProperty> properties;
                public TransformSave transform;

                public SerializedAsset(VivaSessionAsset target)
                {
                    targetsSceneAsset = target.targetsSceneAsset;
                    assetName = target.assetName;
                    transform = new TransformSave(target.transform);
                    sessionReferenceName = target.sessionReferenceName;

                    properties = SerializedVivaProperty.Serialize(target);
                }

                public SerializedAsset(string _sessionReferenceName)
                {
                    targetsSceneAsset = false;
                    assetName = "";
                    transform = new TransformSave();
                    sessionReferenceName = _sessionReferenceName;
                }
            }

            public List<SerializedAsset> serializedAssets = new List<SerializedAsset>();
            public List<SerializedLoli> loliAssets = new List<SerializedLoli>();
        }

        public void Save()
        {

            VivaFile vivaFile = new VivaFile();
            List<GameObject> rootObjects = new List<GameObject>(SceneManager.GetActiveScene().rootCount);
            SceneManager.GetActiveScene().GetRootGameObjects(rootObjects);
            List<VivaSessionAsset> assets = new List<VivaSessionAsset>();
            foreach (GameObject rootObj in rootObjects)
            {
                assets.AddRange(rootObj.GetComponentsInChildren<VivaSessionAsset>(true));
            }

            vivaFile.languageName = languageName;
            foreach (VivaSessionAsset asset in assets)
            {
                if (asset.IgnorePersistance())
                {
                    continue;
                }
                asset.Save(vivaFile);
            }
            Steganography.EnsureFolderExistence("Saves");
            //combine and save byte buffers
            string json = JsonUtility.ToJson(vivaFile, true);
            using (var stream = new FileStream("Saves/save.viva", FileMode.Create))
            {

                byte[] data = Tools.UTF8ToByteArray(json);
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            Debug.Log("[Persistance] Saved File!");
        }

        protected void AttemptLoadVivaFile()
        {

            string path = "Saves/save.viva";
            VivaFile file = null;
            if (File.Exists(path))
            {
                string data = File.ReadAllText(path);
                file = JsonUtility.FromJson(data, typeof(VivaFile)) as VivaFile;
                if (file == null)
                {
                    Debug.LogError("[Persistance] ERROR Could not load VivaFile!");
                }
                else
                {
                    languageName = file.languageName;
                    if (languageName == null)
                    {
                        languageName = "english";
                    }
                }
            }
            StartCoroutine(LoadVivaFile(file));
        }

        private IEnumerator LoadVivaFile(VivaFile file)
        {

            int oldMask = mainCamera.cullingMask;
            mainCamera.cullingMask = Instance.uiMask;

            fileLoadStatus.gameObject.SetActive(true);

            if (file == null)
            {
                //defaults if no file present
                settings.SetWorldTime(GameDirector.skyDirector.firstLoadDayOffset);
                settings.SetDayNightCycleSpeedIndex(1);
                StartCoroutine(FirstLoadTutorial());

                yield return null;
            }
            else
            {
                afterFirstLoadHints.SetActive(true);

                //load lolis
                var cdm = new CoroutineDeserializeManager();

                var toLoad = new List<Tuple<Loli, VivaFile.SerializedLoli>>();
                foreach (var serializedLoli in file.loliAssets)
                {
                    var targetLoli = GameDirector.instance.GetLoliFromPool();
                    cdm.waiting++;
                    StartCoroutine(Loli.LoadLoliFromSerializedLoli(serializedLoli.sourceCardFilename, targetLoli, delegate
                    {
                        toLoad.Add(new Tuple<Loli, VivaFile.SerializedLoli>(targetLoli, serializedLoli));
                        cdm.waiting--;
                    }
                    ));
                }

                //Load all serialized storage variables first
                StartCoroutine(VivaSessionAsset.LoadFileSessionAssets(file, cdm));

                while (!cdm.finished)
                {
                    fileLoadStatus.description.text = "Unpacking " + cdm.waiting + " asset(s)";
                    yield return null;
                }
                Debug.Log("[Persistance] Loaded storage variables...");

                //initialize lolis
                foreach (var entry in toLoad)
                {
                    cdm.waiting++;
                    StartCoroutine(entry._1.InitializeLoli(entry._2, delegate
                    {
                        cdm.waiting--;
                    }));
                }

                while (!cdm.finished)
                {
                    fileLoadStatus.description.text = "Initializing " + cdm.waiting + " character(s)";
                    yield return null;
                }
                Debug.Log("[Persistance] Loaded file!");
            }
            onFinishLoadingVivaFile();

            fileLoadStatus.gameObject.SetActive(false);

            mainCamera.cullingMask = oldMask;
        }

        private void OnPostLoadVivaFile()
        {
            LoadLanguage();
            GameDirector.skyDirector.enabled = true;
            InitMusic();
        }
    }

}