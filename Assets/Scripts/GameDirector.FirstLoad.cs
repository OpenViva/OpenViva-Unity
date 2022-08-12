using System.Collections;
using System.IO;
using UnityEngine;


namespace viva
{


    public partial class GameDirector : MonoBehaviour
    {

        [SerializeField]
        private GameObject firstLoadHints;
        [SerializeField]
        private GameObject afterFirstLoadHints;
        [SerializeField]
        private GameObject firstLoadPrefab;


        private IEnumerator FirstLoadTutorial()
        {

            GameObject.Instantiate(firstLoadPrefab);
            yield return new WaitForSeconds(0.5f);
            settings.SelectBestVRControllerSetup();
            if (player)
            {
                player.OpenPauseMenu();
                player.pauseMenu.ShowFirstLoadInstructions();
            }
            while (IsAnyUIMenuActive())
            {
                yield return null;
            }
            firstLoadHints.SetActive(true);
        }

        private void LoadLanguage()
        {

            string path = "Languages/" + languageName + ".lang";
            if (!File.Exists(path))
            {
                Debug.Log("[Language] could not read [" + path + "]");
                return;
            }
            Debug.Log("[Language] Loading " + languageName);

            string data = File.ReadAllText(path);
            try
            {
                m_language = JsonUtility.FromJson(data, typeof(Language)) as Language;
            }
            catch
            {
                Debug.LogError("[Language] Could not parse!");
                m_language = null;
            }
        }
        private void BuildBoundaryWalls()
        {
            var left = Boundary.AddComponent<BoxCollider>();
            left.center = new Vector3(-18.48f, 38.5f, 265.29f);
            left.size = new Vector3(1029.27f, 160.29f, 3.03f);
            var right = Boundary.AddComponent<BoxCollider>();
            right.center = new Vector3(-18.48f, 38.5f, -246.95f);
            right.size = new Vector3(1029.27f, 160.29f, 3.03f);
            var back = Boundary.AddComponent<BoxCollider>();
            back.center = new Vector3(494.67f, 38.5f, 9.68f);
            back.size = new Vector3(2.56f, 160.29f, 514.69f);
            var front  = Boundary.AddComponent<BoxCollider>();
            front.center = new Vector3(-530.25f, 38.5f, 9.68f);
            front.size = new Vector3(2.56f, 160.29f, 514.69f);
        }
    }

}