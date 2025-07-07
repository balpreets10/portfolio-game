using DG.Tweening;

using TMPro;

using UnityEngine;

public class ContentPrefab : MonoBehaviour
{
    public TextMeshPro textTitle;
    public TextMeshPro textDescription;

    private Transform target;

    public void SetText(string title, string Description)
    {
        textTitle.text = title;
        textDescription.text = Description;
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    public void Activate()
    {
        gameObject.SetActive(true);
    }

    public void AnimateIn()
    {
        transform.DOMoveY(target.position.y, 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                // Optionally, you can add any additional logic after the animation completes
            });
    }
}