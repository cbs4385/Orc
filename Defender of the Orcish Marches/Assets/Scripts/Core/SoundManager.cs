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
    [SerializeField] private AudioClip wallCollapse;

    [Header("Events")]
    [SerializeField] private AudioClip attackStart;

    [Header("Music")]
    [SerializeField] private AudioClip[] musicTracks;

    [Header("Volume")]
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;

    private AudioSource audioSource;
    private AudioSource musicSource;
    private int currentTrackIndex;
    private int[] shuffledOrder;
    private int lastPlayedTrack = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D — no distance rolloff
        audioSource.playOnAwake = false;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.spatialBlend = 0f;
        musicSource.playOnAwake = false;
        musicSource.loop = false; // We handle track advancement manually
        musicSource.volume = musicVolume;

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
        if (musicSource == null)
        {
            // Find the second AudioSource (not the SFX one)
            var sources = GetComponents<AudioSource>();
            musicSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
            musicSource.spatialBlend = 0f;
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.volume = musicVolume;
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // Advance to next track when current finishes
        if (musicSource != null && !musicSource.isPlaying && musicTracks != null && musicTracks.Length > 0 && musicSource.clip != null)
        {
            currentTrackIndex++;
            if (currentTrackIndex >= musicTracks.Length)
            {
                currentTrackIndex = 0;
                ShuffleOrder();
            }
            PlayMusicTrack(shuffledOrder[currentTrackIndex]);
        }
    }

    public void SetVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        Debug.Log($"[SoundManager] SFX volume set to {sfxVolume:F2}");
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null) musicSource.volume = musicVolume;
        Debug.Log($"[SoundManager] Music volume set to {musicVolume:F2}");
    }

    public void StartMusic()
    {
        if (musicTracks == null || musicTracks.Length == 0) return;
        if (musicSource != null && musicSource.isPlaying) return;
        currentTrackIndex = 0;
        ShuffleOrder();
        PlayMusicTrack(shuffledOrder[currentTrackIndex]);
        Debug.Log($"[SoundManager] Music started with {musicTracks.Length} tracks (shuffled).");
    }

    private void ShuffleOrder()
    {
        if (shuffledOrder == null || shuffledOrder.Length != musicTracks.Length)
        {
            shuffledOrder = new int[musicTracks.Length];
            for (int i = 0; i < shuffledOrder.Length; i++)
                shuffledOrder[i] = i;
        }

        // Fisher-Yates shuffle using UnityEngine.Random (properly seeded per play session)
        for (int i = shuffledOrder.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = shuffledOrder[i];
            shuffledOrder[i] = shuffledOrder[j];
            shuffledOrder[j] = tmp;
        }

        // Ensure first track of new shuffle isn't the same as the last played track
        // (prevents repeating a song at the boundary between two shuffles or on restart)
        if (musicTracks.Length > 1 && shuffledOrder[0] == lastPlayedTrack)
        {
            int swapIdx = 1 + Random.Range(1, shuffledOrder.Length);
            int tmp = shuffledOrder[0];
            shuffledOrder[0] = shuffledOrder[swapIdx];
            shuffledOrder[swapIdx] = tmp;
        }

        Debug.Log($"[SoundManager] Shuffled music order: [{string.Join(", ", shuffledOrder)}]");
    }

    private void PlayMusicTrack(int index)
    {
        if (musicTracks == null || index < 0 || index >= musicTracks.Length) return;
        if (musicTracks[index] == null) return;
        lastPlayedTrack = index;
        musicSource.clip = musicTracks[index];
        musicSource.volume = musicVolume;
        musicSource.Play();
        Debug.Log($"[SoundManager] Playing music track {index}: {musicTracks[index].name}");
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
    public void PlayWallCollapse(Vector3 pos) => Play(wallCollapse);

    // --- Events ---
    public void PlayAttackStart(Vector3 pos) => Play(attackStart);
}
