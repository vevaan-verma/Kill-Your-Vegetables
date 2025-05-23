using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour {

    [Header("Data")]
    private List<ItemData> itemData;
    private List<FoodData> grantFoods;

    private void Awake() {

        itemData = new List<ItemData>(Resources.LoadAll<ItemData>(FileConstants.ITEM_DATA_RES_PATH));
        grantFoods = new List<FoodData>();

        #region VALIDATION
        // make sure item array contains one of each of the values in the special item enum
        SpecialItemType[] specialItemTypes = (SpecialItemType[]) System.Enum.GetValues(typeof(SpecialItemType)); // get all the special item types in the enum

        for (int i = 0; i < specialItemTypes.Length; i++) // iterate through the special item types
            if (itemData.Find(c => c is SpecialItemData specialItemData && specialItemData.GetSpecialItemType() == specialItemTypes[i]) == null)
                Debug.LogError($"Special item {specialItemTypes[i]} is missing from the item data.");
        #endregion

        Dictionary<ItemCategory, List<ItemData>> categoryItems = new Dictionary<ItemCategory, List<ItemData>>();

        // loop through all the item data and add them to the category items dictionary in the correct category unless the item is granted by default
        foreach (ItemData data in itemData) {

            // do not add items that are granted by default to the database
            if (data is FoodData foodData && foodData.IsGrantedByDefault()) {

                grantFoods.Add(foodData); // add the food to the list of foods that are granted by default
                continue;

            }

            if (categoryItems.ContainsKey(data.GetCategory()))
                categoryItems[data.GetCategory()].Add(data); // add the item to the category's list since it already exists
            else
                categoryItems[data.GetCategory()] = new List<ItemData>() { data }; // create a new list for the category and add the item to it

        }

        FindFirstObjectByType<CategoryDatabase>().Initialize(categoryItems); // initialize the category database with the category items

    }

    public SpecialItemData GetSpecialItemData(SpecialItemType specialItemType) {

        foreach (ItemData data in itemData)
            if (data is SpecialItemData specialItemData && specialItemData.GetSpecialItemType() == specialItemType)
                return specialItemData;

        return null;

    }

    public List<FoodData> GetGrantFoods() => grantFoods;

}

public enum SpecialItemType {

    HealthSmoothie,
    RageSmoothie,
    EnergySmoothie,
    //Cola

}
