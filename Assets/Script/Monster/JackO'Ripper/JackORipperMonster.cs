using UnityEngine;

public class JackORipperMonster : Monster
{
    [SerializeField] private GameObject flashyScythePrefab;

    private bool useMultiHitAttack = true;
    private bool willUseSummonPattern;

    public override int MonsterId => 104;
    protected override string MonsterName => "잭 오 리퍼";
    protected override int BaseMaxHp => 150;

    protected override void BuildNextAction()
    {
        willUseSummonPattern = !useMultiHitAttack && TryReserveSummonPattern();

        if (willUseSummonPattern)
        {
            SetIntent("현란한 낫을 1개 소환합니다.");
            SetPlannedPattern(10402, MonsterIntentIconType.Summon);
            return;
        }

        int hitDamage = PreviewOutgoingDamage(1);
        SetAttackIntent(hitDamage, $"피해를 {hitDamage} x 6 입힙니다.");
        SetPlannedPattern(10401, MonsterIntentIconType.Attack);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        if (willUseSummonPattern)
        {
            SummonFlashyScythe();
            useMultiHitAttack = true;
            return;
        }

        for (int hitIndex = 0; hitIndex < 6; hitIndex++)
        {
            DealDamage(target, 1);
        }

        if (useMultiHitAttack)
        {
            useMultiHitAttack = false;
        }
    }

    private bool TryReserveSummonPattern()
    {
        if (flashyScythePrefab == null)
        {
            return false;
        }

        MonsterSpawner monsterSpawner = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.monsterSpawner
            : null;
        return monsterSpawner != null && monsterSpawner.TryReserveSummonSlot();
    }

    private void SummonFlashyScythe() // 추후 수정 필요
    {
        if (flashyScythePrefab == null)
        {
            return;
        }

        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        MonsterSpawner monsterSpawner = battleManager != null ? battleManager.monsterSpawner : null;
        if (monsterSpawner == null)
        {
            return;
        }

        Monster summonedMonster = monsterSpawner.SpawnSummonedMonster(flashyScythePrefab);
        if (summonedMonster == null)
        {
            return;
        }

        battleManager.RegisterMonster(summonedMonster);
        summonedMonster.UpdateUI();
        battleManager.UpdateAllUI();
    }
}
