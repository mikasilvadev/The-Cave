using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MonsterAudio : MonoBehaviour
{
    private AudioSource audioSource;
    [Header("Sons de Passos")]
    public AudioClip[] footstepClips;

    [Header("Som de Movimento Contínuo")]
    public AudioClip movingClip;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.loop = false;
        audioSource.playOnAwake = false;
    }

    public void TocarSomDePasso()
    {
        if (!audioSource.enabled) return;

        if (footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.PlayOneShot(clip);
    }

    public void StartMovingSound()
    {
        if (movingClip != null && !audioSource.isPlaying)
        {
            audioSource.clip = movingClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    public void StopMovingSound()
    {
        if (audioSource.clip == movingClip)
        {
            audioSource.Stop();
        }
    }

    public void EnableSound()
    {
        audioSource.enabled = true;
    }

    public void DisableSound()
    {
        if (audioSource.clip == movingClip && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        audioSource.enabled = false;
    }
}