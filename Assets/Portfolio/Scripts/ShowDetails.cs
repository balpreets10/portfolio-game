using System;

using DG.Tweening;

using TMPro;

using UnityEngine;

public class ShowDetails : MonoBehaviour
{
    public TextMeshProUGUI textTitle;
    public TextMeshProUGUI textDescription;

    public TextMeshProUGUI textCurrentContext;

    public RectTransform panel;

    private void Start()
    {
        ManagePanel(false);
        DeactivateContextText();
    }

    private void OnEnable()
    {
        BuildingRaycastManager.OnBuildingHit += OnBuildingHit;
        ResumeBoardLandingDOTween.OnBoardLanded += OnBoardLanded;
    }

    private void OnDisable()
    {
        BuildingRaycastManager.OnBuildingHit -= OnBuildingHit;
        ResumeBoardLandingDOTween.OnBoardLanded -= OnBoardLanded;
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
        }
    }

    private void OnBoardLanded()
    {
        Debug.Log("Board landed, showing context text.");
        ActivateContextText("Press E to continue");
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

    private void OnBuildingHit(SectionHouse house)
    {
        ManagePanel(false);
        SetText(house.resumeSection.title, house.resumeSection.content);
        Activate();
    }

    public void SetText(string title, string Description)
    {
        textTitle.text = title;
        textDescription.text = Description;
        textTitle.rectTransform.DOLocalMoveX(4000, .1f);
        textDescription.rectTransform.DOLocalMoveY(-5000, .1f);
    }

    public void Deactivate()
    {
        panel.DOMoveY(-5000, .1f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            ManagePanel(false);
        });
    }

    public void Activate()
    {
        ManagePanel(true);
        panel.DOLocalMoveY(0, .5f).SetEase(Ease.InBack);
    }

    private void ManagePanel(bool activate)
    {
        panel.gameObject.SetActive(activate);
    }
}