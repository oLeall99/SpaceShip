using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType
    {
        TripleShot, // Ganha mais 2 linhas de tiros (3 projéteis paralelos)
        TimeSlow    // Reduz a velocidade de tudo (inimigos, tiros, parallax) exceto a do player
    }

    [Header("Tipo e Movimento")]
    [Tooltip("Tipo do Power-Up.")]
    [SerializeField] private PowerUpType type = PowerUpType.TripleShot;

    [Tooltip("Velocidade de queda do Power-Up.")]
    [SerializeField] private float fallSpeed = 3f;

    [Tooltip("Duração do efeito em segundos.")]
    [SerializeField] private float effectDuration = 8f;

    [Tooltip("Fator de lentidão para o efeito TimeSlow (ex: 0.35 = 65% mais lento).")]
    [SerializeField] private float timeSlowFactor = 0.35f;

    [Header("Efeito Visual ao Coletar (Opcional)")]
    [SerializeField] private GameObject pickupEffectPrefab;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        // Garante que o objeto tenha um Collider2D como Trigger
        if (TryGetComponent<Collider2D>(out Collider2D col))
        {
            col.isTrigger = true;
        }
        else
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
        }
    }

    private void Update()
    {
        // Cai em direção ao fundo da tela
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        CheckOffScreenAndDestroy();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandlePickup(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePickup(collision.gameObject);
    }

    private void HandlePickup(GameObject other)
    {
        // Verifica se quem coletou foi o Player
        if (other.CompareTag("Player") || other.GetComponent<Player>() != null)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterPowerUpCollected();
            }

            ApplyPowerUpEffect(other);
            SpawnPickupEffect();
            Destroy(gameObject);
        }
    }

    private void ApplyPowerUpEffect(GameObject playerObj)
    {
        if (playerObj.TryGetComponent<Player>(out Player playerScript))
        {
            switch (type)
            {
                case PowerUpType.TripleShot:
                    playerScript.ActivateTripleShot(effectDuration);
                    Debug.Log("PowerUp: TRIPLE SHOT ATIVADO!");
                    break;

                case PowerUpType.TimeSlow:
                    playerScript.ActivateTimeSlow(effectDuration, timeSlowFactor);
                    Debug.Log("PowerUp: SLOW MOTION / TEMPO LENTO ATIVADO!");
                    break;
            }
        }
    }

    private void SpawnPickupEffect()
    {
        if (pickupEffectPrefab != null)
        {
            GameObject effect = Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1.5f);
        }
    }

    private void CheckOffScreenAndDestroy()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float distance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 minBounds = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, distance));

        if (transform.position.y < minBounds.y - 2f)
        {
            Destroy(gameObject);
        }
    }
}
