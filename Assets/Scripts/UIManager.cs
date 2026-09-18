using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Configuração Automática da UI")]
    [Tooltip("Sprite da nave do jogador. Se deixado em branco, o script pegará o sprite do Player automaticamente.")]
    [SerializeField] private Sprite playerLifeSprite;

    [Tooltip("Cor do ícone da vida ativa (Padrão: Branco).")]
    [SerializeField] private Color activeLifeColor = Color.white;

    [Tooltip("Cor do ícone quando a vida é perdida (Padrão: Preto).")]
    [SerializeField] private Color lostLifeColor = Color.black;

    [Header("Tamanhos e Fontes")]
    [Tooltip("Tamanho da fonte dos textos da UI (Vidas e Score).")]
    [SerializeField] private int fontSize = 48;

    [Tooltip("Tamanho base (largura x altura) dos ícones de vida na tela.")]
    [SerializeField] private Vector2 lifeIconSize = new Vector2(54f, 54f);

    [Tooltip("Espaçamento entre os elementos de vida.")]
    [SerializeField] private float iconSpacing = 16f;

    [Header("Pontuação por Sobrevivência")]
    [Tooltip("Pontos concedidos a cada 5 segundos de sobrevivência.")]
    [SerializeField] private int survivalPoints = 50;

    [Tooltip("Intervalo para o bônus de sobrevivência.")]
    [SerializeField] private float survivalInterval = 5f;

    private Canvas canvas;
    private Image[] lifeImages = new Image[3];
    private Text scoreText;
    private Text livesLabelText;

    private int currentScore = 0;
    private float survivalTimer = 0f;
    private bool isPlayerAlive = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        BuildUIAutomatically();
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    private void Update()
    {
        if (isPlayerAlive)
        {
            HandleSurvivalScore();
        }
    }

    /// <summary>
    /// Constrói do zero toda a estrutura da Canvas com fontes e ícones maiores.
    /// </summary>
    private void BuildUIAutomatically()
    {
        // 1. Cria e configura o Canvas
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Obtém a fonte padrão nativa do Unity
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null)
        {
            defaultFont = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        }

        // 2. Container de Vidas (Canto Superior Esquerdo - Ampliado)
        GameObject livesContainer = new GameObject("LivesContainer", typeof(RectTransform));
        livesContainer.transform.SetParent(canvas.transform, false);
        RectTransform livesRect = livesContainer.GetComponent<RectTransform>();
        livesRect.anchorMin = new Vector2(0, 1);
        livesRect.anchorMax = new Vector2(0, 1);
        livesRect.pivot = new Vector2(0, 1);
        livesRect.anchoredPosition = new Vector2(40, -40);
        livesRect.sizeDelta = new Vector2(450, 80);

        HorizontalLayoutGroup layout = livesContainer.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = iconSpacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Texto "Vidas:" Maior
        GameObject labelObj = new GameObject("LivesLabel", typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(livesContainer.transform, false);
        livesLabelText = labelObj.GetComponent<Text>();
        livesLabelText.font = defaultFont;
        livesLabelText.fontSize = fontSize;
        livesLabelText.fontStyle = FontStyle.Bold;
        livesLabelText.color = Color.white;
        livesLabelText.text = "Vidas:";
        labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 60);

        // 3 Ícones das Naves Maiores
        for (int i = 0; i < 3; i++)
        {
            GameObject iconObj = new GameObject($"LifeIcon_{i + 1}", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(livesContainer.transform, false);
            Image img = iconObj.GetComponent<Image>();
            if (playerLifeSprite != null)
            {
                img.sprite = playerLifeSprite;
            }
            img.color = activeLifeColor;
            iconObj.GetComponent<RectTransform>().sizeDelta = lifeIconSize;
            lifeImages[i] = img;
        }

        // 3. Placar de Score (Canto Superior Direito - Maior)
        GameObject scoreObj = new GameObject("ScoreText", typeof(RectTransform), typeof(Text));
        scoreObj.transform.SetParent(canvas.transform, false);
        RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(1, 1);
        scoreRect.anchorMax = new Vector2(1, 1);
        scoreRect.pivot = new Vector2(1, 1);
        scoreRect.anchoredPosition = new Vector2(-40, -40);
        scoreRect.sizeDelta = new Vector2(450, 80);

        scoreText = scoreObj.GetComponent<Text>();
        scoreText.font = defaultFont;
        scoreText.fontSize = fontSize;
        scoreText.fontStyle = FontStyle.Bold;
        scoreText.alignment = TextAnchor.MiddleRight;
        scoreText.color = Color.yellow;
        scoreText.text = "Score: 0";
    }

    /// <summary>
    /// Define automaticamente o sprite da nave a partir do Player caso não tenha sido atribuído no Inspector.
    /// </summary>
    public void SetPlayerSpriteIfMissing(Sprite sprite)
    {
        if (sprite == null) return;

        playerLifeSprite = sprite;
        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] != null && lifeImages[i].sprite == null)
            {
                lifeImages[i].sprite = playerLifeSprite;
            }
        }
    }

    /// <summary>
    /// Soma 50 pontos a cada 5 segundos que o jogador permanece vivo.
    /// </summary>
    private void HandleSurvivalScore()
    {
        survivalTimer += Time.deltaTime;

        if (survivalTimer >= survivalInterval)
        {
            survivalTimer -= survivalInterval;
            AddScore(survivalPoints);
        }
    }

    /// <summary>
    /// Adiciona pontos ao placar.
    /// </summary>
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    /// <summary>
    /// Atualiza o display das 3 vidas. Deixa a cor do ícone em preto quando a vida é perdida.
    /// </summary>
    public void UpdateLivesUI(int currentLives)
    {
        if (lifeImages == null) return;

        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] != null)
            {
                lifeImages[i].color = (i < currentLives) ? activeLifeColor : lostLifeColor;
            }
        }

        if (currentLives <= 0)
        {
            isPlayerAlive = false;
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }

    public int GetCurrentScore() => currentScore;
}
