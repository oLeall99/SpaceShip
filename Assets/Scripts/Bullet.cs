using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Configurações Básicas")]
    [Tooltip("Velocidade do projétil.")]
    [SerializeField] private float speed = 12f;

    [Tooltip("Direção do movimento (ex: Vector2.up para Player, Vector2.down para Inimigo).")]
    [SerializeField] private Vector2 direction = Vector2.up;

    [Tooltip("Tag aplicada ao projétil.")]
    [SerializeField] private string bulletTag = "Bullet";

    [Header("Movimento ZigZag")]
    [Tooltip("Ativar movimento em formato de ZigZag/senoidal.")]
    [SerializeField] private bool isZigZag = false;

    [Tooltip("Frequência da oscilação do ZigZag.")]
    [SerializeField] private float zigZagFrequency = 6f;

    [Tooltip("Amplitude (largura) da oscilação do ZigZag.")]
    [SerializeField] private float zigZagAmplitude = 2.5f;

    [Header("Destruição Fora da Tela")]
    [Tooltip("Destruir automaticamente ao sair da área da câmera?")]
    [SerializeField] private bool destroyOffScreen = true;

    [Tooltip("Tempo limite de vida (em segundos) como garantia de destruição.")]
    [SerializeField] private float maxLifetime = 5f;

    [Header("Colisão e Efeitos")]
    [Tooltip("Efeito opcional de impacto ao colidir com outro projétil.")]
    [SerializeField] private GameObject hitEffectPrefab;

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private float spawnTime;
    private bool isDestroying = false;

    private void Start()
    {
        spawnTime = Time.time;
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Atribui a tag de forma segura contra exceções
        if (!string.IsNullOrEmpty(bulletTag))
        {
            try
            {
                gameObject.tag = bulletTag;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Bullet: A tag '{bulletTag}' não existe nas configurações do Unity. {ex.Message}");
            }
        }

        // Destrói por tempo máximo de vida se configurado
        if (maxLifetime > 0f)
        {
            Destroy(gameObject, maxLifetime);
        }
    }

    private void Update()
    {
        float timeSlowMult = CompareTag("EnemyBullet") ? Parallax.globalSpeedMultiplier : 1f;
        Vector3 move = (Vector3)(direction.normalized * speed * timeSlowMult * Time.deltaTime);

        // Aplica o movimento em ZigZag se ativado
        if (isZigZag)
        {
            float sideOffset = Mathf.Sin((Time.time - spawnTime) * zigZagFrequency) * zigZagAmplitude * timeSlowMult;
            Vector3 sideVector = new Vector3(direction.y, -direction.x, 0f).normalized;
            move += sideVector * sideOffset * Time.deltaTime;
        }

        transform.position += move;

        if (destroyOffScreen)
        {
            CheckOffScreenAndDestroy();
        }
    }

    /// <summary>
    /// Permite definir a direção e velocidade do projétil via código.
    /// </summary>
    public void Setup(Vector2 newDirection, float newSpeed)
    {
        direction = newDirection;
        speed = newSpeed;
    }

    /// <summary>
    /// Ativa o movimento em ZigZag no projétil.
    /// </summary>
    public void EnableZigZag(float frequency = 6f, float amplitude = 2.5f)
    {
        isZigZag = true;
        zigZagFrequency = frequency;
        zigZagAmplitude = amplitude;
    }

    /// <summary>
    /// Define apenas a direção do projétil.
    /// </summary>
    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection;
    }

    /// <summary>
    /// Define apenas a velocidade do projétil.
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    /// <summary>
    /// Chamado pelo Unity quando o sprite deixa de ser renderizado por qualquer câmera.
    /// </summary>
    private void OnBecameInvisible()
    {
        if (destroyOffScreen && Time.time - spawnTime > 0.2f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Verificação direta dos limites da Câmera Principal.
    /// </summary>
    private void CheckOffScreenAndDestroy()
    {
        if (Time.time - spawnTime < 0.2f) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float cameraDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 minBounds = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, cameraDistance));
        Vector3 maxBounds = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, cameraDistance));

        float margin = 1.5f;
        if (spriteRenderer != null)
        {
            margin = Mathf.Max(spriteRenderer.bounds.extents.x, spriteRenderer.bounds.extents.y) + 0.5f;
        }

        Vector3 pos = transform.position;
        if (pos.x < minBounds.x - margin || pos.x > maxBounds.x + margin ||
            pos.y < minBounds.y - margin || pos.y > maxBounds.y + margin)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleBulletCollision(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleBulletCollision(collision.gameObject);
    }

    private void HandleBulletCollision(GameObject other)
    {
        if (isDestroying || other == null) return;

        bool isMyBulletPlayer = CompareTag("Bullet");
        bool isMyBulletEnemy = CompareTag("EnemyBullet");

        bool isOtherBulletPlayer = other.CompareTag("Bullet");
        bool isOtherBulletEnemy = other.CompareTag("EnemyBullet");

        // Colisão entre projétil do jogador e projétil do inimigo
        if ((isMyBulletPlayer && isOtherBulletEnemy) || (isMyBulletEnemy && isOtherBulletPlayer))
        {
            isDestroying = true;

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(other);
            Destroy(gameObject);
        }
    }
}
