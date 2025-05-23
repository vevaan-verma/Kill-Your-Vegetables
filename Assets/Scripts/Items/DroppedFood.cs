using UnityEngine;

public class DroppedFood : MonoBehaviour {

    [Header("References")]
    [SerializeField] private GameObject shadowPrefab;
    private SpriteRenderer spriteRenderer;
    private RarityDatabase rarityDatabase;
    private CategoryDatabase categoryDatabase;
    private Foodbar foodbar;
    private FoodData foodData;

    [Header("Settings")]
    [SerializeField] private bool useShadow;
    [SerializeField] private Vector2 shadowOffset;

    public void Initialize(FoodData foodData) {

        spriteRenderer = GetComponent<SpriteRenderer>();
        rarityDatabase = FindFirstObjectByType<RarityDatabase>();
        categoryDatabase = FindFirstObjectByType<CategoryDatabase>();
        foodbar = FindFirstObjectByType<Foodbar>();

        this.foodData = foodData;
        spriteRenderer.sprite = foodData.GetIcon();

        if (useShadow)
            GenerateShadow();

    }

    private void GenerateShadow() {

        SpriteRenderer sr = Instantiate(shadowPrefab, transform.position, Quaternion.identity, transform).GetComponent<SpriteRenderer>();
        sr.sprite = foodData.GetIcon();
        //sr.sortingOrder = spriteRenderer.sortingOrder - 1; // shadow should be behind the food
        sr.transform.localPosition = shadowOffset;

    }

    private void OnTriggerEnter2D(Collider2D collision) {

        if (collision.CompareTag("Player"))
            if (foodbar.AddFood(new Food(foodData, Rarity.Processed, rarityDatabase, categoryDatabase))) // add the food to the foodbar (rarity is processed by default since it doesn't matter in this case because the food will be grinded into scraps anyways)
                Destroy(gameObject);

    }
}
