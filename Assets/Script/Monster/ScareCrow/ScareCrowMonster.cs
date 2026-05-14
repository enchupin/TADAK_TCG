public class ScareCrowMonster : Monster
{
    public override int MonsterId => 9001;
    protected override string MonsterName => "허수아비";
    protected override int BaseMaxHp => 1;

    protected override void BuildNextAction()
    {
    }

    protected override void ExecuteAction(PlayerData target)
    {
    }

    protected override void OnBeforeTakeDamage(int incomingDamage)
    {
        defense = 0;
    }

    protected override void OnAfterTakeDamage(int incomingDamage, int damageAfterDefense)
    {
        hp = maxHP;
        defense = 0;
    }

    protected override void OnTurnStarted()
    {
        defense = 0;
    }

    protected override void OnTurnEnded()
    {
        defense = 0;
    }

    protected override bool CanDie()
    {
        return false;
    }
}
