using System;
// 패널 스택 검증: 등록 순서 · ESC 해석(필수 항목 건너뜀) · 전투 조작 · 툴팁 허용(숨김)
class Program {
 static int count;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static UIModalEntry E(string name,UIModalKind kind=UIModalKind.Normal,Action close=null)=>new UIModalEntry(name,kind,close);

 static int Main(){
  Order();
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

 static void Escape(){
  var l=new UIModalLayer();
  Check(l.ResolveEscape(false)==UIEscapeAction.SceneDefault,"열린 패널이 없으면 씬 기본 동작");
  Check(l.ResolveEscape(true)==UIEscapeAction.CancelDrag,"드래그 중 ESC는 드래그 취소");
  l.Push(E("deck"));
  Check(l.ResolveEscape(false)==UIEscapeAction.CloseLatest&&(string)l.LatestClosable.Owner=="deck","일반 패널 ESC 닫기 오류");
  Check(l.ResolveEscape(true)==UIEscapeAction.CancelDrag,"드래그 취소가 패널 닫기보다 우선");
  // 덱 목록을 카드 선택으로 바꾸면 ESC로 닫히지 않고 설정 열기
  l.Top.Kind=UIModalKind.Required;
  Check(l.ResolveEscape(false)==UIEscapeAction.SceneDefault&&l.LatestClosable==null,"필수 항목만 있으면 씬 기본 동작");
  // 필수 선택 위에 연 설정 · 덱 보기는 최근 것부터 닫힘
  l.Push(E("settings"));
  Check(l.ResolveEscape(false)==UIEscapeAction.CloseLatest&&(string)l.LatestClosable.Owner=="settings","필수 위 일반 패널 닫기 오류");
  l.Clear();
  // 일반 패널이 열린 뒤 필수 선택이 생겨도 일반 패널을 닫음 (필수 항목 건너뜀)
  l.Push(E("map"));l.Push(E("select",UIModalKind.Required));
  Check((string)l.Top.Owner=="select"&&(string)l.LatestClosable.Owner=="map","필수 항목 건너뛰기 오류");
  l.Remove("map");
  Check(l.ResolveEscape(false)==UIEscapeAction.SceneDefault,"일반 패널을 닫은 뒤 씬 기본 동작");
 }

 static void BattleState(){
  var l=new UIModalLayer();
  Check(!l.BlocksBattleInput&&l.AllowsBattleTooltips,"빈 스택은 전투 조작 · 툴팁 허용");
  l.Push(E("deck"));
  Check(l.BlocksBattleInput&&!l.AllowsBattleTooltips,"패널이 있으면 전투 조작 · 툴팁 정지");
  l.Clear();
  // 필수 선택: 전투 조작 · 툴팁 정지, 숨긴 동안에만 툴팁 허용 (전투 조작은 계속 정지)
  var select=l.Push(E("select",UIModalKind.Required));
  Check(l.BlocksBattleInput&&!l.AllowsBattleTooltips,"필수 선택 중 툴팁 정지 오류");
  select.Hidden=true;
  Check(l.BlocksBattleInput&&l.AllowsBattleTooltips,"숨김 상태 툴팁 허용 오류");
  l.Push(E("settings"));
  Check(!l.AllowsBattleTooltips,"숨김 선택 위에 설정이 열리면 툴팁 정지");
 }
}
