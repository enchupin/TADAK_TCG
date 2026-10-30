using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using static BattleRuntimeDefinitions;
// v1.0 몬스터 데이터(monsters.json)와 DataMonster · AI · 의도 큐 검증, 몬스터 버프 스크립트 검증
class Program {
 static int count;
 static string root;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static void Fresh(){TrainingBattleManager.Instance=new();}
 static T New<T>(int id,int stagger=0) where T:DataMonster,new(){
  DataMonster.PendingDefinition=MonsterDatabase.Get(id);DataMonster.PendingStaggerIndex=stagger;
  var m=new T();m.Init();TrainingBattleManager.Instance.monsters.Add(m);m.StartBattle();return m;
 }
 static T Boss<T>(int id) where T:DataMonster,new(){Fresh();return New<T>(id);}
 static PlayerData Player=>TrainingBattleManager.Instance.playerData;
 static void Main(string[] args){
  root=args[0];UnityEngine.Resources.Root=root;
  foreach(var b in JObject.Parse(File.ReadAllText(Path.Combine(root,"Assets/Resources/JsonData/buffs.json")))["buffs"])BuffMetadataResolver.Names[(int)b["buffId"]]=(string)b["name"];
  MonsterDatabase.Reload();
  VerifyData();
  VerifyBosses();
  VerifyBuffScripts();
  VerifyNormalMonsters();
  VerifyIntentQueue();
  Console.WriteLine($"보스 패턴 검증 {count}개 통과");
 }

 // 데이터 정합성: AI가 가리키는 패턴, 종 클래스, 소환 대상, 버프·카드, 조우 구성
 static void VerifyData(){
  var monsters=MonsterDatabase.AllMonsters.ToList();
  Check(monsters.Count==73,"7절 몬스터 73종");
  string monsterDir=Path.Combine(root,"Assets/Script/Monster");
  string sources=string.Join("\n",Directory.GetFiles(monsterDir,"*.cs",SearchOption.AllDirectories).Select(File.ReadAllText));
  string factory=File.ReadAllText(Path.Combine(monsterDir,"MonsterPrefabFactory.cs"));
  var cardIds=JObject.Parse(File.ReadAllText(Path.Combine(root,"Assets/Resources/JsonData/MonsterCards.json")).TrimStart('﻿'))["cards"].Select(c=>(int)c["cardId"]).ToHashSet();
  foreach(var m in monsters){
   var keys=m.patterns.Select(p=>p.key).ToHashSet();
   Check(m.patterns.All(p=>p.id==m.id*100+int.Parse(p.key.Substring(1))),$"{m.id} 패턴 ID 규칙");
   Check(Regex.IsMatch(sources,$@"class {m.className}\s*:\s*DataMonster"),$"{m.id} 종 클래스 {m.className}");
   Check(factory.Contains($"nameof({m.className})"),$"{m.id} 생성기 종 등록 {m.className}");
   Check(m.tier==m.id/1000&&m.species==m.id%1000,$"{m.id} 계층·종 ID");
   var ai=m.ai; var refs=new List<string>();
   refs.AddRange(ai.order);refs.AddRange(ai.intro);refs.AddRange(ai.loop);refs.AddRange(ai.noRepeat);refs.AddRange(ai.groupExclusive);
   if(ai.start!=null)refs.Add(ai.start);
   refs.AddRange(ai.require.Select(r=>r.pattern));refs.AddRange(ai.@override.Select(r=>r.use));
   refs.AddRange(ai.rules.Where(r=>r.use!=null).Select(r=>r.use));refs.AddRange(ai.rules.Where(r=>r.loop!=null).SelectMany(r=>r.loop));
   refs.AddRange(ai.branches.SelectMany(b=>b.loop));if(ai.@lock!=null)refs.AddRange(ai.@lock.loop);
   foreach(string r in refs)Check(keys.Contains(r),$"{m.id} AI 패턴 {r}");
   foreach(var a in Flatten(m.patterns.SelectMany(p=>p.actions))){
    if(a.buffId>0)Check(BuffMetadataResolver.Names.ContainsKey(a.buffId),$"{m.id} 버프 {a.buffId}");
    if(a.type=="summon"||a.type=="block"&&a.target=="species")Check(MonsterDatabase.Get(m.GetSpeciesIdInSameTier(a.species))!=null,$"{m.id} 같은 계층 {a.species}");
    if(a.type=="card")Check(cardIds.Contains(a.cardId),$"{m.id} 몬스터 카드 {a.cardId}");
    Check(new[]{"attack","block","buff","debuff","reducePlayerBuff","heal","healUnblocked","card","summon","leave","nothing","special"}.Contains(a.type),$"{m.id} 액션 {a.type}");
   }
   foreach(var s in m.start)Check(BuffMetadataResolver.Names.ContainsKey(s.buffId),$"{m.id} 시작 효과 {s.buffId}");
  }
  var enc=MonsterDatabase.AllEncounters;
  Check(enc.Count(e=>e.kind=="normal"&&e.tier==1)==12&&enc.Count(e=>e.kind=="named"&&e.tier==1)==2,"계층 I 조우 수");
  Check(enc.Count(e=>e.kind=="normal"&&e.tier==2)==12&&enc.Count(e=>e.kind=="named"&&e.tier==2)==3,"계층 II 조우 수");
  Check(enc.Count(e=>e.kind=="normal"&&e.tier==3)==12&&enc.Count(e=>e.kind=="named"&&e.tier==3)==3,"계층 III 조우 수");
  Check(enc.Count(e=>e.kind=="normal"&&e.tier==4)==11&&enc.Count(e=>e.kind=="named"&&e.tier==4)==3,"계층 IV 조우 수");
  Check(enc.Count(e=>e.kind=="boss")==4&&MonsterDatabase.GetEncounter("BS02").monsters.SequenceEqual(new[]{5114,5114,5302}),"보스 조우");
  foreach(var e in enc)Check(e.monsters.Count>0&&e.monsters.Count<=4&&e.monsters.All(id=>MonsterDatabase.Get(id)!=null),$"조우 {e.id} 구성");
 }
 static IEnumerable<MonsterActionDefinition> Flatten(IEnumerable<MonsterActionDefinition> actions){
  foreach(var a in actions){if(a.type=="ifSelfStatus"){foreach(var x in Flatten(a.then.Concat(a.@else)))yield return x;}else yield return a;}
 }

 static void VerifyBosses(){
  var spider=Boss<GiantFlowerSpiderBossMonster>(5301);spider.Plan();spider.Act();
  Check(spider.hp==248&&spider.pattern==530101&&Player.hits.Count==8&&Player.hits.All(x=>x==2),"아라크네 초기 체력과 여덟 다리 공격");
  int previous=spider.pattern;for(int i=0;i<10;i++){spider.Plan();Check(previous!=spider.pattern,"아라크네 연속 패턴 방지");previous=spider.pattern;spider.Act();}
  var legs=new MonsterBuffs.EightLegsBuffScript();legs.OnMonsterHpLost(null,spider,49,8);Check(spider.GetBuffStack(EightLegsBuffId)==8,"다리 감소 임계치 전");
  legs.OnMonsterHpLost(null,spider,101,8);Check(spider.GetBuffStack(EightLegsBuffId)==5&&spider.GetBuffStack(StrengthBuffId)==9,"누적 체력 150 손실 시 세 다리와 힘 9");

  var prophet=Boss<ProphetBossMonster>(5302);var predation=TrainingBattleManager.Instance.predation;prophet.hp=100;
  predation.OnMonsterTurnStart(null,prophet,1);Check(prophet.hp==100&&prophet.GetBuffStack(FuturePredationBuffId)==1,"미래 포식 자체 제거 및 무조건 회복 방지");
  prophet.AddBuff(3003,1);predation.OnMonsterTurnStart(null,prophet,1);Check(prophet.hp==111&&prophet.GetBuffStack(StrengthBuffId)==2,"미래 포식 성공 시 11 회복과 힘 2");
  prophet.Plan();Check(prophet.pattern==530201,"선지자 시작 패턴");Player.buffs[3004]=1;prophet.Act();prophet.Plan();Check(prophet.pattern==530202,"훔치기 성공 분기");
  Check(prophet.intentText.Contains("2회"),"미래 포식 발동 수 + 1회 의도 표시");
  predation.OnMonsterTurnStart(null,prophet,1);Player.hits.Clear();prophet.Act();Check(Player.hits.Count==2,"턴 시작 추가 발동은 현재 공격 반복 횟수에 포함하지 않음");
  prophet.Plan();Check(prophet.pattern==530203,"성공 후 빈약");prophet.Act();prophet.Plan();Check(prophet.pattern==530201,"선지자 순환");prophet.Act();prophet.Plan();Check(prophet.pattern==530204,"훔칠 효과 없는 강공격 분기");prophet.Act();prophet.Plan();Check(prophet.pattern==530205,"실패 후 부식");

  var ice=Boss<IceAndFireBossMonster>(5303);ice.Plan();Check(ice.hp==165&&ice.defense==55&&ice.pattern==530302,"얼음과 불 시작");ice.StartTurn();ice.Act();ice.EndTurn();ice.Plan();Check(ice.pattern==530301&&Player.buffs[FrailBuffId]==99,"보호막 없는 다음 패턴");
  Player.hits.Clear();ice.Act();Check(Player.hits.Count==5&&Player.hits.All(x=>x==4)&&ice.defense==20,"조화 다섯 번 공격 및 보호막");ice.EndTurn();ice.Plan();Check(ice.pattern==530303,"보호막 보유 시 힘 패턴");ice.StartTurn();Check(ice.defense==20,"조화 보호막 턴 유지");ice.SetDefense(4);ice.StartTurn();Check(ice.defense==4,"손실된 영구 보호막 복원 방지");ice.Act();Check(ice.GetBuffStack(StrengthBuffId)==3,"명세의 힘 3 유지");

  var lord=Boss<VoidLordBossMonster>(5304);lord.Plan();lord.Act();Check(lord.hp==180&&lord.GetBuffStack(VoidShellBuffId)==6&&lord.pattern==530403&&Player.hits.SequenceEqual(new[]{8,8,8}),"공허 군주 첫 공격");lord.EndTurn();lord.Plan();Check(lord.pattern==530406,"단독 강공 패턴");lord.Act();Check(lord.GetBuffStack(AttackBoostBuffId)==4,"실제 강공 버프 부여");lord.Plan();Player.hits.Clear();lord.Act();Check(Player.hits.SequenceEqual(new[]{12,8,8})&&lord.GetBuffStack(AttackBoostBuffId)==0,"강공은 다단 공격의 첫 타격에만 적용 후 소모");
  lord.AddBuff(4001,1);lord.AddBuff(4002,1);lord.AddBuff(4003,1);lord.EndTurn();lord.Plan();Check(lord.pattern==530404,"해로운 효과 세 종류 고정 순환");lord.Act();Check(!lord.CanHit&&BuffCombatUtility.CountDistinctNegativeBuffTypes(lord)==0,"정화와 무적");lord.EndTurn();lord.Plan();Player.hits.Clear();lord.Act();Check(lord.pattern==530405&&Player.hits.Count==0&&lord.CanHit,"휴식 후 무적 해제");lord.EndTurn();lord.Plan();Check(lord.pattern==530403,"고정 순환 마지막 공격");
  var ally=New<GiantFlowerSpiderBossMonster>(5301);lord.Act();lord.Plan();Check(lord.pattern==530401,"아군 보유 시 카드 생성 패턴");lord.Act();Check(TrainingBattleManager.Instance.usableDeckManager.draw.Count==1&&TrainingBattleManager.Instance.usableDeckManager.discard.Count==1&&TrainingBattleManager.Instance.handManager.cards.Count==1,"세 영역에 공허의 부름 생성");lord.Plan();Check(lord.pattern==530402,"공허 껍질 패턴");lord.Act();Check(ally.GetBuffStack(VoidShellBuffId)==0&&lord.GetBuffStack(VoidShellBuffId)==8,"v1.0 공허 껍질은 자신만");
 }

 static void VerifyBuffScripts(){
  var lord=Boss<VoidLordBossMonster>(5304);
  var shell=new MonsterBuffs.VoidShellBuffScript();lord.SetDefense(0);shell.OnMonsterAfterTakeDamage(null,lord,5,5,11);shell.OnMonsterAfterTakeDamage(null,lord,5,5,11);Check(lord.defense==11,"턴 최초 피해만 공허 껍질 발동");shell.OnMonsterTurnStart(null,lord,11);shell.OnMonsterAfterTakeDamage(null,lord,5,5,11);Check(lord.defense==22,"다음 턴 공허 껍질 재발동");TrainingBattleManager.Instance.TurnSequence++;shell.OnMonsterAfterTakeDamage(null,lord,5,5,11);shell.OnMonsterAfterTakeDamage(null,lord,5,5,11);Check(lord.defense==33,"내 턴에도 첫 피해마다 공허 껍질 발동 (E-14)");int lordHp=lord.hp;lord.hp=0;TrainingBattleManager.Instance.TurnSequence++;shell.OnMonsterAfterTakeDamage(null,lord,5,5,11);Check(lord.defense==33,"첫 피해로 사망하면 공허 껍질 발동 안 함");lord.hp=lordHp;
  var other=New<GiantFlowerSpiderBossMonster>(5301);other.AddBuff(HarmonyBuffId,1);other.Plan();other.Act();other.StartTurn();Check(other.defense==16,"조화가 다른 몬스터에도 적용");other.ConsumeBuffStack(HarmonyBuffId,1);other.Plan();other.Act();other.StartTurn();Check(other.defense==16,"조화 제거 전 얻은 영구 보호막 유지");
  var boost=TrainingBattleManager.Instance.attackBoost;other.AddBuff(AttackBoostBuffId,6);boost.OnAttackActionStarted(other,6);other.AddBuff(AttackBoostBuffId,2);boost.OnAttackActionEnded(other);Check(other.GetBuffStack(AttackBoostBuffId)==2,"공격 도중 추가된 강공 중첩 보존");boost.OnAttackActionEnded(other);Check(other.GetBuffStack(AttackBoostBuffId)==2,"강공 중복 소모 방지");
  var tracker=TrainingBattleManager.Instance.predation;other.AddBuff(FuturePredationBuffId,1);tracker.OnMonsterTurnStart(null,other,1);Check(tracker.GetActivationCount(other)==1,"미래 포식 횟수가 선지자 클래스에 종속되지 않음");tracker.OnMonsterDeath(null,other,1);Check(tracker.GetActivationCount(other)==0,"미래 포식 발동 기록 정리");
 }

 static void VerifyNormalMonsters(){
  // 돌 방패 골렘: 혼자 남으면 피해 후 이탈, 아군 사망 시 즉시 의도 재판정 (E-40)
  Fresh();var shield=New<TestDataMonster>(2106);var thrower=New<TestDataMonster>(2107);
  shield.Plan();Check(shield.pattern==210601,"아군이 있으면 약화·보호막");
  thrower.hp=0;shield.ReevaluateConditionalIntent();Check(shield.pattern==210602,"아군 사망 즉시 자폭 의도로 변경");
  Check(!shield.CanChangeIntent,"이탈 의도는 변경 불가");
  Check(shield.intentLines.Count==1&&shield.intentLines[0].IsSingle(MonsterIntentIconType.Bomb)&&shield.intentLines[0].Sentence=="피해를 14 입힙니다. 사망합니다.","자폭 툴팁은 공격 + 사망 한 줄");
  // 공격 후 이탈은 자폭: 이탈이 아니라 처치가 아닌 사망으로 처리
  shield.Act();Check(Player.hits.SequenceEqual(new[]{14})&&shield.selfDestructed&&shield.IsDead()&&!shield.left,"피해 14 후 이탈");
  // 자폭 피해로 파티가 먼저 쓰러지면 이탈하지 않음 → 남은 적이 있어 몬스터 패배보다 플레이어 패배가 먼저
  Fresh();shield=New<TestDataMonster>(2106);thrower=New<TestDataMonster>(2107);
  shield.Plan();thrower.hp=0;shield.ReevaluateConditionalIntent();Player.hp=10;
  shield.Act();Check(Player.IsDead()&&!shield.selfDestructed&&!shield.left&&!shield.IsDead(),"파티가 먼저 쓰러지면 자폭 생략");

  // 선인장: 가시 최대 9, 9 이상이면 P01 우선
  Fresh();var cactus=New<TestDataMonster>(1115);Check(cactus.GetBuffStack(3010)==3,"선인장 시작 가시 3");
  for(int i=0;i<12;i++){cactus.Plan();cactus.Act();Check(cactus.GetBuffStack(3010)<=9,"가시 최대 9");}
  cactus.buffs[3010]=9;for(int i=0;i<4;i++){cactus.Plan();Check(cactus.pattern==111501,"가시 9 이상이면 공격 우선");cactus.Act();}

  // 목각 인형: P03·P04는 플레이어에게 해당 효과가 없을 때, 같은 종류 중 한 개체만
  Fresh();var puppetA=New<TestDataMonster>(1116);var puppetB=New<TestDataMonster>(1116);
  string[] debuffKeys={"P03","P04"};
  for(int i=0;i<80;i++){
   Player.buffs.Clear();if(i%4==0)Player.buffs[FrailBuffId]=1;
   puppetA.Plan();puppetB.Plan();
   Check(!(debuffKeys.Contains(puppetA.CurrentPatternKey)&&debuffKeys.Contains(puppetB.CurrentPatternKey)),"해로운 효과 패턴은 같은 종류 중 한 개체만");
   if(i%4==0)Check(puppetA.CurrentPatternKey!="P03"&&puppetB.CurrentPatternKey!="P03","빈약 보유 시 빈약 패턴 제외");
   puppetA.Act();puppetB.Act();
  }

  // 변이 고구마: 순서형 intro → loop, 뿌리내림 상태 수치
  Fresh();var potato=New<TestDataMonster>(3112);var order=new List<int>();
  for(int i=0;i<7;i++){potato.Plan();order.Add(potato.pattern);Player.hits.Clear();potato.Act();if(i==2)Check(Player.hits.SequenceEqual(new[]{14}),"뿌리내림 상태 P02 피해 14");}
  Check(order.SequenceEqual(new[]{311202,311201,311202,311203,311204,311202,311203}),"순서형 intro 뒤 loop");

  // 공허 괴수: P01 타격 횟수가 사용할 때마다 증가
  Fresh();var beast=New<TestDataMonster>(4204);beast.Plan();Player.hits.Clear();beast.Act();Check(Player.hits.Count==2,"공허 괴수 첫 P01 2회");
  beast.Plan();beast.Act();beast.Plan();beast.Act();beast.Plan();Player.hits.Clear();beast.Act();Check(beast.pattern==420401&&Player.hits.Count==3,"공허 괴수 두 번째 P01 3회");

  // 소환 자리가 없으면 소환 패턴을 건너뛰고 다음 패턴 (잭 오 리퍼 P01 → P02 소환 → P03)
  Fresh();var jack=New<TestDataMonster>(1104);var spawner=TrainingBattleManager.Instance.monsterSpawner;
  jack.Plan();jack.Act();spawner.FreeSlots=0;jack.Plan();Check(jack.pattern==110403,"자리가 없으면 의도 결정 시 소환 건너뜀");
  jack.Act();spawner.FreeSlots=1;spawner.Spawn=id=>New<TestDataMonster>(id);spawner.ResetSummonReservations();jack.Plan();jack.Plan();Check(jack.pattern==110401,"순환 계속");jack.Act();
  jack.Plan();Check(jack.pattern==110402,"자리가 있으면 소환 의도");spawner.FreeSlots=0;Player.hits.Clear();jack.Act();Check(Player.hits.Count==0&&!TrainingBattleManager.Instance.monsters.Any(m=>m.MonsterId==1105),"시행 시점에 자리가 없으면 행동 없이 패턴을 넘김");
  jack.Plan();Check(jack.pattern==110403,"소환 실패 후 다음 패턴");
  spawner.FreeSlots=1;spawner.ResetSummonReservations();jack.Act();jack.Plan();jack.Act();jack.Plan();Check(jack.pattern==110402,"다음 순환의 소환");jack.Act();Check(TrainingBattleManager.Instance.monsters.Any(m=>m.MonsterId==1105&&((DataMonster)m).Definition.tier==1),"같은 계층 현란한 낫 소환");

  // 패턴 엇갈림 (R-86): 같은 종류 k번째 개체는 순환을 k칸 뒤에서 시작
  Fresh();var bugA=New<TestDataMonster>(1109,0);var bugB=New<TestDataMonster>(1109,1);bugA.Plan();bugB.Plan();
  Check(bugA.pattern==110901&&bugB.pattern==110903,"두 번째 공허 벌레는 P03부터");

  // 귀신 들린 천: 장난꾸러기 유령이 0이면 이탈 우선 (override)
  Fresh();var cloth=New<TestDataMonster>(1110);Check(cloth.GetBuffStack(5006)==5&&cloth.GetBuffStack(5007)==0,"귀신 들린 천 시작 효과 (도둑은 훔칠 때 얻음)");
  cloth.Plan();Check(cloth.pattern==111001,"귀신 들린 천 첫 패턴");cloth.Act();cloth.buffs.Remove(5006);cloth.Plan();Check(cloth.pattern==111003,"유령 0이면 이탈");
 }

 static void VerifyIntentQueue(){
  // n칸 미루기: 큐를 채워 n번째로 바꾸고 앞칸 버림, 이후 순환은 이어짐 (2-8절 2·3)
  Fresh();var bug=New<TestDataMonster>(1109);bug.Plan();Check(bug.pattern==110901,"공허 벌레 첫 의도 P01");
  // 의도 툴팁: 행동 종류별 한 줄, 버프 이름은 (이름) 표기
  Check(bug.intentLines.Count==2&&bug.intentLines[0].IsSingle(MonsterIntentIconType.Attack)&&bug.intentLines[0].Sentence.Contains("6"),"의도 툴팁 공격 줄");
  Check(bug.intentLines[1].IsSingle(MonsterIntentIconType.HarmfulEffect)&&bug.intentLines[1].Sentence.StartsWith("[buff:")&&bug.intentLines[1].Sentence.Contains("]"),"의도 툴팁 해로운 효과 줄 버프 표기 (ID 참조)");
  Check(bug.DelayIntent(2)&&bug.pattern==110902&&bug.QueuedPatternKeys.Count==0,"2칸 미루기로 P02");
  bug.Act();bug.Plan();Check(bug.pattern==110901,"미룬 뒤 순환 계속");

  // 같은 패턴이면 다음 칸으로
  Fresh();var mirror=New<TestDataMonster>(1114);mirror.Plan();Check(mirror.DelayIntent(1)&&mirror.pattern==111402,"같은 패턴은 건너뜀");
  // 거울 괴물은 거울 0을 보유한 채 시작하고, 소모로 0이 되어도 유지
  Fresh();mirror=New<TestDataMonster>(1114);Check(mirror.buffs.TryGetValue(5012,out int mirrorStack)&&mirrorStack==0,"거울 0으로 시작");
  mirror.AddBuff(5012,5);mirror.ConsumeBuffStack(5012,5);Check(mirror.buffs.ContainsKey(5012)&&mirror.GetBuffStack(5012)==0,"거울 소모 후 0으로 유지");

  // 공격 의도 복사: 큐에서 가장 빠른 공격 의도
  Fresh();bug=New<TestDataMonster>(1109);bug.Plan();bug.Act();bug.Plan();Check(bug.pattern==110903,"공허 벌레 힘 패턴");
  Check(bug.CopyEarliestAttackIntent()&&bug.pattern==110902&&bug.HasAttackIntent,"도발로 가장 빠른 공격 의도 복사");

  // 봉인: 큐 맨 앞에 행동 없음, 다음 의도부터 순환 계속
  Fresh();bug=New<TestDataMonster>(1109);bug.Plan();Check(bug.SealNextAction(),"봉인");bug.Act();bug.Plan();
  Check(bug.pattern==0&&bug.intentText=="아무 행동도 하지 않습니다.","봉인된 다음 턴은 행동 없음");Check(bug.intentLines.Count==0,"행동 없음은 의도 설명 한 줄로 대체");Check(!bug.CanChangeIntent,"봉인된 상태는 변경 불가");
  Player.hits.Clear();bug.Act();Check(Player.hits.Count==0,"봉인된 턴 행동 없음");bug.Plan();Check(bug.pattern==110903,"봉인 뒤 순환 계속");

  // 보스 봉인은 피해 15
  var spider=Boss<GiantFlowerSpiderBossMonster>(5301);spider.Plan();spider.SealNextAction();Check(spider.hp==248-15,"보스 봉인은 피해 15로 대체");

  // 변경 불가: 패턴 1개 · 기절 상태
  Fresh();var stealer=New<TestDataMonster>(2108);stealer.Plan();Check(!stealer.CanChangeIntent&&!stealer.DelayIntent(2),"패턴이 1개뿐이면 변경 불가");
  Fresh();bug=New<TestDataMonster>(1109);bug.Plan();bug.stunned=true;Check(!bug.CanChangeIntent,"기절 상태는 변경 불가");

  // 기절은 그 턴의 의도를 소화 (R-71)
  bug.Skip();bug.Plan();Check(bug.pattern==110903,"기절 후 다음 의도");
 }
}
