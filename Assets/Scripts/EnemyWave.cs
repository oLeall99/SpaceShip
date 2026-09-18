using UnityEngine;

public class EnemyWave : MonoBehaviour
{
    public static EnemyWave Instance { get; private set; }

    [Header("Prefabs dos Inimigos")]
    [Tooltip("Prefab do inimigo Small (1 hit, tiro reto).")]
    [SerializeField] private GameObject smallEnemyPrefab;

    [Tooltip("Prefab do inimigo Medium (3 hits, 2 tiros paralelos, movimento ZigZag).")]
    [SerializeField] private GameObject mediumEnemyPrefab;

    [Tooltip("Prefab do inimigo Big (8 hits, rajada em ZigZag, pausa e patrulha em X).")]
    [SerializeField] private GameObject bigEnemyPrefab;

    [Header("Controle de Disparo dos Inimigos (Override)")]
    [Tooltip("Permitir que o EnemyWave controle diretamente se cada tipo de inimigo pode atirar?")]
    [SerializeField] private bool overrideEnemyShooting = true;

    [Tooltip("Permitir que a nave Small atire?")]
    [SerializeField] private bool smallCanShoot = true;

    [Tooltip("Permitir que a nave Medium atire?")]
    [SerializeField] private bool mediumCanShoot = true;

    [Tooltip("Permitir que a nave Big atire?")]
    [SerializeField] private bool bigCanShoot = true;

    [Header("Prefabs de Power-Ups")]
    [Tooltip("Prefab do Power-Up de Tiro Triplo.")]
    [SerializeField] private GameObject tripleShotPowerUpPrefab;

    [Tooltip("Prefab do Power-Up de Time Slow (Congelar/Desacelerar o tempo).")]
    [SerializeField] private GameObject timeSlowPowerUpPrefab;

    [Header("Chances de Drop de Power-Up (%)")]
    [Tooltip("Porcentagem de chance da nave Small soltar um Power-Up (Padrão: 15%).")]
    [SerializeField] private float smallDropChance = 15f;

    [Tooltip("Porcentagem de chance da nave Medium soltar um Power-Up (Padrão: 40%).")]
    [SerializeField] private float mediumDropChance = 40f;

    [Tooltip("Porcentagem de chance da nave Big soltar um Power-Up (Sempre 100%).")]
    [SerializeField] private float bigDropChance = 100f;

    [Header("Timers Base de Spawn (em segundos)")]
    [Tooltip("Intervalo de aparecimento da nave Small (Padrão: a cada 3s).")]
    [SerializeField] private float smallSpawnInterval = 3f;

    [Tooltip("Intervalo de aparecimento da nave Medium (Padrão: a cada 8s).")]
    [SerializeField] private float mediumSpawnInterval = 8f;

    [Tooltip("Intervalo de aparecimento da nave Big (Padrão: a cada 15s).")]
    [SerializeField] private float bigSpawnInterval = 15f;

    [Header("Limites Mínimos de Intervalo (Aceleração Máxima)")]
    [SerializeField] private float minSmallInterval = 0.8f;
    [SerializeField] private float minMediumInterval = 2.5f;
    [SerializeField] private float minBigInterval = 6.0f;

    [Header("Configurações de Posição de Spawn")]
    [Tooltip("Margem interna dos cantos da tela no eixo X (Padrão: 0.4).")]
    [SerializeField] private float spawnPaddingX = 0.4f;

    [Tooltip("Distância acima da tela onde os inimigos serão gerados.")]
    [SerializeField] private float spawnOffsetY = 1.5f;

    [Header("Progressão de Dificuldade")]
    [Tooltip("Intervalo de tempo (em segundos) para aumentar a onda/dificuldade (Padrão: 30s).")]
    [SerializeField] private float difficultyIncreaseInterval = 30f;

    [Header("Status da Wave (Apenas Leitura no Editor)")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private float waveTimer = 0f;

    private Camera mainCamera;
    private float nextSmallSpawnTime;
    private float nextMediumSpawnTime;
    private float nextBigSpawnTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        nextSmallSpawnTime = Time.time + 0.5f;
        nextMediumSpawnTime = Time.time + 3.0f;
        nextBigSpawnTime = Time.time + 8.0f;
    }

    private void Update()
    {
        HandleWaveTimer();
        HandleParallelSpawns();
    }

    private void HandleWaveTimer()
    {
        waveTimer += Time.deltaTime;

        if (waveTimer >= difficultyIncreaseInterval)
        {
            waveTimer -= difficultyIncreaseInterval;
            IncreaseDifficulty();
        }
    }

    private void IncreaseDifficulty()
    {
        currentWave++;
        Debug.Log($"EnemyWave: ONDA {currentWave} INICIADA!");
    }

    private void HandleParallelSpawns()
    {
        if (Time.time >= nextSmallSpawnTime)
        {
            SpawnEnemy(smallEnemyPrefab);
            float currentInterval = Mathf.Max(minSmallInterval, smallSpawnInterval - (currentWave - 1) * 0.35f);
            nextSmallSpawnTime = Time.time + currentInterval;
        }

        if (Time.time >= nextMediumSpawnTime)
        {
            SpawnEnemy(mediumEnemyPrefab);
            float currentInterval = Mathf.Max(minMediumInterval, mediumSpawnInterval - (currentWave - 1) * 0.8f);
            nextMediumSpawnTime = Time.time + currentInterval;
        }

        if (Time.time >= nextBigSpawnTime)
        {
            SpawnEnemy(bigEnemyPrefab);
            float currentInterval = Mathf.Max(minBigInterval, bigSpawnInterval - (currentWave - 1) * 1.5f);
            nextBigSpawnTime = Time.time + currentInterval;
        }
    }

    private void SpawnEnemy(GameObject prefabToSpawn)
    {
        if (prefabToSpawn == null) return;

        Vector3 spawnPosition = GetRandomTopSpawnPosition();
        GameObject enemyObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        if (enemyObj.TryGetComponent<Enemy>(out Enemy enemyScript))
        {
            // Override do controle de disparo diretamente via EnemyWave
            if (overrideEnemyShooting)
            {
                bool allowShooting = enemyScript.Type switch
                {
                    Enemy.EnemyType.Small => smallCanShoot,
                    Enemy.EnemyType.Medium => mediumCanShoot,
                    Enemy.EnemyType.Big => bigCanShoot,
                    _ => true
                };
                enemyScript.SetCanShoot(allowShooting);
            }

            float speedMultiplier = 1f + (currentWave - 1) * 0.10f;
            float fireRateMultiplier = 1f + (currentWave - 1) * 0.15f;
            float bulletSpeedMultiplier = 1f + (currentWave - 1) * 0.12f;

            enemyScript.ApplyDifficultyModifiers(speedMultiplier, fireRateMultiplier, bulletSpeedMultiplier);
        }
    }

    /// <summary>
    /// Tenta soltar um Power-Up de acordo com as chances do tipo de nave destruída.
    /// A nave Big SEMPRE (100%) solta um Power-Up.
    /// </summary>
    public void TryDropPowerUp(Vector3 spawnPosition, Enemy.EnemyType enemyType)
    {
        float dropChance = enemyType switch
        {
            Enemy.EnemyType.Small => smallDropChance,
            Enemy.EnemyType.Medium => mediumDropChance,
            Enemy.EnemyType.Big => 100f,
            _ => smallDropChance
        };

        float roll = Random.Range(0f, 100f);

        if (roll <= dropChance)
        {
            GameObject powerUpToSpawn = SelectRandomPowerUpPrefab();
            if (powerUpToSpawn != null)
            {
                Instantiate(powerUpToSpawn, spawnPosition, Quaternion.identity);
            }
        }
    }

    private GameObject SelectRandomPowerUpPrefab()
    {
        if (tripleShotPowerUpPrefab != null && timeSlowPowerUpPrefab != null)
        {
            return Random.value > 0.5f ? tripleShotPowerUpPrefab : timeSlowPowerUpPrefab;
        }
        return tripleShotPowerUpPrefab ?? timeSlowPowerUpPrefab;
    }

    private Vector3 GetRandomTopSpawnPosition()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return transform.position;

        float distance = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 minBounds = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, distance));
        Vector3 maxBounds = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, distance));

        float randomX = Random.Range(minBounds.x + spawnPaddingX, maxBounds.x - spawnPaddingX);
        float spawnY = maxBounds.y + spawnOffsetY;

        return new Vector3(randomX, spawnY, 0f);
    }
}
