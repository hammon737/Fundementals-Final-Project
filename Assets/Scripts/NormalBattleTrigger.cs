using UnityEngine;
using UnityEngine.InputSystem;

public sealed class NormalBattleTrigger : MonoBehaviour
{
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private EnemyDefinition[] encounterEnemies;
    [SerializeField] private Sprite battleRoomBackground;
    [SerializeField] private AudioSource battleMusic;
    [SerializeField] private Key normalBattleKey = Key.N;

    private PlayerMove playerMove;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && normalBattleKey != Key.None && keyboard[normalBattleKey].wasPressedThisFrame)
            TriggerNormalBattle();
    }

    public void TriggerNormalBattle()
    {
        if (battleSystem == null)
            battleSystem = FindAnyObjectByType<BattleSystem>();

        if (battleSystem == null)
        {
            Debug.LogError("NormalBattleTrigger could not find a BattleSystem in the scene.", this);
            return;
        }

        if (playerMove == null)
            playerMove = FindAnyObjectByType<PlayerMove>();

        if (!battleSystem.StartBattle(encounterEnemies, battleRoomBackground))
            return;

        if (playerMove != null)
            playerMove.SetMovementPaused(true);
        battleSystem.BattleClosed += StopBattleMusic;
        if (battleMusic != null)
        {
            battleMusic.Play();
        }
    }

    private void StopBattleMusic()
    {
        if (battleMusic != null)
            battleMusic.Stop();

        if (playerMove != null)
            playerMove.SetMovementPaused(false);

        battleSystem.BattleClosed -= StopBattleMusic;
    }
}
