using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Player : MonoBehaviour
{
    [Header("Movimentação")]
    [Tooltip("Velocidade de movimento do jogador.")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Limites de Tela (Câmera)")]
    [Tooltip("Restringir a movimentação do jogador para que nunca saia do campo da câmera.")]
    [SerializeField] private bool clampToScreen = true;

    [Tooltip("Calcular o tamanho da nave automaticamente usando o SpriteRenderer ou Collider2D?")]
    [SerializeField] private bool autoDetectSpriteSize = true;

    [Tooltip("Margem interna adicional no eixo X.")]
    [SerializeField] private float paddingX = 0f;

    [Tooltip("Margem interna adicional no eixo Y.")]
    [SerializeField] private float paddingY = 0f;

    [Header("Animações de Movimento")]
    [Tooltip("Componente Animator do Player. Se deixado vazio, buscará no próprio GameObject.")]
    [SerializeField] private Animator animator;

    [Tooltip("Nome do parâmetro Float no Animator. Valores: -1 (Esquerda), 0 (Parado), 1 (Direita).")]
    [SerializeField] private string moveXParamName = "moveX";

    [Tooltip("Nome do parâmetro Bool para direção Direita.")]
    [SerializeField] private string isRightParamName = "isMovingRight";

    [Tooltip("Nome do parâmetro Bool para direção Esquerda.")]
    [SerializeField] private string isLeftParamName = "isMovingLeft";

    [Tooltip("Ativar a reprodução direta das animações por nome de Estado (Play)? Desativado por padrão.")]
    [SerializeField] private bool useDirectStatePlay = false;

    [Tooltip("Nome do estado de Animação para a Direita no Animator Controller.")]
    [SerializeField] private string rightStateName = "TurnRight";

    [Tooltip("Nome do estado de Animação para a Esquerda no Animator Controller.")]
    [SerializeField] private string leftStateName = "TurnLeft";

    [Tooltip("Nome do estado de Animação Parado (Idle) no Animator Controller.")]
    [SerializeField] private string idleStateName = "Idle";

    [Header("Vidas & Invencibilidade")]
    [Tooltip("Quantidade máxima de vidas do jogador.")]
    [SerializeField] private int maxLives = 3;

    [Tooltip("Duração do tempo de invencibilidade ao levar um hit (em segundos).")]
    [SerializeField] private float invincibilityDuration = 1.0f;

    [Tooltip("Intervalo da piscada durante a invencibilidade.")]
    [SerializeField] private float blinkInterval = 0.1f;

    [Tooltip("Prefab de explosão spawnado quando o Player perde todas as vidas.")]
    [SerializeField] private GameObject explosionPrefab;

    [Header("Configurações de Disparo")]
    [Tooltip("Prefab da Bullet / Kit de Animação do tiro que será disparado.")]
    [SerializeField] private GameObject bulletPrefab;

    [Tooltip("Ponto de origem do disparo. Se deixado em branco, dispara a partir do próprio Player.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Intervalo de tempo (em segundos) entre cada tiro.")]
    [SerializeField] private float fireRate = 0.15f;

    [Tooltip("Tag que será atribuída aos projéteis criados.")]
    [SerializeField] private string bulletTag = "Bullet";

    private float nextFireTime = 0f;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;

    private int currentLives;
    private bool isInvincible = false;
    private Coroutine invincibilityCoroutine;

    // Power-Up State
    private bool isTripleShotActive = false;
    private Coroutine tripleShotCoroutine;
    private Coroutine timeSlowCoroutine;

    public static float globalTimeSlowFactor { get; private set; } = 1f;

    private void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        currentLives = maxLives;
        if (UIManager.Instance != null)
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                UIManager.Instance.SetPlayerSpriteIfMissing(spriteRenderer.sprite);
            }
            UIManager.Instance.UpdateLivesUI(currentLives);
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleShooting();
    }

    private void HandleMovement()
    {
        float moveX = 0f;
        float moveY = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            var kbd = Keyboard.current;
            if (kbd.wKey.isPressed || kbd.upArrowKey.isPressed) moveY += 1f;
            if (kbd.sKey.isPressed || kbd.downArrowKey.isPressed) moveY -= 1f;
            if (kbd.aKey.isPressed || kbd.leftArrowKey.isPressed) moveX -= 1f;
            if (kbd.dKey.isPressed || kbd.rightArrowKey.isPressed) moveX += 1f;
        }

        if (Gamepad.current != null && moveX == 0f && moveY == 0f)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            Vector2 dpad = Gamepad.current.dpad.ReadValue();
            Vector2 move = stick != Vector2.zero ? stick : dpad;
            moveX = move.x;
            moveY = move.y;
        }
#else
        moveX = Input.GetAxisRaw("Horizontal");
        moveY = Input.GetAxisRaw("Vertical");
#endif

        Vector3 moveDirection = new Vector3(moveX, moveY, 0f).normalized;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        UpdateMovementAnimations(moveX);

        if (clampToScreen)
        {
            ClampToScreenBounds();
        }
    }

    private void UpdateMovementAnimations(float moveX)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        if (!string.IsNullOrEmpty(moveXParamName) && HasParameter(animator, moveXParamName, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(moveXParamName, moveX);
        }

        if (!string.IsNullOrEmpty(isRightParamName) && HasParameter(animator, isRightParamName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(isRightParamName, moveX > 0.1f);
        }

        if (!string.IsNullOrEmpty(isLeftParamName) && HasParameter(animator, isLeftParamName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(isLeftParamName, moveX < -0.1f);
        }

        if (useDirectStatePlay)
        {
            string targetState = moveX > 0.1f ? rightStateName : (moveX < -0.1f ? leftStateName : idleStateName);

            if (!string.IsNullOrEmpty(targetState))
            {
                int stateHash = Animator.StringToHash(targetState);
                if (animator.HasState(0, stateHash))
                {
                    animator.Play(stateHash);
                }
            }
        }
    }

    private bool HasParameter(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        if (anim == null || anim.runtimeAnimatorController == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (var param in anim.parameters)
        {
            if (param.type == type && param.name == paramName)
                return true;
        }
        return false;
    }

    private void HandleShooting()
    {
        bool isShootPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.isPressed || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                isShootPressed = true;
            }
        }
        if (Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.rightTrigger.isPressed))
        {
            isShootPressed = true;
        }
#endif

        if (!isShootPressed)
        {
            try
            {
                isShootPressed = Input.GetKey(KeyCode.Space);
            }
            catch { }
        }

        if (isShootPressed && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Player: Nenhum Prefab de Bullet foi atribuído na caixa 'Bullet Prefab' no Inspector!");
            return;
        }

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        if (isTripleShotActive)
        {
            // Dispara 3 projéteis paralelos ao mesmo tempo
            float offset = 0.45f;
            SpawnSingleBullet(spawnPos);
            SpawnSingleBullet(spawnPos - transform.right * offset);
            SpawnSingleBullet(spawnPos + transform.right * offset);
        }
        else
        {
            // Disparo normal (1 projétil)
            SpawnSingleBullet(spawnPos);
        }
    }

    private void SpawnSingleBullet(Vector3 position)
    {
        Quaternion spawnRot = firePoint != null ? firePoint.rotation : Quaternion.identity;
        GameObject bullet = Instantiate(bulletPrefab, position, spawnRot);

        if (!string.IsNullOrEmpty(bulletTag))
        {
            try
            {
                bullet.tag = bulletTag;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Player: A tag '{bulletTag}' não existe nas configurações do Unity. {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Ativa o Power-Up de Tiro Triplo (3 tiros paralelos) por uma determinada duração.
    /// </summary>
    public void ActivateTripleShot(float duration)
    {
        if (tripleShotCoroutine != null)
        {
            StopCoroutine(tripleShotCoroutine);
        }
        tripleShotCoroutine = StartCoroutine(TripleShotRoutine(duration));
    }

    private IEnumerator TripleShotRoutine(float duration)
    {
        isTripleShotActive = true;
        yield return new WaitForSeconds(duration);
        isTripleShotActive = false;
        tripleShotCoroutine = null;
    }

    /// <summary>
    /// Ativa o Power-Up de Desaceleração do Tempo (Time Slow) por uma determinada duração.
    /// </summary>
    public void ActivateTimeSlow(float duration, float slowFactor)
    {
        if (timeSlowCoroutine != null)
        {
            StopCoroutine(timeSlowCoroutine);
        }
        timeSlowCoroutine = StartCoroutine(TimeSlowRoutine(duration, slowFactor));
    }

    private IEnumerator TimeSlowRoutine(float duration, float slowFactor)
    {
        globalTimeSlowFactor = slowFactor;
        Parallax.globalSpeedMultiplier = slowFactor;

        yield return new WaitForSeconds(duration);

        globalTimeSlowFactor = 1f;
        Parallax.globalSpeedMultiplier = 1f;
        timeSlowCoroutine = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayerCollision(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerCollision(collision.gameObject);
    }

    private void HandlePlayerCollision(GameObject other)
    {
        if (isInvincible) return;

        bool isEnemyBullet = other.CompareTag("EnemyBullet");
        bool isEnemyShip = other.CompareTag("Enemy");

        if (isEnemyBullet || isEnemyShip)
        {
            TakeDamage(1);

            if (isEnemyBullet)
            {
                Destroy(other);
            }
            else if (isEnemyShip && other.TryGetComponent<Enemy>(out Enemy enemy))
            {
                enemy.TakeDamage(999);
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible) return;

        currentLives -= amount;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLivesUI(currentLives);
        }

        if (currentLives <= 0)
        {
            Die();
        }
        else
        {
            StartInvincibility();
        }
    }

    private void StartInvincibility()
    {
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
        }
        invincibilityCoroutine = StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = (color.a == 1f) ? 0.25f : 1f;
                spriteRenderer.color = color;
            }

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        isInvincible = false;
        invincibilityCoroutine = null;
    }

    private void Die()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        gameObject.SetActive(false);
    }

    private void ClampToScreenBounds()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float halfWidth = paddingX;
        float halfHeight = paddingY;

        if (autoDetectSpriteSize)
        {
            if (spriteRenderer != null)
            {
                halfWidth += spriteRenderer.bounds.extents.x;
                halfHeight += spriteRenderer.bounds.extents.y;
            }
            else if (playerCollider != null)
            {
                halfWidth += playerCollider.bounds.extents.x;
                halfHeight += playerCollider.bounds.extents.y;
            }
        }

        float cameraDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 minBounds = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, cameraDistance));
        Vector3 maxBounds = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, cameraDistance));

        float clampedX = Mathf.Clamp(transform.position.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
        float clampedY = Mathf.Clamp(transform.position.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}
