using System;
// 패널 스택 검증: 등록 순서 · 항상 위 패널 · ESC 해석 · 전투 조작 · 툴팁 허용
class Program {
 static int count;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static UIModalEntry E(string name,UIModalKind kind=UIModalKind.Normal,bool top=false,bool tooltips=false,Action close=null)=>new UIModalEntry(name,kind,top,tooltips,close);

 static int Main(){
  Order();
  AlwaysOnTop();
  Escape();
  BattleState();
  Console.WriteLine($"패널 스택 검증 {count}개 통과");
  return 0;
 }

 static void Order(){
  var l=new UIModalLayer();
  Check(!l.HasAny&&l.Top==null,"빈 스택 오류");
  l.Push(E("deck"));l.Push(E("detail"));
  Check(l.Count==2&&(string)l.Top.Owner=="detail","늦게 열린 패널이 맨 위가 아님");
  // 같은 패널 재등록은 순서를 바꾸지 않음
  var again=l.Push(E("deck"));
  Check(l.Count==2&&(string)l.Top.Owner=="detail"&&(string)again.Owner=="deck","중복 등록 오류");
  Check(l.Remove("detail")&&(string)l.Top.Owner=="deck","맨 위 해제 오류");
  Check(!l.Remove("detail"),"없는 패널 해제 오류");
  l.Push(E("a"));
  Check(l.Remove("deck")&&(string)l.Top.Owner=="a"&&l.Count==1,"중간 패널 해제 오류");
 }

 static void AlwaysOnTop(){
  // C1: 설정이 열린 뒤 카드 효과로 선택 패널이 떠도 설정이 맨 위
  var l=new UIModalLayer();
  l.Push(E("settings",top:true));
  l.Push(E("select",UIModalKind.Required));
  Check((string)l.Top.Owner=="settings"&&(string)l.Entries[0].Owner=="select","항상 위 패널 아래로 끼워 넣기 오류");
  Check(l.ResolveEscape(false)==UIEscapeAction.CloseTop,"설정이 맨 위면 ESC로 닫혀야 함");
  l.Remove("settings");
  Check(l.ResolveEscape(false)==UIEscapeAction.Ignore,"설정을 닫은 뒤 필수 패널은 ESC 무시");
  // 항상 위 패널끼리는 등록 순서
  l.Push(E("t1",top:true));l.Push(E("t2",top:true));l.Push(E("n"));
  Check((string)l.Top.Owner=="t2"&&(string)l.Entries[1].Owner=="n","항상 위 패널 순서 오류");
 }

 static void Escape(){
  var l=new UIModalLayer();
  Check(l.ResolveEscape(false)==UIEscapeAction.SceneDefault,"열린 패널이 없으면 씬 기본 동작");
  Check(l.ResolveEscape(true)==UIEscapeAction.CancelDrag,"드래그 중 ESC는 드래그 취소");
  l.Push(E("deck"));
  Check(l.ResolveEscape(false)==UIEscapeAction.CloseTop,"일반 패널 ESC 닫기 오류");
  Check(l.ResolveEscape(true)==UIEscapeAction.CancelDrag,"드래그 취소가 패널 닫기보다 우선");
  // 덱 목록을 카드 선택으로 바꾸면 ESC 무시
  l.Top.Kind=UIModalKind.Required;
  Check(l.ResolveEscape(false)==UIEscapeAction.Ignore,"필수 전환 후 ESC 무시 오류");
  l.Push(E("detail"));
  Check(l.ResolveEscape(false)==UIEscapeAction.CloseTop,"필수 패널 위의 일반 패널은 닫힘");
 }

 static void BattleState(){
  var l=new UIModalLayer();
  Check(!l.BlocksBattleInput&&l.AllowsBattleTooltips,"빈 스택은 전투 조작 · 툴팁 허용");
  l.Push(E("deck"));
  Check(l.BlocksBattleInput&&!l.AllowsBattleTooltips,"패널이 있으면 전투 조작 · 툴팁 정지");
  l.Clear();
  // 몬스터 직접 선택: 전투 조작은 정지, 버프 · 의도 툴팁은 허용
  l.Push(E("party",UIModalKind.Required,tooltips:true));
  Check(l.BlocksBattleInput&&l.AllowsBattleTooltips,"몬스터 선택 중 툴팁 허용 오류");
  l.Push(E("settings",top:true));
  Check(!l.AllowsBattleTooltips,"설정이 맨 위면 툴팁 정지");
 }
}
