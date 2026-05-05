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

        // Find canvas if not assigned
        if (gameCanvas == null)
        {
            gameCanvas = FindObjectOfType<Canvas>();
            Debug.Log($"Found canvas: {gameCanvas?.name}");
        }

        // Ensure spawn point is set up
        if (spawnPoint == null)
        {
            GameObject spawn = new GameObject("SpawnPoint");
            spawn.transform.SetParent(gameCanvas.transform);
            spawnPoint = spawn.transform;
            RectTransform rect = spawnPoint.gameObject.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, 400);
            Debug.Log("Created spawn point at top center");
        }

        // Start spawning
        StartSpawning();

        if (spellingPopup != null)
            spellingPopup.SetActive(false);

        // Ensure time scale starts at 1
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

        // Instantiate word as child of canvas
        GameObject newWord = Instantiate(wordPrefab, gameCanvas.transform);

        // Set initial position at spawn point
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

        // Track active word
        activeWords.Add(newWord);

        // Start falling coroutine
        StartCoroutine(FallWord(newWord));
    }

    IEnumerator FallWord(GameObject word)
    {
        if (word == null) yield break;

        RectTransform rect = word.GetComponent<RectTransform>();
        if (rect == null) yield break;

        float fallSpeed = 150f;
        float bottomY = -980f;

        while (word != null && rect.anchoredPosition.y > bottomY && isGameActive)
        {
            // Only fall if not in bonus mode
            if (!isBonusActive)
            {
                Vector2 newPos = rect.anchoredPosition;
                newPos.y -= fallSpeed * Time.deltaTime;
                rect.anchoredPosition = newPos;
            }
            yield return null;
        }

        // Word reached bottom without being sorted
        if (word != null)
        {
            Debug.Log($"Word reached bottom at Y: {rect.anchoredPosition.y}");
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(-5);

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

        // Stop any existing bonus timer
        if (bonusTimerCoroutine != null)
        {
            StopCoroutine(bonusTimerCoroutine);
            bonusTimerCoroutine = null;
        }

        currentWordForCorrection = word;
        currentCorrectSpelling = word.correctSpelling; // Store the correct spelling separately
        currentWrongWord = word.wordText; // Store the wrong word
        isBonusActive = true;
        isGameActive = false;

        // Stop spawning
        StopSpawning();

        // Pause the game
        Time.timeScale = 0f;

        // Show popup on top
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

        // Start bonus timer
        bonusTimerCoroutine = StartCoroutine(BonusTimerRoutine());
    }

    IEnumerator BonusTimerRoutine()
    {
        float timer = bonusTimerDuration;

        while (timer > 0)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateBonusTimer(timer);

            string timeText = timer.ToString("F1");
            Debug.Log($"Bonus timer: {timeText}");

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
        Debug.Log($"isBonusActive: {isBonusActive}");
        Debug.Log($"currentCorrectSpelling: '{currentCorrectSpelling}'");

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

        // Stop the bonus timer coroutine
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
        Debug.Log($"Was correct: {wasCorrect}");

        // Stop bonus timer coroutine
        if (bonusTimerCoroutine != null)
        {
            StopCoroutine(bonusTimerCoroutine);
            bonusTimerCoroutine = null;
        }

        // Award points
        if (wasCorrect)
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(20);
            Debug.Log($"✓ Bonus! +20 points for correct spelling of '{currentCorrectSpelling}'");
        }
        else
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(-5);
            Debug.Log($"✗ No bonus - incorrect spelling. Correct was '{currentCorrectSpelling}'");
        }

        // Close popup
        if (spellingPopup != null)
            spellingPopup.SetActive(false);

        // Resume game
        Time.timeScale = 1f;

        // Reset game state
        isBonusActive = false;
        isGameActive = true;

        // Clear stored data
        currentCorrectSpelling = "";
        currentWrongWord = "";

        // Remove and destroy the bonus word
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