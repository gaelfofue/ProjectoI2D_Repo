using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Para el Game Over

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
    [SerializeField] bool isHidden = false;
    [SerializeField] float hideAlpha = 0.3f;
    [SerializeField] float timeToDisableCollider = 0.5f; // Tiempo para desactivar collider
    [SerializeField] Color hiddenColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
    [SerializeField] Color normalColor = Color.white;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.2f;

    [Header("Game Over")]
    [SerializeField] GameObject gameOverScreen;
    [SerializeField] string gameOverScene = "GameOver";

    private float horizontal;
    private bool isRunning;
    private bool isExhausted;
    private float exhaustedTimer = 0f;
    private float hideTimer = 0f;
    private bool isGameOver = false;

    // Propiedades públicas
    public bool IsInSafeZone => isInSafeZone;
    public bool IsHidden => isHidden;
    public Vector2 Position => transform.position;
    public bool IsAlive => !isGameOver;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rend = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);
    }

    private void Update()
    {
        if (isGameOver) return;

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

            // Desactivar collider después de un tiempo
            if (hideTimer >= timeToDisableCollider && playerCollider.enabled)
            {
                playerCollider.enabled = false;
                Debug.Log("Collider desactivado - Fusionado con las sombras");
            }
        }
        else
        {
            hideTimer = 0f;

            // Reactivar collider si estaba desactivado
            if (!playerCollider.enabled)
            {
                playerCollider.enabled = true;
                Debug.Log("Collider reactivado");
            }
        }
    }

    private void UpdateAppearance()
    {
        if (isInSafeZone && isHidden)
        {
            // Totalmente escondido
            rend.color = hiddenColor;
        }
        else if (isInSafeZone)
        {
            // En zona segura pero visible
            Color semiHidden = Color.Lerp(normalColor, hiddenColor, 0.5f);
            rend.color = Color.Lerp(rend.color, semiHidden, Time.deltaTime * 3f);
        }
        else
        {
            // Fuera de zona segura
            rend.color = Color.Lerp(rend.color, normalColor, Time.deltaTime * 3f);
            isHidden = false;
        }
    }

    private void FixedUpdate()
    {
        if (isGameOver) return;

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
        if (isGameOver) return;
        horizontal = context.ReadValue<Vector2>().x;
    }

    public void Run(InputAction.CallbackContext context)
    {
        if (isGameOver) return;

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
        if (isGameOver) return;

        if (context.performed && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
        }
    }

    // Tecla para esconderse manualmente (opcional)
    public void ToggleHide(InputAction.CallbackContext context)
    {
        if (isGameOver) return;

        if (context.performed && isInSafeZone)
        {
            isHidden = !isHidden;
            Debug.Log(isHidden ? "🌑 Te has escondido" : "👤 Te has hecho visible");
        }
    }
    #endregion

    private bool IsGrounded()
    {
        if (!playerCollider.enabled) return false;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);

        foreach (Collider2D col in colliders)
        {
            if (col.gameObject != gameObject && !col.isTrigger && col.gameObject.CompareTag("Ground"))
            {
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isGameOver) return;

        if (other.CompareTag("SafeZone"))
        {
            isInSafeZone = true;
            Debug.Log("🛡️ Entrando en zona segura");

            // Auto-esconderse
            Invoke("AutoHide", 0.3f);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("SafeZone"))
        {
            isInSafeZone = false;
            isHidden = false;
            Debug.Log("🚶 Saliendo de zona segura");

            CancelInvoke("AutoHide");
        }
    }

    private void AutoHide()
    {
        if (isInSafeZone && !isHidden && !isGameOver)
        {
            isHidden = true;
            Debug.Log("🌑 Te has fusionado con las sombras...");
        }
    }

    // ==================== MÉTODOS PARA ENEMIGOS ====================

    public bool CanBeSeenByEnemy()
    {
        // No puede ser visto si está escondido (con o sin collider)
        return !(isInSafeZone && isHidden);
    }

    public bool CanBeAttackedByEnemy()
    {
        // Solo puede ser atacado si NO está escondido
        return !(isInSafeZone && isHidden);
    }

    // Enemigo revisa el escondite - PUEDE dar Game Over incluso sin collider
    public bool CheckIfFound(Vector3 enemyPosition, float searchRange, float searchIntensity = 1f)
    {
        // Si ya está muerto, no hacer nada
        if (isGameOver) return false;

        // Solo aplica si está escondido
        if (!isInSafeZone || !isHidden) return false;

        float distance = Vector2.Distance(transform.position, enemyPosition);

        // Si el enemigo está revisando cerca
        if (distance <= searchRange)
        {
            // Calcular probabilidad de ser descubierto
            // Más cerca + más intensidad = mayor chance
            float baseChance = 0.1f;
            float distanceFactor = 1f - (distance / searchRange); // 0 a 1
            float discoveryChance = (baseChance + (distanceFactor * 0.6f)) * searchIntensity;

            if (Random.value < discoveryChance)
            {
                Debug.Log("🚨 ¡TE HAN DESCUBIERTO!");
                GameOver("Te descubrieron en tu escondite");
                return true;
            }
            else
            {
                Debug.Log("El enemigo revisó cerca... pero no te vio");
            }
        }

        return false;
    }

    // ==================== GAME OVER SYSTEM ====================

    public void GameOver(string reason = "Game Over")
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log($"💀 GAME OVER: {reason}");

        // Detener movimiento
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;

        // Mostrar pantalla de Game Over
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }
        else
        {
            // O cargar escena de Game Over
            Invoke("LoadGameOverScene", 2f);
        }

        // Efecto visual de muerte
        rend.color = Color.red;

        // Desactivar controles
        horizontal = 0;
        isRunning = false;
    }

    private void LoadGameOverScene()
    {
        if (!string.IsNullOrEmpty(gameOverScene))
        {
            SceneManager.LoadScene(gameOverScene);
        }
    }

    // Para reiniciar (desde botón UI)
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Para debug visual
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}