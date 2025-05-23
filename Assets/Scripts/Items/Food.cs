using UnityEngine;

public class Food : Item {

    [Header("Data")]
    private readonly StatValue positiveStat;
    private readonly StatValue negativeStat;

    public Food(FoodData foodData, Rarity rarity, RarityDatabase rarityDatabase, CategoryDatabase categoryDatabase) : base(foodData, rarity) {

        StatValue positiveStat = foodData.GetPositiveStat(categoryDatabase);
        StatValue negativeStat = foodData.GetNegativeStat(categoryDatabase);

        this.positiveStat = new StatValue(positiveStat.GetStatType(), rarityDatabase.GetPositiveStatValue(positiveStat.GetValue(), rarity));
        this.negativeStat = new StatValue(negativeStat.GetStatType(), rarityDatabase.GetNegativeStatValue(negativeStat.GetValue(), rarity));

    }

    public new FoodData GetItemData() => (FoodData) itemData;

    public StatValue GetPositiveStat() => positiveStat;

    public StatValue GetNegativeStat() => negativeStat;

    public override bool Equals(object obj) => obj is Food food && itemData.Equals(food.GetItemData()) && rarity == food.GetRarity(); // check if the item data and rarity are equal

    public override int GetHashCode() => itemData.GetHashCode() ^ rarity.GetHashCode(); // use the item data's hash code and rarity as the hash code; this ensures that the hash code is unique for each item, even if they have the same data but different rarities

}
