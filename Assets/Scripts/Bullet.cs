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

    [Header("Destruição Fora da Tela")]
    [Tooltip("Destruir automaticamente ao sair da área da câmera?")]
    [SerializeField] private bool destroyOffScreen = true;

    [Tooltip("Tempo limite de vida (em segundos) como garantia de destruição.")]
    [SerializeField] private float maxLifetime = 5f;

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private float spawnTime;

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
        // Move o projétil na direção e velocidade especificadas
        transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World);

        if (destroyOffScreen)
        {
            CheckOffScreenAndDestroy();
        }
    }

    /// <summary>
    /// Permite definir a direção e velocidade do projétil via código (ex: na hora de instanciar pelo Inimigo ou Player).
    /// </summary>
    public void Setup(Vector2 newDirection, float newSpeed)
    {
        direction = newDirection;
        speed = newSpeed;
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
        // Aguarda 0.2s após o nascimento para evitar destruição no momento do Instantiate
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
        // Aguarda 0.2s após o nascimento para evitar destruição no frame 0
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
}
