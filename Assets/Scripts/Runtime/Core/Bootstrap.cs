using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class Bootstrap : MonoBehaviour
{
    [Header("Level Flow")]
    [SerializeField] private LevelRuntimeLoader levelLoader;
    [SerializeField] private TextAsset[] levelSequence;
    [SerializeField] private bool loadOnStart = true;

    public static bool HasBootstrapped { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        HasBootstrapped = false;
    }

    private void Awake()
    {
        HasBootstrapped = true;
        EnsureManagers();

        if (levelLoader == null)
            levelLoader = FindFirstObjectByType<LevelRuntimeLoader>();

        LevelManager.Instance.ConfigureLevelFlow(levelLoader, levelSequence);
    }

    private void Start()
    {
        _ = SaveLoadManager.Instance;

        AudioManager.Instance.PlayMusic(AudioManager.BgmBackgroundMusic);

        if (loadOnStart)
            LevelManager.Instance.LoadCurrentLevel();
    }

    private static void EnsureManagers()
    {
        _ = EventBus.Instance;
        _ = GameManager.Instance;
        _ = LevelManager.Instance;
        _ = BoardManager.Instance;
        _ = MoveManager.Instance;
        _ = EconomyManager.Instance;
        _ = BoosterInventoryManager.Instance;
        _ = BoosterManager.Instance;
        _ = AudioManager.Instance;
    }
}
