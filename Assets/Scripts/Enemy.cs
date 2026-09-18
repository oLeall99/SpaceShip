using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum EnemyType
    {
        Small,
        Medium,
        Big
    }

    [Header("Tipo e Vida")]
    [Tooltip("Tipo do inimigo (Small: 1 hit, Medium: 3 hits, Big: 8 hits).")]
    [SerializeField] private EnemyType enemyType = EnemyType.Small;

    [Tooltip("Permitir personalizar a vida manualmente no Inspector?")]
    [SerializeField] private bool overrideHealth = false;

    [Tooltip("Quantidade de hits/vida se 'overrideHealth' estiver ativo.")]
    [SerializeField] private int customHealth = 1;

    [Header("Movimentação Geral")]
    [Tooltip("Velocidade de movimento do inimigo para baixo.")]
    [SerializeField] private float speed = 3f;

    [Header("Movimentação ZigZag (Medium)")]
    [Tooltip("Frequência da oscilação em ZigZag da nave Medium.")]
    [SerializeField] private float mediumZigZagFrequency = 4f;

    [Tooltip("Amplitude (largura) da oscilação em ZigZag da nave Medium.")]
    [SerializeField] private float mediumZigZagAmplitude = 3.5f;

    [Header("Movimentação Especial (Big)")]
    [Tooltip("Tempo descendo antes de fazer uma pausa para andar apenas no eixo X.")]
    [SerializeField] private float bigHorizontalPauseInterval = 3f;

    [Tooltip("Duração do movimento exclusivo em X durante a pausa.")]
    [SerializeField] private float bigHorizontalDuration = 2.5f;

    [Tooltip("Velocidade do movimento no eixo X da nave Big.")]
    [SerializeField] private float bigHorizontalSpeed = 4f;

    [Header("Disparo do Inimigo")]
    [Tooltip("O inimigo pode atirar?")]
    [SerializeField] private bool canShoot = true;

    [Tooltip("Prefab do tiro/bullet do inimigo.")]
    [SerializeField] private GameObject bulletPrefab;

    [Tooltip("Ponto de origem do disparo.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Intervalo entre disparos (em segundos).")]
    [SerializeField] private float fireRate = 2f;

    [Tooltip("Velocidade do tiro do inimigo.")]
    [SerializeField] private float bulletSpeed = 7f;

    [Tooltip("Tag aplicada ao tiro do inimigo (Padrão: EnemyBullet).")]
    [SerializeField] private string enemyBulletTag = "EnemyBullet";

    [Header("Efeitos & Feedback de Dano")]
    [Tooltip("Cor que a nave assume brevemente ao ser atingida por um tiro (Cinza/Branco).")]
    [SerializeField] private Color hitFlashColor = new Color(0.65f, 0.65f, 0.65f, 1f);

    [Tooltip("Duração do piscar em segundos.")]
    [SerializeField] private float flashDuration = 0.1f;

    [Tooltip("Prefab de explosão spawnado ao destruir o inimigo.")]
    [SerializeField] private GameObject explosionPrefab;

    [Tooltip("Tempo de vida da explosão caso não consiga detectar a animação automaticamente.")]
    [SerializeField] private float fallbackExplosionDuration = 1f;

    private int currentHealth;
    private float nextFireTime = 0f;
    private Camera mainCamera;
    private float bulletSpeedMultiplier = 1f;
    private float spawnTime;

    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider;
    private Color originalColor = Color.white;
    private Coroutine flashCoroutine;

    // Variáveis internas da nave Big
    private float bigStateTimer = 0f;
    private bool isBigMovingHorizontal = false;
    private float bigHorizontalDirection = 1f;

    public EnemyType Type => enemyType;

    public bool CanShoot
    {
        get => canShoot;
        set => canShoot = value;
    }

    public void SetCanShoot(bool allowShooting)
    {
        canShoot = allowShooting;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        spawnTime = Time.time;
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        InitializeHealth();

        if (enemyCollider == null)
        {
            Debug.LogWarning($"Enemy ({gameObject.name}): Nenhum Collider2D foi encontrado! Adicione um BoxCollider2D com 'Is Trigger = true'.");
        }
    }

    private void Update()
    {
        MoveEnemy();
        HandleShooting();
        CheckOffScreenAndDestroy();
    }

    private void InitializeHealth()
    {
        if (overrideHealth)
        {
            currentHealth = customHealth;
            return;
        }

        switch (enemyType)
        {
            case EnemyType.Small:
                currentHealth = 1;
                break;
            case EnemyType.Medium:
                currentHealth = 3;
                break;
            case EnemyType.Big:
                currentHealth = 8;
                break;
        }
    }

    public void ApplyDifficultyModifiers(float speedMult, float fireRateMult, float bulletSpeedMult)
    {
        speed *= speedMult;
        fireRate = Mathf.Max(0.4f, fireRate / fireRateMult);
        bulletSpeedMultiplier = bulletSpeedMult;
    }

    /// <summary>
    /// Gerencia a movimentação. O multiplicador 'Parallax.globalSpeedMultiplier' desacelera o movimento no Time Slow.
    /// </summary>
    private void MoveEnemy()
    {
        float slowMult = Parallax.globalSpeedMultiplier;

        switch (enemyType)
        {
            case EnemyType.Small:
                transform.Translate(Vector3.down * speed * slowMult * Time.deltaTime, Space.World);
                break;

            case EnemyType.Medium:
                float sideSpeed = Mathf.Sin((Time.time - spawnTime) * mediumZigZagFrequency) * mediumZigZagAmplitude;
                Vector3 mediumMovement = new Vector3(sideSpeed, -speed, 0f) * slowMult * Time.deltaTime;
                transform.position += mediumMovement;
                break;

            case EnemyType.Big:
                bigStateTimer += Time.deltaTime * slowMult;

                if (!isBigMovingHorizontal)
                {
                    transform.Translate(Vector3.down * speed * slowMult * Time.deltaTime, Space.World);

                    if (bigStateTimer >= bigHorizontalPauseInterval)
                    {
                        isBigMovingHorizontal = true;
                        bigStateTimer = 0f;
                        bigHorizontalDirection = Random.value > 0.5f ? 1f : -1f;
                    }
                }
                else
                {
                    transform.Translate(Vector3.right * bigHorizontalDirection * bigHorizontalSpeed * slowMult * Time.deltaTime, Space.World);

                    if (mainCamera == null) mainCamera = Camera.main;
                    if (mainCamera != null)
                    {
                        float cameraDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
                        float minX = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, cameraDistance)).x + 0.8f;
                        float maxX = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, cameraDistance)).x - 0.8f;

                        if (transform.position.x <= minX) bigHorizontalDirection = 1f;
                        if (transform.position.x >= maxX) bigHorizontalDirection = -1f;
                    }

                    if (bigStateTimer >= bigHorizontalDuration)
                    {
                        isBigMovingHorizontal = false;
                        bigStateTimer = 0f;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Atira ciclicamente. A cadência diminui durante o Time Slow.
    /// </summary>
    private void HandleShooting()
    {
        if (!canShoot || bulletPrefab == null) return;

        float effectiveFireRate = fireRate / Mathf.Max(0.1f, Parallax.globalSpeedMultiplier);

        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + effectiveFireRate;
        }
    }

    private void Shoot()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        switch (enemyType)
        {
            case EnemyType.Small:
                SpawnSingleBullet(spawnPos, Vector2.down, false);
                break;

            case EnemyType.Medium:
                float offset = 0.35f;
                SpawnSingleBullet(spawnPos - transform.right * offset, Vector2.down, false);
                SpawnSingleBullet(spawnPos + transform.right * offset, Vector2.down, false);
                break;

            case EnemyType.Big:
                SpawnSingleBullet(spawnPos, Vector2.down, true);
                SpawnSingleBullet(spawnPos - transform.right * 0.4f, new Vector2(-0.15f, -1f).normalized, true);
                SpawnSingleBullet(spawnPos + transform.right * 0.4f, new Vector2(0.15f, -1f).normalized, true);
                break;
        }
    }

    private void SpawnSingleBullet(Vector3 position, Vector2 dir, bool useZigZag)
    {
        if (bulletPrefab == null) return;

        Quaternion spawnRot = firePoint != null ? firePoint.rotation : Quaternion.identity;
        GameObject bulletObj = Instantiate(bulletPrefab, position, spawnRot);

        if (!string.IsNullOrEmpty(enemyBulletTag))
        {
            try
            {
                bulletObj.tag = enemyBulletTag;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Enemy: A tag '{enemyBulletTag}' precisa ser criada na Unity. {ex.Message}");
            }
        }

        Collider2D bulletCol = bulletObj.GetComponent<Collider2D>();
        if (enemyCollider != null && bulletCol != null)
        {
            Physics2D.IgnoreCollision(enemyCollider, bulletCol);
        }

        if (bulletObj.TryGetComponent<Bullet>(out Bullet bulletScript))
        {
            bulletScript.Setup(dir, bulletSpeed * bulletSpeedMultiplier);

            if (useZigZag)
            {
                bulletScript.EnableZigZag(6f, 2.5f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void HandleCollision(GameObject other)
    {
        if (!other.CompareTag("Bullet"))
        {
            return;
        }

        TakeDamage(1);
        Destroy(other);
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            TriggerHitFlash();
        }
    }

    private void TriggerHitFlash()
    {
        if (spriteRenderer == null) return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }

    private void Die()
    {
        AwardScorePoints();
        TryDropPowerUp();
        SpawnExplosion();
        Destroy(gameObject);
    }

    private void TryDropPowerUp()
    {
        if (EnemyWave.Instance != null)
        {
            EnemyWave.Instance.TryDropPowerUp(transform.position, enemyType);
        }
    }

    private void AwardScorePoints()
    {
        if (UIManager.Instance == null) return;

        int points = enemyType switch
        {
            EnemyType.Small => 25,
            EnemyType.Medium => 100,
            EnemyType.Big => 250,
            _ => 25
        };

        UIManager.Instance.AddScore(points);
    }

    private void SpawnExplosion()
    {
        if (explosionPrefab == null) return;

        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        float destroyDelay = fallbackExplosionDuration;

        if (explosion.TryGetComponent<Animator>(out Animator anim) && anim.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
            if (clips != null && clips.Length > 0)
            {
                destroyDelay = clips[0].length;
            }
        }
        else if (explosion.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
        {
            destroyDelay = ps.main.duration;
        }

        Destroy(explosion, destroyDelay);
    }

    private void CheckOffScreenAndDestroy()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float cameraDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 minBounds = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, cameraDistance));

        if (transform.position.y < minBounds.y - 2f)
        {
            Destroy(gameObject);
        }
    }
}
