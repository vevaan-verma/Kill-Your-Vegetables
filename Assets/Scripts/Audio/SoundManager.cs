using UnityEngine;

public abstract class SoundManager : MonoBehaviour {

    protected void PlayClip(AudioSource audioSource, AudioClip clip) {

        audioSource.clip = clip;
        audioSource.Play();

    }
}
