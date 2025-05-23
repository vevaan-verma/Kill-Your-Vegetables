public class FoodStack : ItemStack {

    public FoodStack(Food food, int count) : base(food, count) { } // constructor that takes a Food object and an amount

    public override Item GetItem() => item;

}
