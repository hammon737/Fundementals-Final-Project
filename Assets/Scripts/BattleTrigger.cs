using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BattleTrigger : MonoBehaviour
{
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private EnemyDefinition[] encounterEnemies;
    [SerializeField] private Sprite battleRoomBackground;
    [SerializeField] private AudioSource oneWingedAngel;
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
            TriggerBattle();
    }

    public void TriggerBattle()
    {
        if (battleSystem == null)
            battleSystem = FindAnyObjectByType<BattleSystem>();

        if (battleSystem == null)
        {
            Debug.LogError("BattleTrigger could not find a BattleSystem in the scene.", this);
            return;
        }

        if (!battleSystem.StartBattle(encounterEnemies, battleRoomBackground))
            return;

        battleSystem.BattleClosed += StopBattleMusic;
        if (oneWingedAngel != null && encounterEnemies[0] != null && encounterEnemies[0].DisplayName == "Sephiroth")
        {
            oneWingedAngel.Play();
        }
    }

    private void StopBattleMusic()
    {
        if (oneWingedAngel != null)
            oneWingedAngel.Stop();

        battleSystem.BattleClosed -= StopBattleMusic;
    }
}
