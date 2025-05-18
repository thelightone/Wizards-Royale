using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : Joystick
{
    public GameObject start;
    private Vector2 trans;

    protected override void Start()
    {
        base.Start();
        //background.gameObject.SetActive(false);
        trans = background.anchoredPosition;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
       // start.gameObject.SetActive(false);
        Time.timeScale = 1;
        background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
        background.gameObject.SetActive(true);
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //background.gameObject.SetActive(false);
        base.OnPointerUp(eventData);
        background.anchoredPosition= trans;
    }
}