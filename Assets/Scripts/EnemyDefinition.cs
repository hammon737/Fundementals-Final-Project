using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Parallax/Enemy")]
public sealed class EnemyDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Enemy";
    [SerializeField, Min(1)] private int maxHP = 50;
    [SerializeField, Min(0)] private int attack = 8;
    [SerializeField, Min(0)] private int defense = 2;
    [SerializeField, Min(0)] private int experienceReward = 10;
    [SerializeField, Min(0)] private int goldReward = 5;
    [SerializeField] private Sprite artwork;
    [SerializeField] private ItemDefinition[] possibleDrops = Array.Empty<ItemDefinition>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int MaxHP => Mathf.Max(1, maxHP);
    public int Attack => Mathf.Max(0, attack);
    public int Defense => Mathf.Max(0, defense);
    public int ExperienceReward => Mathf.Max(0, experienceReward);
    public int GoldReward => Mathf.Max(0, goldReward);
    public Sprite Artwork => artwork;
    public ItemDefinition[] PossibleDrops => possibleDrops ?? Array.Empty<ItemDefinition>();
}
