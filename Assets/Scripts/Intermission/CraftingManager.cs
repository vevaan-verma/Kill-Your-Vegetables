using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingManager : MonoBehaviour {

    [Header("References")]
    private ShopManager shopManager; // has to be serialized so it can be hidden at the start of the game for convenience and development purposes
    private FoodInventoryStorage storageInventory;
    private Animator animator;

    [Header("UI References")]
    [SerializeField] private GameObject lunchboxSection;
    [Space]
    [SerializeField] private GameObject weaponStatsSection;
    [SerializeField] private GameObject weaponSection;
    [SerializeField] private GameObject craftingNextSection;
    [SerializeField] private Button craftButton;
    [Space] // the next values are from the shop menu
    private GameObject shopNextSection;
    private Button playButton;
    private GameObject shopSection;

    [Header("Data")]
    private List<Food> selectedFoods; // use this to store the foods that are currently selected instead of directly modifying the inventory contents (to prevent issues with the inventory contents being saved | if the game closes in the middle of the game, the inventory data won't be lost as it wasn't actually modified [however, this may not matter since the player will restart either way, but it is done for better practice])

    [Header("Slots")]
    [SerializeField][Tooltip("These must be in order as they are cycled through in numerical order by index.")] private WeaponSlot[] weaponSlots;
    // create a dictionary to keep track of if the weapon slots are filled or not
    private bool[] filledWeaponSlots;
    private StorageSlot[] craftingSlotMappings; // used to increment the amount of the crafting slot when it is replaced in a weapon slot
    private int selectedWeaponSlotIndex;

    [Header("Stats")]
    [SerializeField] private float statSliderMaxValueMultiplier;
    private Dictionary<StatType, StatSlider> statSliders;

    [Header("Abilities")]
    private List<AbilityType> selectedAbilities;

    // this runs at scene start
    private void Start() {

        shopManager = GetComponent<ShopManager>();

        #region VALIDATION
        if (weaponSlots.Length != GameSettings.WEAPON_SLOTS_COUNT) {

            Debug.LogError("Weapon slots count does not match the game settings. Please check the weapon slots count in the game settings and the number of weapon slots in the crafting manager.");

        }
        #endregion

        shopNextSection = shopManager.GetShopNextSection(); // get the shop next section from the shop manager
        playButton = shopManager.GetPlayButton(); // get the play button from the shop manager
        shopSection = shopManager.GetShopSection(); // get the shop section from the shop manager

        shopNextSection.SetActive(false); // hide the next section by default
        shopSection.SetActive(false); // hide the shop section by default
        playButton.interactable = false; // make the play button uninteractable by default (until shop screen is shown)

        shopManager.enabled = false; // disable the shop manager by default

    }

    // don't use awake for this to avoid being called at the beginning of the game; should only be called when the crafting menu is shown
    public void Initialize() {

        animator = GetComponent<Animator>();

        storageInventory = new FoodInventoryStorage(FoodInventoryType.Storage);
        selectedFoods = new List<Food>();
        filledWeaponSlots = new bool[weaponSlots.Length];
        craftingSlotMappings = new StorageSlot[weaponSlots.Length];
        statSliders = new Dictionary<StatType, StatSlider>();
        selectedAbilities = new List<AbilityType>();

        // add the stat sliders to the dictionary
        foreach (StatSlider statSlider in FindObjectsByType<StatSlider>(FindObjectsSortMode.None))
            statSliders.Add(statSlider.GetStatType(), statSlider);

        // make sure there is a slider for each stat type
        if (statSliders.Count != Enum.GetValues(typeof(StatType)).Length)
            Debug.LogWarning("There are not sliders for each stat type.");

        // set all slider values
        foreach (StatSlider statSlider in statSliders.Values) {

            statSlider.Initialize(statSliderMaxValueMultiplier); // initialize the stat slider with the max value multiplier
            statSlider.SetStatValue(GameData.GetStat(statSlider.GetStatType()));

        }

        craftButton.onClick.AddListener(CraftWeapon); // add the craft method to the craft button's on click event

        // initialize the weapon slots
        for (int i = 0; i < weaponSlots.Length; i++)
            weaponSlots[i].Initialize(this, i);

        weaponSlots[0].Select(); // select the first slot by default

    }

    private void OnDisable() => storageInventory?.SaveContents(); // null check to prevent null reference error if the script is disabled before the storage inventory is initialized

    public void OnStorageSlotClicked(StorageSlot lunchboxSlot, FoodStack foodStack) {

        WeaponSlot currSlot = weaponSlots[selectedWeaponSlotIndex]; // get the current weapon slot
        Food food;

        // if the weapon slot already has an food, add it back to the inventory
        if (currSlot.GetFood() != null) {

            food = currSlot.GetFood();
            selectedFoods.Remove(food); // remove the food from the selected foods list
            craftingSlotMappings[selectedWeaponSlotIndex].IncrementCount(); // increment the amount of the crafting slot

            RemoveStats(food); // subtract the values of the food from the sliders

            if (food is JunkFood removedJunkFood)
                selectedAbilities.Remove(removedJunkFood.GetItemData().GetAbilityType()); // remove the ability type from the selected abilities list if the food is a junk food

        }

        food = (Food) foodStack.GetItem(); // this can be casted to Food since we know it is a Food object (look at the parameter type of this method)
        currSlot.SetStack(new FoodStack(food, 1)); // set the food stack of the current weapon slot to the selected food stack; count is set to 1 since we only want to use one food from the lunchbox slot
        ApplyStats(food); // add the values of the food to the sliders

        if (food is JunkFood addedJunkFood)
            selectedAbilities.Add(addedJunkFood.GetItemData().GetAbilityType()); // add the ability type to the selected abilities list if the food is a junk food

        craftingSlotMappings[selectedWeaponSlotIndex] = lunchboxSlot; // map the crafting slot to the weapon slot

        selectedFoods.Add(food); // add the food to the selected foods list
        filledWeaponSlots[selectedWeaponSlotIndex] = true; // mark the weapon slot as filled (used to enable the craft button; once slot is filled, it cannot be unfilled, only replaced)
        currSlot.Deselect(); // deselect the current slot

        // check if all slots are filled
        bool allSlotsFilled = true;

        foreach (bool filled in filledWeaponSlots) {

            if (!filled) {

                allSlotsFilled = false;
                break;

            }
        }

        if (allSlotsFilled)
            craftButton.interactable = true; // enable the craft button if all slots are filled

        selectedWeaponSlotIndex = (selectedWeaponSlotIndex + 1) % weaponSlots.Length; // increment the selected slot index in a circular fashion
        weaponSlots[selectedWeaponSlotIndex].Select(); // select the next slot

    }

    public void OnWeaponSlotClicked(int index) {

        weaponSlots[selectedWeaponSlotIndex].Deselect(); // deselect the current slot
        selectedWeaponSlotIndex = index; // set the selected slot index to the clicked index
        weaponSlots[selectedWeaponSlotIndex].Select(); // select the new slot

    }

    private void ApplyStats(Food food) {

        StatValue positiveStat = food.GetPositiveStat();
        StatValue negativeStat = food.GetNegativeStat();

        statSliders[positiveStat.GetStatType()].ModifyStatValue(positiveStat.GetValue()); // add the value of the positive stat
        statSliders[negativeStat.GetStatType()].ModifyStatValue(-negativeStat.GetValue()); // subtract the value of the negative stat

    }

    private void RemoveStats(Food food) {

        StatValue positiveStat = food.GetPositiveStat();
        StatValue negativeStat = food.GetNegativeStat();

        statSliders[positiveStat.GetStatType()].ModifyStatValue(-positiveStat.GetValue()); // subtract the value of the positive stat to undo the addition
        statSliders[negativeStat.GetStatType()].ModifyStatValue(negativeStat.GetValue()); // add the value of the negative stat to undo the subtraction

    }

    private void CraftWeapon() {

        // make sure all slots are filled
        foreach (WeaponSlot slot in weaponSlots) {

            if (slot.GetFood() == null) {

                // TODO: add some sort of visual feedback here
                Debug.Log("All slots must be filled to craft a weapon.");
                return;

            }
        }

        craftButton.interactable = false; // disable the craft button to prevent multiple crafts at once

        // set the stat value in the game data to the actual stat value (unconstrained value) from the slider
        foreach (StatSlider statSlider in statSliders.Values)
            GameData.SetStat(statSlider.GetStatType(), statSlider.GetActualStatValue());

        // remove each of the selected foods from the inventory
        foreach (Food food in selectedFoods)
            storageInventory.RemoveFood(food, 1);

        // set the selected abilities in the game data to the selected abilities
        foreach (AbilityType abilityType in selectedAbilities)
            GameData.SetAbilityUnlocked(abilityType, true);

        Debug.Log("Crafted a weapon!");

        gameObject.SetActive(false); // hide the crafting menu
        shopManager.gameObject.SetActive(true); // show the shop menu

        // replace the two lines above with a call to the lerp method to animate the transition between the crafting and shop menus
        StartCoroutine(HandleShopTransition());

    }

    private IEnumerator HandleShopTransition() {

        enabled = false; // disable exchange manager
        shopNextSection.SetActive(true); // show the next section when enabled
        shopSection.SetActive(true); // show the shop section when enabled
        animator.SetTrigger("startShopTransition");

        // set all upgrade buttons to not interactable (will be set to interactable again when the shop manager is initialized/when the animation is completed)
        UpgradeButton[] upgradeButtons = shopManager.GetUpgradeButtons();

        foreach (UpgradeButton upgradeButton in upgradeButtons) {

            upgradeButton.Initialize(transform); // pass this transform to the upgrade button as the root transform
            upgradeButton.SetInteractable(false);

        }

        // set all special item buttons to not interactable (will be set to interactable again when the shop manager is initialized/when the animation is completed)
        SpecialItemButton[] specialItemButtons = shopManager.GetSpecialItemButtons();

        foreach (SpecialItemButton specialItemButton in specialItemButtons) {

            specialItemButton.Initialize(transform); // pass this transform to the special item button as the root transform
            specialItemButton.SetInteractable(false);

        }

        // set all ability buttons to not interactable (will be set to interactable again when the shop manager is initialized/when the animation is completed)
        AbilityButton[] abilityButtons = shopManager.GetAbilityButtons();

        foreach (AbilityButton abilityButton in abilityButtons) {

            abilityButton.Initialize(transform); // pass this transform to the ability button as the root transform
            abilityButton.SetInteractable(false);

        }

        yield return null; // wait for the animation to start
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length - Time.deltaTime); // wait for the animation to finish (subtract the delta time between the last frame to account for the first frame skipped so animations do not snap back)

        animator.enabled = false; // disable the animator to prevent it from resetting the animation

        lunchboxSection.SetActive(false); // hide the lunchbox section
        weaponStatsSection.SetActive(false); // hide the weapon stats section
        craftButton.gameObject.SetActive(false); // hide the craft button
        playButton.interactable = true; // make the play button interactable since the shop screen is fully shown

        shopManager.enabled = true; // enable shop manager
        shopManager.Initialize(); // initialize the shop manager

        // set each weapon slot to not interactable
        foreach (WeaponSlot slot in weaponSlots)
            slot.SetInteractable(false);

    }

    // these getters work even when the script is disabled, so they can be used to access the references from other scripts instead of setting them in those scripts separately
    public GameObject GetWeaponStatsSection() => weaponStatsSection;

    public GameObject GetWeaponSection() => weaponSection;

    public GameObject GetCraftingNextSection() => craftingNextSection;

    public Button GetCraftButton() => craftButton;

}
