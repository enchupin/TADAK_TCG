using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace UnityEngine {
 public static class Mathf {
  public static int Max(int a,int b)=>Math.Max(a,b); public static int Min(int a,int b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b);
  public static int Clamp(int v,int a,int b)=>Math.Clamp(v,a,b); public static int FloorToInt(float v)=>(int)Math.Floor(v); public static int RoundToInt(float v)=>(int)Math.Round(v); public static int Abs(int v)=>Math.Abs(v);
 }
 public static class Random { public static System.Random Rng=new(7); public static int Range(int a,int b)=>b<=a?a:Rng.Next(a,b); }
 public static class Debug { public static void Log(object o){} public static void LogWarning(object o){} public static void LogError(object o)=>throw new Exception(o?.ToString()); }
 public class TextAsset { public string text; }
 public static class Resources {
  public static string Root;
  public static T Load<T>(string path) where T:TextAsset,new(){string file=Path.Combine(Root,"Assets/Resources",path+".json");return File.Exists(file)?new T{text=File.ReadAllText(file)}:null;}
 }
}
public static class BattleRuntimeDefinitions {
 public const int HarmonyBuffId=5014;
 public const int EightLegsBuffId=5009,FuturePredationBuffId=5010,VoidShellBuffId=5002,StrengthBuffId=3002,AttackBoostBuffId=3009,WeakBuffId=4008,CorrosionBuffId=4009,FrailBuffId=4011;
}
public sealed class CombatRulesData { public bool patternStagger=true, stunConsumesIntent=true; }
public static class GameRules { public static CombatRulesData Combat=new(); }
public enum MonsterIntentIconType { None,Attack,Protection,BeneficialEffect,HarmfulEffect,Summon,Bomb,DisruptCard,Heal,Stun,Leave }
public class BuffData {
 public int buffId; public string name;
 public static bool AllowsNegativeStack(int id)=>id==3002||id==4006;
}
public static class BuffMetadataResolver {
 public static Dictionary<int,string> Names=new();
 public static BuffData Resolve(int id)=>new(){buffId=id,name=Names.GetValueOrDefault(id,id.ToString())};
}
public class Buff { public BuffData data=new(); public int stack; }
public class Card { public int cardId; public string cardName=""; }
public static class CardManager { public static Card GetCardAsCard(int id)=>new(){cardId=id,cardName="카드"+id}; }
public class PlayerData {
 public int hp=10000; public List<int> hits=new(); public Dictionary<int,int> buffs=new();
 public bool IsDead()=>hp<=0;
 public void AddBuff(int id,int n)=>buffs[id]=buffs.GetValueOrDefault(id)+n;
 public int GetBuffStack(int id)=>buffs.GetValueOrDefault(id);
 public void ConsumeBuffStack(int id,int n){int c=GetBuffStack(id)-n;if(c<=0)buffs.Remove(id);else buffs[id]=c;}
}
public class Piles {
 public List<Card> draw=new(),discard=new();
 public void AddToDrawPileRandom(Card c)=>draw.Add(c);
 public void AddToDiscard(Card c)=>discard.Add(c);
}
public class Hand { public List<Card> cards=new(); public void AddCard(Card c)=>cards.Add(c); public void AddCard(List<Card> c)=>cards.AddRange(c); }
public class MonsterSpawner {
 public Func<int,Monster> Spawn; public int FreeSlots, Reserved;
 public Monster SpawnSummonedMonster(int id){if(FreeSlots<=0)return null;FreeSlots--;Reserved=Math.Max(0,Reserved-1);return Spawn?.Invoke(id);}
 public bool TryReserveSummonSlot(){if(FreeSlots-Reserved<=0)return false;Reserved++;return true;}
 public void ResetSummonReservations()=>Reserved=0;
}
public class TrainingBattleManager {
 public static TrainingBattleManager Instance=new(); public PlayerData playerData=new(); public int TurnSequence=1;
 public Piles usableDeckManager=new();public Hand handManager=new();public List<Monster> monsters=new();
 public MonsterSpawner monsterSpawner=new();
 public MonsterBuffs.FuturePredationBuffScript predation=new();
 public MonsterBuffs.HarmonyBuffScript harmony=new();
 public MonsterBuffs.AttackBoostBuffScript attackBoost=new();
 public int GetMonsterBuffActivationCount(Monster m,int id)=>predation.GetActivationCount(m);
 public List<Monster> GetLivingMonsters()=>monsters.Where(m=>!m.IsDead()&&!m.left).ToList();
 public void UpdateAllUI(){} public void RefreshHandPlayableState(){}
 public int ResolvePlayerEffectDamage(int n)=>n; public int ResolveMonsterBarrierGain(Monster m,int n)=>n;
 public int ResolvePersistentUpgradeCardId(int id)=>id;
 public List<Card> ProcessGeneratedCards(List<Card> c,bool allow)=>c; public bool CanGainCardsToHand()=>true;
 public void RegisterMonster(Monster m){if(!monsters.Contains(m))monsters.Add(m);}
}
public static class BuffCombatUtility {
 public static Buff RemoveRandomBeneficialBuff(Monster m,int excludedBuffId=0){
  int id=m.buffs.Keys.FirstOrDefault(id=>id!=excludedBuffId&&id<4000||id!=excludedBuffId&&id>=5000);
  if(id==0)return null; var b=new Buff{data=new(){buffId=id},stack=m.buffs[id]};m.buffs.Remove(id);return b;
 }
 public static Buff RemoveRandomBeneficialBuff(PlayerData p){
  int id=p.buffs.Keys.FirstOrDefault(id=>id<4000);if(id==0)return null;
  var b=new Buff{data=new(){buffId=id},stack=p.buffs[id]};p.buffs.Remove(id);return b;
 }
 public static int CountDistinctNegativeBuffTypes(Monster m)=>m.buffs.Keys.Count(id=>id>=4000&&id<5000);
 public static void RemoveAllNegativeBuffs(Monster m){foreach(int id in m.buffs.Keys.Where(id=>id>=4000&&id<5000).ToArray())m.buffs.Remove(id);}
}
public abstract class Monster {
 public abstract int MonsterId{get;} protected virtual string MonsterName=>GetType().Name; protected abstract int BaseMaxHp{get;}
 protected virtual bool IsBossMonster=>false; private int permanentDefense; private bool attack;
 public int hp,defense,pattern,maxHP;public Dictionary<int,int> buffs=new(); public string name="",intentText=""; public bool left,stunned;
 public string DisplayName=>MonsterName; public bool IsBoss=>IsBossMonster; public bool IsStunned=>stunned; public bool HasAttackIntent=>attack;
 protected virtual void Awake(){}
 public void Init(){Awake();}
 public void StartBattle(){maxHP=BaseMaxHp;hp=BaseMaxHp;OnBattleStart();}
 public void Plan()=>BuildNextAction(); public void PlanNextAction()=>BuildNextAction();
 public void Act(){var service=TrainingBattleManager.Instance;bool wasAttack=attack;if(wasAttack)service.attackBoost.OnAttackActionStarted(this,GetBuffStack(BattleRuntimeDefinitions.AttackBoostBuffId));ExecuteAction(service.playerData);if(wasAttack)service.attackBoost.OnAttackActionEnded(this);attack=false;}
 public void Skip(){stunned=false;OnTurnActionSkipped();}
 public void StartTurn(){SetDefense(Math.Min(defense,permanentDefense));}
 public void EndTurn()=>OnTurnEnded();
 public bool CanHit=>CanReceiveDamage(1);
 public void SetDefense(int value){defense=value;permanentDefense=Math.Min(permanentDefense,defense);}
 public bool IsDead()=>hp<=0;
 public void AddBuff(int id,int n)=>buffs[id]=GetBuffStack(id)+n;
 public int GetBuffStack(int id)=>buffs.GetValueOrDefault(id);
 public void ConsumeBuffStack(int id,int n){int count=GetBuffStack(id)-n;if(count<=0)buffs.Remove(id);else buffs[id]=count;}
 public void Heal(int n)=>hp=Math.Min(BaseMaxHp,hp+n);
 public void AddDefense(int n)=>defense+=n;
 public void AddPermanentDefense(int n){AddDefense(n);permanentDefense+=n;}
 public int TakeDamage(int n,int s=0){if(!CanReceiveDamage(n))return 0;int d=Math.Max(0,n-defense);defense=Math.Max(0,defense-n);hp-=d;OnAfterTakeDamage(n,d);return d;}
 public void LeaveCombat(){left=true;}
 public bool selfDestructed; public void SelfDestruct(){hp=0;defense=0;selfDestructed=true;}
 public void UpdateUI(){}
 public virtual void ScalePlannedIntent(float m){}
 protected int PreviewOutgoingDamage(int n)=>n+GetBuffStack(BattleRuntimeDefinitions.StrengthBuffId)+TrainingBattleManager.Instance.attackBoost.GetOutgoingDamageFlatBonus(null,this,GetBuffStack(BattleRuntimeDefinitions.AttackBoostBuffId),0);
 protected int PreviewBarrierGain(int n)=>n;
 protected int PreviewMonsterBuffAmount(int id,int n)=>n;
 protected int PreviewPlayerDebuffAmount(int id,int n)=>n;
 protected int DealDamage(PlayerData p,int n){int d=PreviewOutgoingDamage(n);p.hp-=d;p.hits.Add(d);TrainingBattleManager.Instance.harmony.OnMonsterAttackResolved(null,this,p,d,d,GetBuffStack(BattleRuntimeDefinitions.HarmonyBuffId));TrainingBattleManager.Instance.attackBoost.OnMonsterAttackResolved(null,this,p,d,d,GetBuffStack(BattleRuntimeDefinitions.AttackBoostBuffId));return d;}
 protected void AddDebuffToPlayer(PlayerData p,int id,int n)=>p.buffs[id]=p.buffs.GetValueOrDefault(id)+n;
 protected void AddCardToPlayerDiscard(Card c)=>TrainingBattleManager.Instance.usableDeckManager.AddToDiscard(c);
 public List<MonsterIntentLine> intentLines=new();
 protected void SetAttackIntent(int n,string s){attack=true;intentText=s;intentLines.Clear();} protected void SetIntent(string s){attack=false;intentText=s;intentLines.Clear();}
 protected void SetIntentLines(IEnumerable<MonsterIntentLine> l){intentLines=l.ToList();}
 protected void SetPlannedPattern(int id,params MonsterIntentIconType[] icons)=>pattern=id;
 protected void SetProtectionIntentValue(int n){}
 protected virtual void OnBattleStart(){} protected virtual void OnTurnEnded(){} protected virtual void OnRevivedTriggered(){}
 protected virtual void OnTurnActionSkipped(){} protected virtual void OnAfterTakeDamage(int n,int d){}
 protected virtual void OnDefenseChanged(int oldValue,int newValue){}
 protected virtual bool CanReceiveDamage(int n)=>true;
 protected abstract void BuildNextAction();protected abstract void ExecuteAction(PlayerData p);
}
// 종 클래스 없이 데이터만으로 동작하는 일반 몬스터 검증용
public class TestDataMonster : DataMonster {}
