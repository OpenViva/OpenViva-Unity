using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
 {
     public GameObject toolTipGO;
 
     void Start()
     {
         if (toolTipGO!= null)
         {
             toolTipGO.SetActive(false);
         }
     }
 
     public void OnPointerEnter(PointerEventData eventData)
     {
         if (toolTipGO!= null)
         {
             toolTipGO.SetActive(true);
         }
     }
 
     public void OnPointerExit(PointerEventData eventData)
     {
         if (toolTipGO!= null)
         {
             toolTipGO.SetActive(false);
         }
     }
 }