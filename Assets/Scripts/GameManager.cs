using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Word Spawning")]
    public GameObject wordPrefab;
    public Transform spawnPoint;
    public float spawnInterval = 2f;
    public float wordFallSpeed = 100f;

    [Header("Timers")]
    public float gameDuration = 60f;
    private float gameTimer;
    private bool isGameActive = true;

    [Header("Spelling Bonus")]
    private Word currentWordForCorrection;
    public GameObject spellingPopup;
    public TMPro.TMP_InputField spellingInput;
    public float bonusTimerDuration = 5f;
    private bool isBonusActive = false;

    [Header("Word Database")]
    private List<WordData> wordDatabase = new List<WordData>();

    [Header("Canvas")]
    public Canvas gameCanvas;

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

        // Find the Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene! Creating one...");
            canvas = CreateCanvas();
        }

        // Set spawn point as child of Canvas
        if (spawnPoint != null && canvas != null)
        {
            spawnPoint.SetParent(canvas.transform);
            Debug.Log($"SpawnPoint parent set to Canvas: {spawnPoint.parent.name}");
        }

        StartCoroutine(SpawnWords());

        if (spellingPopup != null)
            spellingPopup.SetActive(false);
    }

    Canvas CreateCanvas()
    {
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return canvas;
    }

    void Update()
    {
        if (!isGameActive || isBonusActive) return;

        gameTimer -= Time.deltaTime;
        UIManager.Instance.UpdateTimer(gameTimer);

        if (gameTimer <= 0)
        {
            EndGame();
        }
    }

    void LoadWordDatabase()
    {
        // Add your word list here
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
        // Add more words...
    }

    IEnumerator SpawnWords()
    {
        while (isGameActive && !isBonusActive)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (!isBonusActive && isGameActive)
            {
                SpawnRandomWord();
            }
        }
    }

    void SpawnRandomWord()
    {
        if (wordPrefab == null)
        {
            Debug.LogError("❌ wordPrefab is NOT assigned!");
            return;
        }

        if (wordDatabase == null || wordDatabase.Count == 0)
        {
            Debug.LogError("❌ wordDatabase is empty!");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("❌ spawnPoint is NOT assigned!");
            return;
        }

        WordData data = wordDatabase[Random.Range(0, wordDatabase.Count)];
        bool useCorrect = Random.value > 0.5f;
        string wordToShow = useCorrect ? data.correctWord : data.misspelling;

        Debug.Log($"1. About to instantiate word: {wordToShow}");

        GameObject newWord = Instantiate(wordPrefab, spawnPoint.position, Quaternion.identity);
        newWord.transform.SetParent(gameCanvas.transform, false); // false keeps world position

        Debug.Log($"2. Instantiated: {newWord.name}");

        Word wordScript = newWord.GetComponent<Word>();

        Debug.Log($"3. Got Word component: {wordScript != null}");

        if (wordScript == null)
        {
            Debug.LogError("4. Word component is STILL null!");
            return;
        }

        Debug.Log("5. About to call Initialize...");

        wordScript.Initialize(
            wordToShow,
            data.definition,
            useCorrect,
            useCorrect ? "" : data.correctWord
        );

        Debug.Log("6. Initialize completed, about to start FallWord coroutine");

        // Add falling movement
        StartCoroutine(FallWord(newWord));

        Debug.Log("7. SpawnRandomWord completed successfully");
    }

    IEnumerator FallWord(GameObject word)
    {
        if (word == null) yield break;

        RectTransform rect = word.GetComponent<RectTransform>();
        if (rect == null) yield break;

        float fallSpeed = 150f; // Pixels per second
        float bottomY = -950f; // Bottom of screen

        // Continue falling as long as word exists and hasn't reached bottom
        while (word != null && rect.anchoredPosition.y > bottomY)
        {
            // Only fall if game is active and not in bonus mode
            if (isGameActive && !isBonusActive)
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
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(-5);
            Debug.Log($"Word fell to bottom! -5 points");
            Destroy(word);
        }
    }
    public void StartSpellingCorrection(Word word)
    {
        if (isBonusActive) return;

        Debug.Log($"Starting spelling correction for word: {word.wordText}");

        currentWordForCorrection = word;
        isBonusActive = true;
        isGameActive = false;

        Time.timeScale = 0f; // Pause game

        if (spellingPopup != null)
        {
            spellingPopup.SetActive(true);
            if (spellingInput != null)
            {
                spellingInput.text = "";
                spellingInput.Select();
            }
            Debug.Log("Spelling popup activated");
        }
        else
        {
            Debug.LogError("Spelling popup is null! Assign it in the Inspector");
            EndSpellingCorrection(false);
            return;
        }

        StartCoroutine(BonusTimerRoutine());
    }
    IEnumerator BonusTimerRoutine()
    {
        float timer = bonusTimerDuration;
        while (timer > 0)
        {
            UIManager.Instance.UpdateBonusTimer(timer);
            yield return new WaitForEndOfFrame();
            timer -= Time.unscaledDeltaTime;
        }

        // Time's up
        EndSpellingCorrection(false);
    }

    public void SubmitSpellingCorrection()
    {
        string submitted = spellingInput.text.Trim().ToLower();
        bool isCorrect = (submitted == currentWordForCorrection.correctSpelling);

        EndSpellingCorrection(isCorrect);
    }

    void EndSpellingCorrection(bool wasCorrect)
    {
        StopAllCoroutines();

        if (wasCorrect)
        {
            ScoreManager.Instance.AddPoints(20);
            Debug.Log("Bonus! +20 points");
        }

        // Resume game
        Time.timeScale = 1f;
        spellingPopup.SetActive(false);
        Destroy(currentWordForCorrection.gameObject);
        currentWordForCorrection = null;

        isBonusActive = false;
        isGameActive = true;

        // Restart spawning coroutine if needed
        StartCoroutine(SpawnWords());
    }

    void EndGame()
    {
        isGameActive = false;
        Time.timeScale = 0f;
        UIManager.Instance.ShowGameOver();
    }
}