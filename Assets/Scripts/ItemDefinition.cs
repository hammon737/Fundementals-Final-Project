using UnityEngine;

[CreateAssetMenu(menuName = "Parallax/Item")]
public sealed class ItemDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Item";
    [SerializeField] private ItemCategory category = ItemCategory.Consumable;
    [SerializeField] private ItemEffect effect = ItemEffect.HealHP;
    [SerializeField, Min(0)] private int effectAmount = 25;
    [SerializeField] private bool isKeyItem;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public ItemCategory Category => category;
    public ItemEffect Effect => effect;
    public int EffectAmount => Mathf.Max(0, effectAmount);
    public bool IsKeyItem => isKeyItem || category == ItemCategory.KeyItem;
}
