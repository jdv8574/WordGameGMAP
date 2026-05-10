using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Word Spawning")]
    public GameObject wordPrefab;
    public Transform spawnPoint;
    public Canvas gameCanvas;
    public float baseSpawnInterval = 2f;
    private float currentSpawnInterval;
    private bool isGameActive = true;

    [Header("Timers")]
    public float gameDuration = 60f;
    private float gameTimer;
    public UnityEngine.UI.Image timerFillImage; // Visual timer
    private bool isPaused = false;

    [Header("Power-ups")]
    public bool spellingCorrectionPowerUpEnabled = false;
    private bool isPowerUpActive = false;
    private float powerUpDuration = 5f;
    private float originalSpawnInterval;
    private float originalFallSpeed;

    [Header("Scoring")]
    private int currentScore = 0;
    private int combo = 0;
    private float lastScoreTime = 0f;
    private const float COMBO_WINDOW = 2f;

    [Header("Word Database")]
    private List<WordData> wordDatabase = new List<WordData>();
    private System.Random random = new System.Random();

    [Header("Visual Feedback")]
    public GameObject scorePopupPrefab;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    [System.Serializable]
    public class WordData
    {
        public string correctWord;
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
        LoadWordDatabaseFromFile();
        gameTimer = gameDuration;
        currentSpawnInterval = baseSpawnInterval;
        originalSpawnInterval = baseSpawnInterval;

        // Find canvas if not assigned
        if (gameCanvas == null)
        {
            gameCanvas = FindObjectOfType<Canvas>();
        }

        // Set up spawn point
        if (spawnPoint == null)
        {
            GameObject spawn = new GameObject("SpawnPoint");
            spawn.transform.SetParent(gameCanvas.transform);
            spawnPoint = spawn.transform;
            RectTransform rect = spawnPoint.gameObject.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, 400);
        }

        StartSpawning();
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!isGameActive || isPaused) return;

        gameTimer -= Time.deltaTime;

        // Update visual timer
        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = gameTimer / gameDuration;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateTimer(gameTimer);

        if (gameTimer <= 0)
        {
            EndGame();
        }
    }

    void LoadWordDatabaseFromFile()
    {
        wordDatabase.Clear();

        // For now, use a subset of the words file
        // You can load from the actual file using Resources.Load or direct file reading

        // Sample loading from Resources folder
        TextAsset wordFile = Resources.Load<TextAsset>("words_alpha");
        if (wordFile != null)
        {
            string[] words = wordFile.text.Split('\n');
            foreach (string word in words)
            {
                if (!string.IsNullOrWhiteSpace(word))
                {
                    string cleanWord = word.Trim().ToLower();
                    if (cleanWord.Length >= 3 && cleanWord.Length <= 8) // Filter by length
                    {
                        WordData data = new WordData();
                        data.correctWord = cleanWord;
                        data.misspelling = GenerateMisspelling(cleanWord);
                        wordDatabase.Add(data);
                    }
                }
            }
        }

        // Fallback if no file found
        if (wordDatabase.Count == 0)
        {
            AddFallbackWords();
        }

        Debug.Log($"Loaded {wordDatabase.Count} words");
    }

    string GenerateMisspelling(string correctWord)
    {
        // Simple misspelling generation
        if (correctWord.Length < 2) return correctWord;

        string[] misspellings = new string[]
        {
            SwapAdjacentLetters(correctWord),
            DoubleLetter(correctWord),
            OmitLetter(correctWord),
            AddExtraLetter(correctWord)
        };

        return misspellings[random.Next(misspellings.Length)];
    }

    string SwapAdjacentLetters(string word)
    {
        if (word.Length < 2) return word;
        int pos = random.Next(word.Length - 1);
        char[] chars = word.ToCharArray();
        char temp = chars[pos];
        chars[pos] = chars[pos + 1];
        chars[pos + 1] = temp;
        return new string(chars);
    }

    string DoubleLetter(string word)
    {
        if (word.Length < 1) return word;
        int pos = random.Next(word.Length);
        return word.Insert(pos, word[pos].ToString());
    }

    string OmitLetter(string word)
    {
        if (word.Length < 2) return word;
        int pos = random.Next(word.Length);
        return word.Remove(pos, 1);
    }

    string AddExtraLetter(string word)
    {
        string letters = "abcdefghijklmnopqrstuvwxyz";
        int pos = random.Next(word.Length + 1);
        char extra = letters[random.Next(letters.Length)];
        return word.Insert(pos, extra.ToString());
    }

    void AddFallbackWords()
    {
        // Fallback words in case file loading fails
        string[] words = { "apple", "banana", "cherry", "dog", "cat", "house", "car", "book", "computer", "phone" };
        foreach (string word in words)
        {
            WordData data = new WordData();
            data.correctWord = word;
            data.misspelling = GenerateMisspelling(word);
            wordDatabase.Add(data);
        }
    }

    void StartSpawning()
    {
        StartCoroutine(SpawnWords());
    }

    IEnumerator SpawnWords()
    {
        while (isGameActive)
        {
            yield return new WaitForSeconds(currentSpawnInterval);

            if (isGameActive && !isPaused)
            {
                SpawnRandomWord();
            }
        }
    }

    void SpawnRandomWord()
    {
        if (wordPrefab == null || wordDatabase.Count == 0) return;

        WordData data = wordDatabase[random.Next(wordDatabase.Count)];

        // 50/50 chance for correct vs misspelled
        bool useCorrect = random.Next(2) == 0;
        string wordToShow = useCorrect ? data.correctWord : data.misspelling;

        GameObject newWord = Instantiate(wordPrefab, gameCanvas.transform);

        RectTransform rect = newWord.GetComponent<RectTransform>();
        if (rect != null && spawnPoint != null)
        {
            RectTransform spawnRect = spawnPoint.GetComponent<RectTransform>();
            rect.anchoredPosition = spawnRect != null ? spawnRect.anchoredPosition : new Vector2(0, 400);
        }

        Word wordScript = newWord.GetComponent<Word>();
        if (wordScript != null)
        {
            wordScript.Initialize(wordToShow, useCorrect, useCorrect ? "" : data.correctWord);
        }

        StartCoroutine(FallWord(newWord));
    }

    IEnumerator FallWord(GameObject word)
    {
        if (word == null) yield break;

        RectTransform rect = word.GetComponent<RectTransform>();
        if (rect == null) yield break;

        float fallSpeed = 150f;
        float bottomY = -980f;

        while (word != null && rect.anchoredPosition.y > bottomY)
        {
            if (isGameActive && !isPaused)
            {
                Vector2 newPos = rect.anchoredPosition;
                newPos.y -= fallSpeed * Time.deltaTime;
                rect.anchoredPosition = newPos;
            }
            yield return null;
        }

        if (word != null)
        {
            AddPoints(-10); // Penalty for missing word
            ShowScorePopup(-10, rect.position);
            Destroy(word);
            ResetCombo();
        }
    }

    public void AddPoints(int points)
    {
        // Apply combo multiplier
        if (points > 0)
        {
            // Check if within combo window
            if (Time.time - lastScoreTime < COMBO_WINDOW)
            {
                combo++;
                points *= (1 + combo / 10); // Combo bonus: +10% per combo
            }
            else
            {
                combo = 1;
            }
            lastScoreTime = Time.time;
        }
        else
        {
            ResetCombo();
        }

        currentScore += points;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddPoints(points);

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateCombo(combo);
    }

    void ResetCombo()
    {
        combo = 0;
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateCombo(0);
    }

    public void ShowScorePopup(int points, Vector3 position)
    {
        if (scorePopupPrefab != null)
        {
            GameObject popup = Instantiate(scorePopupPrefab, gameCanvas.transform);
            popup.transform.position = position;
            ScorePopup popupScript = popup.GetComponent<ScorePopup>();
            if (popupScript != null)
            {
                popupScript.Initialize(points);
            }
            Destroy(popup, 0.8f);
        }
    }

    public void ActivatePowerUp()
    {
        if (isPowerUpActive) return;

        isPowerUpActive = true;

        if (spellingCorrectionPowerUpEnabled)
        {
            // Slow down game instead of pausing
            Time.timeScale = 0.5f;
            StartCoroutine(PowerUpTimer());
        }
    }

    IEnumerator PowerUpTimer()
    {
        yield return new WaitForSecondsRealtime(powerUpDuration);

        // Deactivate power-up
        Time.timeScale = 1f;
        isPowerUpActive = false;
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }

    void EndGame()
    {
        isGameActive = false;
        Time.timeScale = 0f;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver(currentScore);

        Debug.Log($"Game Over! Final Score: {currentScore}");
    }

    public int GetScore()
    {
        return currentScore;
    }
}