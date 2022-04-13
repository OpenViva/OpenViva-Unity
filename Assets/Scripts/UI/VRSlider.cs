using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VRSlider : Button, IBeginDragHandler, IDragHandler, IEndDragHandler {

	[SerializeField]
	private RectTransform sliderBackground;

	[SerializeField]
	private RectTransform sliderFill;

	[Range(0.0f, 1.0f)]
	[SerializeField]
	private float percent;

	private float baseWidth = 1.0f;

	protected override void OnEnable(){
		base.OnEnable();

		sliderBackground = transform.parent.GetComponent<RectTransform>();
		sliderFill = transform.parent.GetChild(0).GetComponent<RectTransform>();

		baseWidth = sliderBackground.sizeDelta.x;

		Reposition();
	}

	public void OnBeginDrag( PointerEventData eventData ){
	}
	public void OnDrag( PointerEventData eventData ){
		
		Vector2 localPos = transform.localPosition;
		localPos.x = eventData.position.x-baseWidth*2.0f;
		transform.localPosition = localPos;
		Debug.Log(eventData.position.x);
		// percent = Mathf.Clamp( localPos.x+baseWidth*0.5f, 0, baseWidth )/baseWidth;
		// Reposition();
		
    }
	public void OnEndDrag( PointerEventData eventData ){
	}

	public void Reposition(){

		Vector2 sizeDelta = sliderFill.sizeDelta;
		sizeDelta.x = baseWidth*percent;
		sliderFill.sizeDelta = sizeDelta;

		Vector2 localPos = transform.localPosition;
		localPos.x = baseWidth*percent-baseWidth*0.5f;
		transform.localPosition = localPos;
	}

	public float GetPercent(){
		return percent;
	}
}
