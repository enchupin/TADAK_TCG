/// <summary>
/// 루트 캔버스 정렬 순서 (같은 값의 캔버스가 함께 켜지면 위아래가 계층 · 활성 순서에 의존하므로 명시)
/// </summary>
public static class UICanvasOrder
{
    // 전투 · 로비 등 씬 기본 화면
    public const int Scene = 0;
    // 설정 (DontDestroyOnLoad 시스템 캔버스)
    public const int Settings = 10;
}
