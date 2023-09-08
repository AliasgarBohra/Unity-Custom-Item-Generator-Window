using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ItemEditorWindow : EditorWindow
{
    private const string JSON_FILE_PATH = "Assets/Item Generator/items.json";

    private List<Template.ItemTemplate> itemList;
    private int selectedItemIndex = -1;

    private GameObject itemPrefab;
    private GameObject itemHolder;

    private const string ITEM_PREFAB_PATH_KEY = "ItemPrefabKey";
    private const string ITEM_HOLDER_TAG_KEY = "ItemHolderKey";

    private void OnEnable()
    {
        if (File.Exists(JSON_FILE_PATH))
        {
            string json = File.ReadAllText(JSON_FILE_PATH);
            itemList = JsonUtility.FromJson<Template.ItemTemplates>(json).items;
        }
        else
        {
            //new item list
            itemList = new List<Template.ItemTemplate>();
        }
        LoadReferences();
    }
    private void OnDisable()
    {
        StoreReferences();
    }

    [MenuItem("Window/Item Editor")]
    public static void ShowWindow()
    {
        GetWindow<ItemEditorWindow>("Item Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Item Prefab", EditorStyles.boldLabel);
        itemPrefab = EditorGUILayout.ObjectField("Prefab", itemPrefab, typeof(GameObject), false) as GameObject;

        GUILayout.Space(5);

        GUILayout.Label("Item Holder", EditorStyles.boldLabel);
        itemHolder = EditorGUILayout.ObjectField("Item Holder", itemHolder, typeof(GameObject), true) as GameObject;

        if (itemPrefab != null && itemHolder != null)
        {
            if (itemList.Count > 0)
            {
                GUILayout.Space(20);

                GUILayout.Label("Item Selection", EditorStyles.boldLabel);
                selectedItemIndex = EditorGUILayout.Popup("Select Item", selectedItemIndex, GetItemIds());
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Create New Item"))
            {
                CreateNewItem();
            }

            if (selectedItemIndex >= 0)
            {
                GUILayout.Space(20);

                GUILayout.Label("Edit Item", EditorStyles.boldLabel);

                GUILayout.Space(10);

                Template.ItemTemplate itemTemplate = itemList[selectedItemIndex];

                itemTemplate.itemName = EditorGUILayout.TextField("Item Name", itemTemplate.itemName);
                itemTemplate.itemImage = EditorGUILayout.ObjectField("Item Image", itemTemplate.itemImage, typeof(Sprite), true) as Sprite;
                itemTemplate.itemPrice = EditorGUILayout.IntField("Item Price", itemTemplate.itemPrice);

                GUILayout.Space(10);

                if (GUILayout.Button("Save Item"))
                {
                    SaveItem();
                }

                GUILayout.Space(10);

                if (GUILayout.Button("Delete Item"))
                {
                    DeleteSelectedItem();
                }
            }
        }
    }

    #region Game Object Reference Handler
    private void LoadReferences()
    {
        // loading previous references
        itemPrefab = GetStoredGameObject(ITEM_PREFAB_PATH_KEY);

        if (!string.IsNullOrEmpty(EditorPrefs.GetString(ITEM_HOLDER_TAG_KEY)))
        {
            itemHolder = GameObject.FindGameObjectWithTag(EditorPrefs.GetString(ITEM_HOLDER_TAG_KEY));
        }
    }
    private void StoreReferences()
    {
        // storing reference paths and name
        StoreGameObject(ITEM_PREFAB_PATH_KEY, itemPrefab);

        if (itemHolder != null)
        {
            EditorPrefs.SetString(ITEM_HOLDER_TAG_KEY, itemHolder.tag);
        }
    }
    private GameObject GetStoredGameObject(string key)
    {
        string path = EditorPrefs.GetString(key);
        return !string.IsNullOrEmpty(path) ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
    }
    private void StoreGameObject(string key, GameObject obj)
    {
        if (obj != null)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            EditorPrefs.SetString(key, path);
        }
        else
        {
            EditorPrefs.DeleteKey(key);
        }
    }
    #endregion

    #region Item Naming Handler
    private string GenerateUniqueItemName(string baseName)
    {
        int counter = 1;
        string uniqueName = baseName;

        while (TemplateNameExists(uniqueName))
        {
            uniqueName = $"{baseName} {counter}";
            counter++;
        }

        return uniqueName;
    }

    private bool TemplateNameExists(string nameToCheck)
    {
        foreach (Template.ItemTemplate item in itemList)
        {
            if (item.itemId.Equals(nameToCheck))
            {
                return true;
            }
        }
        return false;
    }
    private string[] GetItemIds()
    {
        string[] itemIds = new string[itemList.Count];

        for (int i = 0; i < itemList.Count; i++)
        {
            itemIds[i] = itemList[i].itemId;
        }

        return itemIds;
    }
    #endregion

    #region Item Create, Load, Edit, Save, Delete
    private void CreateNewItem()
    {
        string baseName = "Item";
        string uniqueName = GenerateUniqueItemName(baseName);

        Template.ItemTemplate newItem = new Template.ItemTemplate
        {
            itemId = uniqueName,
            itemName = "New Item",
            itemImage = null,
            itemPrice = 0,
        };

        itemList.Add(newItem);
        selectedItemIndex = itemList.Count - 1;

        CreateItemObjectFromItemTemplate();
        SaveItem();
    }
    private void DeleteSelectedItem()
    {
        if (selectedItemIndex >= 0 && selectedItemIndex < itemList.Count)
        {
            GameObject selectedObject = GameObject.Find(itemList[selectedItemIndex].itemId);
            if (selectedObject != null)
            {
                DestroyImmediate(selectedObject);
            }

            itemList.RemoveAt(selectedItemIndex);

            SaveItem();

            selectedItemIndex = -1;
        }
    }
    private void SaveItem()
    {
        // Serialize the list of templates to JSON
        Template.ItemTemplates itemsData = new Template.ItemTemplates
        {
            items = itemList
        };

        string json = JsonUtility.ToJson(itemsData, true);

        // Write the JSON data to the file
        File.WriteAllText(JSON_FILE_PATH, json);

        // Check if a template is selected
        if (selectedItemIndex >= 0 && selectedItemIndex < itemList.Count)
        {
            Template.ItemTemplate selectedItem = itemList[selectedItemIndex];
            GameObject existingItem = GameObject.Find(selectedItem.itemId);

            if (existingItem != null)
            {
                EditItem(existingItem, selectedItem);
            }
            else
            {
                CreateItemObjectFromItemTemplate();
            }
        }
        // Refresh the AssetDatabase to make the file visible in Unity's Project window
        AssetDatabase.Refresh();
    }
    private void CreateItemObjectFromItemTemplate()
    {
        if (selectedItemIndex >= 0 && selectedItemIndex < itemList.Count)
        {
            Template.ItemTemplate selectedItem = itemList[selectedItemIndex];
            GameObject itemObject = Instantiate(itemPrefab, itemHolder.transform);

            EditItem(itemObject, selectedItem);
        }
    }
    private void EditItem(GameObject itemObject, Template.ItemTemplate itemTemplate)
    {
        itemObject.name = itemTemplate.itemId;
        itemObject.transform.GetChild(0).GetComponent<Text>().text = itemTemplate.itemName;
        itemObject.transform.GetChild(1).GetComponent<Image>().sprite = itemTemplate.itemImage;
        itemObject.transform.GetChild(2).GetComponent<Text>().text = "$" + itemTemplate.itemPrice;
    }
    #endregion
}