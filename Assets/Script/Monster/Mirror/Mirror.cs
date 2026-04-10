using UnityEngine;

public class Mirror : Monster
{
    private int patternIndex;

    public override int MonsterId => 114;
    protected override string MonsterName => "거울 괴물";
    protected override int BaseMaxHp => 70;

    protected override void OnBattleStart()
    {
        patternIndex = 0;
    }

    protected override void BuildNextAction()
    {
        if (patternIndex < 2)
        {
            SetMirrorAttackIntent();
            return;
        }

        SetIntent("빈약, 약화를 2씩 부여합니다.");
        SetPlannedPattern(11402, MonsterIntentIconType.HarmfulEffect);
    }

    protected override void ExecuteAction(PlayerData target)
    {
        if (patternIndex < 2)
        {
            DealDamage(target, 0);
        }
        else
        {
            target?.AddBuff(BattleRuntimeDefinitions.FrailBuffId, 2);
            target?.AddBuff(BattleRuntimeDefinitions.WeakBuffId, 2);
        }

        patternIndex = (patternIndex + 1) % 3;
    }

    protected override void OnAfterTakeDamage(int incomingDamage, int damageAfterDefense)
    {
        if (damageAfterDefense <= 0)
        {
            return;
        }

        if (PlannedPatternId == 11401)
        {
            SetMirrorAttackIntent();
            UpdateUI();
        }
    }

    private void SetMirrorAttackIntent()
    {
        int mirrorStack = GetMirrorStack();
        SetAttackIntent(mirrorStack, $"피해를 {mirrorStack} 입힙니다. 거울을 0으로 초기화합니다.");
        SetPlannedPattern(11401, MonsterIntentIconType.Attack);
    }

    private int GetMirrorStack()
    {
        return PreviewOutgoingDamage(0);
    }
}
