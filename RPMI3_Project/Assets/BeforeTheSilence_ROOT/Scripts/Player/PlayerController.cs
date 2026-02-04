using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// Controlador completo del jugador con sistemas de stamina, miedo, escondite y animaciones.
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region VARIABLES
    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer rend;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private Animator animator;

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
    private bool isFacingRight = true;

    // Referencias cacheadas
    private VignetteController vignetteController;

    // Animation parameter hashes (más eficiente que strings)
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsRunning = Animator.StringToHash("IsRunning");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private static readonly int AnimIsHidden = Animator.StringToHash("IsHidden");
    private static readonly int AnimIsScared = Animator.StringToHash("IsScared");
    private static readonly int AnimIsPanicking = Animator.StringToHash("IsPanicking");
    private static readonly int AnimIsExhausted = Animator.StringToHash("IsExhausted");
    private static readonly int AnimIsDead = Animator.StringToHash("IsDead");
    private static readonly int AnimJump = Animator.StringToHash("Jump");
    private static readonly int AnimHide = Animator.StringToHash("Hide");
    private static readonly int AnimUnhide = Animator.StringToHash("Unhide");
    private static readonly int AnimDie = Animator.StringToHash("Die");
    private static readonly int AnimPanic = Animator.StringToHash("Panic");
    #endregion

    #region PROPERTIES
    public bool IsInSafeZone => isInSafeZone;
    public bool IsHidden => isHidden;
    public bool IsDead => isDead;
    public bool IsExhausted => isExhausted;
    public Vector2 Position => transform.position;
    public bool IsFacingRight => isFacingRight;

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
        if (animator == null) animator = GetComponent<Animator>();

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

        // Buscar VignetteController
        FindVignetteController();

        // Verificar Animator
        if (animator == null)
        {
            Debug.LogWarning("[PlayerController] No hay Animator asignado!");
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
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        ApplyMovement();
    }
    #endregion

    #region ANIMATION SYSTEM
    /// <summary>
    /// Actualiza todos los parámetros del Animator
    /// </summary>
    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Velocidad horizontal (absoluta para animación de caminar)
        float speed = Mathf.Abs(horizontal);
        animator.SetFloat(AnimSpeed, speed);

        // Estados de movimiento
        animator.SetBool(AnimIsRunning, isRunning && speed > 0.1f);
        animator.SetBool(AnimIsGrounded, IsGrounded());
        animator.SetFloat(AnimVerticalVelocity, rb.linearVelocity.y);

        // Estados especiales
        animator.SetBool(AnimIsHidden, isHidden);
        animator.SetBool(AnimIsScared, IsScared);
        animator.SetBool(AnimIsPanicking, IsPanicking);
        animator.SetBool(AnimIsExhausted, isExhausted);
        animator.SetBool(AnimIsDead, isDead);

        // Flip del sprite según dirección
        UpdateSpriteDirection();
    }

    /// <summary>
    /// Voltea el sprite según la dirección del movimiento
    /// </summary>
    private void UpdateSpriteDirection()
    {
        // Solo cambiar si hay movimiento horizontal significativo
        if (Mathf.Abs(horizontal) > 0.1f)
        {
            bool shouldFaceRight = horizontal > 0;

            if (shouldFaceRight != isFacingRight)
            {
                isFacingRight = shouldFaceRight;
                FlipSprite();
            }
        }
    }

    /// <summary>
    /// Voltea el sprite horizontal
    /// </summary>
    private void FlipSprite()
    {
        // Opción 1: Usar SpriteRenderer.flipX
        if (rend != null)
        {
            rend.flipX = !isFacingRight;
        }

        // Opción 2: Usar escala (descomenta si prefieres este método)
        // Vector3 scale = transform.localScale;
        // scale.x = isFacingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        // transform.localScale = scale;
    }

    /// <summary>
    /// Dispara un trigger de animación
    /// </summary>
    private void TriggerAnimation(int triggerHash)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerHash);
        }
    }

    /// <summary>
    /// Ajusta la velocidad de la animación actual
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }

    /// <summary>
    /// Reproduce una animación específica por nombre
    /// </summary>
    public void PlayAnimation(string animationName, int layer = 0)
    {
        if (animator != null)
        {
            animator.Play(animationName, layer);
        }
    }
    #endregion

    #region VIGNETTE INTEGRATION
    private void FindVignetteController()
    {
        if (vignetteController == null)
        {
            vignetteController = FindFirstObjectByType<VignetteController>();

            if (vignetteController != null)
            {
                Debug.Log("[PlayerController] VignetteController encontrado");
            }
        }
    }

    private void TriggerPanicFlash()
    {
        if (vignetteController == null)
            FindVignetteController();

        if (vignetteController != null)
            vignetteController.PanicFlash();
    }

    private void TriggerDiscoveryFlash()
    {
        if (vignetteController == null)
            FindVignetteController();

        if (vignetteController != null)
            vignetteController.DiscoveryFlash();
    }

    private void TriggerDeathFlash()
    {
        if (vignetteController == null)
            FindVignetteController();

        if (vignetteController != null)
            vignetteController.DeathFlash();
    }

    private void TriggerDamageFlash()
    {
        if (vignetteController == null)
            FindVignetteController();

        if (vignetteController != null)
            vignetteController.DamageFlash();
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
        if (IsPanicking) return runSpeed * 0.5f;
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
            currentStamina -= runCost * Time.deltaTime;

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                BecomeExhausted();
            }
        }
        else if (isExhausted)
        {
            currentStamina += exhaustedRecoveryRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
        else
        {
            currentStamina += staminaRecoveryRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

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
            currentFearTime += fearBuildupRate * Time.deltaTime;

            if (!wasNervous && FearPercentage >= nervousThreshold)
            {
                wasNervous = true;
                OnFearStart?.Invoke();
                Debug.Log("La niña empieza a ponerse nerviosa...");
            }

            if (currentFearTime >= maxFearTime)
            {
                TriggerPanic();
            }
        }
        else
        {
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

        if (Mathf.Abs(previousFear - currentFearTime) > 0.01f)
        {
            OnFearChanged?.Invoke(FearPercentage);
        }
    }

    private void TriggerPanic()
    {
        Debug.Log("¡La niña no aguanta más la oscuridad!");

        OnPanic?.Invoke();
        TriggerPanicFlash();

        // Trigger de animación de pánico
        TriggerAnimation(AnimPanic);

        ForceUnhide();
        currentFearTime = maxFearTime * 0.5f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBreathingState(BreathingState.Panic);
        }

        StartCoroutine(PanicRun());
    }

    private IEnumerator PanicRun()
    {
        float panicDuration = 1.5f;
        float elapsed = 0f;
        float panicDirection = Random.value > 0.5f ? 1f : -1f;

        // Forzar dirección del sprite durante pánico
        isFacingRight = panicDirection > 0;
        FlipSprite();

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

        // Trigger de animación de salir
        TriggerAnimation(AnimUnhide);

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

            // Trigger de animación de salto
            TriggerAnimation(AnimJump);

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
                isHidden = true;

                // Trigger de animación de esconderse
                TriggerAnimation(AnimHide);

                OnHide?.Invoke();

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayHideSound();
                }

                Debug.Log("Escondido");
            }
            else
            {
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
        TriggerDamageFlash();
        Die("Un enemigo te atacó");
    }

    public void GetDiscovered()
    {
        TriggerDiscoveryFlash();

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

        TriggerDeathFlash();

        // Trigger de animación de muerte
        TriggerAnimation(AnimDie);

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        rend.color = deathColor;

        horizontal = 0;
        isRunning = false;
        isHidden = false;

        SetHiddenLayer(false);

        OnDeath?.Invoke();

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