using UnityEngine;

[CreateAssetMenu(menuName = "Characters/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterID;
    public string displayName;

    [Header("Unlock")]
    public bool isFree;
    public int unlockCost;

    [Header("Perks")]
    [Range(0f, 0.5f)]
    public float shotPowerBonus;      // 0.15 = +15%

    [Range(0f, 0.5f)]
    public float accuracyAssist;      // 0.15 = 15% reduction

    public int bufferStrokes;         // recommended max = 2

    [Range(0.8f, 1f)]
    public float timerMultiplier;     // 0.9 = 90% timer speed
}
