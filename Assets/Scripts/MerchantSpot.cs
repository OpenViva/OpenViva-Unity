using System.Collections.Generic;
using UnityEngine;

namespace viva
{


    public class MerchantSpot : Service
    {

        [System.Serializable]
        public class MerchantGood
        {
            public GameObject itemPrefab;
            public Item.Type itemType;
            [Range(0.005f, 0.1f)]
            public float approximateSizeRadius = 0.01f;
            [Range(1, 16)]
            public int maxItems = 5;
            public Vector3 spawnEuler = Vector3.zero;
            [Range(0, 0.4f)]
            public float spawnHeight = 0.0f;
            public bool applyJoint = false;
        }

        [SerializeField]
        private MerchantGood[] merchantGoods;
        [HideInInspector]
        public List<MarketContainer> marketContainers { get; protected set; } = new List<MarketContainer>();
        private int merchantGoodIndexCounter = 0;


        protected override void OnInitializeEmployment(Loli targetLoli)
        {
            targetLoli.active.merchant.merchantSession.merchantSpotAsset = this;
        }

        public void AddNearbyMarketContainer(MarketContainer mc)
        {
            marketContainers.Add(mc);
        }

        public void RemoveNearbyMarketContainer(MarketContainer mc)
        {
            marketContainers.Remove(mc);
        }

        public override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            float itemRadiusSum = 0.0f;
            foreach (var good in merchantGoods)
            {
                if (good.itemPrefab)
                {
                    MeshFilter mf = good.itemPrefab.transform.GetComponentInChildren<MeshFilter>();
                    if (mf)
                    {
                        Gizmos.color = Color.white;
                        Vector3 holoPos = transform.TransformPoint(itemRadiusSum, 1.0f, 0.0f);
                        Gizmos.DrawWireMesh(mf.sharedMesh, 0, holoPos + Vector3.up * good.spawnHeight, Quaternion.Euler(good.spawnEuler), Vector3.one);
                        itemRadiusSum += good.approximateSizeRadius + 0.2f;
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(holoPos, good.approximateSizeRadius);
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube(transform.TransformPoint(itemRadiusSum, 1.0f, 0.0f), Vector3.one * 0.05f);
                        itemRadiusSum += 0.25f;
                    }
                }
            }
        }

        public int GetNextMerchantGoodIndex(bool increment)
        {
            int old = merchantGoodIndexCounter;
            old++;
            old %= merchantGoods.Length;
            if (increment)
            {
                merchantGoodIndexCounter = old;
            }
            return old;
        }

        public MarketContainer FindEmptyMarketContainer()
        {

            foreach (MarketContainer container in marketContainers)
            {
                int? count = CalculateAllowedSpawnCount(container);
                if (!count.HasValue)
                {
                    continue;
                }
                if (count.Value > 0)
                {
                    return container;
                }
            }
            return null;
        }

        public int? CalculateAllowedSpawnCount(MarketContainer container)
        {

            int? index = container.GetMerchangGoodIndex(this);
            if (!index.HasValue)
            {
                return null;
            }
            var merchantGood = merchantGoods[index.Value];
            int currentCount = container.itemSpaceRegister.GetItemTypeCount(merchantGood.itemType);
            int available = container.CalculateMaxCountForItem(merchantGood.approximateSizeRadius);
            int toSpawn = Mathf.Max(0, Mathf.Min(available, merchantGood.maxItems) - currentCount);
            return Mathf.Min(toSpawn, 1);
        }

        public override void OnMechanismUpdate()
        {
            foreach (var c in marketContainers)
            {
                int? i = CalculateAllowedSpawnCount(c);
                if (i.HasValue)
                {
                    RefillContainer(c, i.Value);
                }
            }
        }

        public override void EndUse(Character targetCharacter)
        {

        }

        public void RefillContainer(MarketContainer container, int toSpawn)
        {

            if (!container.isReady)
            {
                return;
            }
            int? index = container.GetMerchangGoodIndex(this);
            if (!index.HasValue)
            {
                return;
            }
            var good = merchantGoods[index.Value];
            for (int i = 0; i < toSpawn; i++)
            {
                Vector3 spawnPos = container.CalculateEmptySpawnSpot(good.approximateSizeRadius);
                spawnPos += container.transform.up * good.spawnHeight;
                GameObject spawnObj = GameObject.Instantiate(good.itemPrefab, spawnPos, container.transform.rotation * Quaternion.Euler(good.spawnEuler));
                Item newItem = spawnObj.GetComponent<Item>();
                if (newItem)
                {
                    newItem.rigidBody.WakeUp();
                    newItem.rigidBody.velocity = container.transform.up * -1.0f;

                    if (good.applyJoint)
                    {
                        var fj = newItem.rigidBody.gameObject.AddComponent<FixedJoint>();
                        fj.connectedBody = container.GetComponent<Rigidbody>();
                        fj.breakForce = 220.0f;
                    }
                }
                else
                {
                    Debug.LogError("[MerchantSpot] itemPrefab has no Item component in the root!");
                }
            }
        }

    }

}