using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Player Component References")]
    [SerializeField] private Rigidbody2D rb;

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
    [SerializeField] float ExhaustedDuration = 2f; // Tiempo que dura el agotamiento

    [Header("Hiding System")]

    [SerializeField] SpriteRenderer rend;
    [SerializeField] bool canHide = false;
    [SerializeField] bool hiding = false;

    [Header("Grounding")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Transform groundCheck;
    [SerializeField] Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);

    private float horizontal;
    private bool isRunning;
    private bool isExhausted;
    private float exhaustedTimer = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rend = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        HandleStamina();
        UpdateUI();

        // Temporizador para el estado de agotamiento
        if (isExhausted)
        {
            exhaustedTimer -= Time.deltaTime;
            if (exhaustedTimer <= 0f)
            {
                isExhausted = false;
            }
        }

        if (canHide)
        {
            Physics2D.IgnoreLayerCollision(8, 9, true); // Ignorar colisiones entre jugador y enemigos
            rend.sortingOrder = 0; // Asegura que el sprite esté detrás de otros objetos
            hiding = true;
        }

        else
        {
            Physics2D.IgnoreLayerCollision(8, 9, false); // Restaurar colisiones entre jugador y enemigos
            rend.sortingOrder = 2; // Asegura que el sprite esté delante de otros objetos
            hiding = false;
        }
    }
        


    private void FixedUpdate()
    {
        float currentSpeed = speed;

        if (isExhausted)
        {
            // Estado de agotamiento - velocidad reducida
            currentSpeed = slowSpeed;
        }
        else if (isRunning && horizontal != 0 && Stamina > 0)
        {
            // Estado corriendo - velocidad aumentada
            currentSpeed = runSpeed;
        }

        // Aplica la velocidad correcta (usa currentSpeed en lugar de speed)
        rb.linearVelocity = new Vector2(horizontal * currentSpeed, rb.linearVelocity.y);
    }

    private void HandleStamina()
    {
        // Si está corriendo y tiene stamina, consume stamina
        if (isRunning && horizontal != 0 && Stamina > 0 && !isExhausted)
        {
            Stamina -= RunCost * Time.deltaTime;

            if (Stamina <= 0)
            {
                Stamina = 0;
                isExhausted = true;
                exhaustedTimer = ExhaustedDuration; // Inicia el temporizador de agotamiento
                isRunning = false; // Se cancela el estado de correr
            }
        }
        else if (!isExhausted)
        {
            // Solo recupera stamina si no está exhausto
            Stamina += StaminaRecoveryRate * Time.deltaTime;

            if (Stamina > MaxStamina)
            {
                Stamina = MaxStamina;
            }
        }
        else
        {
            // Recuperación lenta cuando está exhausto
            Stamina += ExhaustedRecoveryRate * Time.deltaTime;

            if (Stamina >= MaxStamina * 0.3f && exhaustedTimer <= 0f)
            {
                // Solo sale del estado exhausto si tiene al menos 30% de stamina
                // y ha pasado el tiempo de agotamiento
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
        horizontal = context.ReadValue<Vector2>().x;
    }

    public void Run(InputAction.CallbackContext context)
    {
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
        if (context.performed && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
        }
    }
    #endregion

    private bool IsGrounded()
    {
        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.name.Equals("HidingSpot"))
        {
            canHide = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.name.Equals("HidingSpot"))
        {
            canHide = false;
            hiding = false;
            rend.enabled = true; // Asegura que el sprite sea visible al salir
        }
    }

    // Para debug visual en el editor
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}
