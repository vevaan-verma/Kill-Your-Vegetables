using UnityEngine;

public abstract class ItemData : ScriptableObject {

    [Header("Data")]
    [SerializeField] private new string name;
    [SerializeField] private string description;
    [SerializeField] protected ItemCategory category;
    [SerializeField] private Sprite icon;

    public string GetName() => name;

    public string GetDescription() => description;

    public ItemCategory GetCategory() => category;

    public Sprite GetIcon() => icon;

    public override bool Equals(object other) => other is ItemData itemData && name == itemData.name && description == itemData.description && category == itemData.category && icon == itemData.icon;

    public override int GetHashCode() => name.GetHashCode() ^ description.GetHashCode() ^ category.GetHashCode() ^ icon.GetHashCode(); // use the name, description, category and icon as the hash code

}
