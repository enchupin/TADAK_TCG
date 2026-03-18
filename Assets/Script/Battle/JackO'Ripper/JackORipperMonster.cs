using UnityEngine;

public class JackORipperMonster : Monster
{
    [SerializeField] private GameObject flashyScythePrefab;
    [SerializeField] private Vector2 summonOffsetMin = new Vector2(220f, -120f);
    [SerializeField] private Vector2 summonOffsetMax = new Vector2(420f, 120f);

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

        Transform parentTransform = transform.parent;
        GameObject summonedObject = parentTransform != null
            ? Instantiate(flashyScythePrefab, parentTransform)
            : Instantiate(flashyScythePrefab);

        if (summonedObject == null)
        {
            return;
        }

        Vector2 summonOffset = GetRandomSummonOffset();
        RectTransform sourceRectTransform = transform as RectTransform;
        RectTransform summonedRectTransform = summonedObject.transform as RectTransform;
        if (sourceRectTransform != null && summonedRectTransform != null)
        {
            summonedRectTransform.anchoredPosition = sourceRectTransform.anchoredPosition + summonOffset;
            summonedRectTransform.localScale = sourceRectTransform.localScale;
        }
        else
        {
            summonedObject.transform.localPosition = transform.localPosition + new Vector3(summonOffset.x, summonOffset.y, 0f);
        }

        Monster summonedMonster = summonedObject.GetComponent<Monster>();
        if (summonedMonster != null)
        {
            TrainingBattleManager.Instance?.RegisterMonster(summonedMonster);
            summonedMonster.UpdateUI();
        }

        TrainingBattleManager.Instance?.UpdateAllUI();
    }

    private Vector2 GetRandomSummonOffset()
    {
        float offsetX = Random.Range(summonOffsetMin.x, summonOffsetMax.x);
        float offsetY = Random.Range(summonOffsetMin.y, summonOffsetMax.y);
        return new Vector2(offsetX, offsetY);
    }
}
