using System.Collections.Generic;
using UnityEngine;

namespace viva
{


    public class ItemSpaceRegister : MonoBehaviour
    {

        protected class ItemEntry
        {
            public readonly Item item = null;
            public int entries = 1;

            public ItemEntry(Item _item)
            {
                item = _item;
            }
        }

        private List<ItemEntry> itemEntries = new List<ItemEntry>();


        public int GetItemTypeCount(Item.Type itemType)
        {
            int count = 0;
            foreach (var itemEntry in itemEntries)
            {
                if (itemEntry.item && itemEntry.item.settings.itemType == itemType)
                {
                    count++;
                }
            }
            return count;
        }

        private void OnTriggerEnter(Collider collider)
        {

            Item item = collider.GetComponent<Item>();
            if (item)
            {
                for (int i = itemEntries.Count; i-- > 0;)
                {
                    ItemEntry itemEntry = itemEntries[i];
                    if (itemEntry.item == null)
                    {
                        itemEntries.RemoveAt(i);
                        continue;
                    }
                    if (itemEntry.item == item)
                    {
                        itemEntry.entries++;
                        return;
                    }
                }
                itemEntries.Add(new ItemEntry(item));
            }
        }

        private void OnTriggerExit(Collider collider)
        {

            Item item = collider.GetComponent<Item>();
            if (item)
            {
                for (int i = itemEntries.Count; i-- > 0;)
                {
                    ItemEntry itemEntry = itemEntries[i];
                    if (itemEntry.item == item)
                    {
                        if (--itemEntry.entries == 0)
                        {
                            itemEntries.RemoveAt(i);
                        }
                        return;
                    }
                }
            }
        }
    }

}