using System.Collections.Generic;
using UnityEngine;

public class Template : MonoBehaviour
{
    [System.Serializable]
    public class ItemTemplate
    {
        public string itemId;
        public string itemName;
        public Sprite itemImage;
        public int itemPrice;
    }

    [System.Serializable]
    public class ItemTemplates
    {
        public List<ItemTemplate> items;
    }
}