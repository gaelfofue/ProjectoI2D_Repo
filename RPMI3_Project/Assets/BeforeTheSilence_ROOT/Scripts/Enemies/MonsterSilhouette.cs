using UnityEngine;

/// <summary>
/// Monstruo visual que pasa en parallax como silueta.
/// NO tiene colisiones ni lógica de enemigo.
/// </summary>
public class MonsterSilhouette : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Color silhouetteColor = Color.black;

    [Header("Animation")]
    [SerializeField] private string walkAnimationName = "Walk";
    [SerializeField] private bool flipBasedOnDirection = true;

    private void Awake()
    {
        // Obtener componentes
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // Aplicar color de silueta
        ApplySilhouetteColor();

        // Desactivar cualquier collider
        DisableColliders();

        // Desactivar cualquier script de enemigo
        DisableEnemyScripts();
    }

    private void ApplySilhouetteColor()
    {
        // Aplicar a este sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.color = silhouetteColor;
        }

        // Aplicar a todos los hijos (para personajes riggeados)
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in allRenderers)
        {
            sr.color = silhouetteColor;
        }
    }

    private void DisableColliders()
    {
        // Desactivar todos los colliders
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
    }

    private void DisableEnemyScripts()
    {
        // Desactivar SimpleEnemy si existe
        var enemy = GetComponent<SimpleEnemy>();
        if (enemy != null)
        {
            enemy.enabled = false;
        }

        // Desactivar Rigidbody2D si existe
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false;
        }
    }

    /// <summary>
    /// Iniciar animación de caminar
    /// </summary>
    public void StartWalking()
    {
        if (animator != null && !string.IsNullOrEmpty(walkAnimationName))
        {
            animator.Play(walkAnimationName);
        }
    }

    /// <summary>
    /// Establecer dirección del sprite
    /// </summary>
    public void SetDirection(bool movingRight)
    {
        if (!flipBasedOnDirection) return;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !movingRight;
        }
        else
        {
            // Para personajes riggeados, usar escala
            Vector3 scale = transform.localScale;
            scale.x = movingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    /// <summary>
    /// Cambiar el color de la silueta
    /// </summary>
    public void SetColor(Color color)
    {
        silhouetteColor = color;
        ApplySilhouetteColor();
    }
}