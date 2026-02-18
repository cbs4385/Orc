using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Defender Attacks")]
    [SerializeField] private AudioClip scorpioFire;
    [SerializeField] private AudioClip crossbowFire;
    [SerializeField] private AudioClip pikemanAttack;
    [SerializeField] private AudioClip wizardFire;

    [Header("Enemy Attacks")]
    [SerializeField] private AudioClip orcArcherFire;
    [SerializeField] private AudioClip goblinBomberExplode;
    [SerializeField] private AudioClip goblinCannonFire;

    [Header("Hits")]
    [SerializeField] private AudioClip orcHit;
    [SerializeField] private AudioClip trollHit;
    [SerializeField] private AudioClip menialHit;
    [SerializeField] private AudioClip wallHit;

    [Header("Events")]
    [SerializeField] private AudioClip attackStart;

    [Header("Volume")]
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.5f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D — no distance rolloff
        audioSource.playOnAwake = false;
        Debug.Log("[SoundManager] Instance set in Awake.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[SoundManager] Instance re-registered in OnEnable after domain reload.");
        }
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
                audioSource.playOnAwake = false;
            }
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    public void SetVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        Debug.Log($"[SoundManager] Volume set to {sfxVolume:F2}");
    }

    private void Play(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    // --- Defender attacks ---
    public void PlayScorpioFire(Vector3 pos) => Play(scorpioFire);
    public void PlayCrossbowFire(Vector3 pos) => Play(crossbowFire);
    public void PlayPikemanAttack(Vector3 pos) => Play(pikemanAttack);
    public void PlayWizardFire(Vector3 pos) => Play(wizardFire);

    // --- Enemy attacks ---
    public void PlayOrcArcherFire(Vector3 pos) => Play(orcArcherFire);
    public void PlayGoblinBomberExplode(Vector3 pos) => Play(goblinBomberExplode);
    public void PlayGoblinCannonFire(Vector3 pos) => Play(goblinCannonFire);

    // --- Hits ---
    public void PlayOrcHit(Vector3 pos) => Play(orcHit);
    public void PlayTrollHit(Vector3 pos) => Play(trollHit);
    public void PlayMenialHit(Vector3 pos) => Play(menialHit);
    public void PlayWallHit(Vector3 pos) => Play(wallHit);

    // --- Events ---
    public void PlayAttackStart(Vector3 pos) => Play(attackStart);
}
