using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SearchFurniture : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float searchTime = 2f;
    [Tooltip("Nombre de la llave que da (dejar vacío si no da llave)")]
    [SerializeField] private string keyToGive;

    [Header("UI References")]
    [Tooltip("Panel/GameObject que muestra 'Mantén E para buscar'")]
    [SerializeField] private GameObject promptUI;
    [Tooltip("Image con Image Type = Filled para la barra de progreso")]
    [SerializeField] private Image progressBar;

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioClip searchSound;
    [SerializeField] private AudioClip foundSound;

    [Header("Player Lock")]
    [SerializeField] private bool lockPlayerWhileSearching = true;

    // Estado privado
    private bool playerInRange = false;
    private bool isSearching = false;
    private bool alreadySearched = false;
    private float searchProgress = 0f;
    private float cancelGraceTimer = 0f;
    private float cancelGraceTime = 0.15f; // Tolerancia para no cancelar por 1 frame
    private PlayerController playerController;

    private void Awake()
    {
        // Validar collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        // Ocultar UI al inicio
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }

        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
        }
    }

    private void Update()
    {
        if (alreadySearched) return;
        if (!playerInRange) return;

        bool eKeyPressed = Keyboard.current != null && Keyboard.current.eKey.isPressed;

        if (eKeyPressed)
        {
            cancelGraceTimer = 0f;

            if (!isSearching)
            {
                StartSearch();
            }

            searchProgress += Time.deltaTime;

            if (progressBar != null)
            {
                progressBar.fillAmount = searchProgress / searchTime;
            }

            if (searchProgress >= searchTime)
            {
                CompleteSearch();
            }
        }
        else if (isSearching)
        {
            // Dar un pequeño margen antes de cancelar
            cancelGraceTimer += Time.deltaTime;

            if (cancelGraceTimer >= cancelGraceTime)
            {
                CancelSearch();
            }
        }
    }

    private void StartSearch()
    {
        isSearching = true;
        searchProgress = 0f;
        cancelGraceTimer = 0f;

        if (lockPlayerWhileSearching && playerController != null)
        {
            playerController.LockForInteraction(true);
        }

        if (searchSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(searchSound);
        }

        Debug.Log("[SearchFurniture] Búsqueda iniciada...");
    }

    private void CancelSearch()
    {
        isSearching = false;
        searchProgress = 0f;
        cancelGraceTimer = 0f;

        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
        }

        if (lockPlayerWhileSearching && playerController != null)
        {
            playerController.LockForInteraction(false);
        }

        Debug.Log("[SearchFurniture] Búsqueda cancelada");
    }

    private void CompleteSearch()
    {
        alreadySearched = true;
        isSearching = false;

        if (lockPlayerWhileSearching && playerController != null)
        {
            playerController.LockForInteraction(false);
        }

        if (!string.IsNullOrEmpty(keyToGive))
        {
            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.PickUpKey(keyToGive);
                Debug.Log($"[SearchFurniture] ¡Llave encontrada: {keyToGive}!");
            }
            else
            {
                Debug.LogError("[SearchFurniture] GameProgressManager.Instance es null!");
            }
        }
        else
        {
            Debug.Log("[SearchFurniture] No había nada aquí...");
        }

        // Completar barra
        if (progressBar != null)
        {
            progressBar.fillAmount = 1f;
        }

        // Ocultar prompt después de un momento
        Invoke(nameof(HidePrompt), 1.5f);

        if (foundSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(foundSound);
        }
    }

    private void HidePrompt()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !alreadySearched)
        {
            playerInRange = true;

            if (playerController == null)
            {
                playerController = other.GetComponent<PlayerController>();
                if (playerController == null)
                {
                    playerController = other.GetComponentInParent<PlayerController>();
                }
            }

            if (promptUI != null)
            {
                promptUI.SetActive(true);
            }

            Debug.Log("[SearchFurniture] Player cerca - Mantén E para buscar");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (isSearching)
            {
                CancelSearch();
            }

            if (promptUI != null)
            {
                promptUI.SetActive(false);
            }

            if (progressBar != null)
            {
                progressBar.fillAmount = 0f;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = alreadySearched ? Color.gray : Color.yellow;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
    }
}