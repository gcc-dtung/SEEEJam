using System;
using PrimeTween;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ShowTooltip : MonoBehaviour
{
    [SerializeField] private RectTransform backgroundRectTransform;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private float itemShowDuration;
    
    Tween showTween;

    private void OnEnable()
    {
        DragItem.OnShowTooltip.AddListener(ShowToolTip);
        DragItem.OnHideTooltip.AddListener(HideToolTip);
    }

    private void ShowToolTip(string tooltip)
    {
        itemName.text = tooltip;
        float currentHeight = backgroundRectTransform.anchoredPosition.y;
        
        if(showTween.isAlive)
            showTween.Stop();
        showTween = Tween.Custom(currentHeight, -150f, duration: itemShowDuration, onValueChange: newVal => backgroundRectTransform.anchoredPosition = new Vector2(backgroundRectTransform.anchoredPosition.x, newVal));
    }

    private void HideToolTip()
    {
        float currentHeight = backgroundRectTransform.anchoredPosition.y;
        if(showTween.isAlive)
            showTween.Stop();
        showTween = Tween.Custom(currentHeight, 150f, duration: itemShowDuration, onValueChange: newVal => backgroundRectTransform.anchoredPosition = new Vector2(backgroundRectTransform.anchoredPosition.x, newVal)).OnComplete(() =>
            itemName.text = "");
    }
}
