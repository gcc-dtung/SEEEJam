using System.Collections;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class TutorialSequenceUI : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private Image tutorialImage;
    [SerializeField] private Button delayedTutorialButton;
    [SerializeField] private Button instantTutorialButton;
    [SerializeField] private Button advanceButton;

    [Header("Slides")]
    [SerializeField] private Sprite[] tutorialSlides;

    [Header("Pop Settings")]
    [SerializeField, Min(0.05f)] private float popDuration = 0.28f;
    [SerializeField, Min(0f)] private float hiddenScale = 0.18f;
    [SerializeField, Min(0f)] private float tutorialStartDelay = 4f;

    private int _currentIndex = -1;
    private Tween _rootTween;
    private Tween _imageTween;
    private Coroutine _tutorialDelayRoutine;
    private bool _autoStartTriggered;

    private void Awake()
    {
        ResolveReferences();
        BindButtonClick();
        HideImmediate();
    }

    private void Start()
    {
        if (_autoStartTriggered)
            return;

        if (LevelManager.TryGetInstance(out LevelManager levelManager) && levelManager.CurrentLevelIndex == 0)
        {
            _autoStartTriggered = true;
            StartTutorialDelayed();
        }
    }

    private void OnEnable()
    {
        BindButtonClick();
    }

    private void OnDisable()
    {
        if (delayedTutorialButton != null)
            delayedTutorialButton.onClick.RemoveListener(StartTutorialDelayed);

        if (instantTutorialButton != null)
            instantTutorialButton.onClick.RemoveListener(StartTutorial);

        if (advanceButton != null)
            advanceButton.onClick.RemoveListener(AdvanceSequence);

        if (_tutorialDelayRoutine != null)
        {
            StopCoroutine(_tutorialDelayRoutine);
            _tutorialDelayRoutine = null;
        }

        StopTweens();
    }

    public void StartTutorial()
    {
        ResolveReferences();

        if (tutorialSlides == null || tutorialSlides.Length == 0)
        {
            Debug.LogWarning("[TutorialSequenceUI] No tutorial slides assigned. Please fill the Slides array.");
            return;
        }

        if (tutorialImage == null)
        {
            Debug.LogWarning("[TutorialSequenceUI] Tutorial image is not assigned. Please assign the image in the inspector.");
            return;
        }

        if (tutorialRoot != null)
            tutorialRoot.SetActive(true);

        _currentIndex = -1;
        ShowRoot();
        AdvanceSequence();
    }

    public void StartTutorialDelayed()
    {
        ResolveReferences();

        if (_tutorialDelayRoutine != null)
            return;

        _tutorialDelayRoutine = StartCoroutine(DelayedStartTutorial());
    }

    private IEnumerator DelayedStartTutorial()
    {
        yield return new WaitForSeconds(tutorialStartDelay);
        StartTutorial();
        _tutorialDelayRoutine = null;
    }

    public void HideImmediate()
    {
        StopTweens();

        if (tutorialRoot != null && tutorialRoot != delayedTutorialButton?.gameObject && tutorialRoot != instantTutorialButton?.gameObject)
            tutorialRoot.SetActive(false);

        if (advanceButton != null)
            advanceButton.gameObject.SetActive(false);

        if (tutorialImage != null)
            tutorialImage.sprite = null;
    }

    public void AdvanceSequence()
    {
        if (tutorialSlides == null || tutorialSlides.Length == 0)
            return;

        if (_currentIndex < 0)
        {
            _currentIndex = 0;
            ShowCurrentSlide();
            return;
        }

        if (_currentIndex >= tutorialSlides.Length - 1)
        {
            HideImmediate();
            return;
        }

        _currentIndex += 1;
        ShowCurrentSlide();
    }

    private void ShowRoot()
    {
        if (tutorialRoot == null)
            return;

        tutorialRoot.SetActive(true);
        tutorialRoot.transform.localScale = Vector3.one * hiddenScale;
        _rootTween = Tween.Scale(tutorialRoot.transform, Vector3.one, popDuration);

        if (advanceButton != null)
        {
            advanceButton.gameObject.SetActive(true);
            advanceButton.transform.SetAsLastSibling();
        }
    }

    private void ShowCurrentSlide()
    {
        if (tutorialImage == null)
            return;

        tutorialImage.gameObject.SetActive(true);
        tutorialImage.sprite = tutorialSlides[_currentIndex];
        tutorialImage.rectTransform.localScale = Vector3.one * hiddenScale;
        _imageTween = Tween.Scale(tutorialImage.rectTransform, Vector3.one, popDuration);
    }

    private void StopTweens()
    {
        if (_rootTween.isAlive)
            _rootTween.Stop();

        if (_imageTween.isAlive)
            _imageTween.Stop();
    }

    private void ResolveReferences()
    {
        if (delayedTutorialButton == null)
            delayedTutorialButton = GetComponent<Button>();

        if (delayedTutorialButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            if (buttons.Length > 0)
                delayedTutorialButton = buttons[0];
        }

        if (instantTutorialButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            if (buttons.Length > 1)
                instantTutorialButton = buttons[1];
        }

        if (tutorialRoot == null)
        {
            if (delayedTutorialButton != null)
                tutorialRoot = delayedTutorialButton.transform.parent != null ? delayedTutorialButton.transform.parent.gameObject : gameObject;
            else if (instantTutorialButton != null)
                tutorialRoot = instantTutorialButton.transform.parent != null ? instantTutorialButton.transform.parent.gameObject : gameObject;
            else
                tutorialRoot = gameObject;
        }

        if (tutorialImage == null)
        {
            if (tutorialRoot != null)
            {
                Image[] images = tutorialRoot.GetComponentsInChildren<Image>(true);
                foreach (Image image in images)
                {
                    if (image != null && image.gameObject != delayedTutorialButton?.gameObject && image.gameObject != instantTutorialButton?.gameObject)
                    {
                        tutorialImage = image;
                        break;
                    }
                }
            }

            if (tutorialImage == null)
            {
                if (delayedTutorialButton != null)
                    tutorialImage = delayedTutorialButton.GetComponent<Image>();

                if (tutorialImage == null && instantTutorialButton != null)
                    tutorialImage = instantTutorialButton.GetComponent<Image>();
            }
        }
    }

    private void BindButtonClick()
    {
        if (delayedTutorialButton == null && instantTutorialButton == null)
        {
            Debug.LogWarning("[TutorialSequenceUI] No tutorial buttons were found. Drag both buttons into the fields or attach this script to the tutorial button object.");
            return;
        }

        if (delayedTutorialButton != null)
        {
            delayedTutorialButton.onClick.RemoveListener(StartTutorialDelayed);
            delayedTutorialButton.onClick.AddListener(StartTutorialDelayed);
        }

        if (instantTutorialButton != null)
        {
            instantTutorialButton.onClick.RemoveListener(StartTutorial);
            instantTutorialButton.onClick.AddListener(StartTutorial);
        }

        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveListener(AdvanceSequence);
            advanceButton.onClick.AddListener(AdvanceSequence);
        }
    }
}
