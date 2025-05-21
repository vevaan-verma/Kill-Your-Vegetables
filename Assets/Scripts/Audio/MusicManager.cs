using UnityEngine;

public class MusicManager : MonoBehaviour {

    [Header("References")]
    [SerializeField] private AudioClip[] musicClips;
    private AudioSource musicSource;

    private void Start() {

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.clip = musicClips[Random.Range(0, musicClips.Length)]; // play a random music clip
        musicSource.Play();

    }
}
