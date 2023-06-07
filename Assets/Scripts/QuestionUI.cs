using DG.Tweening;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Question {
    public string question;
    public string wordType;
    public string answer;
    public int pointValue;
    public float timeGiven;
}

public class Questions {
    public List<Question> questions;
}

public class QuestionUI : MonoBehaviour
{
    [Header("Text Objects")]
    public TextMeshProUGUI explanationTMP;
    public TextMeshProUGUI currentPointsTMP;
    public TextMeshProUGUI remainingTimeTMP;
    public TextMeshProUGUI wordTypeTMP;
    public TextMeshProUGUI letterCountTMP;
    public TextMeshProUGUI lostPointsTMP;
    public TextMeshProUGUI answerDisplayTMP;

    [Header("Text Input Field")]
    public TMP_InputField answerInputField;

    [Header("Get Initial Letter Button Components")]
    public TextMeshProUGUI getInitialLetterButtonTMP;
    public string getInitialLetterButtonDefaultText = "Get <color=#FF0000>initial</color> letter";
    public string getInitialLetterButtonActivatedText = "<color=#FF0000>Got initial letter!</color>";

    [Header("Get Random Letter Button Components")]
    public TextMeshProUGUI getRandomLetterButtonTMP;
    public string getrandomLetterButtonDefaultText = "Get <color=#0000FF>random</color> letter";
    public string getrandomLetterButtonUnusableText = "<color=#0000FF>Got all letters!</color>";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource endingMusicAudioSource;
    public AudioClip questionStartSFX;
    public AudioClip correctAnswerSFX;
    public AudioClip failureSFX;
    public AudioClip wrongAnswerSFX;
    public AudioClip lowOnTimeSFX;
    public AudioClip letterAppearSFX;

    [Header("Background Image Components")]
    public GameObject backgroundObj;
    public Image backgroundImage;

    [Header("Question End Screen Components")]
    public Image qEndScreenResultImage;
    public TextMeshProUGUI qEndScreenEndResultTMP;
    public TextMeshProUGUI qEndScreenAnswerDisplayTMP;
    public TextMeshProUGUI qEndScreenAcquiredPointsDisplayTMP;
    public Sprite correctAnswerSprite;
    public Sprite failureSprite;

    [Header("RT Objects")]
    public RectTransform lostPointsRT;
    public RectTransform questionEndScreenRT;
    public RectTransform gameEndScreenRT;

    [Header("Variables")]
    public bool isQuestionActive = false;
    public int currentPoints = 10;
    public float remainingTime = 90;
    public bool hasGottenInitialLetter = false;
    public bool hasGottenAllRandomLetters = false;

    private int initialLetterPointsCost = 2;
    private int randomLetterPointsCost = 1;

    // Lost points text tween values
    private Tweener lostPointsTweener;
    private Tweener lostPointsFadeTweener;
    private Vector3 lostPointsTMPDefPos = new Vector3(-175, -100);
    private Vector3 lostPointsTMPTargetPos = new Vector3(-175, -200);
    private float lostPointsTweenDuration = 2f;

    // Question appear tween
    private Tweener explanationTween;
    private Tweener answerDisplayTween;
    private Tweener wordTypeTween;
    private Tweener letterCountTween;
    private float explanationAppearTweenDuration = 2f;
    private float wordTypeAppearTweenDuration = 1f;
    private float letterCountAppearTweenDuration = 1f;
    private float answerDisplayAppearDuration = 2f;

    // Question end screen tweens
    private bool showingQuestionEndScreen = false;
    private Tweener questionEndScreenTween;
    private float questionEndScreenAppearDisappearDuration = 0.5f;
    private Vector3 questionEndScreenHiddenPos = new Vector3(1500, 0);
    private Vector3 questionEndScreenAppearedPos = new Vector3(0, 0);

    // Game end screen tweens
    private bool showingGameEndScreen = false;
    private Tweener gameEndScreenTween;
    private float gameEndScreenAppearDisappearDuration = 0.5f;
    private Vector3 gameEndScreenHiddenPos = new Vector3(-1500, 0);
    private Vector3 gameEndScreenAppearedPos = new Vector3(0, 0);

    // Background dim tween
    private Tweener backgroundDimTween;
    private float backgroundDimDuration = 0.5f;

    // Wrong answer colour tween
    private Tweener wrongAnswerTween;
    private float wrongAnswerTweenDuration = 0.75f;

    // Private low on time values
    private float lowOnTimeRemainingDuration = 9f;
    private bool playedLowOnTimeSFX = false;

    // Current question values
    private string currentCorrectAnswer;
    private List<int> currentlyHiddenTextIndexes = new();

    // ALL question data
    Questions questions;
    private int currentQuestionIndex = 0;

    private void Start() {
        // Try to load the JSON. If it fails, create a question in the form of an error.
        string JSONDirectory = Application.streamingAssetsPath + "/questions.json";
        if (File.Exists(JSONDirectory)) {
            string JSONData = File.ReadAllText(JSONDirectory);
            questions = JsonConvert.DeserializeObject<Questions>(JSONData);
        }
        else {
            // Horrible hack but it's 4am and I am sleepy asf and can't be bothered
            string fakeJSON = "{\r\n    \"questions\": [\r\n        {\r\n            \"question\": \"The JSON path was not found within the instructed directory, or there was an error. The answer to this question is \\\"fruitninja\\\".\",\r\n            \"wordType\": \"Error\",\r\n            \"answer\": \"fruitninja\",\r\n            \"pointValue\": 404,\r\n            \"timeGiven\": 99999\r\n        }\r\n    ]\r\n}";
            questions = JsonConvert.DeserializeObject<Questions>(fakeJSON);
        }

        StartGame();
    }

    public void StartGame() {
        endingMusicAudioSource.Stop();

        if (showingGameEndScreen) {
            GameEndScreenDisappear();
        }

        currentQuestionIndex = 0;
        InitializeQuestion(questions.questions[currentQuestionIndex]);
    }

    public void ExitGame() {
        Application.Quit(1);
    }

    public void GoToGithub() {
        Application.OpenURL("https://github.com/emredesu");
    }

    private void Update() {
        if (isQuestionActive) {
            remainingTime -= Time.deltaTime;
            remainingTimeTMP.SetText(remainingTime.ToString("F1"));

            if (remainingTime < lowOnTimeRemainingDuration && !playedLowOnTimeSFX) {
                audioSource.PlayOneShot(lowOnTimeSFX);
                playedLowOnTimeSFX = true;
            }

            if (remainingTime < 0) {
                TriggerEndScreenForTimeout();
            }
        }
    }


    public void InitializeQuestion(Question question) {
        string questionText = question.question;
        string wordType = question.wordType;
        string correctAnswer = question.answer;
        int pointValue = question.pointValue;
        float givenTime = question.timeGiven;

        // Reset values
        currentPoints = pointValue;
        remainingTime = givenTime;
        hasGottenInitialLetter = false;
        hasGottenAllRandomLetters = false;
        playedLowOnTimeSFX = false;

        // Reset button texts
        getRandomLetterButtonTMP.SetText(getrandomLetterButtonDefaultText);
        getInitialLetterButtonTMP.SetText(getInitialLetterButtonDefaultText);

        // Create the "hidden text indexes" list
        currentlyHiddenTextIndexes.Clear();
        for (int i = 0; i < correctAnswer.Length; i++) {
            currentlyHiddenTextIndexes.Add(i);
        }

        // Set values
        currentCorrectAnswer = correctAnswer;
        currentPointsTMP.SetText(currentPoints.ToString());

        // Get rid of tweens in case they are active
        KillTween(lostPointsTweener);
        KillTween(lostPointsFadeTweener);
        lostPointsRT.gameObject.SetActive(false);

        // Start tweens
        KillTween(explanationTween);
        explanationTMP.SetText("");
        explanationTween = explanationTMP.DOText($"{currentQuestionIndex + 1} - " + questionText, explanationAppearTweenDuration).SetEase(Ease.Linear);

        KillTween(wordTypeTween);
        wordTypeTMP.SetText("");
        wordTypeTween = wordTypeTMP.DOText(wordType, wordTypeAppearTweenDuration).SetEase(Ease.Linear);

        string letterCountTarget = $"{correctAnswer.Length} letters";
        KillTween(letterCountTween);
        letterCountTMP.SetText("");
        letterCountTween = letterCountTMP.DOText(letterCountTarget, letterCountAppearTweenDuration).SetEase(Ease.Linear);

        string answerDisplayTarget = CreateUnderlinedStr(correctAnswer);
        KillTween(answerDisplayTween);
        answerDisplayTMP.SetText("");
        answerDisplayTween = answerDisplayTMP.DOText(answerDisplayTarget, answerDisplayAppearDuration).SetEase(Ease.Linear).OnComplete(() => StartQuestion()); // This tween starts the question (timer)
    }

    void StartQuestion() {
        isQuestionActive = true;
        audioSource.PlayOneShot(questionStartSFX);
    }

    void InitializeLostPointsTween(int lostPoints) {
        // Kill tweens in case they are active
        KillTween(lostPointsTweener);
        KillTween(lostPointsFadeTweener);

        lostPointsRT.anchoredPosition = lostPointsTMPDefPos; // Restore pos
        lostPointsTMP.color = new Color(lostPointsTMP.color.r, lostPointsTMP.color.g, lostPointsTMP.color.b, 1); // Restore alpha
        lostPointsTMP.SetText("-" + lostPoints); // Set text

        lostPointsRT.gameObject.SetActive(true);

        // Start tweens
        lostPointsTweener = lostPointsRT.DOAnchorPos(lostPointsTMPTargetPos, lostPointsTweenDuration);
        lostPointsFadeTweener = lostPointsTMP.DOFade(0, lostPointsTweenDuration).OnComplete(() => lostPointsRT.gameObject.SetActive(false));
    }

    void ReducePoints(int lostPoints) {
        // Prevent points from going into negatives.
        if (currentPoints - lostPoints < 0) {
            return;
        }

        currentPoints -= lostPoints;
        currentPointsTMP.SetText(currentPoints.ToString());

        InitializeLostPointsTween(lostPoints);
    }

    public void OnGetInitialLetter() {
        if (hasGottenInitialLetter || !isQuestionActive)
            return;

        ReducePoints(initialLetterPointsCost);
        hasGottenInitialLetter = true;

        getInitialLetterButtonTMP.SetText(getInitialLetterButtonActivatedText);

        GetLetterForIndex(0);
    }

    public void OnGetRandomLetter() {
        if (hasGottenAllRandomLetters || !isQuestionActive)
            return;

        ReducePoints(randomLetterPointsCost);

        int randomIndex = hasGottenInitialLetter ? currentlyHiddenTextIndexes[Random.Range(0, currentlyHiddenTextIndexes.Count)] : currentlyHiddenTextIndexes[Random.Range(1, currentlyHiddenTextIndexes.Count)]; // Have we gotten the initial letter? If so, do not include the 0th index in the random list - otherwise do include it.

        GetLetterForIndex(randomIndex);
    }

    void PlayWrongAnswerTween() {
        KillTween(wrongAnswerTween);

        SetTextColours(Color.white);

        wrongAnswerTween = DOVirtual.Color(Color.white, Color.red, wrongAnswerTweenDuration, SetTextColours).OnComplete(() => wrongAnswerTween = DOVirtual.Color(Color.red, Color.white, wrongAnswerTweenDuration, SetTextColours));
    }

    void SetTextColours(Color color) {
        wordTypeTMP.color = color;
        explanationTMP.color = color;
        answerDisplayTMP.color = color;
    }

    void DimBackground() {
        KillTween(backgroundDimTween);
        backgroundDimTween = backgroundImage.DOFade(0.5f, backgroundDimDuration);
    }

    void NoBackground() {
        KillTween(backgroundDimTween);
        backgroundDimTween = backgroundImage.DOFade(0, backgroundDimDuration);
    }

    void QuestionEndScreenAppear() {
        showingQuestionEndScreen = true;
        isQuestionActive = false;

        DimBackground();

        KillTween(questionEndScreenTween);
        questionEndScreenTween = questionEndScreenRT.DOAnchorPos(questionEndScreenAppearedPos, questionEndScreenAppearDisappearDuration);
    }

    void QuestionEndScreenDisappear() {
        showingQuestionEndScreen = false;
        NoBackground();

        KillTween(questionEndScreenTween);
        questionEndScreenTween = questionEndScreenRT.DOAnchorPos(questionEndScreenHiddenPos, questionEndScreenAppearDisappearDuration);
    }

    void GameEndScreenAppear() {
        if (showingQuestionEndScreen) {
            QuestionEndScreenDisappear();
        }

        endingMusicAudioSource.Play();

        isQuestionActive = false;
        showingGameEndScreen = true;

        DimBackground();

        KillTween(gameEndScreenTween);
        gameEndScreenTween = gameEndScreenRT.DOAnchorPos(gameEndScreenAppearedPos, gameEndScreenAppearDisappearDuration);
    }

    void GameEndScreenDisappear() {
        showingGameEndScreen = false;

        NoBackground();

        KillTween(gameEndScreenTween);
        gameEndScreenTween = gameEndScreenRT.DOAnchorPos(gameEndScreenHiddenPos, gameEndScreenAppearDisappearDuration);
    }

    void TriggerEndScreenForTimeout() {
        // Lose all points.
        ReducePoints(currentPoints);

        qEndScreenEndResultTMP.color = Color.red;
        qEndScreenEndResultTMP.SetText("Ran out of time!");
        qEndScreenAnswerDisplayTMP.SetText($"The answer was: <color=#00FF00>{currentCorrectAnswer}</color>");
        qEndScreenAcquiredPointsDisplayTMP.SetText($"Acquired points: <color=#00FF00>{currentPoints}</color>");

        qEndScreenResultImage.sprite = failureSprite;

        QuestionEndScreenAppear();

        audioSource.PlayOneShot(failureSFX);
    }

    void TriggerEndScreenForCorrectAnswer() {
        qEndScreenEndResultTMP.color = Color.green;
        qEndScreenEndResultTMP.SetText("Correct answer!");
        qEndScreenAnswerDisplayTMP.SetText($"The answer was: <color=#00FF00>{currentCorrectAnswer}</color>");
        qEndScreenAcquiredPointsDisplayTMP.SetText($"Acquired points: <color=#00FF00>{currentPoints}</color>");

        qEndScreenResultImage.sprite = correctAnswerSprite;

        QuestionEndScreenAppear();

        audioSource.PlayOneShot(correctAnswerSFX);
    }

    public void OnSubmit() {
        if (answerInputField.text.ToLower() == currentCorrectAnswer.ToLower()) {
            TriggerEndScreenForCorrectAnswer();
        }
        else {
            audioSource.PlayOneShot(wrongAnswerSFX);
            PlayWrongAnswerTween();
        }
    }

    void GetLetterForIndex(int index) {
        int indexInDisplayText = (index * 2);

        char[] currentTextArray = answerDisplayTMP.text.ToCharArray();

        currentTextArray[indexInDisplayText] = currentCorrectAnswer[index];

        answerDisplayTMP.SetText(new string(currentTextArray));

        currentlyHiddenTextIndexes.Remove(index);

        // Check if we used up all the random letters
        if ((!hasGottenInitialLetter && currentlyHiddenTextIndexes.Count < 2) || (hasGottenInitialLetter && currentlyHiddenTextIndexes.Count < 1)) {
            getRandomLetterButtonTMP.SetText(getrandomLetterButtonUnusableText);
            hasGottenAllRandomLetters = true;
        }

        PlayLetterAppearSFX();
    }

    void PlayLetterAppearSFX() {
        audioSource.PlayOneShot(letterAppearSFX);
    }

    public void GoToNextQuestion() {
        if (showingQuestionEndScreen) {
            QuestionEndScreenDisappear();
        }

        if (currentQuestionIndex == questions.questions.Count - 1) {
            GameEndScreenAppear();
            return;
        }

        currentQuestionIndex++;
        InitializeQuestion(questions.questions[currentQuestionIndex]);
    }

    public void GoToPreviousQuestion() {
        if (showingQuestionEndScreen) {
            QuestionEndScreenDisappear();
        }

        if (currentQuestionIndex == 0) {
            return;
        }

        currentQuestionIndex--;
        InitializeQuestion(questions.questions[currentQuestionIndex]);
    }

    static void KillTween(Tweener tweener) {
        if (tweener != null) {
            tweener.Kill();
        }
    }

    static string CreateUnderlinedStr(string answerStr) {
        string targetStr = "";

        for (int i = 0; i < answerStr.Length; i++) {
            targetStr += "_ ";
        }

        targetStr.Remove(targetStr.Length - 1, 1); // Remove last space

        return targetStr;
    }
}
