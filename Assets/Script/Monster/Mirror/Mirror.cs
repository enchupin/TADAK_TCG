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
        EnsureMirrorBuff();
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
            DealDamage(target, GetMirrorStack());
            SetMirrorStack(0);
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

        SetMirrorStack(GetMirrorStack() + damageAfterDefense);
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
        Buff mirrorBuff = EnsureMirrorBuff();
        return mirrorBuff != null ? Mathf.Max(0, mirrorBuff.stack) : 0;
    }

    private void SetMirrorStack(int value)
    {
        Buff mirrorBuff = EnsureMirrorBuff();
        if (mirrorBuff == null)
        {
            return;
        }

        mirrorBuff.stack = Mathf.Max(0, value);
    }

    private Buff EnsureMirrorBuff()
    {
        Buff mirrorBuff = currentBuffs.Find(buff => buff?.data != null && buff.data.buffId == BattleRuntimeDefinitions.MirrorBuffId);
        if (mirrorBuff != null)
        {
            return mirrorBuff;
        }

        BuffData mirrorBuffData = BuffManager.Instance != null
            ? BuffManager.Instance.GetBuffData(BattleRuntimeDefinitions.MirrorBuffId)
            : null;
        if (mirrorBuffData == null)
        {
            mirrorBuffData = new BuffData
            {
                buffId = BattleRuntimeDefinitions.MirrorBuffId,
                name = "거울",
                description = "받은 피해만큼 중첩됩니다. 공격하면 0으로 초기화됩니다."
            };
        }

        mirrorBuff = new Buff(mirrorBuffData, 0, 0);
        currentBuffs.Add(mirrorBuff);
        return mirrorBuff;
    }
}
