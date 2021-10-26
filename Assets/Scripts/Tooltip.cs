using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject tooltipObject;

    public void OnPointerEnter(PointerEventData eventData)
    {
        tooltipObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltipObject.SetActive(false);
    }
}
