using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
/// Controlador completo del jugador con sistemas de stamina, miedo y escondite.
public class PlayerController : MonoBehaviour
{
    #region VARIABLES
    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer rend;
    [SerializeField] private Collider2D playerCollider;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float slowSpeed = 2f;
    [SerializeField] private float jumpingPower = 10f;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 100f;
    [SerializeField] private float runCost = 20f;
    [SerializeField] private float staminaRecoveryRate = 15f;
    [SerializeField] private float exhaustedRecoveryRate = 5f;
    [SerializeField] private float exhaustedDuration = 2f;

    [Header("Fear System - Miedo a la Oscuridad")]
    [SerializeField] private float maxFearTime = 5f;
    [SerializeField] private float currentFearTime = 0f;
    [SerializeField] private float fearBuildupRate = 1f;
    [SerializeField] private float fearRecoveryRate = 2f;

    [Header("Fear Thresholds")]
    [SerializeField] private float nervousThreshold = 0.3f;
    [SerializeField] private float scaredThreshold = 0.6f;
    [SerializeField] private float panicThreshold = 0.9f;

    [Header("Safe Zone / Hide System")]
    [SerializeField] private bool isInSafeZone = false;
    [SerializeField] private bool isHidden = false;
    [SerializeField] private float hideDelay = 0.5f;
    [SerializeField] private Color hiddenColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color normalColor = Color.white;

    [Header("Layer Settings")]
    [SerializeField] private string playerLayerName = "Player";
    [SerializeField] private string hiddenLayerName = "Hidden";

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Death Settings")]
    [SerializeField] private Color deathColor = Color.red;

    [Header("Events")]
    public UnityEvent OnHide;
    public UnityEvent OnUnhide;
    public UnityEvent OnFearStart;
    public UnityEvent OnPanic;
    public UnityEvent<float> OnFearChanged;
    public UnityEvent<float> OnStaminaChanged;
    public UnityEvent OnExhausted;
    public UnityEvent OnDeath;

    // Estado privado
    private float horizontal;
    private bool isRunning;
    private bool isExhausted;
    private float exhaustedTimer = 0f;
    private float hideTimer = 0f;
    private float totalHideTime = 0f;
    private bool isDead = false;
    private bool wasNervous = false;
    private int originalLayer;
    private BreathingState lastBreathingState = BreathingState.Normal;
    #endregion

    #region PROPERTIES
    public bool IsInSafeZone => isInSafeZone;
    public bool IsHidden => isHidden;
    public bool IsDead => isDead;
    public bool IsExhausted => isExhausted;
    public Vector2 Position => transform.position;

    public float FearPercentage => currentFearTime / maxFearTime;
    public float StaminaPercentage => currentStamina / maxStamina;
    public bool IsScared => FearPercentage >= scaredThreshold;
    public bool IsPanicking => FearPercentage >= panicThreshold;

    public float GetFearNormalized() => FearPercentage;
    public float GetStaminaNormalized() => StaminaPercentage;
    #endregion

    #region UNITY METHODS
    private void Awake()
    {
        // Obtener componentes
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rend == null) rend = GetComponent<SpriteRenderer>();
        if (playerCollider == null) playerCollider = GetComponent<Collider2D>();

        originalLayer = gameObject.layer;
        currentStamina = maxStamina;
    }

    private void Start()
    {
        // Verificar configuración
        if (groundLayer.value == 0)
        {
            Debug.LogWarning("groundLayer no configurado en PlayerController");
        }

        // Configurar layer inicial
        if (!string.IsNullOrEmpty(playerLayerName))
        {
            int layer = LayerMask.NameToLayer(playerLayerName);
            if (layer != -1)
            {
                gameObject.layer = layer;
                originalLayer = layer;
            }
        }
    }

    private void Update()
    {
        if (isDead) return;

        HandleStamina();
        HandleFear();
        UpdateHideState();
        UpdateAppearance();
        UpdateBreathing();
        UpdateExhaustedTimer();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        ApplyMovement();
    }
    #endregion

    #region MOVEMENT
    private void ApplyMovement()
    {
        float currentSpeed = GetCurrentSpeed();
        rb.linearVelocity = new Vector2(horizontal * currentSpeed, rb.linearVelocity.y);
    }

    private float GetCurrentSpeed()
    {
        if (isExhausted) return slowSpeed;
        if (IsPanicking) return runSpeed * 0.5f; // Más lenta cuando está en pánico
        if (isRunning && horizontal != 0 && currentStamina > 0) return runSpeed;
        return walkSpeed;
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        foreach (Collider2D col in colliders)
        {
            if (col != null && col.gameObject != gameObject && !col.isTrigger)
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region STAMINA SYSTEM
    private void HandleStamina()
    {
        float previousStamina = currentStamina;

        if (isRunning && horizontal != 0 && currentStamina > 0 && !isExhausted)
        {
            // Consumir stamina
            currentStamina -= runCost * Time.deltaTime;

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                BecomeExhausted();
            }
        }
        else if (isExhausted)
        {
            // Recuperación lenta cuando está agotada
            currentStamina += exhaustedRecoveryRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
        else
        {
            // Recuperación normal
            currentStamina += staminaRecoveryRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        // Notificar cambio
        if (Mathf.Abs(previousStamina - currentStamina) > 0.1f)
        {
            OnStaminaChanged?.Invoke(StaminaPercentage);
        }
    }

    private void BecomeExhausted()
    {
        isExhausted = true;
        exhaustedTimer = exhaustedDuration;
        isRunning = false;
        OnExhausted?.Invoke();
        Debug.Log("Jugadora agotada");
    }

    private void UpdateExhaustedTimer()
    {
        if (isExhausted)
        {
            exhaustedTimer -= Time.deltaTime;

            // Recuperarse si ha pasado el tiempo Y tiene algo de stamina
            if (exhaustedTimer <= 0f && currentStamina >= maxStamina * 0.3f)
            {
                isExhausted = false;
                Debug.Log("Recuperada del agotamiento");
            }
        }
    }
    #endregion

    #region FEAR SYSTEM
    private void HandleFear()
    {
        float previousFear = currentFearTime;

        if (isHidden && isInSafeZone)
        {
            // Acumular miedo en la oscuridad
            currentFearTime += fearBuildupRate * Time.deltaTime;

            // Detectar cuando empieza el nerviosismo
            if (!wasNervous && FearPercentage >= nervousThreshold)
            {
                wasNervous = true;
                OnFearStart?.Invoke();
                Debug.Log("La niña empieza a ponerse nerviosa...");
            }

            // Pánico
            if (currentFearTime >= maxFearTime)
            {
                TriggerPanic();
            }
        }
        else
        {
            // Recuperar calma fuera del escondite
            if (currentFearTime > 0)
            {
                currentFearTime -= fearRecoveryRate * Time.deltaTime;
                currentFearTime = Mathf.Max(0, currentFearTime);

                if (currentFearTime == 0)
                {
                    wasNervous = false;
                }
            }
        }

        // Notificar cambio
        if (Mathf.Abs(previousFear - currentFearTime) > 0.01f)
        {
            OnFearChanged?.Invoke(FearPercentage);
        }
    }

    private void TriggerPanic()
    {
        Debug.Log("¡La niña no aguanta más la oscuridad!");

        OnPanic?.Invoke();

        // Forzar salir del escondite
        ForceUnhide();
        currentFearTime = maxFearTime * 0.5f; // Mantener algo de miedo

        // Audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBreathingState(BreathingState.Panic);
        }

        // Correr involuntariamente
        StartCoroutine(PanicRun());
    }

    private IEnumerator PanicRun()
    {
        float panicDuration = 1.5f;
        float elapsed = 0f;
        float panicDirection = Random.value > 0.5f ? 1f : -1f;

        while (elapsed < panicDuration)
        {
            elapsed += Time.deltaTime;
            rb.linearVelocity = new Vector2(panicDirection * runSpeed * 0.8f, rb.linearVelocity.y);
            yield return null;
        }
    }
    #endregion

    #region HIDE SYSTEM
    private void UpdateHideState()
    {
        if (isHidden && isInSafeZone)
        {
            hideTimer += Time.deltaTime;
            totalHideTime += Time.deltaTime;

            // Cambiar a layer oculto después del delay
            if (hideTimer >= hideDelay && gameObject.layer != LayerMask.NameToLayer(hiddenLayerName))
            {
                SetHiddenLayer(true);
            }
        }
        else
        {
            hideTimer = 0f;
        }
    }

    private void SetHiddenLayer(bool hidden)
    {
        int targetLayer = hidden
            ? LayerMask.NameToLayer(hiddenLayerName)
            : originalLayer;

        if (targetLayer == -1)
        {
            Debug.LogWarning($"Layer '{(hidden ? hiddenLayerName : playerLayerName)}' no existe");
            return;
        }

        gameObject.layer = targetLayer;

        // También cambiar hijos si los hay
        foreach (Transform child in transform)
        {
            child.gameObject.layer = targetLayer;
        }

        Debug.Log(hidden ? "Cambió a layer Hidden" : "👤 Cambió a layer Player");
    }

    public void EnterSafeZone()
    {
        isInSafeZone = true;
        Debug.Log("Entrando en Safe Zone");
    }

    public void ExitSafeZone()
    {
        isInSafeZone = false;

        if (isHidden)
        {
            ForceUnhide();
        }

        Debug.Log("Saliendo de Safe Zone");
    }

    private void ForceUnhide()
    {
        if (!isHidden) return;

        isHidden = false;
        SetHiddenLayer(false);

        // Registrar estadística
        if (GameData.instance != null)
        {
            GameData.instance.RegisterHideAttempt(totalHideTime);
        }
        totalHideTime = 0f;

        OnUnhide?.Invoke();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUnhideSound();
        }
    }
    #endregion

    #region APPEARANCE
    private void UpdateAppearance()
    {
        if (isDead) return;

        Color targetColor;

        if (isHidden && isInSafeZone)
        {
            targetColor = hiddenColor;
        }
        else if (isInSafeZone)
        {
            targetColor = Color.Lerp(normalColor, hiddenColor, 0.3f);
        }
        else
        {
            targetColor = normalColor;
        }

        rend.color = Color.Lerp(rend.color, targetColor, Time.deltaTime * 5f);
    }

    private void UpdateBreathing()
    {
        if (AudioManager.Instance == null) return;

        BreathingState newState;

        if (IsPanicking)
        {
            newState = BreathingState.Panic;
        }
        else if (IsScared)
        {
            newState = BreathingState.Scared;
        }
        else if (isExhausted)
        {
            newState = BreathingState.Exhausted;
        }
        else if (StaminaPercentage < 0.3f || FearPercentage > 0.3f)
        {
            newState = BreathingState.Heavy;
        }
        else
        {
            newState = BreathingState.Normal;
        }

        if (newState != lastBreathingState)
        {
            AudioManager.Instance.SetBreathingState(newState);
            lastBreathingState = newState;
        }
    }
    #endregion

    #region INPUT HANDLERS
    public void Move(InputAction.CallbackContext context)
    {
        if (isDead) return;
        horizontal = context.ReadValue<Vector2>().x;
    }

    public void Run(InputAction.CallbackContext context)
    {
        if (isDead) return;

        if (context.started && !isExhausted && currentStamina > 0)
        {
            isRunning = true;
        }

        if (context.canceled)
        {
            isRunning = false;
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (isDead) return;

        if (context.performed && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
            Debug.Log("Saltando!");
        }
    }

    public void ToggleHide(InputAction.CallbackContext context)
    {
        if (isDead) return;

        if (context.performed && isInSafeZone)
        {
            if (!isHidden)
            {
                // Esconderse
                isHidden = true;
                OnHide?.Invoke();

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayHideSound();
                }

                Debug.Log("Escondido");
            }
            else
            {
                // Salir del escondite
                ForceUnhide();
                Debug.Log("Visible");
            }
        }
    }
    #endregion

    #region ENEMY INTERACTION
    public bool CanBeSeenByEnemy()
    {
        if (isDead) return false;

        // Si está en layer oculto, no puede ser visto
        if (gameObject.layer == LayerMask.NameToLayer(hiddenLayerName))
        {
            return false;
        }

        return true;
    }

    public bool CanBeAttackedByEnemy()
    {
        if (isDead) return false;

        if (gameObject.layer == LayerMask.NameToLayer(hiddenLayerName))
        {
            return false;
        }

        return true;
    }

    public bool CheckIfFound(Vector3 enemyPosition, float searchRange, float intensity = 1f)
    {
        if (isDead) return false;
        if (!isInSafeZone || !isHidden) return false;

        float distance = Vector2.Distance(transform.position, enemyPosition);

        if (distance <= searchRange)
        {
            // Probabilidad basada en distancia e intensidad
            float baseChance = 0.1f;
            float distanceFactor = 1f - (distance / searchRange);
            float discoveryChance = (baseChance + (distanceFactor * 0.5f)) * intensity;

            Debug.Log($"Revisando escondite... chance: {discoveryChance:P0}");

            if (Random.value < discoveryChance)
            {
                GetDiscovered();
                return true;
            }
        }

        return false;
    }
    #endregion

    #region DEATH SYSTEM
    public void TakeDamage()
    {
        Die("Un enemigo te atacó");
    }

    public void GetDiscovered()
    {
        if (GameData.instance != null)
        {
            GameData.instance.RegisterDiscovered();
        }
        Die("Te descubrieron en tu escondite");
    }

    public void Die(string reason = "")
    {
        if (isDead) return;

        isDead = true;

        // Detener movimiento
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        // Visual
        rend.color = deathColor;

        // Estado
        horizontal = 0;
        isRunning = false;
        isHidden = false;

        // Restaurar layer
        SetHiddenLayer(false);

        OnDeath?.Invoke();

        // Game Over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver(reason);
        }

        Debug.Log($"Muerte: {reason}");
    }
    #endregion

    #region TRIGGERS
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("SafeZone"))
        {
            EnterSafeZone();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("SafeZone"))
        {
            ExitSafeZone();
        }
    }
    #endregion

    #region DEBUG
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
    #endregion
}