using UnityEngine;

/// <summary>
/// Reproduce sonidos del monstruo basados en la distancia al jugador.
/// Añadir al GameObject del monstruo.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MonsterProximityAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource breathingSource;
    [SerializeField] private AudioSource footstepsSource;
    [SerializeField] private AudioSource growlSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] breathingSounds;
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip[] growlSounds;
    [SerializeField] private AudioClip[] distantSounds;

    [Header("Distance Settings")]
    [SerializeField] private float maxHearingDistance = 20f;
    [SerializeField] private float closeDistance = 5f;
    [SerializeField] private float veryCloseDistance = 2f;

    [Header("Volume Settings")]
    [SerializeField] private float maxBreathingVolume = 0.8f;
    [SerializeField] private float maxFootstepVolume = 0.6f;
    [SerializeField] private float maxGrowlVolume = 1f;

    [Header("Timing")]
    [SerializeField] private float footstepInterval = 0.5f;
    [SerializeField] private float growlMinInterval = 5f;
    [SerializeField] private float growlMaxInterval = 15f;

    private Transform player;
    private float footstepTimer = 0f;
    private float growlTimer = 0f;
    private float nextGrowlTime = 0f;
    private bool isMoving = false;
    private Rigidbody2D rb;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();

        SetupAudioSources();
        nextGrowlTime = Random.Range(growlMinInterval, growlMaxInterval);
    }

    private void SetupAudioSources()
    {
        if (breathingSource == null)
        {
            breathingSource = gameObject.AddComponent<AudioSource>();
            breathingSource.loop = true;
            breathingSource.spatialBlend = 1f;
        }

        if (footstepsSource == null)
        {
            footstepsSource = gameObject.AddComponent<AudioSource>();
            footstepsSource.spatialBlend = 1f;
        }

        if (growlSource == null)
        {
            growlSource = gameObject.AddComponent<AudioSource>();
            growlSource.spatialBlend = 1f;
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        isMoving = rb != null && rb.linearVelocity.magnitude > 0.1f;

        UpdateBreathing(distance);
        UpdateFootsteps(distance);
        UpdateGrowls(distance);
    }

    private void UpdateBreathing(float distance)
    {
        if (breathingSounds == null || breathingSounds.Length == 0) return;

        if (distance <= maxHearingDistance)
        {
            float volumeT = 1f - (distance / maxHearingDistance);
            volumeT = Mathf.Pow(volumeT, 0.5f); // Curva más natural

            breathingSource.volume = volumeT * maxBreathingVolume;

            if (!breathingSource.isPlaying)
            {
                breathingSource.clip = breathingSounds[Random.Range(0, breathingSounds.Length)];
                breathingSource.Play();
            }
        }
        else
        {
            breathingSource.volume = 0f;
        }
    }

    private void UpdateFootsteps(float distance)
    {
        if (!isMoving || footstepSounds == null || footstepSounds.Length == 0) return;

        footstepTimer += Time.deltaTime;

        if (footstepTimer >= footstepInterval && distance <= maxHearingDistance)
        {
            footstepTimer = 0f;

            float volumeT = 1f - (distance / maxHearingDistance);
            footstepsSource.volume = volumeT * maxFootstepVolume;
            footstepsSource.pitch = Random.Range(0.9f, 1.1f);
            footstepsSource.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)]);
        }
    }

    private void UpdateGrowls(float distance)
    {
        if (growlSounds == null || growlSounds.Length == 0) return;

        growlTimer += Time.deltaTime;

        if (growlTimer >= nextGrowlTime && distance <= closeDistance)
        {
            growlTimer = 0f;
            nextGrowlTime = Random.Range(growlMinInterval, growlMaxInterval);

            float volumeT = 1f - (distance / closeDistance);
            growlSource.volume = volumeT * maxGrowlVolume;
            growlSource.PlayOneShot(growlSounds[Random.Range(0, growlSounds.Length)]);
        }
    }

    public void PlayScareSound()
    {
        if (growlSounds != null && growlSounds.Length > 0)
        {
            growlSource.volume = maxGrowlVolume;
            growlSource.PlayOneShot(growlSounds[Random.Range(0, growlSounds.Length)]);
        }
    }

    public void StopAllSounds()
    {
        breathingSource?.Stop();
        footstepsSource?.Stop();
        growlSource?.Stop();
    }
}