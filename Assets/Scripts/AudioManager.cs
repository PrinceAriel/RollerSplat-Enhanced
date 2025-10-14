using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume = 0.7f;
    [Range(0.5f, 1.5f)]
    public float pitch = 1f;
    public bool loop = false;
    
    [HideInInspector]
    public AudioSource source;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    public Sound[] sounds;
    public Sound[] musicTracks;
    
    private AudioSource currentMusicSource;
    private string currentMusicName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSounds();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSounds()
    {
        // Initialize sound effects
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
        
        // Initialize music tracks
        foreach (Sound m in musicTracks)
        {
            m.source = gameObject.AddComponent<AudioSource>();
            m.source.clip = m.clip;
            m.source.volume = m.volume;
            m.source.pitch = m.pitch;
            m.source.loop = m.loop;
        }
    }

    public void PlaySound(string name)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        s.source.Play();
    }
    
    public void PlaySoundWithPitch(string name, float pitch)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        s.source.pitch = pitch;
        s.source.Play();
    }

    public void PlayMusic(string name)
    {
        // Stop current music if playing
        if (currentMusicSource != null && currentMusicSource.isPlaying)
        {
            currentMusicSource.Stop();
        }
        
        Sound m = System.Array.Find(musicTracks, music => music.name == name);
        if (m == null)
        {
            Debug.LogWarning("Music: " + name + " not found!");
            return;
        }
        
        currentMusicSource = m.source;
        currentMusicName = name;
        m.source.Play();
    }

    public void StopMusic()
    {
        if (currentMusicSource != null)
        {
            currentMusicSource.Stop();
        }
    }

    public void SetMusicVolume(float volume)
    {
        foreach (Sound m in musicTracks)
        {
            m.source.volume = volume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        foreach (Sound s in sounds)
        {
            s.source.volume = volume;
        }
    }
    
    public void CrossfadeMusic(string newMusicName, float fadeTime = 1f)
    {
        if (currentMusicName == newMusicName) return;
        
        StartCoroutine(CrossfadeCoroutine(newMusicName, fadeTime));
    }
    
    private System.Collections.IEnumerator CrossfadeCoroutine(string newMusicName, float fadeTime)
    {
        Sound newMusic = System.Array.Find(musicTracks, music => music.name == newMusicName);
        if (newMusic == null) yield break;
        
        AudioSource oldSource = currentMusicSource;
        float oldVolume = oldSource != null ? oldSource.volume : 0;
        
        // Start new music at volume 0
        newMusic.source.volume = 0;
        newMusic.source.Play();
        
        float elapsed = 0;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            
            if (oldSource != null)
            {
                oldSource.volume = Mathf.Lerp(oldVolume, 0, t);
            }
            
            newMusic.source.volume = Mathf.Lerp(0, newMusic.volume, t);
            
            yield return null;
        }
        
        if (oldSource != null)
        {
            oldSource.Stop();
            oldSource.volume = oldVolume;
        }
        
        currentMusicSource = newMusic.source;
        currentMusicName = newMusicName;
    }
}
