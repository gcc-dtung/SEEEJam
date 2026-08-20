using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class TutorialSequenceUI : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private Image tutorialImage;
    [SerializeField] private Button tutorialButton;

    [Header("Slides")]
    [SerializeField] private Sprite[] tutorialSlides;

    [Header("Pop Settings")]
    [SerializeField, Min(0.05f)] private float popDuration = 0.28f;
    [SerializeField, Min(0f)] private float hiddenScale = 0.18f;

    private int _currentIndex = -1;
    private Tween _rootTween;
    private Tween _imageTween;

    private void Awake()
    {
        if (tutorialRoot == null)
            tutorialRoot = gameObject;

        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(StartTutorial);

        HideImmediate();
    }

    private void Update()
    {
        if (tutorialRoot == null || !tutorialRoot.activeSelf)
            return;

        if (Input.GetMouseButtonDown(0))
            AdvanceSequence();
    }

    private void OnDisable()
    {
        if (tutorialButton != null)
            tutorialButton.onClick.RemoveListener(StartTutorial);

        StopTweens();
    }

    public void StartTutorial()
    {
        if (tutorialSlides == null || tutorialSlides.Length == 0)
            return;

        _currentIndex = -1;
        ShowRoot();
        AdvanceSequence();
    }

    public void HideImmediate()
    {
        StopTweens();

        if (tutorialRoot != null)
            tutorialRoot.SetActive(false);

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
}
