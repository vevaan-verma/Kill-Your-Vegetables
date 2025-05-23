using UnityEngine;

[CreateAssetMenu(menuName = "Shop/FoodData")]
public class FoodData : ItemData {

    [Header("Data")]
    [SerializeField] private StatValue positiveBaseStat;
    [SerializeField] private StatValue negativeBaseStat;

    [Header("Settings")]
    [SerializeField] private bool grantByDefault;

    private void OnEnable() {

        // check if the positive and negative stat types are not the same
        if (positiveBaseStat.GetStatType() == negativeBaseStat.GetStatType())
            Debug.LogWarning($"The positive and negative stat types are the same for {name}. This will result in a stat being added and subtracted from the same stat type.");

    }

    public StatValue GetPositiveStat(CategoryDatabase categoryDatabase) => new StatValue(positiveBaseStat.GetStatType(), (int) (positiveBaseStat.GetValue() + categoryDatabase.GetCategoryData(category).GetRoundPositiveStatIncrement() * (GameData.GetRoundNumber() - 1)));

    public StatValue GetNegativeStat(CategoryDatabase categoryDatabase) => new StatValue(negativeBaseStat.GetStatType(), (int) (negativeBaseStat.GetValue() + categoryDatabase.GetCategoryData(category).GetRoundNegativeStatIncrement() * (GameData.GetRoundNumber() - 1)));

    public bool IsGrantedByDefault() => grantByDefault;

}
