using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemySoundManager : SoundManager {

    [Header("Sounds")]
    [SerializeField] private EnemySoundData[] enemySoundData;
    private Dictionary<EnemySoundType, AudioClip[]> enemySounds;
    private Dictionary<EnemySoundType, AudioSource> audioSources;

    private void Start() {

        #region VALIDATION
        // make sure each sound type has exactly one sound data
        EnemySoundType[] soundTypes = (EnemySoundType[]) Enum.GetValues(typeof(EnemySoundType));

        foreach (EnemySoundType soundType in soundTypes) {

            int count = 0;

            foreach (EnemySoundData soundData in enemySoundData)
                if (soundData.GetSoundType() == soundType)
                    count++;

            if (count != 1)
                Debug.LogError("There should be exactly one sound data per sound type. " + soundType.ToString() + " on " + transform.name + " does not meet this requirement.");

        }
        #endregion

        // create an audio source for each sound type
        audioSources = new Dictionary<EnemySoundType, AudioSource>();

        foreach (EnemySoundType soundType in soundTypes) {

            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSources.Add(soundType, audioSource);

        }

        enemySounds = enemySoundData.ToDictionary(data => data.GetSoundType(), data => data.GetAudioClips()); // convert enemySoundData to dictionary for efficient access

    }

    public void PlaySound(EnemySoundType soundType) {

        if (enemySounds.ContainsKey(soundType))
            PlayClip(audioSources[soundType], enemySounds[soundType][UnityEngine.Random.Range(0, enemySounds[soundType].Length)]);
        else
            Debug.LogError("Sound not found for " + soundType + " on " + gameObject.name);

    }
}

[Serializable]
public class EnemySoundData {

    [Header("Data")]
    [SerializeField] private EnemySoundType soundType;
    [SerializeField] private AudioClip[] audioClips;

    public EnemySoundType GetSoundType() => soundType;

    public AudioClip[] GetAudioClips() => audioClips;

}

public enum EnemySoundType {

    Attack,
    Damaged,
    Death

}
