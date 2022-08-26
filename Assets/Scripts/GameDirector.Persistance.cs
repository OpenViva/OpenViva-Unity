using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace viva
{
    //TODO: Cleanup all this cause its kinda messy
    [System.Serializable]
    public class GameSettings
    {
        public static GameSettings main { get; private set; } = new GameSettings();

        public float mouseSensitivity = 240.0f;
        public float musicVolume = 0.5f;
        public int dayNightCycleSpeedIndex = 0;
        public bool disableGrabToggle = true;
        public bool pressToTurn = false;
        public Player.VRControlType vrControls = Player.VRControlType.TRACKPAD;
        public bool trackpadMovementUseRight = false;
        //Save current Calibration Position/Rotation
        public Vector3 CalibratePosition = new Vector3(0.003261785f, 0.086780190f, 0.05201015f);
        public Vector3 CalibrateEuler = new Vector3(-350.4226f, -101.9745f, -152.1913f);
        public int qualityLevel = 2;
        public int shadowLevel = 3;
        public int antiAliasing = 2;
        public int reflectionDistance = 100;
        public float lodDistance = 1.0f;
        public int fpsLimit = 90;
        public bool fullScreen = false;
        public bool vSync = false;       
        public bool toggleTooltips = true;
        public bool toggleClouds = false;

        private string[] dayNightCycleSpeedDesc = new string[]{
        "12 minutes",
        "24 minutes",
        "40 minutes",
        "2 hour",
        "Never Change"
        };

        public void Apply()
        {
            int currQuality = GameSettings.main.qualityLevel;
            QualitySettings.SetQualityLevel(qualityLevel);
            bool enableRealtimeReflections = currQuality >= 1;
            float refreshTimeout = currQuality >= 1 ? 0 : 1;
            float maxRefreshTimeout = currQuality >= 1 ? 0 : 8;
            int resolution = currQuality <= 1 ? 16 : 64;

            Vector3 newSize = new Vector3(reflectionDistance, reflectionDistance, reflectionDistance);
            GameDirector.player.realtimeReflectionController.enabled = enableRealtimeReflections;
            GameDirector.player.realtimeReflectionController.reflectionProbe.resolution = resolution;
            GameDirector.player.realtimeReflectionController.refreshTimeout = refreshTimeout;
            GameDirector.player.realtimeReflectionController.maxRefreshTimeout = maxRefreshTimeout;
            GameDirector.player.realtimeReflectionController.reflectionProbe.size = newSize;
            QualitySettings.antiAliasing = antiAliasing;
            QualitySettings.vSyncCount = vSync ? 1 : 0;     
            QualitySettings.lodBias = lodDistance;
            Application.targetFrameRate = vSync ? -1 : fpsLimit;
            GameDirector.player.pauseMenu.ToggleFpsLimitContainer(!vSync);
            ApplyShadowSettings();
        }
        public void Copy(GameSettings copy)
        {
            if (copy == null) return;
            mouseSensitivity = copy.mouseSensitivity;
            musicVolume = copy.musicVolume;
            dayNightCycleSpeedIndex = copy.dayNightCycleSpeedIndex;
            disableGrabToggle = copy.disableGrabToggle;
            pressToTurn = copy.pressToTurn;
            vrControls = copy.vrControls;
            trackpadMovementUseRight = copy.trackpadMovementUseRight;
            CalibratePosition = copy.CalibratePosition;
            CalibrateEuler = copy.CalibrateEuler;
            qualityLevel = copy.qualityLevel;
            shadowLevel = copy.shadowLevel;
            antiAliasing = copy.antiAliasing;
            reflectionDistance = copy.reflectionDistance;
            lodDistance = copy.lodDistance;
            fpsLimit = copy.fpsLimit;
            vSync = copy.vSync;
            fullScreen = copy.fullScreen;
            toggleTooltips = copy.toggleTooltips;
            toggleClouds = copy.toggleClouds;
        }

        public void ApplyShadowSettings()
        {
            switch (shadowLevel){
                default:
                    QualitySettings.shadowCascades = 0;
                    QualitySettings.shadowDistance = 0;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    break;
                case 1:
                    QualitySettings.shadowDistance = 50;
                    break;
                case 2:
                    QualitySettings.shadowDistance = 75;
                    QualitySettings.shadowCascades = 2;
                    break;
                case 3:
                    QualitySettings.shadowDistance = 100;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    break;
                case 4:
                    QualitySettings.shadowDistance = 150;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    break;
                case 5:
                    QualitySettings.shadowDistance = 200;
                    QualitySettings.shadowCascades = 4;
                    QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    break;
            }
        }

        public void AdjustMouseSensitivity(float direction)
        {
            SetMouseSensitivity(mouseSensitivity + direction);
        }
        public void SetMouseSensitivity(float amount)
        {
            mouseSensitivity = Mathf.Clamp(amount, 10.0f, 250.0f);
        }
        public void AdjustMusicVolume(float direction)
        {
            SetMusicVolume(musicVolume + direction);
            GameDirector.instance.UpdateMusicVolume();
        }
        public void SetMusicVolume(float percent)
        {
            musicVolume = Mathf.Clamp01(percent);
        }
        public void AdjustReflectionDistance(int direction)
        {
            SetReflectionDistance(reflectionDistance + direction);
            Apply();
        }
        public void SetReflectionDistance(int amount)
        {           
            reflectionDistance = Mathf.Clamp(amount, 0, 250);
        }
        public void AdjustFpsLimit(int direction)
        {
            SetFpsLimit(fpsLimit + direction);
            Apply();
        }
        public void SetFpsLimit(int amount)
        {
            fpsLimit = Mathf.Clamp(amount, 30, 250);
        }
        public void AdjustLODDistance(float direction)
        {
            SetLODDistance(lodDistance + direction);
            Apply();
        }
        public void SetLODDistance(float amount)
        {
            lodDistance = Mathf.Clamp(amount, 0.1f, 2.0f);
        }
        public void ShiftWorldTime(float timeAmount)
        {
            GameDirector.skyDirector.worldTime += timeAmount;
            GameDirector.skyDirector.ApplyDayNightCycle();
        }
        public void SetWorldTime(float newTime)
        {
            GameDirector.skyDirector.worldTime = newTime;
            GameDirector.skyDirector.ApplyDayNightCycle();
        }
        public string AdjustDayTimeSpeedIndex(int direction)
        {
            SetDayNightCycleSpeedIndex(dayNightCycleSpeedIndex + direction);
            GameDirector.skyDirector.UpdateDayNightCycleSpeed();
            return dayNightCycleSpeedDesc[dayNightCycleSpeedIndex];
        }
        public void SetDayNightCycleSpeedIndex(int index)
        {
            dayNightCycleSpeedIndex = Mathf.Clamp(index, 0, dayNightCycleSpeedDesc.Length - 1);
        }
        public void ToggleDisableGrabToggle()
        {
            disableGrabToggle = !disableGrabToggle;
        }
        public void TogglePresstoTurn()
        {
            pressToTurn = !pressToTurn;
        }
        public void ToggleFullScreen()
        {
            fullScreen = !fullScreen;
        }
        public void ToggleVsync()
        {
            vSync = !vSync;
            Apply();
        }
        public void SetVRControls(Player.VRControlType newVRControls)
        {
            vrControls = newVRControls;
        }
        public void ToggleTrackpadMovementUseRight()
        {
            trackpadMovementUseRight = !trackpadMovementUseRight;
        }
        public void CycleAntiAliasing()
        {
            switch (antiAliasing)
            {
                case 0:
                    antiAliasing = 2;
                    break;
                case 2:
                    antiAliasing = 4;
                    break;
                case 4:
                    antiAliasing = 8;
                    break;
                default:
                    antiAliasing = 0;
                    break;
            }
            Apply();
        }

        public void CycleQualitySetting()
        {
            qualityLevel = (int)QualitySettings.GetQualityLevel();
            qualityLevel = (qualityLevel + 1) % 5;
            Apply();
        }

        public void CycleShadowSetting()
        {
            shadowLevel = (shadowLevel + 1) % 6;
            Apply();
        }

        public void SetDefaultCalibration()
        {
            if (GameDirector.player)
            {
                GameDirector.player.rightPlayerHandState.SetAbsoluteVROffsets(CalibratePosition, CalibrateEuler, true);
                GameDirector.player.leftPlayerHandState.SetAbsoluteVROffsets(CalibratePosition, CalibrateEuler, true);
            }
        }
    }

    public partial class GameDirector : MonoBehaviour
    {

        [Header("Persistence")]
        [SerializeField]
        private GameObject[] itemPrefabManifest;
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
                GameSettings.main.SetWorldTime(GameDirector.skyDirector.firstLoadDayOffset);
                GameSettings.main.SetDayNightCycleSpeedIndex(1);
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
            RebuildCloudRendering();
        }
    }

}