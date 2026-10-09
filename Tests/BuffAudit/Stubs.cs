using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
 public class Coroutine {}
 public static class Random { public static int Range(int a,int b)=>a; }
 public static class Debug { public static void LogWarning(object v){} public static void Log(object v){} }
 public static class Mathf {
  public static int Abs(int n)=>Math.Abs(n); public static int Min(int a,int b)=>Math.Min(a,b); public static int Max(int a,int b)=>Math.Max(a,b); public static float Max(float a,float b)=>Math.Max(a,b);
  public static int FloorToInt(float v)=>(int)Math.Floor(v); public static int Clamp(int v,int a,int b)=>Math.Clamp(v,a,b);
 }
}
public enum CardCostType { Energy, Barrier, Rune }
public enum BattleTurnState { PlayerTurnStart, PlayerAction, PlayerTurnEnd, EnemyTurnStart, EnemyAction, EnemyTurnEnd, CombatEnd }
public enum MoveZoneType { DrawPile, DiscardPile, Source }
public enum WuppiModeState { Attack, Guard }
public class Card {
 public List<ICardEffect> effects=new(); public int cardId,cost; public CardCostType costType; public bool power;
 public Card CloneForRuntimeCopy()=>new(){cardId=cardId,cost=cost,costType=costType,power=power,effects=new(effects)};
 public void ApplyTemplate(Card c){cardId=c.cardId;cost=c.cost;effects=new(c.effects);}
 public System.Collections.IEnumerator PlaySequence(TrainingBattleManager b){
  var previous=b.battleContext.GetContextCards("ThisCard");
  try{b.battleContext.SetContextCards("ThisCard",new(){this});yield return CardEffectSequence.Execute(effects,b);}
  finally{b.battleContext.SetContextCards("ThisCard",previous);}
 }
 public void Play(TrainingBattleManager b)=>b.StartCoroutine(CardEffectSequence.Run(PlaySequence(b),b));
 public void ExecuteEndTurnInHandEffects(TrainingBattleManager b){} public void ExecuteKeepEffects(TrainingBattleManager b){}
 public bool ShouldExhaustAtTurnEnd()=>false; public bool ShouldRetainAtTurnEnd()=>false; public bool CanBePlayed()=>true;
 public bool HasKeyword(int id)=>id==CardKeywordIds.Power && power; public void AddKeyword(int k){} public void SetCost(int c,bool temporary)=>cost=c;
 public bool ShouldExhaustWhenPlayed()=>false; public bool ShouldLeaveCombatWhenPlayed()=>power;
}
public static class CardKeywordIds { public const int Power=1,Shadow=2,Ghost=3,Finale=4; }
public static class CardManager { public static Card GetCardAsCard(int id)=>new(){cardId=id}; }
public static class BuffCardUtility { public static bool IsFeatherCard(Card c)=>c!=null&&(c.cardId==203080||c.cardId==203081); public static bool IsPotionCard(Card c)=>c.cardId==101080; }
public static class CreamBuffRuntimeUtility { public const int ComboBuffId=3048,EnergyOverflowBuffId=3053; public static void SyncEnergyOverflow(PlayerData p){} }
public static class BuffValueUtility { public static float GetOutgoingDamageMultiplier(int id)=>0.75f; }
public static class WuppiModeRuntimeUtility {
 public static void HandleDirectBuffStackChange(TrainingBattleManager b,PlayerData p,int id){}
 public static bool IsMode(PlayerData p,WuppiModeState mode)=>p.mode==mode;
 public static void ChangeMode(TrainingBattleManager b,PlayerData p,WuppiModeState mode)=>p.mode=mode;
}
public partial class PlayerData {
 public bool IsDead()=>hp<=0; public int energy=3; public bool UseEnergy(int n){if(energy<n)return false;energy-=n;return true;}
 public int RemoveDefense(int n){int removed=Math.Min(defense,n);defense-=removed;return removed;}
 public float GetOutgoingDamageMultiplier()=>TrainingBattleManager.Instance.playerService.GetOutgoingDamageMultiplier();
 public List<Buff> currentBuffs=new(); public int hp=50,maxHP=100,defense,hpLostThisTurn; public bool hasLostHpThisTurn; public WuppiModeState mode=WuppiModeState.Guard;
 public void Heal(int n)=>hp=Math.Min(maxHP,hp+n);

}
public class Monster {
 public Dictionary<int,int> buffs=new(); public int hp=100,defense,maxHP=100; public void Heal(int n)=>hp=Math.Min(maxHP,hp+n); public int TakeDamage(int n,int unused=0){hp-=n;return n;}
 public int GetBuffStack(int id)=>buffs.GetValueOrDefault(id);
 public void AddBuff(int id,int amount)=>buffs[id]=GetBuffStack(id)+amount;
 public void ConsumeBuffStack(int id,int amount)=>buffs[id]=Math.Max(0,GetBuffStack(id)-amount);
 public bool IsDead()=>hp<=0;
}
public class MirrorMonster : Monster { }
public class Hand { public List<Card> GetHandCards()=>cards; public void RemoveCard(Card c)=>cards.Remove(c); public void RefreshCardDisplay(Card c){} public void RefreshCardDisplays(List<Card> c){} public List<Card> cards=new(); public void AddCard(Card c)=>cards.Add(TrainingBattleManager.Instance.ApplyHandCardUpgrades(c)); public void AddCard(List<Card> c){foreach(var card in c)AddCard(card);} }
public static class BuffCombatUtility {
 public static int stolen; public static void TransferPlayerMaxHpToMonster(PlayerData p,Monster m,int n)=>stolen+=n;
}
public partial class TrainingBattleManager {
 public FeatherBuffScript feather;
 public Card ApplyPersistentUpgradeToCard(Card card)=>ApplyPersistentUpgradeToCard(card,0);
 public void RefreshHandPlayableState(){}
 public UnityEngine.Coroutine StartCoroutine(System.Collections.IEnumerator e){while(e.MoveNext()){}return new();}
 public int ConsumeRepeatedPlayCount(Card c,bool repeat)=>playerService.ConsumeRepeatCount(c,repeat);
 public int GetCardUseAllEnemiesDamage()=>0;public void ApplyCardUseAllEnemiesDamage(int n){}
 public void HandlePlayedCardPowerEffects(Card c,Monster m,bool repeat)=>playerService.OnCardPlayed(c,m,repeat);
 public bool ShouldPotionGoToDiscardInsteadOfExhaust(Card c)=>false;public bool ShouldExhaustUnlockedUnplayableCard(Card c)=>false;
 public void MoveCardToExhaust(Card c){}public void RemoveCardFromCombat(Card c){} public void ForceEndPlayerTurn(){}
 public void ApplyBuffToAllEnemies(int id,int amount){if(feather.TryApplyToAllEnemies(id,amount))return;foreach(var m in GetLivingMonsters())ApplyBuffToMonster(m,id,amount);}

 public void ResolveAdditionalTurnEndTriggers()=>playerService.ReplayTurnEndTriggeredEffects(); public int GetTurnEndRetainCount()=>0; public void MoveCardsToExhaust(List<Card> c){} public bool HasPendingSelection=>false; public BattleBuffController battleBuffController; public static TrainingBattleManager Instance; public BattleTurnState CurrentTurnState;
 public Monster currentTarget; public BattleContext battleContext=new(); public Piles usableDeckManager=new();
 public List<Monster> spawnedMonsters=>monsters;
 public bool TryHandleCombatEnd()=>false;
 public int GetEffectiveCardCost(Card c)=>playerService.GetEffectiveCardCost(c);
 public int GetCardBaseDamageBonus(Card c,bool attack)=>playerService.GetCardBaseDamageBonus(c,attack);
 public int ApplyCardDamageRuntimeModifiers(Card c,int n)=>playerService.ModifyCardDamage(c,n);
 public int GetPlayerCalculatedCardDamageBonus(float n)=>playerService.GetCalculatedCardDamageBonus(n);
 public float GetPlayerCalculatedCardBaseMultiplier(float n)=>playerService.GetCalculatedCardBaseMultiplier(n);
 public int ResolvePlayerEffectDamage(int n)=>(int)(n*playerService.GetOutgoingDamageMultiplier());
 public int ResolvePlayerIncomingDamage(int n,Monster m)=>(int)(n*playerService.GetIncomingDamageMultiplier(m));
 public bool TryPreventPlayerIncomingDamage(int n,Monster m)=>playerService.TryPreventIncomingDamage(m,n);
 public void HandlePlayerHpLost(int n){playerService.OnPlayerHpLost(n);monsterService.OnPlayerHpLost(n);}
 public void TryConsumeSoulProtection()=>playerService.TryConsumeFatalDamage();
 public void ConsumePlayerIncomingDamageBuffs(Monster m,int n)=>playerService.ConsumeIncomingDamageBuff(m,n);
 public void HandlePlayerHit(Monster m,int blocked,int hp)=>playerService.OnPlayerHit(m,blocked,hp);
 public void HandlePlayerDamageDealt(Monster m,int n)=>playerService.OnPlayerDamageDealt(m,n);
 public void HandlePlayerAttackResolved(Monster m,int before,int after){monsterService.OnAttackedByPlayer(m);playerService.OnPlayerAttackResolved(m,before,after);}
 public void HandleEnemyDebuffApplied(Monster m,int id,int n,int previous)=>playerService.OnEnemyDebuffApplied(m,id,n,previous);
 public PlayerData playerData=new(); public Hand handManager=new(); public List<Monster> monsters=new();
 public int TurnSequence=1,draws,repeats; public bool duplicate,enhance;
 public PlayerBuffRuntimeService playerService; public MonsterBuffRuntimeService monsterService;
 public TrainingBattleManager(){Instance=this;playerService=new(this);battleBuffController=new(playerService,this);monsterService=new(this);feather=new(this,playerService);}
 public void DrawCards(int n){if(playerService.CanDrawCards())draws+=n;}
 public List<Monster> GetLivingMonsters()=>monsters.Where(m=>!m.IsDead()).ToList();
 public bool CanGainCardsToHand()=>playerService.CanGainCardsToHand();
 public List<Card> ProcessGeneratedCards(List<Card> cards,bool allow){
  var result=cards.Select(c=>{var copy=c.CloneForRuntimeCopy();copy.cardId+=(enhance?1:0);return copy;}).ToList();
  if(allow && duplicate)result.AddRange(result.Select(c=>c.CloneForRuntimeCopy()).ToList());return result;
 }
 public void UpdateAllUI(){}
 public void ApplyBuffToPlayer(int id,int n){playerData.AddBuff(id,n);battleBuffController.HandleBuffApplied(id);}
 public void AddTurnEndTriggerRepeat(int n)=>repeats+=n;
 public void ApplyBuffToMonster(Monster m,int id,int n){
  if(feather.TryApplyToMonster(id,m,n))return;
  m.AddBuff(id,n);monsterService.OnEnemyDebuffApplied(m,id,n,0);playerService.OnEnemyDebuffApplied(m,id,n,0);
 }
}

public class Piles { public List<Card> draw=new(),discard=new(),exhaust=new();public void AddToDiscard(List<Card> c)=>discard.AddRange(c);public void AddToDiscard(Card c)=>discard.Add(c);public List<Card> GetExhaustPile()=>exhaust;public List<Card> GetDrawPile()=>draw;public List<Card> GetDiscardPile()=>discard; }
public class BattleContext {
 public void OnCardsDiscarded(int n){} public int lastDamageDealt,totalDamageDealt; public Card card=new();
 public Dictionary<string,List<Card>> contexts=new(); public List<Card> selected=new();
 public Card GetContextCard(string s)=>GetContextCards(s)?.FirstOrDefault(); public Card GetLastPlayedCard()=>card;
 public List<Card> GetContextCards(string s)=>contexts.GetValueOrDefault(s); public void SetContextCards(string s,List<Card> c)=>contexts[s]=c; public void ClearContextCards(string s)=>contexts.Remove(s);public void OnCardPlayed(Card c)=>card=c;public List<Card> GetSelectedCards()=>selected;public void SetSelectedCards(List<Card> c)=>selected=c;
 public void OnDamageDealt(int n){lastDamageDealt=n;totalDamageDealt+=n;} public void OnPlayerCardHpLost(int n){} public void OnEnergySpent(int n){}
}
public static class FormulaEvaluator { public static int Evaluate(string s,BattleContext c,PlayerData p,Monster m,int n)=>n; public static int Evaluate(string s,BattleContext c,PlayerData p,List<int> ids,int n)=>n; }

public partial class TurnSystem {
 private TrainingBattleManager battleManager; private int pendingExtraTurnEndTriggers;
 public TurnSystem(TrainingBattleManager b,int repeats){battleManager=b;pendingExtraTurnEndTriggers=repeats;}
 public void FinishHand(){var e=DiscardRemainingHandCards();while(e.MoveNext()){} }
 private System.Collections.IEnumerator SelectTurnEndRetainCards(List<Card> cards,int n,List<Card> selected){yield break;}
}

public partial class BattleBuffController {
 private PlayerBuffRuntimeService playerBuffRuntimeService;
 public TrainingBattleManager battleManager; private MonsterBuffRuntimeService monsterBuffRuntimeService=>battleManager.monsterService;
 public BattleBuffController(PlayerBuffRuntimeService service,TrainingBattleManager manager){playerBuffRuntimeService=service;battleManager=manager;}
}

public static class BuffMetadataResolver { public static BuffData Resolve(int id)=>new(){buffId=id,name=id.ToString()}; }

public static class CardEffectRuntimeUtility { public static Monster ResolveSingleEnemyTarget(TrainingBattleManager b)=>b.currentTarget??b.GetLivingMonsters().FirstOrDefault(); }
