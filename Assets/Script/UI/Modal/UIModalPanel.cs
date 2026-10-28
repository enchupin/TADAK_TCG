using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 패널 루트에 붙여 활성 · 비활성에 맞춰 패널 스택에 등록 · 해제
/// 씬 작업 없이 패널 소유 스크립트가 Ensure로 붙여 사용, 차단막은 실행 시 자동 생성
/// </summary>
[DisallowMultipleComponent]
public sealed class UIModalPanel : MonoBehaviour
{
    public sealed class Options
    {
        public UIModalKind kind = UIModalKind.Normal;
        public bool useDimmer = true;
        // ESC로 닫을 때 실행 (비우면 패널 비활성)
        public Action close;
    }

    [Tooltip("차단막 색 (실행 시 자동 생성되는 전체 화면 막)")]
    [SerializeField] private Color dimmerColor = new Color(0f, 0f, 0f, 0.6f);

    private Options options;
    private UIModalEntry entry;
    private UIModalDimmer dimmer;
    private CanvasGroup hideGroup;
    private float alphaBeforeHide = 1f;
    private bool isHidden;
    private readonly List<RectTransform> passThroughTargets = new List<RectTransform>();

    public bool IsRegistered => entry != null;
    public bool IsHidden => isHidden;
    public UIModalKind Kind => options != null ? options.kind : UIModalKind.Normal;

    public static UIModalPanel Ensure(GameObject panelRoot, Options options)
    {
        if (panelRoot == null)
        {
            return null;
        }

        UIModalPanel panel = panelRoot.GetComponent<UIModalPanel>();
        if (panel == null)
        {
            panel = panelRoot.AddComponent<UIModalPanel>();
        }
        panel.Configure(options);
        return panel;
    }

    public void Configure(Options newOptions)
    {
        options = newOptions ?? new Options();
        if (entry != null)
        {
            Unregister();
        }
        if (isActiveAndEnabled)
        {
            Register();
        }
    }

    /// <summary>
    /// 같은 패널을 일반 · 필수로 번갈아 쓸 때 (덱 목록 보기 / 카드 선택)
    /// </summary>
    public void SetKind(UIModalKind kind)
    {
        if (options == null)
        {
            options = new Options();
        }
        options.kind = kind;
        if (entry != null && entry.Kind != kind)
        {
            entry.Kind = kind;
            UIModalStack.NotifyChanged();
        }
    }

    /// <summary>
    /// 차단막을 통과해 클릭되는 대상 (패널을 연 버튼으로 다시 닫기)
    /// </summary>
    public void SetPassThrough(params RectTransform[] targets)
    {
        passThroughTargets.Clear();
        if (targets != null)
        {
            foreach (RectTransform target in targets)
            {
                if (target != null)
                {
                    passThroughTargets.Add(target);
                }
            }
        }
        if (dimmer != null)
        {
            dimmer.SetPassThrough(passThroughTargets);
        }
    }

    /// <summary>
    /// 필수 패널 숨김 버튼용: 패널 · 차단막만 숨기고 등록은 유지 (전투 조작은 계속 정지)
    /// </summary>
    public void SetHidden(bool hidden)
    {
        if (isHidden == hidden)
        {
            return;
        }

        isHidden = hidden;
        // 숨긴 동안에만 툴팁 허용
        if (entry != null)
        {
            entry.Hidden = hidden;
            UIModalStack.NotifyChanged();
        }
        if (hideGroup == null)
        {
            hideGroup = GetComponent<CanvasGroup>();
            if (hideGroup == null)
            {
                hideGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (hidden)
        {
            alphaBeforeHide = hideGroup.alpha;
            hideGroup.alpha = 0f;
            hideGroup.interactable = false;
            hideGroup.blocksRaycasts = false;
        }
        else
        {
            hideGroup.alpha = alphaBeforeHide;
            hideGroup.interactable = true;
            hideGroup.blocksRaycasts = true;
        }
        RefreshDimmer();
    }

    public void ToggleHidden()
    {
        SetHidden(!isHidden);
    }

    /// <summary>
    /// 숨김 상태에서도 보이고 눌려야 하는 자식 (펼치기 버튼)
    /// </summary>
    public void KeepVisibleWhenHidden(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }
        group.ignoreParentGroups = true;
    }

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        Unregister();
        SetHidden(false);
    }

    private void OnDestroy()
    {
        if (dimmer != null)
        {
            Destroy(dimmer.gameObject);
        }
    }

    private void Register()
    {
        if (options == null || entry != null)
        {
            return;
        }

        entry = new UIModalEntry(this, options.kind, CloseFromEscape) { Hidden = isHidden };
        UIModalStack.Register(entry);
        RefreshDimmer();
    }

    private void Unregister()
    {
        if (entry == null)
        {
            return;
        }

        entry = null;
        UIModalStack.Unregister(this);
        RefreshDimmer();
    }

    private void CloseFromEscape()
    {
        if (options != null && options.close != null)
        {
            options.close();
            return;
        }
        gameObject.SetActive(false);
    }

    private void RefreshDimmer()
    {
        bool show = entry != null && !isHidden && options != null && options.useDimmer;
        if (!show)
        {
            if (dimmer != null)
            {
                dimmer.gameObject.SetActive(false);
            }
            return;
        }

        if (dimmer == null)
        {
            dimmer = UIModalDimmer.Create(transform, dimmerColor);
            if (dimmer == null)
            {
                return;
            }
            dimmer.SetPassThrough(passThroughTargets);
        }

        // 패널 바로 아래 형제로 유지 (패널 순서가 바뀌어도 패널 뒤를 덮음)
        dimmer.gameObject.SetActive(true);
        dimmer.PlaceBehind(transform);
    }
}
