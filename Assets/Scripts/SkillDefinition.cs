using UnityEngine;

[CreateAssetMenu(menuName = "Parallax/Skill")]
public sealed class SkillDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Skill";
    [SerializeField, Min(0)] private int mpCost = 5;
    [SerializeField] private SkillEffect effect = SkillEffect.DamageEnemy;
    [SerializeField, Min(0)] private int power = 10;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int MPCost => Mathf.Max(0, mpCost);
    public SkillEffect Effect => effect;
    public int Power => Mathf.Max(0, power);
}
