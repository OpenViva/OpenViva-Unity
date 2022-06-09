using System.Collections.Generic;
using UnityEngine;

namespace viva
{


    public class MarketContainer : MonoBehaviour
    {

        public class MerchantGoodRegistry
        {
            public MerchantSpot owner;
            public int goodIndex;

            public MerchantGoodRegistry(MerchantSpot _owner, int _goodIndex)
            {
                owner = _owner;
                goodIndex = _goodIndex;
            }
        }

        [SerializeField]
        private Vector3 itemSpawnPos = Vector3.up;
        [SerializeField]
        private ItemSpaceRegister m_itemSpaceRegister;
        public ItemSpaceRegister itemSpaceRegister { get { return m_itemSpaceRegister; } }
        [SerializeField]
        private BoxCollider triggerZone;
        [SerializeField]
        private MarketContainerStatus status;

        public List<MerchantGoodRegistry> merchantRegistries = new List<MerchantGoodRegistry>();
        public bool isReady { get; private set; } = false;
        public MerchantSpot owner { get; protected set; } = null;
        private int lastSpawnSpotIndex = 0;


        public void SetMerchantGoodRegistry(MerchantSpot newOwner, int index)
        {
            foreach (var registry in merchantRegistries)
            {
                if (registry.owner == newOwner)
                {
                    return;
                }
            }
            merchantRegistries.Add(new MerchantGoodRegistry(newOwner, index));
        }

        public bool ContainsMerchangGoodRegistry(MerchantSpot owner)
        {
            foreach (var registry in merchantRegistries)
            {
                if (registry.owner == owner)
                {
                    return true;
                }
            }
            return false;
        }

        public int? GetMerchangGoodIndex(MerchantSpot owner)
        {
            foreach (var registry in merchantRegistries)
            {
                if (registry.owner == owner)
                {
                    return registry.goodIndex;
                }
            }
            return null;
        }

        public void MarkAsReady()
        {
            isReady = true;
        }

        public int CalculateMaxCountForItem(float sizeRadius)
        {
            if (sizeRadius == 0.0f)
            {
                Debug.LogError("[MarketContainer] sizeRadius cannot be 0!");
                return 0;
            }
            sizeRadius *= 2.0f;
            int xCount = Mathf.FloorToInt((triggerZone.size.x) / sizeRadius);
            int zCount = Mathf.FloorToInt((triggerZone.size.z) / sizeRadius);
            return xCount * zCount;
        }

        public bool IsValidMarket()
        {
            return (transform.up.y > 0.71f && owner != null);
        }

        public Vector3 CalculateEmptySpawnSpot(float sizeRadius)
        {

            if (sizeRadius == 0.0f)
            {
                Debug.LogError("[MarketContainer] sizeRadius cannot be 0!");
                return transform.position;
            }
            sizeRadius *= 2.0f;
            int xCount = Mathf.FloorToInt((triggerZone.size.x) / sizeRadius);
            int zCount = Mathf.FloorToInt((triggerZone.size.z) / sizeRadius);
            if (xCount * zCount == 0)
            {
                Debug.LogError("[MarketContainer] sizeRadius is too big!");
                return transform.position;
            }
            lastSpawnSpotIndex = (++lastSpawnSpotIndex) % (xCount * zCount);

            int spawnZ = lastSpawnSpotIndex / xCount;
            int spawnX = lastSpawnSpotIndex - spawnZ * xCount;

            Vector3 spawnSpotSquare = new Vector3(
                (-0.5f + (0.5f + spawnX) / xCount) * triggerZone.size.x,
                0.005f,
                (-0.5f + (0.5f + spawnZ) / zCount) * triggerZone.size.z
            );

            return transform.TransformPoint(triggerZone.center + spawnSpotSquare);
        }

        public void SetParentLocator(MerchantSpot newOwner)
        {
            if (newOwner == owner)
            {
                return;
            }
            if (owner != null)
            {
                owner.RemoveNearbyMarketContainer(this);
                status.StopValidateReadyContainer();
            }
            owner = newOwner;
            if (owner)
            {
                owner.AddNearbyMarketContainer(this);
                status.BeginValidateReadyContainer();
                isReady = false;
            }
        }

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint(itemSpawnPos), 0.025f);
        }

    }

}