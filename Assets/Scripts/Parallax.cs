using UnityEngine;

public class Parallax : MonoBehaviour
{
    public enum LoopMode
    {
        FullBackground, // Para o mapa/fundo de tela cheia (usa 2 sprites empilhados)
        ScreenBounds    // Para nuvens e objetos menores que a tela (reaparecem no topo ao saírem embaixo)
    }

    public static float globalSpeedMultiplier = 1f;

    [Header("Configurações Gerais")]
    [SerializeField] private LoopMode loopMode = LoopMode.FullBackground;
    [SerializeField] private float parallaxEffect = 1f;

    [Header("Efeito Visual de Time Slow (Escurecer Background)")]
    [Tooltip("Cor do background quando o efeito de lentidão temporal está ativo.")]
    [SerializeField] private Color timeSlowBackgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Tooltip("Velocidade da transição de cor ao escurecer/clarear o fundo.")]
    [SerializeField] private float dimTransitionSpeed = 6f;

    [Header("Configurações da Câmera / Tela")]
    [Tooltip("Detecta a altura da Câmera Principal automaticamente.")]
    [SerializeField] private bool autoDetectCamera = true;
    [SerializeField] private float customScreenHeight = 10f;

    private float spriteHeight;
    private SpriteRenderer spriteRenderer;
    private Color normalBackgroundColor = Color.white;

    void Start()
    {
        if (TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
        {
            spriteRenderer = sr;
            normalBackgroundColor = sr.color;
            spriteHeight = sr.bounds.size.y;
        }
        else
        {
            spriteHeight = 2f;
        }
    }

    void Update()
    {
        // Movimento continuo para baixo (afetado pelo multiplicador de Time Slow)
        transform.position += Vector3.down * Time.deltaTime * parallaxEffect * globalSpeedMultiplier;

        // Transição de cor suave para escurecer o background no Time Slow
        HandleBackgroundDimming();

        if (loopMode == LoopMode.FullBackground)
        {
            if (transform.position.y <= -spriteHeight)
            {
                transform.position += new Vector3(0, spriteHeight * 2f, 0);
            }
        }
        else if (loopMode == LoopMode.ScreenBounds)
        {
            float screenHeight = GetScreenHeight();
            float cameraY = (autoDetectCamera && Camera.main != null) ? Camera.main.transform.position.y : 0f;
            
            float bottomLimit = cameraY - (screenHeight / 2f) - (spriteHeight / 2f);
            float loopDistance = screenHeight + spriteHeight;

            if (transform.position.y <= bottomLimit)
            {
                transform.position += new Vector3(0, loopDistance, 0);
            }
        }
    }

    /// <summary>
    /// Transiciona suavemente a cor do background para escuro durante o Time Slow e retorna à cor normal ao encerrar.
    /// </summary>
    private void HandleBackgroundDimming()
    {
        if (spriteRenderer == null) return;

        Color targetColor = (globalSpeedMultiplier < 0.99f) ? timeSlowBackgroundColor : normalBackgroundColor;
        spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * dimTransitionSpeed);
    }

    private float GetScreenHeight()
    {
        if (autoDetectCamera && Camera.main != null && Camera.main.orthographic)
        {
            return Camera.main.orthographicSize * 2f;
        }
        return customScreenHeight;
    }
}
