using UnityEngine;

public class JackORipperMonster : Monster
{
    [SerializeField] private GameObject flashyScythePrefab;

    private bool useMultiHitAttack = true;

    public override int MonsterId => 104;
    protected override string MonsterName => "잭 오 리퍼";
    protected override int BaseMaxHp => 150;

    protected override void BuildNextAction()
    {
        if (useMultiHitAttack)
        {
            SetAttackIntent(6, "피해를 1 x 6 입힙니다.");
            SetPlannedPattern(10401, MonsterIntentIconType.Attack);
            return;
        }

        SetIntent("현란한 낫을 1개 소환합니다.");
        SetPlannedPattern(10402, MonsterIntentIconType.Summon);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        if (useMultiHitAttack)
        {
            for (int hitIndex = 0; hitIndex < 6; hitIndex++)
            {
                DealDamage(target, 1);
            }
        }
        else
        {
            SummonFlashyScythe();
        }

        useMultiHitAttack = !useMultiHitAttack;
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
            Debug.LogWarning("[JackORipper] MonsterSpawner가 설정되지 않아 현란한 낫을 소환할 수 없습니다");
            return;
        }

        Monster summonedMonster = monsterSpawner.SpawnSummonedMonster(flashyScythePrefab);
        if (summonedMonster == null)
        {
            battleManager.UpdateAllUI();
            return;
        }

        battleManager.RegisterMonster(summonedMonster);
        summonedMonster.UpdateUI();
        battleManager.UpdateAllUI();
    }
}
