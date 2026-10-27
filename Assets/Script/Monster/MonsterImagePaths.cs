/// <summary>
/// 몬스터 이미지 이름 · 상태별 Resources 경로 규칙
/// 상태 컨트롤러 · 여백 측정 도구 · 검증 테스트가 같은 규칙을 쓰도록 Unity 의존 없이 분리
/// </summary>
public static class MonsterImagePaths
{
    public const string ImageResourceFolder = "Image/Monster";
    public const string BossImageResourceFolder = "Image/BossMonster";
    public const string IdleImageSuffix = "Idle";
    public const string AttackImageSuffix = "Attack";
    public const string HitImageSuffix = "Hit";

    public static readonly string[] StateSuffixes = { IdleImageSuffix, AttackImageSuffix, HitImageSuffix };

    // 클래스 이름 끝의 Monster를 떼어 이미지 이름으로 사용
    public static string NormalizeImageName(string imageName)
    {
        const string monsterSuffix = "Monster";

        if (string.IsNullOrWhiteSpace(imageName) || !imageName.EndsWith(monsterSuffix, System.StringComparison.Ordinal))
        {
            return imageName;
        }

        return imageName.Substring(0, imageName.Length - monsterSuffix.Length);
    }

    public static string BuildResourcePath(string imageName, string stateSuffix)
    {
        if (string.IsNullOrWhiteSpace(imageName))
        {
            return string.Empty;
        }

        // 보스는 별도 폴더의 이미지 이름을 사용
        string bossImagePath = imageName switch
        {
            "GiantFlowerSpiderBoss" => $"{BossImageResourceFolder}/Arachne/Arachne",
            "ProphetBoss" => $"{BossImageResourceFolder}/Prophet/Prophet",
            "IceAndFireBoss" => $"{BossImageResourceFolder}/IceAndFire/IceAndFire",
            "VoidLordBoss" => $"{BossImageResourceFolder}/VoidLord/VoidLord",
            _ => null
        };
        if (bossImagePath != null)
        {
            return $"{bossImagePath}_{stateSuffix}";
        }

        return $"{ImageResourceFolder}/{imageName}_{stateSuffix}";
    }
}
