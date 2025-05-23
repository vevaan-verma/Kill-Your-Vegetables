using UnityEngine;

public class JunkFood : Food {

    [Header("Data")]
    private readonly AbilityType abilityType;

    public JunkFood(JunkFoodData junkFoodData, Rarity rarity, RarityDatabase rarityDatabase, CategoryDatabase categoryDatabase) : base(junkFoodData, rarity, rarityDatabase, categoryDatabase) => this.abilityType = junkFoodData.GetAbilityType();

    public new JunkFoodData GetItemData() => (JunkFoodData) itemData;

    public AbilityType GetAbilityType() => abilityType;

    public override bool Equals(object obj) => obj is JunkFood junkFood && itemData.Equals(junkFood.GetItemData()) && rarity == junkFood.GetRarity() && abilityType == junkFood.abilityType && junkFood.GetAbilityType() == abilityType; // check if the item data, rarity, and ability type are equal

    public override int GetHashCode() => itemData.GetHashCode() ^ rarity.GetHashCode() ^ abilityType.GetHashCode(); // use the item data's hash code, rarity, and ability type as the hash code; this ensures that the hash code is unique for each item, even if they have the same data but different rarities and ability types

}
