using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class GameManager : MonoBehaviour
{
    public static GameManager singleton;

    [Header("Audio")]
    public AudioClip backgroundMusic;
    public AudioClip levelCompleteSound;
    public AudioClip perfectSound;
    private AudioSource musicSource;
    private AudioSource sfxSource;
    
    [Header("UI Elements")]
    public GameObject levelCompletePanel;
    public Text comboText;
    public Text levelNumberText;
    public Text starRatingText;
    public Image fadePanel;
    public GameObject perfectText;
    
    [Header("Level Complete Effects")]
    public ParticleSystem confettiEffect;
    public ParticleSystem fireworksEffect;
    
    [Header("Game Settings")]
    public float levelTransitionDelay = 2f;
    public int moveCountForPerfect = 10; // Moves needed for 3 stars
    
    private GroundPiece[] allGroundPieces;
    private int moveCount = 0;
    private int currentLevel = 1;
    private bool levelComplete = false;
    private float levelStartTime;

    private void FindUIElements()
    {
        // Automatically find UI in current scene
        if (levelNumberText == null)
            levelNumberText = GameObject.Find("LevelNumberText")?.GetComponent<Text>();

        if (comboText == null)
            comboText = GameObject.Find("ComboText")?.GetComponent<Text>();

        if (levelCompletePanel == null)
            levelCompletePanel = GameObject.Find("LevelCompletePanel");

        if (starRatingText == null)
            starRatingText = GameObject.Find("StarRatingText")?.GetComponent<Text>();

        if (perfectText == null)
            perfectText = GameObject.Find("PerfectText");

        if (fadePanel == null)
            fadePanel = GameObject.Find("FadePanel")?.GetComponent<Image>();
    }

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
            DontDestroyOnLoad(gameObject);
            
            // Setup audio sources
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = 0.3f;
            
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.volume = 0.6f;
        }
        else if (singleton != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupNewLevel();
        PlayBackgroundMusic();
    }

    private void SetupNewLevel()
    {
        allGroundPieces = FindObjectsOfType<GroundPiece>();
        moveCount = 0;
        levelComplete = false;
        levelStartTime = Time.time;
        currentLevel = SceneManager.GetActiveScene().buildIndex + 1;
        
        FindUIElements();
        // Update level UI
        if (levelNumberText != null)
        {
            levelNumberText.text = "LEVEL " + currentLevel;
        }
        
        // Hide level complete panel
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
        
        // Fade in
        StartCoroutine(FadeIn());
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        SetupNewLevel();
    }

    public void CheckComplete()
    {
        if (levelComplete) return;
        
        bool isFinished = true;

        for (int i = 0; i < allGroundPieces.Length; i++)
        {
            if (allGroundPieces[i].isColored == false)
            {
                isFinished = false;
                break;
            }
        }

        if (isFinished)
        {
            OnLevelComplete();
        }
    }

    private void OnLevelComplete()
    {
        levelComplete = true;
        
        // Calculate stats
        float completionTime = Time.time - levelStartTime;
        int stars = CalculateStarRating();
        bool isPerfect = (stars == 3);
        
        // Play level complete sound
        if (isPerfect && perfectSound != null)
        {
            sfxSource.PlayOneShot(perfectSound);
        }
        else if (levelCompleteSound != null)
        {
            sfxSource.PlayOneShot(levelCompleteSound);
        }
        
        // Spawn celebration particles
        SpawnCelebrationEffects(isPerfect);
        
        // Screen shake
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCamera(0.2f, 0.5f);
        }
        
        // Show completion UI
        StartCoroutine(ShowLevelCompleteUI(stars, completionTime, isPerfect));
    }

    private IEnumerator ShowLevelCompleteUI(int stars, float time, bool isPerfect)
    {
        yield return new WaitForSeconds(1f);
        
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
            
            // Animate star rating
            if (starRatingText != null)
            {
                StartCoroutine(AnimateStars(stars));
            }
            
            // Show perfect text
            if (isPerfect && perfectText != null)
            {
                perfectText.SetActive(true);
            }
        }
        
        // Auto-advance to next level
        yield return new WaitForSeconds(levelTransitionDelay);
        NextLevel();
    }

    private IEnumerator AnimateStars(int starCount)
    {
        string starsText = "";
        for (int i = 0; i < starCount; i++)
        {
            starsText += "* ";
            starRatingText.text = starsText;
            
            // Play a sound for each star
            sfxSource.pitch = 1f + (i * 0.2f);
            sfxSource.PlayOneShot(levelCompleteSound, 0.3f);
            
            yield return new WaitForSeconds(0.3f);
        }
    }

    private int CalculateStarRating()
    {
        // Calculate stars based on efficiency
        int totalPieces = allGroundPieces.Length;
        
        // Perfect: minimal moves
        if (moveCount <= moveCountForPerfect)
            return 3;
        // Good: reasonable moves
        else if (moveCount <= moveCountForPerfect * 1.5f)
            return 2;
        // Completed
        else
            return 1;
    }

    private void SpawnCelebrationEffects(bool isPerfect)
    {
        // Spawn confetti at each colored ground piece
        foreach (GroundPiece piece in allGroundPieces)
        {
            if (confettiEffect != null)
            {
                Vector3 pos = piece.transform.position + Vector3.up * 0.5f;
                ParticleSystem confetti = Instantiate(confettiEffect, pos, Quaternion.identity);
                confetti.Play();
                Destroy(confetti.gameObject, 3f);
            }
        }
        
        // Extra fireworks for perfect completion
        if (isPerfect && fireworksEffect != null)
        {
            ParticleSystem fireworks = Instantiate(fireworksEffect, Vector3.up * 5, Quaternion.identity);
            fireworks.Play();
            Destroy(fireworks.gameObject, 5f);
        }
    }

    private void NextLevel()
    {
        StartCoroutine(FadeOutAndLoad());
    }

    private IEnumerator FadeIn()
    {
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            Color c = fadePanel.color;
            
            for (float t = 1f; t >= 0; t -= Time.deltaTime * 2f)
            {
                c.a = t;
                fadePanel.color = c;
                yield return null;
            }
            
            fadePanel.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeOutAndLoad()
    {
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            Color c = fadePanel.color;
            
            for (float t = 0f; t <= 1f; t += Time.deltaTime * 2f)
            {
                c.a = t;
                fadePanel.color = c;
                yield return null;
            }
        }
        
        // Load next level or loop back to first level
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            nextSceneIndex = 0; // Loop back to first level
        }
        
        SceneManager.LoadScene(nextSceneIndex);
    }

    private void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void ShowComboText(int combo)
    {
        if (comboText != null)
        {
            StartCoroutine(DisplayCombo(combo));
        }
    }

    private IEnumerator DisplayCombo(int combo)
    {
        comboText.text = combo + "x COMBO!";
        comboText.gameObject.SetActive(true);
        
        // Animate scale
        Vector3 originalScale = comboText.transform.localScale;
        comboText.transform.localScale = Vector3.zero;
        
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            comboText.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale * 1.2f, t);
            yield return null;
        }
        
        yield return new WaitForSeconds(1f);
        
        // Fade out
        t = 0;
        Color c = comboText.color;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            c.a = 1 - t;
            comboText.color = c;
            yield return null;
        }
        
        comboText.gameObject.SetActive(false);
        c.a = 1;
        comboText.color = c;
    }

    public void IncrementMoveCount()
    {
        moveCount++;
    }
}