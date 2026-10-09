using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
 public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b); public static int Min(int a,int b)=>Math.Min(a,b); }
 public static class Random { public static int Range(int a,int b)=>a; }
}
public static class BattleRuntimeDefinitions {
 public const int HarmonyBuffId=5014;
 public const int EightLegsBuffId=5009,FuturePredationBuffId=5010,VoidShellBuffId=5002,StrengthBuffId=3002,AttackBoostBuffId=3009,WeakBuffId=4008,CorrosionBuffId=4009,FrailBuffId=4011;
}
public enum MonsterIntentIconType { Attack,HarmfulEffect,BeneficialEffect,DisruptCard,Protection,Heal,Stun }
public class BuffData { public int buffId; }
public class Buff { public BuffData data=new(); public int stack; }
public class Card { public int cardId; }
public static class CardManager { public static Card GetCardAsCard(int id)=>new(){cardId=id}; }
public class PlayerData {
 public int hp=10000; public List<int> hits=new(); public Dictionary<int,int> buffs=new();
 public bool IsDead()=>hp<=0;
}
public class Piles {
 public List<Card> draw=new(),discard=new();
 public void AddToDrawPileRandom(Card c)=>draw.Add(c);
 public void AddToDiscard(Card c)=>discard.Add(c);
}
public class Hand { public List<Card> cards=new(); public void AddCard(Card c)=>cards.Add(c); }
public class TrainingBattleManager {
 public static TrainingBattleManager Instance=new(); public PlayerData playerData=new();
 public Piles usableDeckManager=new();public Hand handManager=new();public List<Monster> monsters=new();
 public MonsterBuffs.FuturePredationBuffScript predation=new();
 public MonsterBuffs.HarmonyBuffScript harmony=new();
 public MonsterBuffs.AttackBoostBuffScript attackBoost=new();
 public int GetMonsterBuffActivationCount(Monster m,int id)=>predation.GetActivationCount(m);
 public List<Monster> GetLivingMonsters()=>monsters.Where(m=>!m.IsDead()).ToList();
 public void UpdateAllUI(){} public void RefreshHandPlayableState(){}
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
 public abstract int MonsterId{get;} protected abstract string MonsterName{get;} protected abstract int BaseMaxHp{get;}
 protected virtual bool IsBossMonster=>false; private int permanentDefense; private bool attack;
 public int hp,defense,pattern;public Dictionary<int,int> buffs=new();
 public void StartBattle(){hp=BaseMaxHp;OnBattleStart();}
 public void Plan()=>BuildNextAction(); public void Act(){var service=TrainingBattleManager.Instance;bool wasAttack=attack;if(wasAttack)service.attackBoost.OnAttackActionStarted(this,GetBuffStack(BattleRuntimeDefinitions.AttackBoostBuffId));ExecuteAction(service.playerData);if(wasAttack)service.attackBoost.OnAttackActionEnded(this);attack=false;}
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
 protected int PreviewOutgoingDamage(int n)=>n+GetBuffStack(BattleRuntimeDefinitions.StrengthBuffId)+TrainingBattleManager.Instance.attackBoost.GetOutgoingDamageFlatBonus(null,this,GetBuffStack(BattleRuntimeDefinitions.AttackBoostBuffId),0);
 protected int PreviewBarrierGain(int n)=>n;
 protected int PreviewMonsterBuffAmount(int id,int n)=>n;
 protected int PreviewPlayerDebuffAmount(int id,int n)=>n;
 protected int DealDamage(PlayerData p,int n){int d=PreviewOutgoingDamage(n);p.hp-=d;p.hits.Add(d);TrainingBattleManager.Instance.harmony.OnMonsterAttackResolved(null,this,p,d,d,GetBuffStack(BattleRuntimeDefinitions.HarmonyBuffId));return d;}
 protected void AddDebuffToPlayer(PlayerData p,int id,int n)=>p.buffs[id]=p.buffs.GetValueOrDefault(id)+n;
 protected void AddCardToPlayerDiscard(Card c)=>TrainingBattleManager.Instance.usableDeckManager.AddToDiscard(c);
 protected void SetAttackIntent(int n,string s){attack=true;} protected void SetIntent(string s){attack=false;}
 protected void SetPlannedPattern(int id,params MonsterIntentIconType[] icons)=>pattern=id;
 protected virtual void OnBattleStart(){} protected virtual void OnTurnEnded(){}
 protected virtual void OnDefenseChanged(int oldValue,int newValue){}
 protected virtual bool CanReceiveDamage(int n)=>true;
 protected abstract void BuildNextAction();protected abstract void ExecuteAction(PlayerData p);
}
