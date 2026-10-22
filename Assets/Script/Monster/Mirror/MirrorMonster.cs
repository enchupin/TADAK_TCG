// v1.0 114 거울 괴물
// 체력·패턴·AI는 monsters.json 데이터 사용 (DataMonster)
// P01 피해는 거울 버프 스크립트가 거울 중첩만큼 더하고 0으로 초기화하므로, 피해를 받으면 의도 수치를 갱신
public class MirrorMonster : DataMonster
{
    protected override void OnAfterTakeDamage(int incomingDamage, int damageAfterDefense)
    {
        if (incomingDamage > 0)
        {
            RefreshIntent();
        }
    }
}
