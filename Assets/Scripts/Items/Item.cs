using UnityEngine;

public class Item {

    // this class cannot be serialized, it is meant to be used as a base class for other items

    [Header("Data")]
    [SerializeField] protected ItemData itemData;
    [SerializeField] protected Rarity rarity;

    public Item(ItemData itemData, Rarity rarity) {

        this.itemData = itemData;
        this.rarity = rarity;

    }

    public virtual ItemData GetItemData() => itemData; // this method must be implemented in derived classes to return the specific item data

    public Rarity GetRarity() => rarity; // this method must be implemented in derived classes to return the specific item rarity

    public override bool Equals(object obj) => obj is Item item && itemData.Equals(item.itemData) && rarity == item.rarity; // check if the item data and rarity are equal

    public override int GetHashCode() => itemData.GetHashCode() ^ rarity.GetHashCode(); // use the item data's hash code and rarity as the hash code; this ensures that the hash code is unique for each item, even if they have the same data but different rarities

}
