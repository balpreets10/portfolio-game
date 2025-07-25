using System;
using System.Collections;
using System.Text;

using DG.Tweening;

using Reflex.Attributes;

using TMPro;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SplashScreen : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI textPersistentText;
    public TextMeshProUGUI textCurrentContext;

    public Image panel;
    private GamePlatform platform;
    private StringBuilder sb = new();

    public static event Action OnLoadingComplete;

    public UnityEvent OnSliderFill;

    [Inject] private IPlatformDetector platformDetector;

    private void Start()
    {
        panel.color = new Color(0, 0, 0, 1f);
        ManagePanel(true);
        slider.value = 0;
        slider.DOValue(1, 1f, false).OnComplete(() =>
        {
            OnSliderFill?.Invoke();
        });
        DeactivateContextText();

        platform = platformDetector.CurrentPlatform;
        switch (platform)
        {
            case GamePlatform.PC:
                textPersistentText.text = "Use Arrow keys / WASD to move, Mouse to Rotate. Press ESC to unlock mouse\n";
                break;

            case GamePlatform.Mobile:
                textPersistentText.text = "Use LeftHalf to Move / Right half to rotate Camera";
                break;

            case GamePlatform.Console:
                textPersistentText.text = "Press 'A' to exit the gate";
                break;

            default:
                textPersistentText.text = "Use Joystick to move";
                break;
        }
    }

    private void OnEnable()
    {
        RaycastManager.OnInteractableHit += OnInteractableHit;
        RaycastManager.OnInteractableLost += OnInteractableLost;
    }

    private void OnDisable()
    {
        RaycastManager.OnInteractableHit -= OnInteractableHit;
        RaycastManager.OnInteractableLost -= OnInteractableLost;
    }

    private void OnInteractableHit(IInteractable interactable)
    {
        Debug.Log("Board landed, showing context text.");
        sb.Clear();
        sb.Append("Press");
        switch (platform)
        {
            case GamePlatform.PC:
                sb.Append(" E ");
                break;

            case GamePlatform.Mobile:
                sb.Append(" Action ");
                break;

            case GamePlatform.Console:
                sb.Append(" A ");
                break;

            default:
                sb.Append(" Button ");
                break;
        }
        sb.Append(interactable.GetInteractionText());

        ActivateContextText(sb.ToString());
    }

    private void OnInteractableLost()
    {
        DeactivateContextText();
    }

    private void ActivateContextText(string text)
    {
        textCurrentContext.text = text;
        textCurrentContext.gameObject.SetActive(true);
    }

    private void DeactivateContextText()
    {
        textCurrentContext.text = string.Empty;
        textCurrentContext.gameObject.SetActive(false);
    }

    public void SetText(string title, string Description)
    {
        //textTitle.text = title;
        //textDescription.text = Description;
        //textTitle.rectTransform.DOLocalMoveX(4000, .1f);
        //textDescription.rectTransform.DOLocalMoveY(-5000, .1f);
    }

    private void ManagePanel(bool activate)
    {
        panel.gameObject.SetActive(activate);
    }

    public void OnBeginClick()
    {
        OnLoadingComplete?.Invoke();
        panel.DOFade(0, .5f).OnComplete(() =>
        {
            ManagePanel(false);
        });
    }
}