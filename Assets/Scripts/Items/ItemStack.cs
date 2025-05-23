using UnityEngine;

public class ItemStack {

    [Header("Data")]
    [SerializeField] protected Item item;
    [SerializeField] protected int count;

    public ItemStack(Item item, int count) {

        this.item = item;
        this.count = count;

    }

    public virtual Item GetItem() => item; // this method must be implemented in derived classes to return the specific item type

    public int GetCount() => count;

    public void AddAmount(int amount) => this.count += amount;

    public void RemoveAmount(int amount) => this.count -= amount;

}
