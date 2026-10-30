using System;
// 화면비 검증: 16:9 영역 고정(contain) · 배경 확대(cover) · 1920×1080 Expand 기준 캔버스 크기
class Program {
 static int count;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static bool Near(float a,float b)=>Math.Abs(a-b)<0.5f;
 const float Wide=16f/9f;

 static int Main(){
  Contain();
  Cover();
  CanvasSizes();
  Console.WriteLine($"화면비 검증 {count}개 통과");
  return 0;
 }

 static void Contain(){
  AspectLayoutMath.Contain(1920,1080,Wide,out float w,out float h);
  Check(Near(w,1920)&&Near(h,1080),"16:9 화면에서 16:9 영역은 화면 전체");
  AspectLayoutMath.Contain(2560,1080,Wide,out w,out h);
  Check(Near(w,1920)&&Near(h,1080),"21:9 화면에서 16:9 영역은 높이 기준 1920×1080");
  AspectLayoutMath.Contain(1920,1200,Wide,out w,out h);
  Check(Near(w,1920)&&Near(h,1080),"세로가 긴 화면에서는 폭 기준");
  AspectLayoutMath.Contain(0,1080,Wide,out w,out h);
  Check(w==0&&h==1080,"크기가 없으면 그대로");
 }

 static void Cover(){
  // 16:9 배경을 21:9 화면에 비율 유지로 덮으면 위아래로 넘침
  AspectLayoutMath.Cover(2560,1080,Wide,out float w,out float h);
  Check(Near(w,2560)&&Near(h,1440),"21:9 화면 16:9 배경은 2560×1440");
  AspectLayoutMath.Cover(1920,1080,Wide,out w,out h);
  Check(Near(w,1920)&&Near(h,1080),"16:9 화면 16:9 배경은 그대로");
  // 21:9 배경을 16:9 화면에 덮으면 좌우로 넘침
  AspectLayoutMath.Cover(1920,1080,64f/27f,out w,out h);
  Check(Near(h,1080)&&w>1920,"16:9 화면 21:9 배경은 좌우로 넘침");
  AspectLayoutMath.Cover(1920,1080,0,out w,out h);
  Check(w==1920&&h==1080,"비율이 없으면 그대로");
 }

 // CanvasScaler Scale With Screen Size 1920×1080 · Expand와 같은 계산
 static void ExpandCanvas(float screenW,float screenH,out float w,out float h){
  float scale=Math.Min(screenW/1920f,screenH/1080f);
  w=screenW/scale;h=screenH/scale;
 }

 static void CanvasSizes(){
  // 설정의 지원 해상도: 16:9는 1920×1080 단위, 21:9는 높이 1080을 공유하고 가로만 넓어짐
  foreach(var r in new[]{(1280f,720f),(1366f,768f),(1920f,1080f),(2560f,1440f)}){
   ExpandCanvas(r.Item1,r.Item2,out float w,out float h);
   // 1366×768은 정확한 16:9가 아니라 가로가 1단위 넓음
   Check(Math.Abs(w-1920)<2&&Near(h,1080),$"{r.Item1}×{r.Item2} 캔버스는 1920×1080 단위");
  }
  ExpandCanvas(2560,1080,out float ww,out float wh);
  Check(Near(ww,2560)&&Near(wh,1080),"2560×1080 캔버스는 2560×1080 단위");
  AspectLayoutMath.Contain(ww,wh,Wide,out float fw,out float fh);
  Check(Near(fw,1920)&&Near(fh,1080)&&Near((ww-fw)/2,320),"21:9 중앙 16:9 프레임 좌우 여백 320");
 }
}
