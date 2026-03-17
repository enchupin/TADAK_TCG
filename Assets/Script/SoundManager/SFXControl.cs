using UnityEngine;

/// <summary>
/// 효과음(SFX)을 관리하는 컨트롤러
/// </summary>
public class SFXControl : MonoBehaviour
{
    public static SFXControl Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxAudioSource;

    // PlayerPrefs 키
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const float DEFAULT_SFX_VOLUME = 0.15f;

    // 현재 SFX 볼륨
    private float currentSFXVolume;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        } else {
            Destroy(gameObject);
            return;
        }

        // AudioSource가 할당되지 않았다면 자동으로 추가
        if (sfxAudioSource == null) {
            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource == null) {
                Debug.LogWarning("SFX AudioSource가 없어서 자동으로 추가합니다.");
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // PlayerPrefs에서 SFX 볼륨 로드
        currentSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
        ApplysfxVolume();
    }


    private void ApplysfxVolume() {
        if (sfxAudioSource == null) {
            return;
        }
        sfxAudioSource.volume = currentSFXVolume;
    }

    /// <summary>
    /// SFX 볼륨 설정
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        currentSFXVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, currentSFXVolume);
        PlayerPrefs.Save();
        ApplysfxVolume();
    }

    /// <summary>
    /// 현재 SFX 볼륨 가져오기
    /// </summary>
    public float GetSFXVolume()
    {
        return currentSFXVolume;
    }


    /// <summary>
    /// 특정 효과음을 재생 (설정된 볼륨 적용)
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(clip, currentSFXVolume);
        }
    }


}
