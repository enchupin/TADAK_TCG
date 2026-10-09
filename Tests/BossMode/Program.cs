using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UI;

static class Program {
 static int passed;
 static void Check(bool ok,string label) { if(!ok) throw new Exception(label); passed++; }
 static void Field(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
 static void Main() {
  var panel=new DictionarySavedDeckPanel(); var button=new Button(); var selection=new BossModeDeckSelection();
  Field(selection,"savedDeckPanel",panel); Field(selection,"startButton",button);
  typeof(BossModeDeckSelection).GetMethod("OnEnable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(selection,null);
  Check(!button.interactable,"미선택 시작 불가");
  selection.SelectBoss(301); Check(!button.interactable,"보스만 선택하면 시작 불가");
  selection.SelectBoss(301);
  foreach(Character c in new[]{Character.Isla,Character.Jack,Character.Mio,Character.Polar}) {
   var lib=new Library(); lib.decks.Add(CharacterDeckSave.Create("기본",new(){(int)c}));
   lib.decks.Add(CharacterDeckSave.Create("대체",new(){(int)c+1000})); ProfileSaveManager.CurrentProfile.Libraries[(int)c]=lib;
  }
  void Show(Character c) {panel.Character=c;panel.Decks=ProfileSaveManager.CurrentProfile.FindLibrary((int)c).decks;panel.Refresh();}
  Show(Character.Isla); selection.ToggleDeck(0); selection.ToggleDeck(1);
  Show(Character.Jack); selection.ToggleDeck(0);
  Check(!button.interactable,"같은 캐릭터 덱 교체는 한 자리만 사용");
  Show(Character.Mio); selection.ToggleDeck(0);
  Check(!button.interactable,"덱 세 개만 선택하면 시작 불가");
  selection.StartBattle(); Check(!BossModeSession.IsActive,"보스 미선택 시 직접 시작 호출 차단");
  selection.SelectBoss(9901); Check(!button.interactable,"지원하지 않는 보스 선택 차단");
  selection.SelectBoss(301); Check(button.interactable,"서로 다른 세 캐릭터 선택");
  selection.SelectBoss(302); Check(button.interactable,"선택 보스 교체");
  Show(Character.Polar); selection.ToggleDeck(0);
  selection.StartBattle(); Check(BossModeSession.IsActive,"전투 세션 시작");
  Check(BossModeSession.SelectedBossId==302,"선택 보스 전달");
  Check(SelectedButtonControl.selectedCharacterList.Count==3 && !SelectedButtonControl.selectedCharacterList.Contains(Character.Polar),"네 번째 캐릭터 선택 제한");
  Check(BossModeSession.CreateDeck().Cards.Contains(1101) && !BossModeSession.CreateDeck().Cards.Contains(101),"교체한 저장덱 사용");
  panel.Decks[0].cardIds.Clear(); Check(BossModeSession.CreateDeck().Cards.Count==3,"전투 덱 스냅샷 유지");
  Check(UnityEngine.SceneManagement.SceneManager.Loaded=="CombatScene","맵 없이 전투 씬 진입");
  BossModeSession.AddDamage(20);BossModeSession.AddDamage(-1);Check(BossModeSession.TotalDamage==20,"피해만 누적");
  ProfileSaveManager.Fail=true;BossModeSession.SaveResult();Check(ProfileSaveManager.Saves==0,"저장 실패 처리");
  ProfileSaveManager.Fail=false;BossModeSession.SaveResult();BossModeSession.SaveResult();
  Check(ProfileSaveManager.Saves==1 && ProfileSaveManager.CurrentProfile.bossModeLastDamage==20,"재시도 및 중복 저장 방지");
  BossModeSession.AddDamage(9);Check(BossModeSession.TotalDamage==20,"종료 후 피해 변경 방지");
  BossModeSession.Begin(new(){Character.Isla,Character.Jack,Character.Mio},new(){101,102,204},304);
  Check(BossModeSession.TotalDamage==0,"새 전투 피해 초기화");
  BossModeSession.AddDamage(10);BossModeSession.SaveResult();Check(ProfileSaveManager.CurrentProfile.bossModeBestDamage==20,"최고 기록 보존");
  BossModeSession.Reset();Check(!BossModeSession.IsActive,"훈련 복귀 상태 초기화");
  Check(BossModeSession.SelectedBossId==0,"선택 보스 초기화");
  Check(BossModeSession.GetBossType(301)==typeof(GiantFlowerSpiderBossMonster),"아라크네 생성 타입");
  Check(BossModeSession.GetBossType(302)==typeof(ProphetBossMonster),"선지자 생성 타입");
  Check(BossModeSession.GetBossType(303)==typeof(IceAndFireBossMonster),"얼음과 불 생성 타입");
  Check(BossModeSession.GetBossType(304)==typeof(VoidLordBossMonster),"공허 군주 생성 타입");
  typeof(BossModeDeckSelection).GetMethod("OnEnable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(selection,null);
  Check(!button.interactable,"패널 재진입 시 선택 초기화");
  selection.SelectBoss(303);
  Show(Character.Isla);selection.ToggleDeck(0);Show(Character.Jack);selection.ToggleDeck(0);Show(Character.Mio);selection.ToggleDeck(0);
  selection.ToggleDeck(0);Check(!button.interactable,"재클릭 선택 해제");selection.ToggleDeck(0);panel.Decks.Clear();panel.Refresh();Check(!button.interactable,"선택된 덱 삭제 시 시작 취소");
  Console.WriteLine($"보스모드 동작 검증 {passed}개 통과");
 }
}
