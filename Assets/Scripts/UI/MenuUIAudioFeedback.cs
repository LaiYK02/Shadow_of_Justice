using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MenuUIAudioFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerDownHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        MainMenuAudioController.Instance?.PlayHover();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            MainMenuAudioController.Instance?.PlayPress();
    }
}
