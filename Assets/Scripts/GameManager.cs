using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Nomes das Cenas")]
    [Tooltip("Nome da cena inicial (Menu / Start).")]
    [SerializeField] private string initialSceneName = "initial_scene";

    [Tooltip("Nome da primeira fase (Grama).")]
    [SerializeField] private string grassSceneName = "fase_grass";

    [Tooltip("Nome da segunda fase (Deserto).")]
    [SerializeField] private string desertSceneName = "fase_desert";

    [Header("Pontuação para Troca de Fase")]
    [Tooltip("Pontuação necessária no Grass para ir para o Desert.")]
    [SerializeField] private int grassToDesertTargetScore = 1500;

    [Tooltip("Pontuação necessária no Desert para voltar para o Grass.")]
    [SerializeField] private int desertToGrassTargetScore = 3500;

    [Header("Recompensa por Pontuação")]
    [Tooltip("Intervalo de pontuação necessário para recuperar 1 vida (Padrão: 1000).")]
    [SerializeField] private int extraLifeScoreInterval = 1000;

    [Header("Estatísticas da Partida (Leitura)")]
    public int totalScore = 0;
    public float totalTime = 0f;
    public int smallKilled = 0;
    public int mediumKilled = 0;
    public int bigKilled = 0;
    public int powerUpsCollected = 0;

    public bool isGameOver { get; private set; } = false;
    private bool isGameActive = false;
    private bool canRestartOnKeyPress = false;
    private bool hasTransitionedToDesert = false;

    private int lastAwardedExtraLifeScore = 0;
    private GameObject gameOverPanelObj;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (isGameActive && !isGameOver)
        {
            totalTime += Time.deltaTime;
            CheckSceneTransition();
        }

        if (isGameOver && canRestartOnKeyPress)
        {
            if (WasAnyKeyPressed())
            {
                RestartToInitialScene();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name.ToLower();

        if (sceneName.Contains("initial"))
        {
            isGameActive = false;
            isGameOver = false;
            canRestartOnKeyPress = false;
            hasTransitionedToDesert = false;
            ResetStats();
        }
        else
        {
            isGameActive = true;
            isGameOver = false;
            canRestartOnKeyPress = false;
        }
    }

    /// <summary>
    /// Inicia o jogo a partir da cena inicial ao pressionar qualquer tecla.
    /// </summary>
    public void StartGameFromInitial()
    {
        ResetStats();
        LoadSceneSafely(grassSceneName, "fase_grass", "scene_grass");
    }

    /// <summary>
    /// Adiciona pontos ao placar e verifica se o jogador atingiu marco para recuperar 1 vida.
    /// </summary>
    public void AddScore(int points)
    {
        if (isGameOver) return;
        totalScore += points;

        CheckExtraLifeReward();
    }

    /// <summary>
    /// Concede 1 vida extra a cada 1000 pontos acumulados.
    /// </summary>
    private void CheckExtraLifeReward()
    {
        if (extraLifeScoreInterval <= 0) return;

        int extraLivesEarned = (totalScore / extraLifeScoreInterval) - (lastAwardedExtraLifeScore / extraLifeScoreInterval);

        if (extraLivesEarned > 0)
        {
            lastAwardedExtraLifeScore = (totalScore / extraLifeScoreInterval) * extraLifeScoreInterval;

            if (Player.Instance != null)
            {
                Player.Instance.RecoverLife(extraLivesEarned);
            }
        }
    }

    public void RegisterEnemyKilled(Enemy.EnemyType enemyType)
    {
        if (isGameOver) return;

        switch (enemyType)
        {
            case Enemy.EnemyType.Small:
                smallKilled++;
                break;
            case Enemy.EnemyType.Medium:
                mediumKilled++;
                break;
            case Enemy.EnemyType.Big:
                bigKilled++;
                break;
        }
    }

    public void RegisterPowerUpCollected()
    {
        if (isGameOver) return;
        powerUpsCollected++;
    }

    private void CheckSceneTransition()
    {
        string activeSceneName = SceneManager.GetActiveScene().name.ToLower();

        if (!hasTransitionedToDesert && (activeSceneName.Contains("grass") || activeSceneName.Contains("sample")))
        {
            if (totalScore >= grassToDesertTargetScore)
            {
                hasTransitionedToDesert = true;
                Debug.Log($"GameManager: Alvo ativado! Troca de fase: Grass -> Desert ({desertSceneName})");
                LoadSceneSafely(desertSceneName, "fase_desert", "scene_desert");
            }
        }
        else if (hasTransitionedToDesert && activeSceneName.Contains("desert"))
        {
            if (totalScore >= desertToGrassTargetScore)
            {
                hasTransitionedToDesert = false;
                Debug.Log($"GameManager: Alvo ativado! Troca de fase: Desert -> Grass ({grassSceneName})");
                LoadSceneSafely(grassSceneName, "fase_grass", "scene_grass");
            }
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        isGameActive = false;

        StartCoroutine(ShowGameOverUIWithDelay());
    }

    private IEnumerator ShowGameOverUIWithDelay()
    {
        yield return new WaitForSeconds(0.8f);

        BuildGameOverUI();

        yield return new WaitForSeconds(0.6f);
        canRestartOnKeyPress = true;
    }

    private void BuildGameOverUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null)
        {
            defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
        }

        gameOverPanelObj = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        gameOverPanelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = gameOverPanelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image panelImg = gameOverPanelObj.GetComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.90f);

        GameObject contentObj = new GameObject("GameOverContent", typeof(RectTransform));
        contentObj.transform.SetParent(gameOverPanelObj.transform, false);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(850, 650);

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18f;
        layout.childControlWidth = false;

        CreateText(contentObj, "GAME OVER", defaultFont, 56, Color.red, FontStyle.Bold);

        int minutes = Mathf.FloorToInt(totalTime / 60f);
        int seconds = Mathf.FloorToInt(totalTime % 60f);
        string timeStr = $"{minutes:00}m {seconds:00}s";

        CreateText(contentObj, $"Pontuação Total: {totalScore}", defaultFont, 38, Color.yellow, FontStyle.Bold);
        CreateText(contentObj, $"Tempo de Jogo: {timeStr}", defaultFont, 30, Color.white, FontStyle.Normal);

        string killsStr = $"Naves Destruídas:\nSmall: {smallKilled}  |  Medium: {mediumKilled}  |  Big: {bigKilled}";
        CreateText(contentObj, killsStr, defaultFont, 28, Color.cyan, FontStyle.Normal);

        CreateText(contentObj, $"Power-Ups Coletados: {powerUpsCollected}", defaultFont, 28, Color.green, FontStyle.Normal);

        CreateText(contentObj, "\nPressione qualquer tecla para voltar ao início", defaultFont, 30, Color.white, FontStyle.Italic);
    }

    private Text CreateText(GameObject parent, string textStr, Font font, int size, Color color, FontStyle style)
    {
        GameObject textObj = new GameObject("TextItem", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(parent.transform, false);
        Text txt = textObj.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = color;
        txt.text = textStr;
        txt.alignment = TextAnchor.MiddleCenter;
        textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, size + 22);
        return txt;
    }

    public void RestartToInitialScene()
    {
        isGameOver = false;
        canRestartOnKeyPress = false;
        ResetStats();
        LoadSceneSafely(initialSceneName, "initial_scene");
    }

    private void ResetStats()
    {
        totalScore = 0;
        totalTime = 0f;
        smallKilled = 0;
        mediumKilled = 0;
        bigKilled = 0;
        powerUpsCollected = 0;
        lastAwardedExtraLifeScore = 0;

        if (Player.Instance != null)
        {
            Player.Instance.ResetPowerUps();
        }
        else
        {
            Player.ResetStaticModifiers();
        }
    }

    private bool WasAnyKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Gamepad.current != null)
        {
            var pad = Gamepad.current;
            if (pad.buttonSouth.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
                pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame ||
                pad.startButton.wasPressedThisFrame) return true;
        }
#endif
        try
        {
            if (Input.anyKeyDown) return true;
        }
        catch { }

        return false;
    }

    private void LoadSceneSafely(params string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            if (Application.CanStreamedLevelBeLoaded(name))
            {
                SceneManager.LoadScene(name);
                return;
            }
        }
        if (possibleNames.Length > 0)
        {
            SceneManager.LoadScene(possibleNames[0]);
        }
    }
}
