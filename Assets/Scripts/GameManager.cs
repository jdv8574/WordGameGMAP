using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public UnityEngine.UI.Image timerFillImage;
    public TextMeshProUGUI timerText;
    private bool isPaused = false;

    [Header("Power-ups")]
    public bool spellingCorrectionPowerUpEnabled = false;
    private bool isPowerUpActive = false;
    private float powerUpDuration = 5f;
    private float originalSpawnInterval;

    [Header("Scoring")]
    private int currentScore = 0;
    private int combo = 0;
    private float lastScoreTime = 0f;
    private float comboExpireTime = 0f;
    private const float COMBO_WINDOW = 3.5f;
    private Coroutine comboExpireCoroutine;

    [Header("Dynamic Difficulty")]
    public float baseFallSpeed = 150f;
    private float currentFallSpeed;
    private int lastDifficultyCombo = 0;
    public int baseCommonWordChance = 70;
    private int currentCommonWordChance;

    [System.Serializable]
    public class DifficultyTier
    {
        public int comboThreshold;
        public float spawnIntervalMultiplier;
        public float fallSpeedMultiplier;
        public int commonWordChanceReduction; // How much to reduce common word chance by (%)
    }

    public DifficultyTier[] difficultyTiers = new DifficultyTier[]
    {
        new DifficultyTier { comboThreshold = 5, spawnIntervalMultiplier = 0.85f, fallSpeedMultiplier = 1.15f, commonWordChanceReduction = 10 },
        new DifficultyTier { comboThreshold = 10, spawnIntervalMultiplier = 0.7f, fallSpeedMultiplier = 1.3f, commonWordChanceReduction = 15 },
        new DifficultyTier { comboThreshold = 15, spawnIntervalMultiplier = 0.55f, fallSpeedMultiplier = 1.5f, commonWordChanceReduction = 25 },
        new DifficultyTier { comboThreshold = 20, spawnIntervalMultiplier = 0.4f, fallSpeedMultiplier = 1.65f, commonWordChanceReduction = 40 }
    };

    public ParticleSystem speedEffectParticles;

    [Header("Word Database")]
    private List<WordData> wordDatabase = new List<WordData>();
    private List<WordData> commonWords = new List<WordData>();
    private List<WordData> uncommonWords = new List<WordData>();
    private System.Random random = new System.Random();

    [Header("Word Frequency Settings")]
    public int minWordLength = 3;
    public int maxWordLength = 8;

    [Header("Visual Feedback")]
    public GameObject scorePopupPrefab;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    [System.Serializable]
    public class WordData
    {
        public string correctWord;
        public string misspelling;
        public int frequencyRank = 999;
    }

    private HashSet<string> commonWordSet = new HashSet<string>
    {
        "the", "be", "to", "of", "and", "a", "in", "that", "have", "i",
        "it", "for", "not", "on", "with", "he", "as", "you", "do", "at",
        "this", "but", "his", "by", "from", "they", "we", "say", "her", "she",
        "or", "an", "will", "my", "one", "all", "would", "there", "their", "what",
        "so", "up", "out", "if", "about", "who", "get", "which", "go", "me",
        "when", "make", "can", "like", "time", "no", "just", "him", "know", "take",
        "people", "into", "year", "your", "good", "some", "could", "them", "see", "other",
        "than", "then", "now", "look", "only", "come", "its", "over", "think", "also",
        "back", "after", "use", "two", "how", "our", "work", "first", "well", "way",
        "even", "new", "want", "because", "any", "these", "give", "day", "most", "us"
    };

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
        currentFallSpeed = baseFallSpeed;
        currentCommonWordChance = baseCommonWordChance;

        UpdateTimerDisplay();

        if (gameCanvas == null)
            gameCanvas = FindObjectOfType<Canvas>();

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

        Debug.Log($"Game started! Base spawn: {baseSpawnInterval}s, Base fall speed: {baseFallSpeed}, Common word chance: {baseCommonWordChance}%");
    }

    void Update()
    {
        if (!isGameActive || isPaused) return;

        gameTimer -= Time.deltaTime;
        UpdateTimerDisplay();

        if (gameTimer <= 0)
        {
            gameTimer = 0;
            EndGame();
        }

        if (UIManager.Instance != null && combo > 0)
        {
            float remainingTime = comboExpireTime - Time.time;
            if (remainingTime > 0)
            {
                UIManager.Instance.UpdateComboTime(remainingTime);
            }
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerFillImage != null)
        {
            float fillAmount = Mathf.Clamp01(gameTimer / gameDuration);
            timerFillImage.fillAmount = fillAmount;

            if (fillAmount < 0.3f)
                timerFillImage.color = Color.red;
            else if (fillAmount < 0.6f)
                timerFillImage.color = Color.yellow;
            else
                timerFillImage.color = Color.green;
        }

        if (timerText != null)
        {
            timerText.text = $"{Mathf.CeilToInt(gameTimer)}s";
        }

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateTimer(gameTimer);
    }

    void UpdateDifficultyBasedOnCombo()
    {
        // Find current difficulty tier
        DifficultyTier currentTier = null;
        for (int i = difficultyTiers.Length - 1; i >= 0; i--)
        {
            if (combo >= difficultyTiers[i].comboThreshold)
            {
                currentTier = difficultyTiers[i];
                break;
            }
        }

        // Apply difficulty changes if tier changed
        if (currentTier != null && lastDifficultyCombo != combo)
        {
            float newSpawnInterval = baseSpawnInterval * currentTier.spawnIntervalMultiplier;
            float newFallSpeed = baseFallSpeed * currentTier.fallSpeedMultiplier;
            int newCommonWordChance = Mathf.Max(0, baseCommonWordChance - currentTier.commonWordChanceReduction);

            if (Mathf.Abs(currentSpawnInterval - newSpawnInterval) > 0.01f)
            {
                currentSpawnInterval = newSpawnInterval;
                currentFallSpeed = newFallSpeed;
                currentCommonWordChance = newCommonWordChance;

                Debug.Log($"Difficulty increased! Spawn: {currentSpawnInterval:F2}s, Fall speed: {newFallSpeed:F0}, Common word chance: {currentCommonWordChance}%");

                // Play effect
                if (speedEffectParticles != null)
                    speedEffectParticles.Play();
            }

            lastDifficultyCombo = combo;
        }
        else if (currentTier == null && lastDifficultyCombo != 0)
        {
            // Reset to base difficulty when combo resets
            if (Mathf.Abs(currentSpawnInterval - baseSpawnInterval) > 0.01f)
            {
                currentSpawnInterval = baseSpawnInterval;
                currentFallSpeed = baseFallSpeed;
                currentCommonWordChance = baseCommonWordChance;
                Debug.Log($"Difficulty reset to Normal! Spawn: {currentSpawnInterval}s, Fall speed: {currentFallSpeed:F0}, Common word chance: {currentCommonWordChance}%");
                lastDifficultyCombo = 0;
            }
        }
    }

    void LoadWordDatabaseFromFile()
    {
        wordDatabase.Clear();
        commonWords.Clear();
        uncommonWords.Clear();

        TextAsset wordFile = Resources.Load<TextAsset>("words_alpha");
        if (wordFile != null)
        {
            string[] words = wordFile.text.Split('\n');
            int frequencyCounter = 1;

            foreach (string word in words)
            {
                if (!string.IsNullOrWhiteSpace(word))
                {
                    string cleanWord = word.Trim().ToLower();

                    if (cleanWord.Length >= minWordLength && cleanWord.Length <= maxWordLength)
                    {
                        WordData data = new WordData();
                        data.correctWord = cleanWord;
                        data.misspelling = GenerateMisspelling(cleanWord);

                        if (IsCommonWord(cleanWord))
                        {
                            data.frequencyRank = frequencyCounter++;
                            commonWords.Add(data);
                        }
                        else
                        {
                            data.frequencyRank = 999;
                            uncommonWords.Add(data);
                        }

                        wordDatabase.Add(data);
                    }
                }
            }
        }

        if (wordDatabase.Count == 0)
        {
            AddFallbackWords();
        }

        ShuffleList(commonWords);
        ShuffleList(uncommonWords);

        Debug.Log($"Loaded {commonWords.Count} common words, {uncommonWords.Count} uncommon words");
    }

    bool IsCommonWord(string word)
    {
        return commonWordSet.Contains(word);
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = random.Next(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    string GenerateMisspelling(string correctWord)
    {
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
        string[] words = { "apple", "banana", "cherry", "dog", "cat", "house", "car", "book", "computer", "phone" };
        foreach (string word in words)
        {
            WordData data = new WordData();
            data.correctWord = word;
            data.misspelling = GenerateMisspelling(word);
            if (IsCommonWord(word))
                commonWords.Add(data);
            else
                uncommonWords.Add(data);
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
        if (wordPrefab == null) return;

        WordData data = SelectWordByFrequency();
        if (data == null) return;

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

    WordData SelectWordByFrequency()
    {
        // Use current common word chance (which decreases with combo)
        bool pickCommon = random.Next(100) < currentCommonWordChance;

        if (pickCommon && commonWords.Count > 0)
        {
            // Pick from common words
            return commonWords[random.Next(commonWords.Count)];
        }
        else if (uncommonWords.Count > 0)
        {
            // Pick from uncommon words
            return uncommonWords[random.Next(uncommonWords.Count)];
        }
        else if (commonWords.Count > 0)
        {
            return commonWords[random.Next(commonWords.Count)];
        }

        return null;
    }

    IEnumerator FallWord(GameObject word)
    {
        if (word == null) yield break;

        RectTransform rect = word.GetComponent<RectTransform>();
        if (rect == null) yield break;

        float bottomY = -980f;

        while (word != null && rect.anchoredPosition.y > bottomY)
        {
            if (isGameActive && !isPaused)
            {
                Vector2 newPos = rect.anchoredPosition;
                newPos.y -= currentFallSpeed * Time.deltaTime;
                rect.anchoredPosition = newPos;
            }
            yield return null;
        }

        if (word != null)
        {
            AddPoints(-10);
            ShowScorePopup(-10, rect.position);
            Destroy(word);
            ResetCombo();
        }
    }

    public void AddPoints(int points)
    {
        int pointsToAdd = points;
        int comboBonus = 0;

        if (points > 0)
        {
            if (Time.time - lastScoreTime < COMBO_WINDOW)
            {
                combo++;
                float multiplier = 1f + (Mathf.Min(combo, 10) * 0.1f);
                pointsToAdd = Mathf.RoundToInt(points * multiplier);
                comboBonus = pointsToAdd - points;
            }
            else
            {
                combo = 1;
                comboBonus = 0;
            }
            lastScoreTime = Time.time;

            if (comboExpireCoroutine != null)
                StopCoroutine(comboExpireCoroutine);
            comboExpireCoroutine = StartCoroutine(ComboExpireRoutine());

            // Update difficulty based on new combo
            UpdateDifficultyBasedOnCombo();
        }
        else
        {
            ResetCombo();
        }

        currentScore += pointsToAdd;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddPoints(pointsToAdd);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCombo(combo);
            if (comboBonus > 0)
            {
                UIManager.Instance.ShowComboBonus(comboBonus);
            }
        }
    }

    IEnumerator ComboExpireRoutine()
    {
        float expirationTime = COMBO_WINDOW;
        comboExpireTime = Time.time + expirationTime;

        while (expirationTime > 0)
        {
            expirationTime -= Time.deltaTime;
            comboExpireTime = Time.time + expirationTime;
            yield return null;
        }

        if (combo > 0)
        {
            ResetCombo();
        }
    }

    void ResetCombo()
    {
        combo = 0;
        if (comboExpireCoroutine != null)
        {
            StopCoroutine(comboExpireCoroutine);
            comboExpireCoroutine = null;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateCombo(0);

        // Reset difficulty
        UpdateDifficultyBasedOnCombo();
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

    public float GetCurrentFallSpeed()
    {
        return currentFallSpeed;
    }

    public int GetCurrentWordChance()
    {
        return currentCommonWordChance;
    }

    public void ActivatePowerUp()
    {
        if (isPowerUpActive) return;

        isPowerUpActive = true;

        if (spellingCorrectionPowerUpEnabled)
        {
            Time.timeScale = 0.5f;
            StartCoroutine(PowerUpTimer());
        }
    }

    IEnumerator PowerUpTimer()
    {
        yield return new WaitForSecondsRealtime(powerUpDuration);
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

    public int GetCombo()
    {
        return combo;
    }
}