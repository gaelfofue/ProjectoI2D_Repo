using UnityEngine;
using UnityEngine.UI;

public class SearchFurniture : MonoBehaviour
{
    [SerializeField] private float searchTime = 2f;
    [SerializeField] private string keyToGive; // "MotherRoomKey" o "ExitKey"
    [SerializeField] private GameObject promptUI;
    [SerializeField] private Image progressBar;

    private bool playerInRange = false;
    private bool isSearching = false;
    private bool alreadySearched = false;
    private float searchProgress = 0f;

    private void Update()
    {
        if (alreadySearched) return;

        if (playerInRange && Input.GetKey(KeyCode.E))
        {
            isSearching = true;
            searchProgress += Time.deltaTime;

            if (progressBar != null)
                progressBar.fillAmount = searchProgress / searchTime;

            if (searchProgress >= searchTime)
            {
                CompleteSearch();
            }
        }
        else
        {
            isSearching = false;
            searchProgress = 0f;
            if (progressBar != null) progressBar.fillAmount = 0f;
        }
    }

    private void CompleteSearch()
    {
        alreadySearched = true;

        if (!string.IsNullOrEmpty(keyToGive) && GameState.Instance != null)
        {
            GameState.Instance.PickUpKey(keyToGive);
        }

        if (promptUI != null) promptUI.SetActive(false);
        Debug.Log($"Búsqueda completada! Llave: {keyToGive}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !alreadySearched)
        {
            playerInRange = true;
            if (promptUI != null) promptUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }
}