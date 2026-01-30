using UnityEngine;
using TMPro;

/// <summary>
/// Muestra un prompt de interacción (tecla E) encima de objetos interactuables.
/// </summary>
public class InteractionPrompt : MonoBehaviour
{
    #region SETTINGS
    [Header("Display Settings")]
    [SerializeField] private string promptText = "E";
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);

    [Header("Visual Settings")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color highlightColor = new Color(0.3f, 0.9f, 0.4f); // Verde cuando puede interactuar
    [SerializeField] private float fontSize = 2f;
    [SerializeField] private float padding = 0.3f;

    [Header("Animation")]
    [SerializeField] private bool enableBobbing = true;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.1f;
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseAmount = 0.1f;
    [SerializeField] private float fadeSpeed = 8f;

    [Header("Behavior")]
    [SerializeField] private bool autoHideWhenUsed = true;
    [SerializeField] private bool showOnlyWhenPlayerNear = true;
    [SerializeField] private float detectionRadius = 2f;
    #endregion

    #region PRIVATE VARIABLES
    private GameObject promptObject;
    private TextMeshPro textMesh;
    private SpriteRenderer backgroundSprite;
    private Transform promptTransform;

    private PlayerController player;
    private bool isPlayerInRange = false;
    private bool isVisible = false;
    private float currentAlpha = 0f;
    private float bobTimer = 0f;
    private float pulseTimer = 0f;
    private Vector3 baseScale;

    private bool canInteract = true;
    #endregion

    #region UNITY METHODS
    private void Start()
    {
        CreatePrompt();
        FindPlayer();

        // Iniciar oculto
        SetAlpha(0f);
    }

    private void Update()
    {
        if (promptObject == null) return;

        // Verificar si el jugador está cerca
        CheckPlayerDistance();

        // Determinar si debe mostrarse
        bool shouldShow = isPlayerInRange && canInteract && !IsPlayerHidden();

        // Actualizar visibilidad
        UpdateVisibility(shouldShow);

        // Si está visible, animar
        if (currentAlpha > 0.01f)
        {
            UpdatePosition();
            UpdateAnimation();
        }
    }

    private void OnDestroy()
    {
        if (promptObject != null)
        {
            Destroy(promptObject);
        }
    }
    #endregion

    #region CREATION
    private void CreatePrompt()
    {
        // Crear objeto contenedor
        promptObject = new GameObject("InteractionPrompt");
        promptObject.transform.SetParent(null); // Independiente en la jerarquía
        promptTransform = promptObject.transform;

        // Crear fondo
        GameObject bgObject = new GameObject("Background");
        bgObject.transform.SetParent(promptTransform);
        backgroundSprite = bgObject.AddComponent<SpriteRenderer>();
        backgroundSprite.sprite = CreateRoundedRectSprite();
        backgroundSprite.color = backgroundColor;
        backgroundSprite.sortingOrder = 99;
        bgObject.transform.localScale = new Vector3(fontSize + padding * 2, fontSize + padding, 1f);

        // Crear texto
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(promptTransform);
        textMesh = textObject.AddComponent<TextMeshPro>();
        textMesh.text = promptText;
        textMesh.fontSize = fontSize * 10;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = textColor;
        textMesh.sortingOrder = 100;

        // Configurar RectTransform del texto
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(fontSize * 2, fontSize * 2);
        textRect.localPosition = new Vector3(0f, -fontSize * 0.1f, -0.01f);

        // Escala base
        promptTransform.localScale = Vector3.one * 0.5f;
        baseScale = promptTransform.localScale;

        // Posición inicial
        UpdatePosition();
    }

    private Sprite CreateRoundedRectSprite()
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
                // Calcular distancia a las esquinas para redondear
                bool inCorner = false;
                float dist = 0f;

                // Esquina inferior izquierda
                if (x < radius && y < radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    inCorner = true;
                }
                // Esquina inferior derecha
                else if (x >= size - radius && y < radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - radius - 1, radius));
                    inCorner = true;
                }
                // Esquina superior izquierda
                else if (x < radius && y >= size - radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, size - radius - 1));
                    inCorner = true;
                }
                // Esquina superior derecha
                else if (x >= size - radius && y >= size - radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - radius - 1, size - radius - 1));
                    inCorner = true;
                }

                if (inCorner)
                {
                    texture.SetPixel(x, y, dist <= radius ? white : transparent);
                }
                else
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
            float distance = Vector2.Distance(transform.position, player.transform.position);
            isPlayerInRange = distance <= detectionRadius;
        }
        else
        {
            isPlayerInRange = true;
        }
    }

    private bool IsPlayerHidden()
    {
        return player != null && player.IsHidden;
    }

    private void UpdateVisibility(bool shouldShow)
    {
        float targetAlpha = shouldShow ? 1f : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        SetAlpha(currentAlpha);
        isVisible = currentAlpha > 0.01f;
    }

    private void UpdatePosition()
    {
        if (promptTransform == null) return;

        Vector3 targetPos = transform.position + offset;

        // Bobbing
        if (enableBobbing)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bob = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmount;
            targetPos.y += bob;
        }

        promptTransform.position = targetPos;
    }

    private void UpdateAnimation()
    {
        if (promptTransform == null) return;

        // Pulse
        if (enablePulse)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = Mathf.Sin(pulseTimer * Mathf.PI * 2f);
            float scale = 1f + pulse * pulseAmount;
            promptTransform.localScale = baseScale * scale;
        }

        // Highlight cuando el jugador está muy cerca
        if (player != null && textMesh != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            float closeThreshold = detectionRadius * 0.5f;

            if (distance <= closeThreshold)
            {
                textMesh.color = Color.Lerp(textMesh.color, highlightColor, Time.deltaTime * 5f);
                backgroundSprite.color = Color.Lerp(backgroundSprite.color,
                    new Color(highlightColor.r * 0.3f, highlightColor.g * 0.3f, highlightColor.b * 0.3f, 0.8f),
                    Time.deltaTime * 5f);
            }
            else
            {
                textMesh.color = Color.Lerp(textMesh.color, textColor, Time.deltaTime * 5f);
                backgroundSprite.color = Color.Lerp(backgroundSprite.color, backgroundColor, Time.deltaTime * 5f);
            }
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

        if (backgroundSprite != null)
        {
            Color c = backgroundSprite.color;
            c.a = alpha * backgroundColor.a;
            backgroundSprite.color = c;
            backgroundSprite.enabled = alpha > 0.01f;
        }
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
        if (textMesh != null)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        Color originalColor = textMesh.color;
        textMesh.color = highlightColor;

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
    #endregion

    #region GIZMOS
    private void OnDrawGizmosSelected()
    {
        if (showOnlyWhenPlayerNear)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        // Mostrar offset
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + offset);
        Gizmos.DrawWireSphere(transform.position + offset, 0.1f);
    }
    #endregion
}