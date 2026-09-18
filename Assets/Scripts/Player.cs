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

    private void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleShooting();
    }

    /// <summary>
    /// Gerencia a movimentação com WASD, Setinhas e Controle.
    /// </summary>
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

        // Atualiza as animações de movimentação horizontal de forma segura
        UpdateMovementAnimations(moveX);

        if (clampToScreen)
        {
            ClampToScreenBounds();
        }
    }

    /// <summary>
    /// Controla os parâmetros e estados do Animator verificando com segurança se o Controller e os estados existem.
    /// </summary>
    private void UpdateMovementAnimations(float moveX)
    {
        // Se não houver Animator ou o Animator Controller não estiver atribuído na Unity, ignora sem gerar erros
        if (animator == null || animator.runtimeAnimatorController == null) return;

        // 1. Atualiza o parâmetro Float (ex: moveX = -1, 0, 1) se existir no Animator
        if (!string.IsNullOrEmpty(moveXParamName) && HasParameter(animator, moveXParamName, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(moveXParamName, moveX);
        }

        // 2. Atualiza os parâmetros Bools se existirem
        if (!string.IsNullOrEmpty(isRightParamName) && HasParameter(animator, isRightParamName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(isRightParamName, moveX > 0.1f);
        }

        if (!string.IsNullOrEmpty(isLeftParamName) && HasParameter(animator, isLeftParamName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(isLeftParamName, moveX < -0.1f);
        }

        // 3. Reprodução direta por estado (apenas se a opção estiver ativada e o estado existir no Animator Controller)
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

    /// <summary>
    /// Verifica se um determinado parâmetro existe no Animator Controller para evitar warnings no console.
    /// </summary>
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

    /// <summary>
    /// Gerencia o disparo ao pressionar a tecla Espaço.
    /// </summary>
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

    /// <summary>
    /// Instancia a bullet e define a tag solicitada.
    /// </summary>
    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Player: Nenhum Prefab de Bullet foi atribuído na caixa 'Bullet Prefab' no Inspector!");
            return;
        }

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Quaternion spawnRot = firePoint != null ? firePoint.rotation : Quaternion.identity;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, spawnRot);

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
    /// Mantém a nave estritamente dentro do campo de visão da câmera (Viewport).
    /// </summary>
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
