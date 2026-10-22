using System;
// 툴팁 고정 위치 계산 · 리치 텍스트 강조 검증
class Program {
 static int count;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static void Near(float actual,float expected,string message)=>Check(Math.Abs(actual-expected)<0.001f,$"{message} (기대 {expected}, 실제 {actual})");
 static void Main(){
  Layout();
  RichText();
  Console.WriteLine($"툴팁 검증 {count}개 통과");
 }

 static void Layout(){
  // 화면 -400 ~ 400, -300 ~ 300 (캔버스 중앙 피벗)
  var screen=new TooltipRect(-400,-300,400,300);
  var icon=new TooltipRect(-100,100,-60,140);

  // 버프: 아이콘 하단, 좌측 맞춤
  var (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Bottom,icon,200,80,4,screen,8);
  Near(x,-100,"하단 배치 좌측 맞춤");
  Near(y,96,"하단 배치 간격");

  // 의도: 영역 우측, 상단 맞춤
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Right,icon,200,80,6,screen,8);
  Near(x,-54,"우측 배치 간격");
  Near(y,140,"우측 배치 상단 맞춤");

  // 화면 오른쪽을 넘으면 방향은 유지하고 안으로 밀어 넣음
  var rightEdge=new TooltipRect(300,0,350,40);
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Right,rightEdge,200,80,6,screen,8);
  Near(x,192,"우측 경계 클램프");
  Near(y,40,"우측 경계에서 세로 위치 유지");

  // 화면 아래를 넘으면 위로 밀어 넣음
  var bottomEdge=new TooltipRect(-100,-280,-60,-240);
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Bottom,bottomEdge,200,80,4,screen,8);
  Near(y,-212,"하단 경계 클램프");

  // 화면 위를 넘으면 아래로
  var topEdge=new TooltipRect(-100,250,-60,320);
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Right,topEdge,200,80,6,screen,8);
  Near(y,292,"상단 경계 클램프");

  // 화면 왼쪽을 넘으면 오른쪽으로
  var leftEdge=new TooltipRect(-420,0,-380,40);
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Bottom,leftEdge,200,80,4,screen,8);
  Near(x,-392,"좌측 경계 클램프");

  // 툴팁이 화면보다 크면 좌측 · 상단 우선
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Bottom,icon,1000,900,4,screen,8);
  Near(x,-392,"큰 툴팁 좌측 우선");
  Near(y,292,"큰 툴팁 상단 우선");

  // 뒤집힌 좌표도 정규화
  var flipped=new TooltipRect(-60,140,-100,100);
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Bottom,flipped,200,80,4,screen,8);
  Near(x,-100,"사각형 정규화 x");
  Near(y,96,"사각형 정규화 y");
 }

 static void RichText(){
  var colors=TooltipColors.Default;
  Check(colors.BeneficialHex=="26619C"&&colors.HarmfulHex=="FF2400"&&colors.IntentHex=="808000"&&colors.ValueHex=="FBCEB1","기본 색상 라피스 · 스칼렛 · 올리브 · 아프리콧");
  Check(TooltipRichText.EffectName("힘",false,colors)=="<color=#26619C>힘</color>","이로운 효과 이름 라피스");
  Check(TooltipRichText.EffectName("부식",true,colors)=="<color=#FF2400>부식</color>","해로운 효과 이름 스칼렛");
  Check(TooltipRichText.IntentName("공격",colors)=="<color=#808000>공격</color>","의도 이름 올리브");
  Check(TooltipRichText.Value(3,colors)=="<color=#FBCEB1>3</color>","수치 아프리콧");
  Check(TooltipRichText.Value(-2,colors)=="<color=#FBCEB1>-2</color>","음수 수치");
  Check(TooltipRichText.Colorize("",colors.ValueHex)==""&&TooltipRichText.Colorize(null,colors.ValueHex)=="","빈 문자열은 태그 없음");
  Check(TooltipRichText.Colorize("힘",null)=="힘","색 없으면 원문");
 }
}
