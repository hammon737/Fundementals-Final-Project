using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Parallax/Character")]
public sealed class CharacterDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Character";
    [SerializeField, Min(1)] private int maxHP = 100;
    [SerializeField, Min(0)] private int maxMP = 30;
    [SerializeField, Min(0)] private int attackPower = 10;
    [SerializeField, Min(0)] private int defensePower = 5;
    [SerializeField] private Sprite portrait;
    [SerializeField] private Sprite battleSprite;
    [SerializeField] private SkillDefinition[] skills = Array.Empty<SkillDefinition>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int MaxHP => Mathf.Max(1, maxHP);
    public int MaxMP => Mathf.Max(0, maxMP);
    public int AttackPower => Mathf.Max(0, attackPower);
    public int DefensePower => Mathf.Max(0, defensePower);
    public Sprite Portrait => portrait;
    public Sprite BattleSprite => battleSprite;
    public SkillDefinition[] Skills => skills ?? Array.Empty<SkillDefinition>();
}
