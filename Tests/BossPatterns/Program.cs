using System;
using System.Linq;
using static BattleRuntimeDefinitions;
class Program {
 static int count;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static T New<T>() where T:Monster,new(){TrainingBattleManager.Instance=new();var m=new T();TrainingBattleManager.Instance.monsters.Add(m);m.StartBattle();return m;}
 static PlayerData Player=>TrainingBattleManager.Instance.playerData;
 static void Main(){
  var spider=New<GiantFlowerSpiderBossMonster>();spider.Plan();spider.Act();
  Check(spider.hp==450&&spider.pattern==30101&&Player.hits.Count==8&&Player.hits.All(x=>x==2),"거미 초기 체력과 여덟 번 공격");
  int previous=spider.pattern;for(int i=0;i<10;i++){spider.Plan();Check(previous!=spider.pattern,"거미 연속 패턴 방지");previous=spider.pattern;spider.Act();}
  var legs=new MonsterBuffs.EightLegsBuffScript();legs.OnMonsterHpLost(null,spider,49,8);Check(spider.GetBuffStack(EightLegsBuffId)==8,"다리 감소 임계치 전");
  legs.OnMonsterHpLost(null,spider,101,8);Check(spider.GetBuffStack(EightLegsBuffId)==5&&spider.GetBuffStack(StrengthBuffId)==9,"누적 체력 150 손실 시 세 다리와 힘 9");
  var prophet=New<ProphetBossMonster>();var predation=TrainingBattleManager.Instance.predation;prophet.hp=100;
  predation.OnMonsterTurnStart(null,prophet,1);Check(prophet.hp==100&&prophet.GetBuffStack(FuturePredationBuffId)==1,"미래 포식 자체 제거 및 무조건 회복 방지");
  prophet.AddBuff(3003,1);predation.OnMonsterTurnStart(null,prophet,1);Check(prophet.hp==120&&prophet.GetBuffStack(StrengthBuffId)==2,"미래 포식 성공 시 20 회복과 힘 2");
  prophet.Plan();Check(prophet.pattern==30201,"선지자 시작 패턴");Player.buffs[3004]=1;prophet.Act();prophet.Plan();Check(prophet.pattern==30202,"훔치기 성공 분기");
  predation.OnMonsterTurnStart(null,prophet,1);Player.hits.Clear();prophet.Act();Check(Player.hits.Count==2,"턴 시작 추가 발동은 현재 공격 반복 횟수에 포함하지 않음");
  prophet.Plan();Check(prophet.pattern==30203,"성공 후 빈약");prophet.Act();prophet.Plan();Check(prophet.pattern==30201,"선지자 순환");prophet.Act();prophet.Plan();Check(prophet.pattern==30204,"훔칠 효과 없는 강공격 분기");prophet.Act();prophet.Plan();Check(prophet.pattern==30205,"실패 후 부식");
  var ice=New<IceAndFireBossMonster>();ice.Plan();Check(ice.hp==300&&ice.defense==100&&ice.pattern==30302,"얼음과 불 시작");ice.StartTurn();ice.Act();ice.EndTurn();ice.Plan();Check(ice.pattern==30301&&Player.buffs[FrailBuffId]==99,"보호막 없는 다음 패턴");
  Player.hits.Clear();ice.Act();Check(Player.hits.Count==5&&Player.hits.All(x=>x==2)&&ice.defense==10,"조화 다섯 번 공격 및 보호막");ice.EndTurn();ice.Plan();Check(ice.pattern==30303,"보호막 보유 시 힘 패턴");ice.StartTurn();Check(ice.defense==10,"조화 보호막 턴 유지");ice.SetDefense(4);ice.StartTurn();Check(ice.defense==4,"손실된 영구 보호막 복원 방지");ice.Act();Check(ice.GetBuffStack(StrengthBuffId)==3,"명세의 힘 3 유지");
  var lord=New<VoidLordBossMonster>();lord.Plan();lord.Act();Check(lord.hp==400&&lord.pattern==30403&&Player.hits.SequenceEqual(new[]{14,14,14}),"공허 군주 첫 공격");lord.EndTurn();lord.Plan();Check(lord.pattern==30406,"단독 강공 패턴");lord.Act();Check(lord.GetBuffStack(AttackBoostBuffId)==6,"실제 강공 버프 부여");lord.Plan();Player.hits.Clear();lord.Act();Check(Player.hits.SequenceEqual(new[]{20,20,20})&&lord.GetBuffStack(AttackBoostBuffId)==0,"강공 1회 공격 후 소모");
  lord.AddBuff(4001,1);lord.AddBuff(4002,1);lord.AddBuff(4003,1);lord.EndTurn();lord.Plan();Check(lord.pattern==30404,"부정적 효과 세 종류 분기");lord.Act();Check(!lord.CanHit&&BuffCombatUtility.CountDistinctNegativeBuffTypes(lord)==0,"정화와 무적");lord.EndTurn();lord.Plan();Player.hits.Clear();lord.Act();Check(lord.pattern==30405&&Player.hits.Count==0&&lord.CanHit,"휴식 후 무적 해제");lord.EndTurn();lord.Plan();Check(lord.pattern==30403,"회복 후 공격 고정");
  var ally=new GiantFlowerSpiderBossMonster();ally.StartBattle();TrainingBattleManager.Instance.monsters.Add(ally);lord.Act();lord.Plan();Check(lord.pattern==30401,"아군 보유 시 카드 생성 패턴");lord.Act();Check(TrainingBattleManager.Instance.usableDeckManager.draw.Count==1&&TrainingBattleManager.Instance.usableDeckManager.discard.Count==1&&TrainingBattleManager.Instance.handManager.cards.Count==1,"세 영역에 공허의 부름 생성");lord.Plan();Check(lord.pattern==30402,"아군 보호 패턴");lord.Act();Check(ally.GetBuffStack(VoidShellBuffId)==4&&lord.GetBuffStack(VoidShellBuffId)==15,"모든 아군에게 공허 껍질");
  var shell=new MonsterBuffs.VoidShellBuffScript();lord.SetDefense(0);shell.OnMonsterBeforeTakeDamage(null,lord,5,11);shell.OnMonsterBeforeTakeDamage(null,lord,5,11);Check(lord.defense==11,"턴 최초 피해만 공허 껍질 발동");shell.OnMonsterTurnStart(null,lord,11);shell.OnMonsterBeforeTakeDamage(null,lord,5,11);Check(lord.defense==22,"다음 턴 공허 껍질 재발동");
  var other=New<GiantFlowerSpiderBossMonster>();other.AddBuff(HarmonyBuffId,1);other.Plan();other.Act();other.StartTurn();Check(other.defense==16,"조화가 다른 몬스터에도 적용");other.ConsumeBuffStack(HarmonyBuffId,1);other.Plan();other.Act();Check(other.defense==16,"조화 제거 후 추가 보호막 없음");other.StartTurn();Check(other.defense==16,"조화 제거 전 얻은 영구 보호막 유지");
  var boost=TrainingBattleManager.Instance.attackBoost;other.AddBuff(AttackBoostBuffId,6);boost.OnAttackActionStarted(other,6);other.AddBuff(AttackBoostBuffId,2);boost.OnAttackActionEnded(other);Check(other.GetBuffStack(AttackBoostBuffId)==2,"공격 도중 추가된 강공 중첩 보존");boost.OnAttackActionEnded(other);Check(other.GetBuffStack(AttackBoostBuffId)==2,"강공 중복 소모 방지");
  var tracker=TrainingBattleManager.Instance.predation;other.AddBuff(FuturePredationBuffId,1);tracker.OnMonsterTurnStart(null,other,1);Check(tracker.GetActivationCount(other)==1,"미래 포식 횟수가 선지자 클래스에 종속되지 않음");tracker.OnMonsterDeath(null,other,1);Check(tracker.GetActivationCount(other)==0,"미래 포식 발동 기록 정리");
  Console.WriteLine($"보스 패턴 검증 {count}개 통과");
 }
}
