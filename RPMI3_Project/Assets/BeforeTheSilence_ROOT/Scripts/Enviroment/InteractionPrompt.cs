using UnityEngine;
using TMPro;

/// <summary>
/// Muestra un prompt de interacción personalizable encima de objetos interactuables.
/// </summary>
public class InteractionPrompt : MonoBehaviour
{
    #region PROMPT TYPE
    public enum PromptType
    {
        TextOnly,           // Solo texto (E)
        SpriteOnly,         // Solo sprite/imagen
        SpriteWithText      // Sprite + texto
    }
    #endregion

    #region SETTINGS
    [Header("Prompt Type")]
    [SerializeField] private PromptType promptType = PromptType.TextOnly;

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private bool followTarget = true;
    [SerializeField] private Transform customTarget; // Si es null, usa este transform

    [Header("Text Settings")]
    [SerializeField] private string promptText = "E";
    [SerializeField] private TMP_FontAsset customFont;
    [SerializeField] private float fontSize = 5f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color textOutlineColor = Color.black;
    [SerializeField] private float outlineWidth = 0.2f;
    [SerializeField] private FontStyles fontStyle = FontStyles.Bold;

    [Header("Background Settings")]
    [SerializeField] private bool showBackground = true;
    [SerializeField] private Sprite customBackgroundSprite;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Vector2 backgroundPadding = new Vector2(0.5f, 0.3f);
    [SerializeField] private Vector2 backgroundSize = new Vector2(1f, 1f);

    [Header("Icon/Sprite Settings")]
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private Color iconColor = Color.white;
    [SerializeField] private Vector2 iconSize = new Vector2(1f, 1f);
    [SerializeField] private Vector3 iconOffset = Vector3.zero;

    [Header("Highlight Settings")]
    [SerializeField] private bool enableHighlight = true;
    [SerializeField] private Color highlightTextColor = new Color(0.3f, 1f, 0.4f);
    [SerializeField] private Color highlightBackgroundColor = new Color(0.1f, 0.3f, 0.1f, 0.85f);
    [SerializeField] private Color highlightIconColor = new Color(0.3f, 1f, 0.4f);
    [SerializeField] private float highlightDistance = 1.5f;

    [Header("Animation - Bobbing")]
    [SerializeField] private bool enableBobbing = true;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.1f;

    [Header("Animation - Pulse/Scale")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseMinScale = 0.95f;
    [SerializeField] private float pulseMaxScale = 1.05f;

    [Header("Animation - Fade")]
    [SerializeField] private float fadeSpeed = 8f;

    [Header("Behavior")]
    [SerializeField] private bool showOnlyWhenPlayerNear = true;
    [SerializeField] private float detectionRadius = 2.5f;
    [SerializeField] private bool hideWhenPlayerHidden = true;
    [SerializeField] private int sortingOrder = 100;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    #endregion

    #region PRIVATE VARIABLES
    private GameObject promptContainer;
    private Transform promptTransform;

    // Text components
    private GameObject textObject;
    private TextMeshPro textMesh;

    // Background components
    private GameObject backgroundObject;
    private SpriteRenderer backgroundRenderer;

    // Icon components
    private GameObject iconObject;
    private SpriteRenderer iconRenderer;

    // State
    private PlayerController player;
    private bool isPlayerInRange = false;
    private float currentAlpha = 0f;
    private float bobTimer = 0f;
    private float pulseTimer = 0f;
    private Vector3 baseScale;
    private bool canInteract = true;
    private bool isInitialized = false;

    // Original colors for lerping
    private Color originalTextColor;
    private Color originalBackgroundColor;
    private Color originalIconColor;
    #endregion

    #region UNITY METHODS
    private void Start()
    {
        CreatePrompt();
        FindPlayer();
        SetAlpha(0f);
        isInitialized = true;

        if (debugMode)
            Debug.Log($"[InteractionPrompt] Inicializado en {gameObject.name}");
    }

    private void Update()
    {
        if (!isInitialized || promptContainer == null) return;

        CheckPlayerDistance();

        bool shouldShow = isPlayerInRange && canInteract && !IsPlayerHidden();

        UpdateVisibility(shouldShow);

        if (currentAlpha > 0.01f)
        {
            UpdatePosition();
            UpdateAnimation();

            if (enableHighlight)
                UpdateHighlight();
        }
    }

    private void OnDestroy()
    {
        if (promptContainer != null)
        {
            Destroy(promptContainer);
        }
    }

    private void OnValidate()
    {
        // Actualizar en tiempo real en el editor
        if (isInitialized && promptContainer != null)
        {
            UpdatePromptAppearance();
        }
    }
    #endregion

    #region CREATION
    private void CreatePrompt()
    {
        // Contenedor principal
        promptContainer = new GameObject("InteractionPrompt_" + gameObject.name);
        promptContainer.transform.SetParent(null);
        promptTransform = promptContainer.transform;

        // Crear según el tipo
        switch (promptType)
        {
            case PromptType.TextOnly:
                CreateBackground();
                CreateText();
                break;

            case PromptType.SpriteOnly:
                CreateIcon();
                break;

            case PromptType.SpriteWithText:
                CreateBackground();
                CreateIcon();
                CreateText();
                break;
        }

        // Escala base
        promptTransform.localScale = Vector3.one;
        baseScale = promptTransform.localScale;

        // Guardar colores originales
        originalTextColor = textColor;
        originalBackgroundColor = backgroundColor;
        originalIconColor = iconColor;

        UpdatePosition();
    }

    private void CreateText()
    {
        textObject = new GameObject("PromptText");
        textObject.transform.SetParent(promptTransform);
        textObject.transform.localPosition = Vector3.zero;

        textMesh = textObject.AddComponent<TextMeshPro>();

        // Configurar texto
        textMesh.text = promptText;
        textMesh.fontSize = fontSize;
        textMesh.color = textColor;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontStyle = fontStyle;

        // Fuente personalizada
        if (customFont != null)
        {
            textMesh.font = customFont;
        }

        // Outline
        if (outlineWidth > 0)
        {
            textMesh.outlineWidth = outlineWidth;
            textMesh.outlineColor = textOutlineColor;
        }

        // Sorting
        textMesh.sortingOrder = sortingOrder + 1;

        // RectTransform
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(10f, 5f);

        // Ajustar posición si hay icono
        if (promptType == PromptType.SpriteWithText && iconSprite != null)
        {
            textObject.transform.localPosition = new Vector3(iconSize.x * 0.6f, 0f, -0.01f);
        }
    }

    private void CreateBackground()
    {
        if (!showBackground) return;

        backgroundObject = new GameObject("PromptBackground");
        backgroundObject.transform.SetParent(promptTransform);
        backgroundObject.transform.localPosition = new Vector3(0f, 0f, 0.01f);

        backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();

        // Sprite personalizado o generado
        if (customBackgroundSprite != null)
        {
            backgroundRenderer.sprite = customBackgroundSprite;
        }
        else
        {
            backgroundRenderer.sprite = CreateDefaultBackgroundSprite();
        }

        backgroundRenderer.color = backgroundColor;
        backgroundRenderer.sortingOrder = sortingOrder;

        // Tamaño
        UpdateBackgroundSize();
    }

    private void CreateIcon()
    {
        if (iconSprite == null && promptType != PromptType.TextOnly)
        {
            if (debugMode)
                Debug.LogWarning("[InteractionPrompt] No hay sprite de icono asignado");
            return;
        }

        iconObject = new GameObject("PromptIcon");
        iconObject.transform.SetParent(promptTransform);
        iconObject.transform.localPosition = iconOffset;

        iconRenderer = iconObject.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = iconSprite;
        iconRenderer.color = iconColor;
        iconRenderer.sortingOrder = sortingOrder + 2;

        // Tamaño
        iconObject.transform.localScale = new Vector3(iconSize.x, iconSize.y, 1f);
    }

    private Sprite CreateDefaultBackgroundSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Bilinear;

        int radius = 12;
        Color white = Color.white;
        Color transparent = new Color(0, 0, 0, 0);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                bool inCorner = false;
                float dist = 0f;

                // Esquinas redondeadas
                if (x < radius && y < radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    inCorner = true;
                }
                else if (x >= size - radius && y < radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - radius - 1, radius));
                    inCorner = true;
                }
                else if (x < radius && y >= size - radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, size - radius - 1));
                    inCorner = true;
                }
                else if (x >= size - radius && y >= size - radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - radius - 1, size - radius - 1));
                    inCorner = true;
                }

                texture.SetPixel(x, y, inCorner ? (dist <= radius ? white : transparent) : white);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void UpdateBackgroundSize()
    {
        if (backgroundObject == null) return;

        Vector2 size = backgroundSize;

        // Ajustar según contenido
        if (promptType == PromptType.SpriteWithText && iconSprite != null)
        {
            size.x += iconSize.x * 0.5f;
        }

        size += backgroundPadding;
        backgroundObject.transform.localScale = new Vector3(size.x, size.y, 1f);
    }
    #endregion

    #region UPDATE METHODS
    private void FindPlayer()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }
    }

    private void CheckPlayerDistance()
    {
        if (player == null)
        {
            FindPlayer();
            isPlayerInRange = false;
            return;
        }

        if (showOnlyWhenPlayerNear)
        {
            float distance = Vector2.Distance(GetTargetPosition(), player.transform.position);
            isPlayerInRange = distance <= detectionRadius;
        }
        else
        {
            isPlayerInRange = true;
        }
    }

    private bool IsPlayerHidden()
    {
        if (!hideWhenPlayerHidden) return false;
        return player != null && player.IsHidden;
    }

    private Vector3 GetTargetPosition()
    {
        return customTarget != null ? customTarget.position : transform.position;
    }

    private void UpdateVisibility(bool shouldShow)
    {
        float targetAlpha = shouldShow ? 1f : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        SetAlpha(currentAlpha);
    }

    private void UpdatePosition()
    {
        if (promptTransform == null) return;

        Vector3 targetPos = GetTargetPosition() + offset;

        // Bobbing
        if (enableBobbing)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bob = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmount;
            targetPos.y += bob;
        }

        if (followTarget)
        {
            promptTransform.position = targetPos;
        }
    }

    private void UpdateAnimation()
    {
        if (promptTransform == null) return;

        // Pulse
        if (enablePulse)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = Mathf.Sin(pulseTimer * Mathf.PI * 2f);
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, (pulse + 1f) * 0.5f);
            promptTransform.localScale = baseScale * scale;
        }
    }

    private void UpdateHighlight()
    {
        if (player == null) return;

        float distance = Vector2.Distance(GetTargetPosition(), player.transform.position);
        bool isClose = distance <= highlightDistance;

        float lerpSpeed = Time.deltaTime * 5f;

        // Lerp colores
        if (textMesh != null)
        {
            Color targetTextColor = isClose ? highlightTextColor : originalTextColor;
            textMesh.color = Color.Lerp(textMesh.color, targetTextColor, lerpSpeed);
        }

        if (backgroundRenderer != null)
        {
            Color targetBgColor = isClose ? highlightBackgroundColor : originalBackgroundColor;
            targetBgColor.a *= currentAlpha;
            backgroundRenderer.color = Color.Lerp(backgroundRenderer.color, targetBgColor, lerpSpeed);
        }

        if (iconRenderer != null)
        {
            Color targetIconColor = isClose ? highlightIconColor : originalIconColor;
            iconRenderer.color = Color.Lerp(iconRenderer.color, targetIconColor, lerpSpeed);
        }
    }

    private void SetAlpha(float alpha)
    {
        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = alpha;
            textMesh.color = c;
        }

        if (backgroundRenderer != null)
        {
            Color c = backgroundRenderer.color;
            c.a = alpha * backgroundColor.a;
            backgroundRenderer.color = c;
            backgroundRenderer.enabled = alpha > 0.01f;
        }

        if (iconRenderer != null)
        {
            Color c = iconRenderer.color;
            c.a = alpha;
            iconRenderer.color = c;
            iconRenderer.enabled = alpha > 0.01f;
        }
    }

    private void UpdatePromptAppearance()
    {
        if (textMesh != null)
        {
            textMesh.text = promptText;
            textMesh.fontSize = fontSize;
            textMesh.color = textColor;
            textMesh.fontStyle = fontStyle;

            if (customFont != null)
                textMesh.font = customFont;

            textMesh.outlineWidth = outlineWidth;
            textMesh.outlineColor = textOutlineColor;
        }

        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = backgroundColor;
            if (customBackgroundSprite != null)
                backgroundRenderer.sprite = customBackgroundSprite;
            UpdateBackgroundSize();
        }

        if (iconRenderer != null)
        {
            iconRenderer.sprite = iconSprite;
            iconRenderer.color = iconColor;
            iconObject.transform.localScale = new Vector3(iconSize.x, iconSize.y, 1f);
            iconObject.transform.localPosition = iconOffset;
        }

        originalTextColor = textColor;
        originalBackgroundColor = backgroundColor;
        originalIconColor = iconColor;
    }
    #endregion

    #region PUBLIC METHODS
    /// <summary>
    /// Cambiar el texto del prompt
    /// </summary>
    public void SetText(string text)
    {
        promptText = text;
        if (textMesh != null)
        {
            textMesh.text = text;
        }
    }

    /// <summary>
    /// Cambiar el icono/sprite
    /// </summary>
    public void SetIcon(Sprite sprite)
    {
        iconSprite = sprite;
        if (iconRenderer != null)
        {
            iconRenderer.sprite = sprite;
        }
    }

    /// <summary>
    /// Cambiar el sprite de fondo
    /// </summary>
    public void SetBackground(Sprite sprite)
    {
        customBackgroundSprite = sprite;
        if (backgroundRenderer != null)
        {
            backgroundRenderer.sprite = sprite;
        }
    }

    /// <summary>
    /// Cambiar color del texto
    /// </summary>
    public void SetTextColor(Color color)
    {
        textColor = color;
        originalTextColor = color;
        if (textMesh != null)
        {
            textMesh.color = color;
        }
    }

    /// <summary>
    /// Cambiar color del icono
    /// </summary>
    public void SetIconColor(Color color)
    {
        iconColor = color;
        originalIconColor = color;
        if (iconRenderer != null)
        {
            iconRenderer.color = color;
        }
    }

    /// <summary>
    /// Cambiar color del fondo
    /// </summary>
    public void SetBackgroundColor(Color color)
    {
        backgroundColor = color;
        originalBackgroundColor = color;
        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = color;
        }
    }

    /// <summary>
    /// Habilitar/deshabilitar interacción
    /// </summary>
    public void SetCanInteract(bool can)
    {
        canInteract = can;
    }

    /// <summary>
    /// Forzar mostrar
    /// </summary>
    public void Show()
    {
        canInteract = true;
        currentAlpha = 1f;
        SetAlpha(1f);
    }

    /// <summary>
    /// Forzar ocultar
    /// </summary>
    public void Hide()
    {
        currentAlpha = 0f;
        SetAlpha(0f);
    }

    /// <summary>
    /// Flash de confirmación cuando se usa
    /// </summary>
    public void FlashConfirm()
    {
        StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        // Flash de color
        if (textMesh != null)
            textMesh.color = highlightTextColor;
        if (iconRenderer != null)
            iconRenderer.color = highlightIconColor;

        // Scale up
        Vector3 originalScale = promptTransform.localScale;
        promptTransform.localScale = originalScale * 1.3f;

        yield return new WaitForSeconds(0.1f);

        // Volver
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.2f;
            promptTransform.localScale = Vector3.Lerp(originalScale * 1.3f, originalScale, t);
            yield return null;
        }

        promptTransform.localScale = originalScale;
    }

    /// <summary>
    /// Reconstruir el prompt (útil después de cambiar el tipo)
    /// </summary>
    public void Rebuild()
    {
        if (promptContainer != null)
        {
            Destroy(promptContainer);
        }

        CreatePrompt();
        SetAlpha(currentAlpha);
    }
    #endregion

    #region GIZMOS
    private void OnDrawGizmosSelected()
    {
        Vector3 targetPos = customTarget != null ? customTarget.position : transform.position;

        // Radio de detección
        if (showOnlyWhenPlayerNear)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(targetPos, detectionRadius);
        }

        // Radio de highlight
        if (enableHighlight)
        {
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(targetPos, highlightDistance);
        }

        // Posición del prompt
        Gizmos.color = Color.cyan;
        Vector3 promptPos = targetPos + offset;
        Gizmos.DrawWireSphere(promptPos, 0.15f);
        Gizmos.DrawLine(targetPos, promptPos);
    }
    #endregion
}