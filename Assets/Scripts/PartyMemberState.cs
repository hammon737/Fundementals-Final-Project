using System;
using UnityEngine;

[Serializable]
public sealed class PartyMemberState
{
    [SerializeField] private CharacterDefinition character;
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(0)] private int experience;
    [SerializeField, Min(0)] private int currentHP;
    [SerializeField, Min(0)] private int currentMP;

    public PartyMemberState(CharacterDefinition character)
    {
        this.character = character;
        currentHP = MaxHP;
        currentMP = MaxMP;
    }

    public CharacterDefinition Character => character;
    public int Level
    {
        get => Mathf.Max(1, level);
        set => level = Mathf.Max(1, value);
    }
    public int Experience
    {
        get => Mathf.Max(0, experience);
        set => experience = Mathf.Max(0, value);
    }
    public int MaxHP => character != null ? character.MaxHP : 0;
    public int MaxMP => character != null ? character.MaxMP : 0;
    public int AttackPower => character != null ? character.AttackPower : 0;
    public int DefensePower => character != null ? character.DefensePower : 0;
    public bool IsAlive => character != null && CurrentHP > 0;

    public int CurrentHP
    {
        get => Mathf.Clamp(currentHP, 0, MaxHP);
        set => currentHP = Mathf.Clamp(value, 0, MaxHP);
    }

    public int CurrentMP
    {
        get => Mathf.Clamp(currentMP, 0, MaxMP);
        set => currentMP = Mathf.Clamp(value, 0, MaxMP);
    }
}

[Serializable]
public sealed class InventoryStack
{
    [SerializeField] private ItemDefinition item;
    [SerializeField, Min(0)] private int quantity;

    public InventoryStack(ItemDefinition item, int quantity)
    {
        this.item = item;
        this.quantity = Mathf.Max(0, quantity);
    }

    public ItemDefinition Item => item;
    public int Quantity
    {
        get => Mathf.Max(0, quantity);
        set => quantity = Mathf.Max(0, value);
    }
}
