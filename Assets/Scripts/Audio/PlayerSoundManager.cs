using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSoundManager : SoundManager {

    [Header("Sounds")]
    [SerializeField] private PlayerSoundData[] playerSoundData;
    private Dictionary<PlayerSoundType, AudioClip[]> playerSounds;
    private Dictionary<PlayerSoundType, AudioSource> audioSources;

    private void Start() {

        #region VALIDATION
        // make sure each sound type has exactly one sound data
        PlayerSoundType[] soundTypes = (PlayerSoundType[]) Enum.GetValues(typeof(PlayerSoundType));

        foreach (PlayerSoundType soundType in soundTypes) {

            int count = 0;

            foreach (PlayerSoundData soundData in playerSoundData)
                if (soundData.GetSoundType() == soundType)
                    count++;

            if (count != 1)
                Debug.LogError("There should be exactly one sound data per sound type. " + soundType.ToString() + " does not meet this requirement.");

        }
        #endregion

        // create an audio source for each sound type
        audioSources = new Dictionary<PlayerSoundType, AudioSource>();

        foreach (PlayerSoundType soundType in soundTypes) {

            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSources.Add(soundType, audioSource);

        }

        playerSounds = playerSoundData.ToDictionary(data => data.GetSoundType(), data => data.GetAudioClips()); // convert playerSoundData to dictionary for efficient access

    }

    public void PlaySound(PlayerSoundType soundType) {

        if (playerSounds.ContainsKey(soundType))
            PlayClip(audioSources[soundType], playerSounds[soundType][UnityEngine.Random.Range(0, playerSounds[soundType].Length)]);
        else
            Debug.LogError("Sound not found for " + soundType + " on " + gameObject.name);

    }
}

[Serializable]
public class PlayerSoundData {

    [Header("Data")]
    [SerializeField] private PlayerSoundType soundType;
    [SerializeField] private AudioClip[] audioClips;

    public PlayerSoundType GetSoundType() => soundType;

    public AudioClip[] GetAudioClips() => audioClips;

}

public enum PlayerSoundType {

    Walk,
    Damaged,
    Dash,
    Death

}