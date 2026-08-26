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
    [SerializeField] private Image targetImage;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private string monsterImageNameOverride;
    [SerializeField] private bool preserveImageAspect = true;
    [SerializeField] private bool logMissingImage = true;

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
        CacheImageTarget();
    }

    private void OnDisable()
    {
        StopReturnToIdle();
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
        CacheImageTarget();

        if (targetImage == null && targetSpriteRenderer == null)
        {
            LogMissingImageTarget();
            return;
        }

        Sprite stateSprite = LoadStateSprite(state);
        if (stateSprite == null)
        {
            return;
        }

        if (targetImage != null)
        {
            targetImage.sprite = stateSprite;
            targetImage.preserveAspect = preserveImageAspect;
        }

        if (targetSpriteRenderer != null)
        {
            targetSpriteRenderer.sprite = stateSprite;
        }
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

    private void CacheImageTarget()
    {
        if (targetImage == null)
        {
            TryGetComponent(out targetImage);
        }

        if (targetSpriteRenderer == null)
        {
            TryGetComponent(out targetSpriteRenderer);
        }
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
