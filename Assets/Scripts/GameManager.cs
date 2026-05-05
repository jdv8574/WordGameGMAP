using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Word Spawning")]
    public GameObject wordPrefab;
    public Transform spawnPoint;
    public Canvas gameCanvas;
    public float spawnInterval = 2f;

    [Header("Timers")]
    public float gameDuration = 60f;
    private float gameTimer;
    private bool isGameActive = true;

    [Header("Spelling Bonus")]
    private Word currentWordForCorrection;
    private string currentCorrectSpelling;
    private string currentWrongWord;
    public GameObject spellingPopup;
    public TMP_InputField spellingInput;
    public float bonusTimerDuration = 5f;
    private bool isBonusActive = false;
    private Coroutine spawningCoroutine;
    private Coroutine bonusTimerCoroutine;
    private List<GameObject> activeWords = new List<GameObject>();
    private List<Coroutine> activeFallCoroutines = new List<Coroutine>(); // Track fall coroutines

    [Header("Word Database")]
    private List<WordData> wordDatabase = new List<WordData>();

    [System.Serializable]
    public class WordData
    {
        public string correctWord;
        public string definition;
        public string misspelling;
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        LoadWordDatabase();
        gameTimer = gameDuration;

        if (gameCanvas == null)
        {
            gameCanvas = FindObjectOfType<Canvas>();
            Debug.Log($"Found canvas: {gameCanvas?.name}");
        }

        if (spawnPoint == null)
        {
            GameObject spawn = new GameObject("SpawnPoint");
            spawn.transform.SetParent(gameCanvas.transform);
            spawnPoint = spawn.transform;
            RectTransform rect = spawnPoint.gameObject.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, 400);
            Debug.Log("Created spawn point at top center");
        }

        StartSpawning();

        if (spellingPopup != null)
            spellingPopup.SetActive(false);

        Time.timeScale = 1f;
    }

    void StartSpawning()
    {
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
        }
        spawningCoroutine = StartCoroutine(SpawnWords());
    }

    void StopSpawning()
    {
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }
    }

    void Update()
    {
        if (!isGameActive || isBonusActive) return;

        gameTimer -= Time.deltaTime;

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateTimer(gameTimer);

        if (gameTimer <= 0)
        {
            EndGame();
        }
    }

    void LoadWordDatabase()
    {
        wordDatabase.Clear();
        wordDatabase.Add(new WordData
        {
            correctWord = "necessary",
            definition = "required to be done; essential",
            misspelling = "neccessary"
        });
        wordDatabase.Add(new WordData
        {
            correctWord = "definitely",
            definition = "without doubt",
            misspelling = "definately"
        });
        wordDatabase.Add(new WordData
        {
            correctWord = "separate",
            definition = "to move apart",
            misspelling = "seperate"
        });
        wordDatabase.Add(new WordData
        {
            correctWord = "accommodate",
            definition = "to provide with something needed",
            misspelling = "accomodate"
        });
        wordDatabase.Add(new WordData
        {
            correctWord = "embarrass",
            definition = "to cause to feel self-conscious",
            misspelling = "embarass"
        });

        Debug.Log($"Loaded {wordDatabase.Count} words");
    }

    IEnumerator SpawnWords()
    {
        while (isGameActive && !isBonusActive)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (!isBonusActive && isGameActive && gameTimer > 0)
            {
                SpawnRandomWord();
            }
        }
        Debug.Log("SpawnWords coroutine ended");
    }

    void SpawnRandomWord()
    {
        if (wordPrefab == null || wordDatabase.Count == 0) return;

        WordData data = wordDatabase[Random.Range(0, wordDatabase.Count)];
        bool useCorrect = Random.value > 0.5f;
        string wordToShow = useCorrect ? data.correctWord : data.misspelling;

        GameObject newWord = Instantiate(wordPrefab, gameCanvas.transform);

        RectTransform rect = newWord.GetComponent<RectTransform>();
        if (rect != null && spawnPoint != null)
        {
            RectTransform spawnRect = spawnPoint.GetComponent<RectTransform>();
            if (spawnRect != null)
            {
                rect.anchoredPosition = spawnRect.anchoredPosition;
            }
            else
            {
                rect.anchoredPosition = new Vector2(0, 400);
            }
        }

        Word wordScript = newWord.GetComponent<Word>();
        if (wordScript != null)
        {
            wordScript.Initialize(wordToShow, data.definition, useCorrect, useCorrect ? "" : data.correctWord);
        }

        activeWords.Add(newWord);

        // Track the coroutine
        Coroutine fallCoroutine = StartCoroutine(FallWord(newWord));
        activeFallCoroutines.Add(fallCoroutine);
    }

    IEnumerator FallWord(GameObject word)
    {
        if (word == null) yield break;

        RectTransform rect = word.GetComponent<RectTransform>();
        if (rect == null) yield break;

        float fallSpeed = 150f;
        float bottomY = -527f;

        while (word != null && rect.anchoredPosition.y > bottomY)
        {
            // Only fall if not in bonus mode AND game is active
            if (!isBonusActive && isGameActive)
            {
                Vector2 newPos = rect.anchoredPosition;
                newPos.y -= fallSpeed * Time.deltaTime;
                rect.anchoredPosition = newPos;
            }
            yield return null;
        }

        // Word reached bottom without being sorted - ONLY penalize if not in bonus mode
        if (word != null && !isBonusActive && isGameActive)
        {
            Debug.Log($"Word '{word.GetComponent<Word>()?.wordText}' fell off screen! -10 points");
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(-10);

            activeWords.Remove(word);
            Destroy(word);
        }
        else if (word != null)
        {
            // Word was destroyed during bonus mode - just remove from tracking
            activeWords.Remove(word);
            Destroy(word);
        }
    }

    public void StartSpellingCorrection(Word word)
    {
        if (isBonusActive) return;

        Debug.Log($"=== STARTING SPELLING CORRECTION ===");
        Debug.Log($"Word: {word.wordText}");
        Debug.Log($"Correct spelling: {word.correctSpelling}");

        if (bonusTimerCoroutine != null)
        {
            StopCoroutine(bonusTimerCoroutine);
            bonusTimerCoroutine = null;
        }

        currentWordForCorrection = word;
        currentCorrectSpelling = word.correctSpelling;
        currentWrongWord = word.wordText;
        isBonusActive = true;
        isGameActive = false;

        StopSpawning();

        // Note: We DON'T pause time or destroy other words
        // The FallWord coroutines check isBonusActive and will pause automatically

        // Show popup
        if (spellingPopup != null)
        {
            spellingPopup.SetActive(true);
            spellingPopup.transform.SetAsLastSibling();

            if (spellingInput != null)
            {
                spellingInput.text = "";
                spellingInput.Select();
                spellingInput.ActivateInputField();
            }
        }

        bonusTimerCoroutine = StartCoroutine(BonusTimerRoutine());
    }

    IEnumerator BonusTimerRoutine()
    {
        float timer = bonusTimerDuration;

        while (timer > 0)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateBonusTimer(timer);

            yield return new WaitForSecondsRealtime(0.1f);
            timer -= 0.1f;
        }

        if (isBonusActive)
        {
            Debug.Log("Bonus timer expired!");
            EndSpellingCorrection(false);
        }
    }

    public void SubmitSpellingCorrection()
    {
        Debug.Log("=== SUBMIT SPELLING CORRECTION CALLED IN GAMEMANAGER ===");

        if (!isBonusActive)
        {
            Debug.LogWarning("Not in bonus mode - ignoring submission");
            return;
        }

        if (string.IsNullOrEmpty(currentCorrectSpelling))
        {
            Debug.LogError("currentCorrectSpelling is null or empty!");
            return;
        }

        string submitted = "";
        if (spellingInput != null)
        {
            submitted = spellingInput.text.Trim().ToLower();
            Debug.Log($"Input text: '{submitted}'");
        }
        else
        {
            Debug.LogError("spellingInput is null!");
            return;
        }

        bool isCorrect = (submitted == currentCorrectSpelling);
        Debug.Log($"Comparing '{submitted}' with '{currentCorrectSpelling}': {isCorrect}");

        if (bonusTimerCoroutine != null)
        {
            StopCoroutine(bonusTimerCoroutine);
            bonusTimerCoroutine = null;
        }

        EndSpellingCorrection(isCorrect);
    }

    void EndSpellingCorrection(bool wasCorrect)
    {
        Debug.Log($"=== ENDING SPELLING CORRECTION ===");

        if (bonusTimerCoroutine != null)
        {
            StopCoroutine(bonusTimerCoroutine);
            bonusTimerCoroutine = null;
        }

        // Award bonus points
        if (wasCorrect)
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(15); // Changed to 15 as per your rules
            Debug.Log($"✓ Bonus! +15 points for correct spelling");
        }
        else
        {
            Debug.Log($"✗ No bonus - incorrect spelling");
        }

        // Close popup
        if (spellingPopup != null)
            spellingPopup.SetActive(false);

        // Reset game state (but don't change time scale)
        isBonusActive = false;
        isGameActive = true;

        // Remove and destroy the bonus word only
        if (currentWordForCorrection != null)
        {
            activeWords.Remove(currentWordForCorrection.gameObject);
            Destroy(currentWordForCorrection.gameObject);
            currentWordForCorrection = null;
        }

        // Restart spawning
        if (isGameActive && gameTimer > 0)
        {
            StartSpawning();
        }

        Debug.Log("Bonus mode ended - words will resume falling");
    }

    public void RemoveWord(GameObject word)
    {
        if (activeWords.Contains(word))
        {
            activeWords.Remove(word);
        }
    }

    void EndGame()
    {
        isGameActive = false;
        StopSpawning();
        Time.timeScale = 0f;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver();

        Debug.Log($"Game Over! Final Score: {(ScoreManager.Instance != null ? ScoreManager.Instance.GetScore() : 0)}");
    }

    public bool IsGameActive()
    {
        return isGameActive && !isBonusActive;
    }
}