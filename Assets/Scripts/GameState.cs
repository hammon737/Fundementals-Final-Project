using System;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    [Header("Battle party")]
    [SerializeField] private CharacterDefinition[] partyMembers = Array.Empty<CharacterDefinition>();
    [Header("Battle rewards")]
    [SerializeField] private int startingGold = 100;
    [SerializeField] private ItemDefinition[] startingItems = Array.Empty<ItemDefinition>();
    [SerializeField, Min(0)] private int startingItemQuantity = 3;

    [SerializeField] private List<PartyMemberState> party = new List<PartyMemberState>();
    [SerializeField] private List<InventoryStack> inventory = new List<InventoryStack>();
    [SerializeField] private int gold;

    public IReadOnlyList<PartyMemberState> Party => party;
    public IReadOnlyList<InventoryStack> Inventory => inventory;
    public int Gold => gold;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (party.Count == 0)
            StartNewGame();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartNewGame()
    {
        party.Clear();
        inventory.Clear();
        gold = Mathf.Max(0, startingGold);

        foreach (CharacterDefinition character in partyMembers ?? Array.Empty<CharacterDefinition>())
        {
            if (character != null && party.Count < 4)
                party.Add(new PartyMemberState(character));
        }
        if (party.Count == 0)
            Debug.LogWarning("GameState has no starting characters. Assign CharacterDefinition assets to its battle party.", this);

        foreach (ItemDefinition item in startingItems ?? Array.Empty<ItemDefinition>())
        {
            if (item != null && startingItemQuantity > 0)
                AddItem(item, startingItemQuantity);
        }
    }

    public PartyMemberState GetMember(int index)
    {
        return index >= 0 && index < party.Count ? party[index] : null;
    }

    public int GetItemCount(ItemDefinition item)
    {
        InventoryStack stack = FindStack(item);
        return stack == null ? 0 : stack.Quantity;
    }

    public void AddItem(ItemDefinition item, int quantity = 1)
    {
        if (item == null || quantity <= 0)
            return;

        InventoryStack stack = FindStack(item);
        if (stack == null)
            inventory.Add(new InventoryStack(item, quantity));
        else
            stack.Quantity += quantity;
    }

    public bool TryUseItem(ItemDefinition item, int partyMemberIndex)
    {
        if (item == null || item.IsKeyItem || item.Category != ItemCategory.Consumable)
            return false;

        PartyMemberState member = GetMember(partyMemberIndex);
        InventoryStack stack = FindStack(item);
        if (member == null || !member.IsAlive || stack == null || stack.Quantity <= 0)
            return false;

        int restored;
        switch (item.Effect)
        {
            case ItemEffect.HealHP:
                restored = Mathf.Min(item.EffectAmount, member.MaxHP - member.CurrentHP);
                if (restored <= 0) return false;
                member.CurrentHP += restored;
                break;
            case ItemEffect.RestoreMP:
                restored = Mathf.Min(item.EffectAmount, member.MaxMP - member.CurrentMP);
                if (restored <= 0) return false;
                member.CurrentMP += restored;
                break;
            default:
                return false;
        }

        RemoveItem(item, 1);
        return true;
    }

    private bool RemoveItem(ItemDefinition item, int quantity = 1)
    {
        InventoryStack stack = FindStack(item);
        if (stack == null || quantity <= 0 || stack.Quantity < quantity)
            return false;

        stack.Quantity -= quantity;
        if (stack.Quantity == 0)
            inventory.Remove(stack);
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;
        gold = Mathf.Min(int.MaxValue - amount, gold) + amount;
    }

    public void AwardExperience(int amount)
    {
        if (amount <= 0)
            return;

        foreach (PartyMemberState member in party)
        {
            if (member == null || member.Character == null)
                continue;
            member.Experience += amount;
            while (member.Experience >= ExperienceToNextLevel(member.Level))
            {
                member.Experience -= ExperienceToNextLevel(member.Level);
                member.Level++;
                member.CurrentHP = member.MaxHP;
                member.CurrentMP = member.MaxMP;
            }
        }
    }

    private InventoryStack FindStack(ItemDefinition item)
    {
        return inventory.Find(stack => stack != null && stack.Item == item);
    }

    private static int ExperienceToNextLevel(int level)
    {
        return Mathf.Max(1, level * 100);
    }
}
