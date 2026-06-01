using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager Instance { get; private set; }

    [SerializeField] private TerrainModeConfig initialModeConfig;
    [SerializeField] private ChunkSystem chunkSystem;
    [SerializeField] private ParallaxController parallaxController;
    [SerializeField] private SilhouetteChunkSystem silhouetteChunkSystem;
    [SerializeField] private Camera mainCamera;

    private TerrainModeConfig activeConfig;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        if (initialModeConfig == null)
        {
            Debug.LogWarning("TerrainManager: Initial mode config is missing");
            return;
        }

        activeConfig = ScriptableObject.Instantiate(initialModeConfig);
        activeConfig.noiseSeed = Random.Range(0f, 9999f);

        ApplyBackgroundColor(activeConfig);

        if (chunkSystem != null)
        {
            chunkSystem.Initialize(activeConfig, mainCamera);
        }

        if (silhouetteChunkSystem != null && chunkSystem != null)
        {
            silhouetteChunkSystem.Initialize(activeConfig, mainCamera, chunkSystem.ChunkWidth, chunkSystem.PointsPerChunk, chunkSystem.BottomY);
        }

        if (parallaxController != null)
        {
            parallaxController.Initialize(activeConfig);
        }
    }

    public void SwitchMode(TerrainModeConfig newConfig)
    {
        if (newConfig == null)
        {
            return;
        }

        activeConfig = ScriptableObject.Instantiate(newConfig);
        activeConfig.noiseSeed = Random.Range(0f, 9999f);

        ApplyBackgroundColor(activeConfig);

        if (chunkSystem != null)
        {
            chunkSystem.SwitchMode(activeConfig);
        }

        if (silhouetteChunkSystem != null)
        {
            silhouetteChunkSystem.SwitchMode(activeConfig);
        }

        if (parallaxController != null)
        {
            parallaxController.Initialize(activeConfig);
        }
    }

    private void ApplyBackgroundColor(TerrainModeConfig config)
    {
        if (mainCamera != null && config != null && config.colorTheme != null)
        {
            mainCamera.backgroundColor = config.colorTheme.skyColorTop;
        }
    }
}
