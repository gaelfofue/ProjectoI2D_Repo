using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Player Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer rend;
    [SerializeField] private Collider2D playerCollider;

    [Header("Player Settings")]
    [SerializeField] float runSpeed = 8f;
    [SerializeField] float speed = 5f;
    [SerializeField] float slowSpeed = 2f;
    [SerializeField] float jumpingPower = 10f;

    [Header("Stamina Settings")]
    [SerializeField] Image StaminaBar;
    [SerializeField] float Stamina = 100f;
    [SerializeField] float MaxStamina = 100f;
    [SerializeField] float RunCost = 20f;
    [SerializeField] float StaminaRecoveryRate = 15f;
    [SerializeField] float ExhaustedRecoveryRate = 5f;
    [SerializeField] float ExhaustedDuration = 2f;

    [Header("Safe Zone System")]
    [SerializeField] bool isInSafeZone = false;
    [SerializeField] private SafeZone currentSafeZone;
    [SerializeField] bool isHidden = false;
    [SerializeField] float hideAlpha = 0.3f;
    [SerializeField] float timeToDisableCollider = 0.5f;
    [SerializeField] Color hiddenColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
    [SerializeField] Color normalColor = Color.white;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.2f;
    [SerializeField] LayerMask groundLayer; // Asegúrate de configurar esto en el Inspector

    [Header("Death Settings")]
    [SerializeField] private string deathReason = "¡Te atraparon!";
    [SerializeField] private Color deathColor = Color.red;

    private float horizontal;
    private bool isRunning;
    private bool isExhausted;
    private float exhaustedTimer = 0f;
    private float hideTimer = 0f;
    private bool isDead = false;
    private RigidbodyType2D originalBodyType;

    // Propiedades públicas
    public bool IsInSafeZone => isInSafeZone;
    public bool IsHidden => isHidden;
    public Vector2 Position => transform.position;
    public bool IsDead => isDead;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rend = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        originalBodyType = rb.bodyType;

        // Debug: Verificar si groundLayer está configurado
        if (groundLayer.value == 0)
        {
            Debug.LogWarning("⚠️ groundLayer no configurado en PlayerController. El jugador no podrá saltar.");
        }

        GameManager.Instance.OnGameRestart.AddListener(ResetPlayer);
    }

    private void Update()
    {
        if (isDead) return;

        HandleStamina();
        UpdateUI();
        UpdateExhaustedTimer();
        UpdateAppearance();
        UpdateHideTimer();
    }

    private void UpdateExhaustedTimer()
    {
        if (isExhausted)
        {
            exhaustedTimer -= Time.deltaTime;
            if (exhaustedTimer <= 0f)
            {
                isExhausted = false;
            }
        }
    }

    private void UpdateHideTimer()
    {
        if (isHidden)
        {
            hideTimer += Time.deltaTime;

            if (hideTimer >= timeToDisableCollider && playerCollider.enabled)
            {
                playerCollider.enabled = false;
                Debug.Log("Collider desactivado");
            }
        }
        else
        {
            hideTimer = 0f;

            if (!playerCollider.enabled)
            {
                playerCollider.enabled = true;
                Debug.Log("Collider reactivado");
            }
        }
    }

    private void UpdateAppearance()
    {
        if (isDead) return;

        if (isInSafeZone && isHidden)
        {
            rend.color = hiddenColor;
        }
        else if (isInSafeZone)
        {
            Color semiHidden = Color.Lerp(normalColor, hiddenColor, 0.5f);
            rend.color = Color.Lerp(rend.color, semiHidden, Time.deltaTime * 3f);
        }
        else
        {
            rend.color = Color.Lerp(rend.color, normalColor, Time.deltaTime * 3f);
            isHidden = false;
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        float currentSpeed = speed;

        if (isExhausted)
        {
            currentSpeed = slowSpeed;
        }
        else if (isRunning && horizontal != 0 && Stamina > 0)
        {
            currentSpeed = runSpeed;
        }

        rb.linearVelocity = new Vector2(horizontal * currentSpeed, rb.linearVelocity.y);
    }

    private void HandleStamina()
    {
        if (isRunning && horizontal != 0 && Stamina > 0 && !isExhausted)
        {
            Stamina -= RunCost * Time.deltaTime;

            if (Stamina <= 0)
            {
                Stamina = 0;
                isExhausted = true;
                exhaustedTimer = ExhaustedDuration;
                isRunning = false;
            }
        }
        else if (!isExhausted)
        {
            Stamina += StaminaRecoveryRate * Time.deltaTime;

            if (Stamina > MaxStamina)
            {
                Stamina = MaxStamina;
            }
        }
        else
        {
            Stamina += ExhaustedRecoveryRate * Time.deltaTime;

            if (Stamina >= MaxStamina * 0.3f && exhaustedTimer <= 0f)
            {
                isExhausted = false;
            }
        }
    }

    private void UpdateUI()
    {
        if (StaminaBar != null)
        {
            StaminaBar.fillAmount = Stamina / MaxStamina;
        }
    }

    #region PLAYER_CONTROLS

    public void Move(InputAction.CallbackContext context)
    {
        if (isDead) return;
        horizontal = context.ReadValue<Vector2>().x;
    }

    public void Run(InputAction.CallbackContext context)
    {
        if (isDead) return;

        if (context.started && !isExhausted && Stamina > 0)
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
            Debug.Log("🦘 Saltando!");
        }
        else if (context.performed && !IsGrounded())
        {
            Debug.Log("No puedo saltar: no estoy en el suelo");
        }
    }

    public void ToggleHide(InputAction.CallbackContext context)
    {
        if (isDead) return;

        if (context.performed && isInSafeZone)
        {
            isHidden = !isHidden;
            Debug.Log(isHidden ? "🌑 Escondido" : "👤 Visible");
        }
    }
    #endregion

    private bool IsGrounded()
    {
        // Método MEJORADO con debug
        if (groundCheck == null)
        {
            Debug.LogError("❌ groundCheck no asignado en PlayerController");
            return false;
        }

        // Usar LayerMask
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);

        // Debug: Mostrar cuántos colliders se detectaron
        if (colliders.Length > 0)
        {
            foreach (Collider2D col in colliders)
            {
                if (col != null && col.gameObject != gameObject && !col.isTrigger)
                {
                    Debug.Log($"✅ Suelo detectado: {col.gameObject.name}");
                    return true;
                }
            }
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("SafeZone"))
        {
            isInSafeZone = true;
            currentSafeZone = other.GetComponent<SafeZone>();
            Debug.Log("Entrando en Safe Zone");

            if (currentSafeZone != null && currentSafeZone.autoHide)
            {
                Invoke("AutoHide", currentSafeZone.autoHideDelay);
            }
        }


    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("SafeZone"))
        {
            isInSafeZone = false;
            isHidden = false;
            Debug.Log("Saliendo de Safe Zone");
            CancelInvoke("AutoHide");
        }
    }

    private void AutoHide()
    {
        if (isInSafeZone && !isHidden && !isDead)
        {
            isHidden = true;
            Debug.Log("Auto-escondido en Safe Zone");
        }
    }

    // Metodos para enemigos

    public bool CanBeSeenByEnemy()
    {
        if (isDead)
        {
            return false;
        }
        if (isInSafeZone && isHidden)
        {
            return false;
        }
        return true;
    }

    public bool CanBeAttackedByEnemy()
    {
        if (isDead)
        {
            return false;
        }
        if (isInSafeZone && isHidden)
        {
            return false;
        }
        return true;
    }

    public bool CheckIfFound(Vector3 enemyPosition, float searchRange, float searchIntensity = 1f)
    {
        if (isDead) return false;
        if (!isInSafeZone || !isHidden) return false;

        float distance = Vector2.Distance(transform.position, enemyPosition);

        if (distance <= searchRange)
        {
            float discoveryChance = 0.1f; // Base

            // Si tenemos una SafeZone, usar su riesgo
            if (currentSafeZone != null)
            {
                discoveryChance = currentSafeZone.CheckZone(enemyPosition);
            }
            else
            {
                // Cálculo alternativo
                float distanceFactor = 1f - (distance / searchRange);
                discoveryChance = (0.1f + (distanceFactor * 0.6f)) * searchIntensity;
            }

            Debug.Log($"🎲 Chance de descubrimiento: {discoveryChance:P0}");

            if (Random.value < discoveryChance)
            {
                GetDiscovered();
                return true;
            }
        }

        return false;
    }

    // ==================== SISTEMA DE MUERTE ====================

    public void TakeDamage()
    {
        Die("Un enemigo te atacó");
    }

    public void GetDiscovered()
    {
        Die("Te descubrieron en tu escondite");
    }

    public void Die(string customReason = "")
    {
        if (isDead) return;

        isDead = true;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        rend.color = deathColor;

        horizontal = 0;
        isRunning = false;

        isHidden = false;
        if (!playerCollider.enabled)
        {
            playerCollider.enabled = true;
        }

        string finalReason = string.IsNullOrEmpty(customReason) ? deathReason : customReason;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver(finalReason);
        }
        else
        {
            Debug.Log($"💀 {finalReason}");
            StartCoroutine(FallbackGameOver());
        }
    }

    private IEnumerator FallbackGameOver()
    {
        yield return new WaitForSeconds(2f);

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void ResetPlayer()
    {
        isDead = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;

        playerCollider.enabled = true;

        isHidden = false;
        isInSafeZone = false;
        isRunning = false;
        isExhausted = false;

        exhaustedTimer = 0f;
        hideTimer = 0f;

        Stamina = MaxStamina;
        rend.color = normalColor;
    }
    // Para debug visual
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}