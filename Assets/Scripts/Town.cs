using System.Collections.Generic;
using UnityEngine;


namespace viva
{

    using LoliInit = Tuple<Loli, GameDirector.VivaFile.SerializedLoli>;

    public class Town : VivaSessionAsset
    {

        [SerializeField]
        private int maxResidents = 8;
        [SerializeField]
        private Waypoints m_mainWaypoints;
        public Waypoints mainWaypoints { get { return m_mainWaypoints; } }
        [SerializeField]
        private List<Service> m_services = new List<Service>();
        public List<Service> services { get { return m_services; } }



        protected override void OnAwake()
        {
            GameDirector.instance.AddOnFinishLoadingCallback(OnPostLoad);
        }

        private void OnPostLoad()
        {
            var cardsAvailable = ModelCustomizer.main.characterCardBrowser.FindAllExistingCardsInFolders();

            if (cardsAvailable.Length == 0)
            {
                GameDirector.instance.fileLoadStatus.description.text = "No Cards Found!";
            }

            BuildTownLolis(cardsAvailable, maxResidents - (GameDirector.characters.Count - 1), null);
        }

        public void BuildTownLolis(string[] cards, int count, Vector3? defaultSpawnPos)
        {
            if (count == 0)
            {
                return;
            }
            List<LoliInit> startingTownLolis = new List<LoliInit>();
            Debug.Log("[Town] Generating " + count + " loli residents...");
            for (int i = 0; i < count; i++)
            {
                var cardFilename = cards[i % cards.Length] + ".png";
                var serializedLoli = new GameDirector.VivaFile.SerializedLoli(cardFilename, new GameDirector.VivaFile.SerializedAsset(cardFilename));
                var targetLoli = GameDirector.instance.GetLoliFromPool();

                GameDirector.instance.StartCoroutine(Loli.LoadLoliFromSerializedLoli(serializedLoli.sourceCardFilename, targetLoli, delegate ()
                {
                    startingTownLolis.Add(new LoliInit(targetLoli, serializedLoli));
                    if (startingTownLolis.Count == count)
                    {
                        PrepareTownLolis(startingTownLolis, defaultSpawnPos);
                    }
                }
                ));
            }
        }

        private void PrepareTownLolis(List<LoliInit> startingTownLolis, Vector3? defaultSpawnPos)
        {

            int loliIndex = startingTownLolis.Count;
            if (!defaultSpawnPos.HasValue)
            {
                for (int i = 0; i < services.Count; i++)
                {
                    var service = services[i];

                    int employeeInfosAvailable = service.employeeInfosAvailable;
                    while (employeeInfosAvailable-- > 0 && loliIndex-- > 0)
                    {
                        var employeeSlot = service.GetEmployeeInfo(employeeInfosAvailable);
                        var worldSpawnPosition = service.transform.TransformPoint(employeeSlot.localPos);
                        var worldSpawnForward = service.transform.TransformDirection(employeeSlot.localRootFacePos);
                        startingTownLolis[loliIndex]._2.serviceIndex = i;
                        startingTownLolis[loliIndex]._2.propertiesAsset.transform.position = worldSpawnPosition;
                        startingTownLolis[loliIndex]._2.propertiesAsset.transform.rotation = Quaternion.LookRotation(worldSpawnForward, Vector3.up);
                    }
                }
            }
            while (loliIndex-- > 0)
            {
                int nodeIndex = Random.Range(0, mainWaypoints.nodes.Length - 1);
                Vector3 worldSpawnPosition;
                if (defaultSpawnPos.HasValue)
                {
                    worldSpawnPosition = defaultSpawnPos.Value;
                }
                else
                {
                    worldSpawnPosition = transform.TransformPoint(mainWaypoints.nodes[nodeIndex].position);
                }
                startingTownLolis[loliIndex]._2.propertiesAsset.transform.position = worldSpawnPosition;
            }

            foreach (var pair in startingTownLolis)
            {
                GameDirector.instance.StartCoroutine(pair._1.InitializeLoli(pair._2, null));
            }
        }
    }

}