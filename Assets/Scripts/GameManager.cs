using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Word Spawning")]
    public Transform spawnPoint;
    public Transform[] spawnPoints;
    public Canvas gameCanvas;
    public float baseSpawnInterval = 2f;
    private float currentSpawnInterval;
    private bool isGameActive = true;
    private Coroutine spawningCoroutine;

    // These will be loaded from Resources
    private GameObject wordPrefab;
    private GameObject powerUpPrefab;

    [Header("Timers & Rounds")]
    public float roundDuration = 30f;
    private float roundTimer;
    public int currentRound = 1;
    public int maxRounds = 5;
    private bool isRoundActive = true;
    private bool isRoundReviewActive = false;

    [Header("Timer Bar")]
    public Image timerBarImage;
    public Gradient timerBarGradient;

    [Header("Round Review")]
    public GameObject roundReviewPanel;
    public TextMeshProUGUI roundReviewText;
    public TextMeshProUGUI nextRoundWordsText;
    public float roundReviewDuration = 3f;
    public Button continueButton;

    [Header("Difficulty Progression")]
    public AnimationCurve spawnSpeedCurve;
    public AnimationCurve fallSpeedCurve;
    public float baseFallSpeed = 100f;
    private float currentFallSpeed;

    [Header("Scoring")]
    private int currentScore = 0;
    private int highScore = 0;
    private int combo = 0;
    private float lastScoreTime = 0f;
    private float comboExpireTime = 0f;
    private const float COMBO_WINDOW = 3.5f;
    private Coroutine comboExpireCoroutine;

    [Header("Word Pool")]
    private List<WordData> allWords = new List<WordData>();
    private List<WordData> currentRoundWords = new List<WordData>();
    private List<WordData> upcomingWords = new List<WordData>();
    private int wordsPerRound = 15;

    [Header("Power-ups")]
    private bool isPowerUpActive = false;
    private float powerUpDuration = 3f;
    public float powerUpSpawnChance = 0.08f;

    [Header("Visual Feedback")]
    public GameObject scorePopupPrefab;

    private System.Random random = new System.Random();

    // Simplified word list
    private List<string> easyWords = new List<string>
    {
        "cat", "dog", "bird", "fish", "tree", "flower", "house", "car", "book", "pen",
        "apple", "banana", "cherry", "grape", "lemon", "orange", "peach", "pear", "berry", "melon",
        "red", "blue", "green", "yellow", "black", "white", "pink", "purple", "brown", "gray",
        "happy", "sad", "big", "small", "hot", "cold", "fast", "slow", "new", "old",
        "run", "walk", "jump", "sit", "stand", "sleep", "eat", "drink", "play", "work",
        "mother", "father", "sister", "brother", "friend", "teacher", "doctor", "nurse", "driver", "farmer"
    };

    public enum PowerUpType
    {
        ExtraPoints,
        TimeBonus,
        AutoSort,
        SlowTime
    }

    [System.Serializable]
    public class WordData
    {
        public string correctWord;
        public string misspelling;
        public int length;
        public float difficultyScore;
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        LoadHighScore();

        // Load prefabs from Resources
        LoadPrefabs();
    }

    void LoadPrefabs()
    {
        // Load WordPrefab from Resources folder
        wordPrefab = Resources.Load<GameObject>("FallingWord");
        if (wordPrefab == null)
        {
            Debug.LogError("WordPrefab not found in Resources folder! Please create a Resources folder and put your WordPrefab there.");
        }
        else
        {
            Debug.Log("WordPrefab loaded successfully from Resources");
        }

        // Load PowerUpPrefab from Resources folder (optional)
        powerUpPrefab = Resources.Load<GameObject>("PowerUp");
        if (powerUpPrefab == null)
        {
            Debug.Log("PowerUpPrefab not found in Resources folder - power-ups disabled");
        }
    }

    void Start()
    {
        LoadWordDatabase();
        SetupAnimationCurves();
        SetupTimerBar();
        StartRound();
        Time.timeScale = 1f;
    }

    void LoadWordDatabase()
    {
        allWords.Clear();

        foreach (string word in easyWords)
        {
            WordData data = new WordData();
            data.correctWord = word;
            data.misspelling = GenerateSimpleMisspelling(word);
            data.length = word.Length;
            data.difficultyScore = CalculateSimpleDifficulty(word);
            allWords.Add(data);
        }

        allWords = allWords.OrderBy(w => w.difficultyScore).ToList();

        Debug.Log($"Loaded {allWords.Count} easy words");
        PrepareWordPools();
    }

    float CalculateSimpleDifficulty(string word)
    {
        float score = 0f;
        score += word.Length * 0.1f;

        if (word.All(c => "aeiou".Contains(c))) score -= 0.3f;
        if (word.Length <= 4) score -= 0.2f;

        return Mathf.Clamp(score, 0.3f, 1.5f);
    }

    string GenerateSimpleMisspelling(string correctWord)
    {
        if (correctWord.Length < 3) return correctWord;

        string[] misspellings = new string[]
        {
            SwapOneLetter(correctWord),
            DoubleOneLetter(correctWord),
            ChangeVowel(correctWord)
        };

        return misspellings[Random.Range(0, misspellings.Length)];
    }

    string SwapOneLetter(string word)
    {
        int pos = Random.Range(0, word.Length - 1);
        char[] chars = word.ToCharArray();
        char temp = chars[pos];
        chars[pos] = chars[pos + 1];
        chars[pos + 1] = temp;
        return new string(chars);
    }

    string DoubleOneLetter(string word)
    {
        int pos = Random.Range(0, word.Length);
        return word.Insert(pos, word[pos].ToString());
    }

    string ChangeVowel(string word)
    {
        char[] vowels = { 'a', 'e', 'i', 'o', 'u' };
        char[] chars = word.ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (vowels.Contains(chars[i]))
            {
                char newVowel = vowels[Random.Range(0, vowels.Length)];
                chars[i] = newVowel;
                break;
            }
        }

        return new string(chars);
    }

    void SetupTimerBar()
    {
        if (timerBarImage != null)
        {
            if (timerBarGradient == null)
            {
                timerBarGradient = new Gradient();
                timerBarGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.green, 0f),
                        new GradientColorKey(Color.yellow, 0.5f),
                        new GradientColorKey(Color.red, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
            }

            timerBarImage.type = Image.Type.Filled;
            timerBarImage.fillMethod = Image.FillMethod.Horizontal;
            timerBarImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    void UpdateTimerBar()
    {
        if (timerBarImage != null)
        {
            float fillAmount = roundTimer / roundDuration;
            timerBarImage.fillAmount = fillAmount;
            timerBarImage.color = timerBarGradient.Evaluate(1f - fillAmount);
        }
    }

    void SetupAnimationCurves()
    {
        if (spawnSpeedCurve == null || spawnSpeedCurve.keys.Length == 0)
        {
            spawnSpeedCurve = new AnimationCurve();
            spawnSpeedCurve.AddKey(0f, 1f);
            spawnSpeedCurve.AddKey(0.25f, 0.9f);
            spawnSpeedCurve.AddKey(0.5f, 0.8f);
            spawnSpeedCurve.AddKey(0.75f, 0.7f);
            spawnSpeedCurve.AddKey(1f, 0.6f);
        }

        if (fallSpeedCurve == null || fallSpeedCurve.keys.Length == 0)
        {
            fallSpeedCurve = new AnimationCurve();
            fallSpeedCurve.AddKey(0f, 1f);
            fallSpeedCurve.AddKey(0.25f, 1.1f);
            fallSpeedCurve.AddKey(0.5f, 1.2f);
            fallSpeedCurve.AddKey(0.75f, 1.35f);
            fallSpeedCurve.AddKey(1f, 1.5f);
        }
    }

    void PrepareWordPools()
    {
        currentRoundWords.Clear();
        upcomingWords.Clear();

        float roundProgress = (float)(currentRound - 1) / (maxRounds - 1);
        float targetDifficulty = Mathf.Lerp(0.3f, 1.2f, roundProgress);

        var eligibleWords = allWords.Where(w =>
            Mathf.Abs(w.difficultyScore - targetDifficulty) < 0.5f).ToList();

        if (eligibleWords.Count < wordsPerRound)
            eligibleWords = allWords;

        eligibleWords = eligibleWords.OrderBy(x => random.Next()).ToList();

        for (int i = 0; i < Mathf.Min(wordsPerRound, eligibleWords.Count); i++)
        {
            currentRoundWords.Add(eligibleWords[i]);
        }

        float nextTargetDifficulty = Mathf.Lerp(0.3f, 1.4f, (float)currentRound / maxRounds);
        var nextEligible = allWords.Where(w =>
            Mathf.Abs(w.difficultyScore - nextTargetDifficulty) < 0.5f).ToList();

        nextEligible = nextEligible.OrderBy(x => random.Next()).ToList();
        for (int i = 0; i < 10 && i < nextEligible.Count; i++)
        {
            upcomingWords.Add(nextEligible[i]);
        }

        if (UIManager.Instance != null && currentRoundWords.Count > 0)
        {
            string newWordsList = string.Join(", ", currentRoundWords.Take(5).Select(w => w.correctWord));
            UIManager.Instance.ShowNewWordsAnnouncement(newWordsList);
        }
    }

    void StartRound()
    {
        Debug.Log($"Starting Round {currentRound}");

        // Verify prefab is loaded
        if (wordPrefab == null)
        {
            LoadPrefabs();
            if (wordPrefab == null)
            {
                Debug.LogError("Cannot start round - WordPrefab is missing!");
                return;
            }
        }

        isRoundActive = true;
        isRoundReviewActive = false;
        roundTimer = roundDuration;

        float roundProgress = (float)(currentRound - 1) / (maxRounds - 1);
        currentSpawnInterval = baseSpawnInterval * spawnSpeedCurve.Evaluate(roundProgress);
        currentFallSpeed = baseFallSpeed * fallSpeedCurve.Evaluate(roundProgress);

        UpdateTimerBar();

        // Stop any existing spawning coroutine
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
        }

        // Start new spawning coroutine
        spawningCoroutine = StartCoroutine(SpawnWords());

        if (UIManager.Instance != null)
            UIManager.Instance.ShowRoundStart(currentRound);

        Debug.Log($"Round {currentRound} started! Spawn interval: {currentSpawnInterval:F2}s");
    }

    void StopRound()
    {
        isRoundActive = false;

        // Stop spawning
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }

        // Destroy all active words
        Word[] activeWords = FindObjectsOfType<Word>();
        Debug.Log($"Destroying {activeWords.Length} active words");
        foreach (Word word in activeWords)
        {
            if (word != null && word.gameObject != null)
                Destroy(word.gameObject);
        }

        // Destroy all power-ups
        PowerUp[] powerUps = FindObjectsOfType<PowerUp>();
        foreach (PowerUp powerUp in powerUps)
        {
            if (powerUp != null && powerUp.gameObject != null)
                Destroy(powerUp.gameObject);
        }
    }

    void Update()
    {
        if (!isGameActive || !isRoundActive || isRoundReviewActive) return;

        roundTimer -= Time.deltaTime;
        UpdateTimerBar();

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateRoundTimer(roundTimer, currentRound);

        if (roundTimer <= 0)
        {
            EndRound();
        }
    }

    void EndRound()
    {
        Debug.Log($"Ending Round {currentRound}");
        StopRound();
        StartCoroutine(RoundReview());
    }

    IEnumerator RoundReview()
    {
        isRoundReviewActive = true;

        if (roundReviewPanel != null)
        {
            roundReviewPanel.SetActive(true);
            if (roundReviewText != null)
            {
                roundReviewText.text = $"ROUND {currentRound} COMPLETE!\n\nScore: {currentScore}";
            }

            if (nextRoundWordsText != null && upcomingWords.Count > 0)
            {
                string nextWords = string.Join(", ", upcomingWords.Take(5).Select(w => w.correctWord));
                string nextMessage = (currentRound < maxRounds) ?
                    $"Next round words:\n{nextWords}\n\nClick Continue for Round {currentRound + 1}!" :
                    "FINAL ROUND COMPLETE!\nClick Continue for Final Score!";
                nextRoundWordsText.text = nextMessage;
            }
        }

        // Wait for continue button or auto-continue
        float autoTime = 0f;
        bool continuePressed = false;

        // Set up button listener if button exists
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => { continuePressed = true; });
        }

        while (autoTime < roundReviewDuration && !continuePressed)
        {
            autoTime += Time.deltaTime;
            yield return null;
        }

        ContinueToNextRound();
    }

    public void ContinueToNextRound()
    {
        Debug.Log("ContinueToNextRound called");

        if (roundReviewPanel != null)
            roundReviewPanel.SetActive(false);

        if (currentRound < maxRounds)
        {
            currentRound++;
            PrepareWordPools();
            StartRound();
        }
        else
        {
            EndGame();
        }

        isRoundReviewActive = false;
    }

    IEnumerator SpawnWords()
    {
        Debug.Log("SpawnWords coroutine started");

        while (isGameActive && isRoundActive && !isRoundReviewActive)
        {
            float actualSpawnInterval = currentSpawnInterval * Random.Range(0.8f, 1.2f);
            yield return new WaitForSeconds(actualSpawnInterval);

            if (isGameActive && isRoundActive && !isRoundReviewActive)
            {
                SpawnRandomWord();

                if (powerUpPrefab != null && Random.value < powerUpSpawnChance)
                {
                    SpawnPowerUp();
                }
            }
        }

        Debug.Log("SpawnWords coroutine ended");
    }

    void SpawnRandomWord()
    {
        // Verify prefab is loaded
        if (wordPrefab == null)
        {
            LoadPrefabs();
            if (wordPrefab == null)
            {
                Debug.LogError("WordPrefab is missing! Make sure it's in a Resources folder.");
                return;
            }
        }

        if (currentRoundWords.Count == 0)
        {
            Debug.LogWarning("No words available for current round!");
            return;
        }

        WordData data = currentRoundWords[Random.Range(0, currentRoundWords.Count)];
        if (data == null) return;

        bool useCorrect = Random.value > 0.5f;
        string wordToShow = useCorrect ? data.correctWord : data.misspelling;

        Transform selectedSpawn = spawnPoint;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            selectedSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        GameObject newWord = Instantiate(wordPrefab, gameCanvas.transform);

        RectTransform rect = newWord.GetComponent<RectTransform>();
        if (rect != null && selectedSpawn != null)
        {
            RectTransform spawnRect = selectedSpawn.GetComponent<RectTransform>();
            rect.anchoredPosition = spawnRect != null ? spawnRect.anchoredPosition : new Vector2(0, 400);
        }

        Word wordScript = newWord.GetComponent<Word>();
        if (wordScript != null)
        {
            wordScript.Initialize(wordToShow, useCorrect, useCorrect ? "" : data.correctWord);
        }

        StartCoroutine(FallWord(newWord));
    }

    void SpawnPowerUp()
    {
        if (powerUpPrefab == null) return;

        PowerUpType[] types = { PowerUpType.ExtraPoints, PowerUpType.TimeBonus, PowerUpType.AutoSort, PowerUpType.SlowTime };
        PowerUpType type = types[Random.Range(0, types.Length)];

        GameObject powerUp = Instantiate(powerUpPrefab, gameCanvas.transform);

        Transform selectedSpawn = spawnPoint;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            selectedSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        RectTransform rect = powerUp.GetComponent<RectTransform>();
        if (rect != null && selectedSpawn != null)
        {
            RectTransform spawnRect = selectedSpawn.GetComponent<RectTransform>();
            rect.anchoredPosition = spawnRect != null ? spawnRect.anchoredPosition : new Vector2(0, 400);
        }

        PowerUp powerUpScript = powerUp.GetComponent<PowerUp>();
        if (powerUpScript != null)
        {
            powerUpScript.Initialize(type);
        }

        StartCoroutine(FallPowerUp(powerUp));
    }

    IEnumerator FallPowerUp(GameObject powerUp)
    {
        if (powerUp == null) yield break;

        RectTransform rect = powerUp.GetComponent<RectTransform>();
        float fallSpeed = 80f;
        float bottomY = -980f;

        while (powerUp != null && rect.anchoredPosition.y > bottomY)
        {
            if (isGameActive && isRoundActive && !isRoundReviewActive)
            {
                Vector2 newPos = rect.anchoredPosition;
                newPos.y -= fallSpeed * Time.deltaTime;
                rect.anchoredPosition = newPos;
            }
            yield return null;
        }

        if (powerUp != null)
            Destroy(powerUp);
    }

    public void ActivatePowerUp(PowerUpType type)
    {
        if (isPowerUpActive) return;

        isPowerUpActive = true;

        switch (type)
        {
            case PowerUpType.ExtraPoints:
                AddPoints(50);
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowPowerUpMessage("+50 POINTS!");
                break;
            case PowerUpType.TimeBonus:
                roundTimer += 5f;
                UpdateTimerBar();
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowPowerUpMessage("+5 SECONDS!");
                break;
            case PowerUpType.AutoSort:
                StartCoroutine(AutoSortPowerUp());
                break;
            case PowerUpType.SlowTime:
                StartCoroutine(SlowTimePowerUp());
                break;
        }

        StartCoroutine(PowerUpCooldown());
    }

    IEnumerator AutoSortPowerUp()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPowerUpMessage("AUTO SORT!");

        Word[] words = FindObjectsOfType<Word>();
        foreach (Word word in words)
        {
            Bucket[] buckets = FindObjectsOfType<Bucket>();
            foreach (Bucket bucket in buckets)
            {
                if (bucket.isCorrectBucket == word.isCorrectSpelling)
                {
                    word.OnSortedIntoBucket(bucket.isCorrectBucket);
                    break;
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator SlowTimePowerUp()
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.5f;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPowerUpMessage("TIME SLOWED!");

        yield return new WaitForSecondsRealtime(4f);

        Time.timeScale = originalTimeScale;
    }

    IEnumerator PowerUpCooldown()
    {
        yield return new WaitForSeconds(powerUpDuration);
        isPowerUpActive = false;
    }

    IEnumerator FallWord(GameObject word)
    {
        if (word == null) yield break;

        RectTransform rect = word.GetComponent<RectTransform>();
        float bottomY = -980f;

        while (word != null && rect.anchoredPosition.y > bottomY)
        {
            if (isGameActive && isRoundActive && !isRoundReviewActive)
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
                UIManager.Instance.ShowComboBonus(comboBonus);
        }

        if (currentScore > highScore)
        {
            highScore = currentScore;
            SaveHighScore();
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateHighScore(highScore);
        }
    }

    void SaveHighScore()
    {
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    }

    void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHighScore(highScore);
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
            ResetCombo();
    }

    void ResetCombo()
    {
        combo = 0;
        if (comboExpireCoroutine != null)
            StopCoroutine(comboExpireCoroutine);

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
                popupScript.Initialize(points);
            Destroy(popup, 0.8f);
        }
    }

    void EndGame()
    {
        isGameActive = false;
        isRoundActive = false;
        StopRound();
        Time.timeScale = 0f;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver(currentScore, highScore);

        Debug.Log($"Game Over! Score: {currentScore}, High Score: {highScore}");
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