using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva
{

    public class SessionMenu : MonoBehaviour
    {
        [SerializeField]
        private LibraryExplorer sessionExplorer;
        [SerializeField]
        private SceneSwitcher sceneSwitcherPrefab;

        private string selectedSessionFullpath = null;
        private bool loadingScene = false;

        public bool loadMode = false;

        
        public void SetLoadMode( bool _loadMode ){
            loadMode = _loadMode;
        }

        private void OnEnable()
        {
            if (loadMode)
            {
                DisplaySessionOptions("New Session", "save");
            }
            else
            {
                DisplaySessionOptions("New Session", "template");
            }
        }

        private void DisplaySessionOptions(string title, string sessionTypeFilter)
        {
            sessionExplorer.SetDefaultPrepare(delegate
            {
                sessionExplorer.DisplaySelection(
                    title,
                    sessionExplorer.ExpandDialogOptions(ImportRequestType.SESSION, null),
                    delegate (DialogOption option, LibraryEntry source)
                    {
                        SelectSession(option.value);
                    },
                    null,
                    null,
                    delegate (SpawnableImportRequest request)
                    {
                        SceneRequest sceneRequest = request as SceneRequest;
                        return sceneRequest.sceneSettings.type == sessionTypeFilter;
                    }
                );
            });
        }

        public void SelectSession(string session, string subfolder = null)
        {
            selectedSessionFullpath = System.IO.Path.GetFullPath((subfolder == null ? SceneSettings.root : subfolder) + "/" + session + ".viva");
            Sound.main.PlayGlobalUISound(UISound.BUTTON2);
        }

        public void LoadSession(string session, string subfolder = null)
        {
            SelectSession(session, subfolder);
            LoadSelectedSession();
        }

        public void LoadSelectedSession()
        {
            if (loadingScene) return;
            if (selectedSessionFullpath == null) return;
            if (selectedSessionFullpath == null) return;

            Debug.Log("Loading session \"" + selectedSessionFullpath + "\"");
            var request = ImportRequest.CreateRequest(selectedSessionFullpath) as SceneRequest;
            if (request == null) return;
            loadingScene = true;

            request.Import();
            SpawnScene(request);
        }

        private void SpawnScene(SceneRequest request)
        {
            if (request.imported)
            {
                request.sceneSwitcher = GameObject.Instantiate(sceneSwitcherPrefab);
                GameObject.DontDestroyOnLoad(request.sceneSwitcher);
                request._InternalSpawnUnlinked(false, new SpawnProgress(delegate { loadingScene = false; }));
            }
        }
    }

}