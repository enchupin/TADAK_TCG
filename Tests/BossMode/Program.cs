using System;
using System.Collections.Generic;

static class Program {
 static int passed;
 static void Check(bool ok,string label) { if(!ok) throw new Exception(label); passed++; }
 static void Main() {
  SelectionRules();
  Session();
  // 캐릭터 버튼 역할 아이콘: ID 백의 자리 (1xx 서포터 · 2xx 딜러 · 3xx 탱커)
  Check(CharacterRoles.FromCharacterId(101)==CharacterRole.Supporter&&CharacterRoles.FromCharacterId(103)==CharacterRole.Supporter,"서포터 판정");
  Check(CharacterRoles.FromCharacterId(201)==CharacterRole.Dealer&&CharacterRoles.FromCharacterId(204)==CharacterRole.Dealer,"딜러 판정");
  Check(CharacterRoles.FromCharacterId(301)==CharacterRole.Tank&&CharacterRoles.FromCharacterId(303)==CharacterRole.Tank,"탱커 판정");
  Check(CharacterRoles.FromCharacterId(0)==CharacterRole.None&&CharacterRoles.FromCharacterId(401)==CharacterRole.None&&CharacterRoles.FromCharacterId(1101)==CharacterRole.None,"역할 없는 ID");
  Console.WriteLine($"보스모드 동작 검증 {passed}개 통과");
 }

 // 캐릭터 · 덱 선택 규칙 (팝업 · 덱 하나 생략 · 3명 제한 · 취소)
 static void SelectionRules() {
  var state=new BossModeSelectionState();
  var two=new List<string>{"a","b"}; var one=new List<string>{"only"};
  bool valid(int id)=>BossModeSession.IsValidBoss(id);

  Check(state.Click(Character.Isla,two)==BossModeSelectionState.ClickResult.OpenDeckPopup&&state.PendingCharacter==Character.Isla,"덱 여러 개면 팝업");
  Check(!state.IsSelected(Character.Isla),"팝업 중에는 아직 선택 아님");
  state.CancelPending();
  Check(state.SelectedCount==0&&state.PendingCharacter==null,"팝업 취소 시 선택 취소");

  state.Click(Character.Isla,two); state.ChooseDeck("b");
  Check(state.TryGetDeckId(Character.Isla,out string isla)&&isla=="b"&&state.PendingCharacter==null,"팝업에서 덱 선택");
  Check(state.Click(Character.Isla,two)==BossModeSelectionState.ClickResult.OpenDeckPopup,"선택된 캐릭터 재클릭 = 덱 선택으로");
  state.ChooseDeck("a"); Check(state.TryGetDeckId(Character.Isla,out isla)&&isla=="a","덱 교체");
  state.Click(Character.Isla,two); state.CancelPending();
  Check(!state.IsSelected(Character.Isla),"재선택 팝업을 닫으면 선택 취소");

  Check(state.Click(Character.Jack,one)==BossModeSelectionState.ClickResult.SelectedDirectly&&state.TryGetDeckId(Character.Jack,out string jack)&&jack=="only","덱 하나면 팝업 생략");
  Check(state.Click(Character.Jack,one)==BossModeSelectionState.ClickResult.Deselected&&!state.IsSelected(Character.Jack),"덱 하나인 캐릭터 재클릭 = 해제");
  Check(state.Click(Character.Jack,new List<string>())==BossModeSelectionState.ClickResult.Ignored,"저장 덱 없음 무시");

  state.Click(Character.Isla,one); state.Click(Character.Jack,one); state.Click(Character.Mio,two); state.ChooseDeck("b");
  Check(state.SelectedCount==3,"세 명 선택");
  Check(state.Click(Character.Polar,two)==BossModeSelectionState.ClickResult.Ignored&&state.PendingCharacter==null,"네 번째 캐릭터 선택 불가");
  Check(state.Click(Character.Mio,two)==BossModeSelectionState.ClickResult.OpenDeckPopup,"세 명 선택 후에도 고른 캐릭터 덱 변경 가능");
  state.ChooseDeck("a");
  Check(!state.ChooseDeck("x"),"팝업 없이 덱 선택 무시");

  Check(!state.CanStart(0,valid),"보스 미선택 시작 불가");
  Check(!state.CanStart(9901,valid),"지원하지 않는 보스 시작 불가");
  Check(state.CanStart(301,valid),"세 명 + 보스 시작 가능");

  state.RemoveMissing((character,deckId)=>character!=Character.Mio);
  Check(state.SelectedCount==2&&!state.CanStart(301,valid),"삭제된 덱 선택 제거");
  state.Clear(); Check(state.SelectedCount==0&&state.PendingCharacter==null,"패널 재진입 초기화");
 }

 static void Session() {
  BossModeSession.Begin(new(){Character.Mio,Character.Isla,Character.Jack},new(){101,1102,204},302);
  Check(BossModeSession.IsActive,"전투 세션 시작");
  Check(BossModeSession.SelectedBossId==302,"선택 보스 전달");
  Check(SelectedButtonControl.selectedCharacterList.Count==3&&SelectedButtonControl.selectedCharacterList[0]==Character.Isla,"캐릭터 ID 순서 배치");
  Check(BossModeSession.CreateDeck().Cards.Contains(1102),"선택한 저장덱 사용");
  Check(TrainingRunDeckPersistence.PermanentBonusResets==1,"영구 강화 초기화");
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
  // v1.0 보스는 조우 BS01 ~ BS04로 생성 (원본 ID 301 ~ 304와 v1.0 ID 5301 ~ 5304 모두 허용)
  Check(BossModeSession.GetBossEncounterId(301)=="BS01","아라크네 생성 타입");
  Check(BossModeSession.GetBossEncounterId(302)=="BS02","선지자 생성 타입");
  Check(BossModeSession.GetBossEncounterId(303)=="BS03","얼음과 불 생성 타입");
  Check(BossModeSession.GetBossEncounterId(304)=="BS04","공허 군주 생성 타입");
  Check(BossModeSession.GetBossEncounterId(5302)=="BS02"&&!BossModeSession.IsValidBoss(305),"v1.0 보스 ID와 잘못된 ID 처리");
 }
}
