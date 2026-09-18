using UnityEngine;

public class Parallax : MonoBehaviour
{
    public enum LoopMode
    {
        FullBackground, // Para o mapa/fundo de tela cheia (usa 2 sprites empilhados)
        ScreenBounds    // Para nuvens e objetos menores que a tela (reaparecem no topo ao saírem embaixo)
    }

    [Header("Configurações Gerais")]
    [SerializeField] private LoopMode loopMode = LoopMode.FullBackground;
    [SerializeField] private float parallaxEffect = 1f;

    [Header("Configurações da Câmera / Tela")]
    [Tooltip("Detecta a altura da Câmera Principal automaticamente.")]
    [SerializeField] private bool autoDetectCamera = true;
    [SerializeField] private float customScreenHeight = 10f;

    private float spriteHeight;

    void Start()
    {
        if (TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
        {
            spriteHeight = sr.bounds.size.y;
        }
        else
        {
            spriteHeight = 2f;
        }
    }

    void Update()
    {
        // Movimento continuo para baixo
        transform.position += Vector3.down * Time.deltaTime * parallaxEffect;

        if (loopMode == LoopMode.FullBackground)
        {
            // Loop para mapas/fundo de tela cheia
            if (transform.position.y <= -spriteHeight)
            {
                transform.position += new Vector3(0, spriteHeight * 2f, 0);
            }
        }
        else if (loopMode == LoopMode.ScreenBounds)
        {
            // Loop para nuvens: quando sai por baixo da tela, reaparece no topo
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

    private float GetScreenHeight()
    {
        if (autoDetectCamera && Camera.main != null && Camera.main.orthographic)
        {
            return Camera.main.orthographicSize * 2f;
        }
        return customScreenHeight;
    }
}
