using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva
{

    public class Bathtub : Mechanism
    {

        public static readonly int WATER_LAYERS = 4;

        [SerializeField]
        private Transform bathtubModelTransform = null;
        [SerializeField]
        private Vector3[] waterMeshPoints = new Vector3[0];
        [SerializeField]
        private List<Vector2> meshUVs = new List<Vector2>();
        [SerializeField]
        private Transform fillSplashParent;
        [SerializeField]
        private GameObject spigotFlow;
        [SerializeField]
        private Material waterMat;
        [SerializeField]
        private GameObject waterSurfaceBubbles = null;
        [SerializeField]
        private Transform hintSpawnPos = null;

        private Coroutine fillCoroutine = null;

        private readonly float weight0 = 0.125f;
        private readonly float weight1 = 0.55f;
        private static int BubbleSizeID = Shader.PropertyToID("_BubbleSize");

        [SerializeField]
        private MeshRenderer waterRenderer;
        [SerializeField]
        private MeshFilter waterMeshFilter;
        private Mesh waterMesh;

        private List<Vector3> meshVertices = null;
        private int[] indices = null;
        public float waterHeight;
        private int pointsPerLayer;
        private readonly int clarityID = Shader.PropertyToID("_Clarity");
        private readonly int fillChokeID = Shader.PropertyToID("_FillChoke");

        [SerializeField]
        private float edgeRadius = 0.55f;

        [SerializeField]
        private Transform[] sideAnchorAnimationPoints = new Transform[2];

        [SerializeField]
        private Vector3 centerLocalAnchorPointMin = Vector3.zero;

        [SerializeField]
        private Vector3 centerLocalAnchorPointMax = new Vector3(0.0f, 0.0f, 1.0f);

        [SerializeField]
        private ParticleSystem hotWaterEffect;

        [SerializeField]
        private AudioSource spigotAudioSource;

        public enum SoundType
        {
            WATER_KERPLUNK,
            WATER_MOVEMENT,
            WATER_SPLASH
        }

        //must match order of enum
        [SerializeField]
        private SoundSet[] waterSoundSets = new SoundSet[System.Enum.GetValues(typeof(SoundType)).Length];

        private SoundSet.StableSoundRandomizer[] soundRandomizers;

        private float hotWaterFill = 0.0f;
        private float coldWaterFill = 0.0f;
        private Temperature temperature;

        public enum Temperature
        {
            COLD,
            LUKEWARM,
            HOT
        }

        [SerializeField]
        private Door[] doorsInBathroom;

        public Vector3 GetHintSpawnPos()
        {
            return hintSpawnPos.position;
        }

        private Character characterUsingTub = null;

        public override bool AttemptCommandUse(Loli targetLoli, Character commandSource)
        {

            if (targetLoli == null)
            {
                return false;
            }
            if (characterUsingTub != null)
            {
                return false;
            }
            if (targetLoli.active.bathing.AttemptStartBathingBehavior(this, commandSource))
            {
                characterUsingTub = targetLoli;
            }
            return characterUsingTub != null;
        }

        public override void EndUse(Character targetCharacter)
        {

            Loli shinobu = targetCharacter as Loli;
            if (shinobu == null)
            {
                return;
            }
            if (targetCharacter != characterUsingTub)
            {
                return;
            }
            characterUsingTub = null;
            //character might still be using bathtub
        }

        protected class RoomObject
        {

            public readonly GameObject roomObject;
            public int partsInside = 0;

            public RoomObject(GameObject _roomObject)
            {
                roomObject = _roomObject;
            }
        }

        private List<RoomObject> objectsInBathroom = new List<RoomObject>();

        public bool IsCharacterInBathroom(Character character)
        {
            return FindRoomObject(character.gameObject) != null;
        }

        public AudioClip GetNextAudioClip(SoundType type)
        {
            SoundSet set = waterSoundSets[(int)type];
            return set.GetRandomAudioClip();
        }

        public Temperature GetTemperature()
        {
            return temperature;
        }

        public bool AreAllBathroomDoorsClosed()
        {
            for (int i = 0; i < doorsInBathroom.Length; i++)
            {
                if (!doorsInBathroom[i].IsClosed())
                {
                    return false;
                }
            }
            return true;
        }

        public Door GetBathroomDoor(int index)
        {
            return doorsInBathroom[index];
        }

        public override void OnMechanismAwake()
        {
            base.OnMechanismAwake();
            InitWaterRendering();
            soundRandomizers = new SoundSet.StableSoundRandomizer[waterSoundSets.Length];
            for (int i = 0; i < soundRandomizers.Length; i++)
            {
                soundRandomizers[i] = new SoundSet.StableSoundRandomizer(waterSoundSets[i]);
            }

            // fillCoroutine =GameDirector.instance.StartCoroutine( FillCoroutine() );
            // hotWaterFill = 1.0f;
            // coldWaterFill = 1.0f;
        }

        public bool IsClosestEdgeRightSide(Vector3 characterPos)
        {
            float localZ = bathtubModelTransform.InverseTransformPoint(characterPos).z;
            return localZ < 0.0f;
        }

        public Transform GetSideAnchorAnimationTransform(bool rightSide)
        {
            return sideAnchorAnimationPoints[System.Convert.ToInt32(rightSide)];
        }

        public Transform GetBathtubModelTransform()
        {
            return bathtubModelTransform;
        }

        public Vector3 ProjectToAnchorSegment(Vector3 point, out float segRatio)
        {

            //project to XZ plane along both min max center anchor points
            Vector3 minAnchorPos = bathtubModelTransform.TransformPoint(centerLocalAnchorPointMin);
            Vector3 maxAnchorPos = bathtubModelTransform.TransformPoint(centerLocalAnchorPointMax);

            segRatio = Mathf.Clamp01(Tools.PointOnRayRatio(minAnchorPos, maxAnchorPos, point));
            return minAnchorPos + (maxAnchorPos - minAnchorPos) * segRatio;
        }

        private IEnumerator FillCoroutine()
        {

            const float minWaterY = 2.65f;
            const float maxWaterY = 3.05f;

            spigotFlow.SetActive(true);
            spigotFlow.transform.localScale = Vector3.zero;

            waterSurfaceBubbles.SetActive(true);
            MeshRenderer waterSurfaceBubblesMR = waterSurfaceBubbles.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
            if (waterSurfaceBubblesMR == null)
            {
                Debug.LogError("ERROR water surface bubbles mr not found on prefab!");
                Debug.Break();
            }
            Material waterSurfaceBubblesMat = waterSurfaceBubblesMR.material;
            float endBubbleSize = waterSurfaceBubblesMat.GetFloat(BubbleSizeID);

            const float maxFlowSize = 1.0f;
            const float minFlowSize = 0.5f;
            Vector3 flowSize = Vector3.one * 0.5f;
            float fallTime = (1.0f - waterHeight);
            float flowSizeZ = 0.0f;
            while (true)
            {

                float targetSize = Mathf.LerpUnclamped(maxFlowSize, minFlowSize, waterHeight);

                //clamp it so a little bit is visible
                float waterFillRate = Mathf.Max(hotWaterFill + coldWaterFill, 0.1f);
                flowSizeZ = Mathf.Min(flowSizeZ + Time.deltaTime * 100.0f, targetSize);
                flowSize.x = waterFillRate * 0.5f;
                flowSize.y = flowSizeZ;
                flowSize.z = waterFillRate * 0.5f;
                spigotFlow.transform.localScale = flowSize;

                float waterSurfaceY = Mathf.LerpUnclamped(minWaterY, maxWaterY, waterHeight);

                if (flowSizeZ >= targetSize)
                {

                    waterMat.SetFloat(clarityID, 0.98f - waterHeight * waterHeight);    //Do not use 1.0f to avoid division by zero in mat
                    waterMat.SetFloat(fillChokeID, Mathf.Lerp(1.0f, -2.0f, waterHeight * 6.0f));

                    if (!fillSplashParent.gameObject.activeSelf)
                    {
                        fillSplashParent.gameObject.SetActive(true);
                    }
                    Vector3 splashPos = fillSplashParent.position;
                    splashPos.y = waterSurfaceY;
                    fillSplashParent.position = splashPos;
                    fillSplashParent.localScale = Vector3.one * waterFillRate * 0.5f;

                    waterHeight = Mathf.Clamp01(waterHeight + Time.deltaTime * 0.1f * waterFillRate);

                    UpdateWaterRendering();
                }
                //update water surface bubbles
                waterSurfaceBubblesMat.SetFloat(BubbleSizeID, waterHeight * endBubbleSize);
                Vector3 waterSurfaceBubblesPos = waterSurfaceBubbles.transform.position;
                waterSurfaceBubblesPos.y = transform.TransformPoint(Vector3.up * waterSurfaceY).y;
                waterSurfaceBubbles.transform.position = waterSurfaceBubblesPos;

                yield return null;
            }
        }

        private void InitWaterRendering()
        {
            waterMesh = new Mesh();
            waterMeshFilter.mesh = waterMesh;

            pointsPerLayer = waterMeshPoints.Length / WATER_LAYERS;
            meshVertices = new List<Vector3>();
            meshVertices.Capacity = pointsPerLayer;
            indices = new int[(pointsPerLayer - 2) * 3];

            for (int i = 0; i < pointsPerLayer; i++)
            {
                meshVertices.Add(Vector3.zero);
            }

            //build triangle fan
            int triIndex = 0;
            for (int j = 1, i = 2; i < pointsPerLayer; j = i++)
            {
                indices[triIndex++] = 0;
                indices[triIndex++] = j;
                indices[triIndex++] = i;

            }
        }

        private void UpdateWaterRendering()
        {

            //calculate height layer with weight
            int layerA;
            int layerB;
            float ratio;
            if (waterHeight - weight0 < 0.0f)
            {
                layerA = 0;
                layerB = 1;
                ratio = waterHeight / weight0;

            }
            else if (waterHeight - weight0 - weight1 < 0.0f)
            {
                layerA = 1;
                layerB = 2;
                ratio = (waterHeight - weight0) / weight1;

            }
            else
            {
                layerA = 2;
                layerB = 3;
                ratio = (waterHeight - weight0 - weight1) / (1.0f - weight0 - weight1);
            }

            for (int i = 0; i < pointsPerLayer; i++)
            {

                Vector3 vertexA = waterMeshPoints[layerA * pointsPerLayer + i];
                Vector3 vertexB = waterMeshPoints[layerB * pointsPerLayer + i];
                Vector3 vertex = Vector3.LerpUnclamped(vertexA, vertexB, ratio);
                meshVertices[i] = vertex;
            }

            waterMesh.SetVertices(meshVertices);
            waterMesh.SetUVs(0, meshUVs);
            waterMesh.SetIndices(indices, MeshTopology.Triangles, 0);
            waterMesh.RecalculateBounds();
        }

        public override void OnItemRotationChange(Item item, float newPercentRotated)
        {
            if (item.name.Contains("hot"))
            {
                hotWaterFill = newPercentRotated;
            }
            else
            {
                coldWaterFill = newPercentRotated;
            }

            //calculate temperature
            float ratio = (1.0f + hotWaterFill) / (1.0f + coldWaterFill);
            if (ratio > 1.5f)
            {
                temperature = Temperature.HOT;
            }
            else if (ratio > 0.8f)
            {
                temperature = Temperature.LUKEWARM;
            }
            else
            {
                temperature = Temperature.COLD;
            }

            ParticleSystem.EmissionModule emission = hotWaterEffect.emission;
            if (temperature == Temperature.HOT)
            {
                emission.enabled = true;
            }
            else
            {
                emission.enabled = false;
            }

            if (newPercentRotated > 0.0f)
            {
                if (fillCoroutine != null)
                {
                    return;
                }

                fillCoroutine = GameDirector.instance.StartCoroutine(FillCoroutine());
            }
            else if (fillCoroutine != null)
            {

                GameDirector.instance.StopCoroutine(fillCoroutine);
                fillCoroutine = null;
                fillSplashParent.gameObject.SetActive(false);
                spigotFlow.SetActive(false);
            }
        }

        private RoomObject FindRoomObject(GameObject target)
        {

            for (int i = 0; i < objectsInBathroom.Count; i++)
            {
                if (objectsInBathroom[i].roomObject == target)
                {
                    return objectsInBathroom[i];
                }
            }
            return null;
        }

        public override void OnMechanismTriggerEnter(MechanismCollisionCallback self, Collider collider)
        {
            Item colliderItem = collider.gameObject.GetComponent(typeof(Item)) as Item;
            if (colliderItem == null || colliderItem.mainOwner == null)
            {
                return;
            }
            if (colliderItem.settings.itemType != Item.Type.CHARACTER)
            {
                return;
            }
            RoomObject roomObject = FindRoomObject(colliderItem.mainOwner.gameObject);
            if (roomObject == null)
            {
                roomObject = new RoomObject(colliderItem.mainOwner.gameObject);
                objectsInBathroom.Add(roomObject);
            }
            roomObject.partsInside++;
        }
        public override void OnMechanismTriggerExit(MechanismCollisionCallback self, Collider collider)
        {
            Item colliderItem = collider.gameObject.GetComponent(typeof(Item)) as Item;
            if (colliderItem == null || colliderItem.mainOwner == null)
            {
                return;
            }
            if (colliderItem.settings.itemType != Item.Type.CHARACTER)
            {
                return;
            }
            RoomObject roomObject = FindRoomObject(colliderItem.mainOwner.gameObject);
            if (roomObject == null)
            {
                Debug.LogError("ERROR RoomObject not found but still exited! " + colliderItem.name);
                return;
            }
            if (--roomObject.partsInside == 0)
            {
                objectsInBathroom.Remove(roomObject);
            }
        }
    }

}