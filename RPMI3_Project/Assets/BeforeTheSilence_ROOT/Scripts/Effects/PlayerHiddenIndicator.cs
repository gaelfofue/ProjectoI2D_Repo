using UnityEngine;

/// <summary>
/// Muestra una flecha/indicador encima del jugador cuando está escondido.
/// </summary>
public class PlayerHiddenIndicator : MonoBehaviour
{
    #region SETTINGS
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private SpriteRenderer indicatorSprite;
    [SerializeField] private Transform indicatorTransform;

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private bool followPlayer = true;

    [Header("Animation - Bobbing")]
    [SerializeField] private bool enableBobbing = true;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.15f;

    [Header("Animation - Pulse")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseMinScale = 0.9f;
    [SerializeField] private float pulseMaxScale = 1.1f;

    [Header("Animation - Fade")]
    [SerializeField] private float fadeSpeed = 5f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 0.3f, 0.9f); // Verde
    [SerializeField] private Color nervousColor = new Color(0.9f, 0.8f, 0.2f, 0.9f); // Amarillo
    [SerializeField] private Color scaredColor = new Color(0.9f, 0.4f, 0.2f, 0.9f); // Naranja
    [SerializeField] private Color panicColor = new Color(0.9f, 0.2f, 0.2f, 0.9f); // Rojo
    #endregion

    #region PRIVATE VARIABLES
    private float bobTimer = 0f;
    private float pulseTimer = 0f;
    private Vector3 baseScale;
    private float currentAlpha = 0f;
    private bool isVisible = false;
    #endregion

    #region UNITY METHODS
    private void Awake()
    {
        // Auto-encontrar PlayerController si no está asignado
        if (player == null)
        {
            player = GetComponentInParent<PlayerController>();
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }
        }

        // Crear indicador si no existe
        if (indicatorSprite == null)
        {
            CreateDefaultIndicator();
        }

        if (indicatorTransform == null && indicatorSprite != null)
        {
            indicatorTransform = indicatorSprite.transform;
        }

        if (indicatorTransform != null)
        {
            baseScale = indicatorTransform.localScale;
        }

        // Iniciar oculto
        SetAlpha(0f);
    }

    private void Update()
    {
        if (player == null) return;

        // Determinar visibilidad
        bool shouldBeVisible = player.IsHidden && player.IsInSafeZone && !player.IsDead;

        // Fade in/out
        UpdateVisibility(shouldBeVisible);

        // Si es visible, aplicar animaciones
        if (currentAlpha > 0.01f)
        {
            UpdatePosition();
            UpdateAnimations();
            UpdateColor();
        }
    }
    #endregion

    #region INDICATOR CREATION
    private void CreateDefaultIndicator()
    {
        // Crear GameObject para el indicador
        GameObject indicatorObj = new GameObject("HiddenIndicator");

        // Si este script está en el Player, hacer hijo
        if (player != null && transform == player.transform)
        {
            indicatorObj.transform.SetParent(null); // No ser hijo para no heredar escala
        }
        else
        {
            indicatorObj.transform.SetParent(transform);
        }

        // Añadir SpriteRenderer
        indicatorSprite = indicatorObj.AddComponent<SpriteRenderer>();
        indicatorTransform = indicatorObj.transform;

        // Crear sprite de flecha proceduralmente
        indicatorSprite.sprite = CreateArrowSprite();
        indicatorSprite.color = normalColor;
        indicatorSprite.sortingOrder = 100; // Encima de todo

        // Escala inicial
        indicatorTransform.localScale = Vector3.one * 0.5f;
        baseScale = indicatorTransform.localScale;

        Debug.Log("[PlayerHiddenIndicator] Indicador creado automáticamente");
    }

    private Sprite CreateArrowSprite()
    {
        // Crear textura de flecha simple (triángulo apuntando abajo)
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Bilinear;

        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;

        // Limpiar textura
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                texture.SetPixel(x, y, transparent);
            }
        }

        // Dibujar flecha (triángulo apuntando hacia abajo)
        int centerX = size / 2;
        int topY = size - 4;
        int bottomY = 4;
        int width = size / 2 - 2;

        for (int y = bottomY; y <= topY; y++)
        {
            float progress = (float)(y - bottomY) / (topY - bottomY);
            int halfWidth = Mathf.RoundToInt(width * progress);

            for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
            {
                if (x >= 0 && x < size)
                {
                    texture.SetPixel(x, y, white);
                }
            }
        }

        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    #endregion

    #region UPDATE METHODS
    private void UpdateVisibility(bool shouldBeVisible)
    {
        float targetAlpha = shouldBeVisible ? 1f : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        SetAlpha(currentAlpha);
        isVisible = currentAlpha > 0.01f;
    }

    private void UpdatePosition()
    {
        if (indicatorTransform == null || player == null) return;

        Vector3 targetPos = player.transform.position + offset;

        // Añadir bobbing
        if (enableBobbing)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bob = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmount;
            targetPos.y += bob;
        }

        if (followPlayer)
        {
            indicatorTransform.position = targetPos;
        }
    }

    private void UpdateAnimations()
    {
        if (indicatorTransform == null) return;

        // Pulse
        if (enablePulse)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = Mathf.Sin(pulseTimer * Mathf.PI * 2f);
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, (pulse + 1f) * 0.5f);
            indicatorTransform.localScale = baseScale * scale;
        }
    }

    private void UpdateColor()
    {
        if (indicatorSprite == null || player == null) return;

        float fear = player.GetFearNormalized();
        Color targetColor;

        if (player.IsPanicking)
        {
            targetColor = panicColor;
            // Parpadeo rápido en pánico
            float blink = Mathf.Sin(Time.time * 10f);
            targetColor.a = Mathf.Lerp(0.5f, 1f, (blink + 1f) * 0.5f);
        }
        else if (player.IsScared)
        {
            targetColor = Color.Lerp(nervousColor, scaredColor, (fear - 0.6f) / 0.3f);
        }
        else if (fear > 0.3f)
        {
            targetColor = Color.Lerp(normalColor, nervousColor, (fear - 0.3f) / 0.3f);
        }
        else
        {
            targetColor = normalColor;
        }

        targetColor.a *= currentAlpha;
        indicatorSprite.color = Color.Lerp(indicatorSprite.color, targetColor, Time.deltaTime * 5f);
    }

    private void SetAlpha(float alpha)
    {
        if (indicatorSprite == null) return;

        Color color = indicatorSprite.color;
        color.a = alpha;
        indicatorSprite.color = color;

        // También habilitar/deshabilitar para optimización
        indicatorSprite.enabled = alpha > 0.01f;
    }
    #endregion

    #region PUBLIC METHODS
    /// <summary>
    /// Forzar mostrar el indicador
    /// </summary>
    public void Show()
    {
        currentAlpha = 1f;
        SetAlpha(1f);
    }

    /// <summary>
    /// Forzar ocultar el indicador
    /// </summary>
    public void Hide()
    {
        currentAlpha = 0f;
        SetAlpha(0f);
    }

    /// <summary>
    /// Establecer sprite personalizado
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (indicatorSprite != null && sprite != null)
        {
            indicatorSprite.sprite = sprite;
        }
    }
    #endregion

    #region CLEANUP
    private void OnDestroy()
    {
        // Limpiar el indicador si fue creado dinámicamente
        if (indicatorSprite != null && indicatorSprite.gameObject != this.gameObject)
        {
            Destroy(indicatorSprite.gameObject);
        }
    }
    #endregion
}