using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum MonsterStateType
{
    Idle,
    Attack,
    Hit
}

/// <summary>
/// 몬스터의 연출 상태와 상태별 효과음을 관리합니다
/// </summary>
[DisallowMultipleComponent]
public class MonsterStateController : MonoBehaviour
{
    private const string ImageResourceFolder = "Image/Monster";
    private const string IdleImageSuffix = "Idle";
    private const string AttackImageSuffix = "Attack";
    private const string HitImageSuffix = "Hit";

    [Header("상태 애니메이션")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleTriggerName = "Idle";
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string hitTriggerName = "Hit";
    [SerializeField] private bool autoReturnToIdle = true;
    [SerializeField] private float attackStateDuration = 0.35f;
    [SerializeField] private float hitStateDuration = 0.25f;

    [Header("상태 이미지")]
    // 스폰 위치별 몬스터 이미지
    // 스폰 위치 목록과 같은 순서로 각 MonsterImage의 MonsterImageUI를 연결
    // 별도 컴포넌트 없이 Image를 직접 연결하도록 변경
    // 스폰 위치 목록과 같은 순서로 각 MonsterImage의 Image를 연결
    // 각 슬롯에 배치한 프리팹 내부 이미지를 직접 연결
    [Tooltip("스폰 위치의 몬스터 이미지")]
    public Image monsterImage;
    [SerializeField] private string monsterImageNameOverride;
    [SerializeField] private bool preserveImageAspect = true;
    [SerializeField] private bool logMissingImage = true;

    [Header("스폰 위치별 체력 UI")]
    // 스폰 위치 목록과 같은 순서로 각 HPSlider를 연결
    // 각 HPSlider의 Fill 이미지를 같은 순서로 연결
    // 슬롯별 목록 대신 프리팹 내부의 체력바와 Fill 이미지를 연결
    public Slider hpSlider;
    public Image hpFillImage;

    [Header("스폰 위치별 버프 UI")]
    // 스폰 위치 목록과 같은 순서로 각 Buff 오브젝트의 BuffUI를 연결
    // 몬스터 프리팹의 부모인 스폰 위치에 미리 배치된 버프 슬롯을 연결
    // 스포너가 인스펙터에 연결된 참조를 전달하며 이름으로 검색하지 않음
    // 프리팹 내부의 BuffUI와 버프 이미지 순서를 인스펙터에 연결
    public BuffUI buffUI;

    [Header("상태 효과음")]
    [SerializeField, HideInInspector] private AudioClip attackSound;
    [SerializeField, HideInInspector] private AudioClip hitSound;
    [SerializeField] private float attackSoundVolumeScale = 1f;
    [SerializeField] private float hitSoundVolumeScale = 1f;

    private Coroutine returnToIdleCoroutine;
    private string monsterImageName;
    private bool hasLoggedMissingImageTarget;
    private readonly Dictionary<MonsterStateType, Sprite> stateSpriteCache = new();

    public MonsterStateType CurrentState { get; private set; } = MonsterStateType.Idle;

    private void Awake()
    {
        CacheAnimator();
    }

    private void OnEnable()
    {
        if (monsterImage != null)
        {
            ApplyStateImage(CurrentState);
        }
    }

    private void OnDisable()
    {
        StopReturnToIdle();
        if (monsterImage != null)
        {
            monsterImage.enabled = false;
            monsterImage.raycastTarget = false;
            monsterImage.sprite = null;
        }
    }

    public void EnterIdle()
    {
        ChangeState(MonsterStateType.Idle, idleTriggerName, null, 1f, 0f);
    }

    public void EnterAttack()
    {
        ChangeState(MonsterStateType.Attack, attackTriggerName, attackSound, attackSoundVolumeScale, attackStateDuration);
    }

    public void EnterHit()
    {
        ChangeState(MonsterStateType.Hit, hitTriggerName, hitSound, hitSoundVolumeScale, hitStateDuration);
    }

    public void SetMonsterImageName(string imageName)
    {
        string resolvedImageName = !string.IsNullOrWhiteSpace(monsterImageNameOverride)
            ? monsterImageNameOverride
            : NormalizeMonsterImageName(imageName);

        if (monsterImageName == resolvedImageName)
        {
            return;
        }

        monsterImageName = resolvedImageName;
        stateSpriteCache.Clear();
        ApplyStateImage(CurrentState);
    }

    private static string NormalizeMonsterImageName(string imageName)
    {
        const string monsterSuffix = "Monster";

        if (string.IsNullOrWhiteSpace(imageName) || !imageName.EndsWith(monsterSuffix, System.StringComparison.Ordinal))
        {
            return imageName;
        }

        return imageName.Substring(0, imageName.Length - monsterSuffix.Length);
    }

    private void ChangeState(MonsterStateType nextState, string triggerName, AudioClip sound, float volumeScale, float idleReturnDelay)
    {
        CacheAnimator();
        StopReturnToIdle();

        CurrentState = nextState;
        ApplyStateImage(nextState);
        SetAnimatorTrigger(triggerName);
        PlayStateSound(sound, volumeScale);

        if (nextState != MonsterStateType.Idle)
        {
            StartReturnToIdle(idleReturnDelay);
        }
    }

    private void ApplyStateImage(MonsterStateType state)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (monsterImage == null)
        {
            LogMissingImageTarget();
            return;
        }

        Sprite stateSprite = LoadStateSprite(state);
        if (stateSprite == null)
        {
            return;
        }

        monsterImage.sprite = stateSprite;
        monsterImage.preserveAspect = preserveImageAspect;
        monsterImage.enabled = true;
        monsterImage.raycastTarget = true;
    }

    private Sprite LoadStateSprite(MonsterStateType state)
    {
        if (stateSpriteCache.TryGetValue(state, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        string resourcePath = BuildStateImageResourcePath(state);
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);
        if (loadedSprite == null)
        {
            loadedSprite = LoadSpriteFromMultipleResource(resourcePath);
        }

        if (loadedSprite == null)
        {
            LogMissingStateImage(resourcePath);
            return null;
        }

        stateSpriteCache[state] = loadedSprite;
        return loadedSprite;
    }

    private string BuildStateImageResourcePath(MonsterStateType state)
    {
        if (string.IsNullOrWhiteSpace(monsterImageName))
        {
            return string.Empty;
        }

        // 보스도 일반 몬스터와 같은 상태 전환과 스프라이트 캐시를 사용
        string bossImagePath = monsterImageName switch
        {
            "GiantFlowerSpiderBoss" => "Image/BossMonster/Arachne/Arachne",
            "ProphetBoss" => "Image/BossMonster/Prophet/Prophet",
            "IceAndFireBoss" => "Image/BossMonster/IceAndFire/IceAndFire",
            "VoidLordBoss" => "Image/BossMonster/VoidLord/VoidLord",
            _ => null
        };
        if (bossImagePath != null)
        {
            return $"{bossImagePath}_{GetStateImageSuffix(state)}";
        }

        string fileName = $"{monsterImageName}_{GetStateImageSuffix(state)}";
        if (string.IsNullOrWhiteSpace(ImageResourceFolder))
        {
            return fileName;
        }

        return $"{ImageResourceFolder.TrimEnd('/')}/{fileName}";
    }

    private string GetStateImageSuffix(MonsterStateType state)
    {
        switch (state)
        {
            case MonsterStateType.Attack:
                return AttackImageSuffix;
            case MonsterStateType.Hit:
                return HitImageSuffix;
            default:
                return IdleImageSuffix;
        }
    }

    private void LogMissingStateImage(string resourcePath)
    {
        if (!logMissingImage)
        {
            return;
        }

        Debug.LogWarning($"[MonsterStateController] 상태 이미지 리소스를 찾을 수 없습니다. path={resourcePath}", this);
    }

    private void LogMissingImageTarget()
    {
        if (hasLoggedMissingImageTarget)
        {
            return;
        }

        hasLoggedMissingImageTarget = true;
        Debug.LogWarning("[MonsterStateController] 상태 이미지를 적용할 Image 또는 SpriteRenderer가 연결되지 않았습니다", this);
    }

    private static Sprite LoadSpriteFromMultipleResource(string resourcePath)
    {
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>(resourcePath);
        if (loadedSprites == null || loadedSprites.Length == 0)
        {
            return null;
        }

        string fileName = ExtractResourceFileName(resourcePath);
        foreach (Sprite loadedSprite in loadedSprites)
        {
            if (loadedSprite != null && loadedSprite.name.StartsWith(fileName, System.StringComparison.Ordinal))
            {
                return loadedSprite;
            }
        }

        return loadedSprites[0];
    }

    private static string ExtractResourceFileName(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return string.Empty;
        }

        int slashIndex = resourcePath.LastIndexOf('/');
        return slashIndex >= 0 && slashIndex < resourcePath.Length - 1
            ? resourcePath.Substring(slashIndex + 1)
            : resourcePath;
    }

    private void CacheAnimator()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    private void SetAnimatorTrigger(string triggerName)
    {
        if (!CanSetTrigger(triggerName))
        {
            return;
        }

        animator.SetTrigger(triggerName);
    }

    private bool CanSetTrigger(string triggerName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(triggerName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
            {
                return true;
            }
        }

        return false;
    }

    private static void PlayStateSound(AudioClip sound, float volumeScale)
    {
        if (sound == null || SFXControl.Instance == null)
        {
            return;
        }

        SFXControl.Instance.PlaySFX(sound, volumeScale);
    }

    private void StartReturnToIdle(float delay)
    {
        if (!autoReturnToIdle || delay <= 0f || !isActiveAndEnabled)
        {
            return;
        }

        returnToIdleCoroutine = StartCoroutine(ReturnToIdleAfterDelay(delay));
    }

    private void StopReturnToIdle()
    {
        if (returnToIdleCoroutine == null)
        {
            return;
        }

        StopCoroutine(returnToIdleCoroutine);
        returnToIdleCoroutine = null;
    }

    private IEnumerator ReturnToIdleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        returnToIdleCoroutine = null;
        EnterIdle();
    }
}
