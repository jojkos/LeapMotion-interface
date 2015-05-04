using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour {
	//*výběr barvy lodičky, nepouziva se*//
	public Image colorBox;
	public GameObject objectToColor;
	public int slidersMaxValue;
	Slider activeSlider = null;
	float red;
	float green;
	float blue;


	// Use this for initialization
	void Start () {
		//nastavi rozsah vsech pod slideru
		for (int i = 0; i < transform.childCount; i++) {
			Slider slider = transform.GetChild(i).GetComponent<Slider>();
			if (slider != null){
				slider.maxValue = slidersMaxValue;
			}
		}

		red = 0;
		green = 0;
		blue = 0;
		updateColor ();
		SetFirstSliderActive ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void updateColor(){
		Color tmp = Color.black;
		tmp += new Color(red, 0f, 0f, 0f);
		tmp += new Color(0f, green, 0f, 0f);
		tmp += new Color(0f, 0f, blue, 0f);

		if (tmp == Color.black)
			tmp = Color.white;
		colorBox.color = tmp;

		objectToColor.renderer.material.color = tmp;
	}

	public void updateRed(float number){
		red = number / slidersMaxValue;
		updateColor ();
	}
	public void updateGreen(float number){
		green = number / slidersMaxValue;
		updateColor ();
	}
	public void updateBlue(float number){
		blue = number / slidersMaxValue;
		updateColor ();
	}

	public void SetFirstSliderActive(){
		//nejakej bug, bez toho to neoznaci prvniho, nejdriv se musi odoznacit
		Slider secondSlider = transform.GetChild (1).GetComponent<Slider>();
			secondSlider.Select();
			
		foreach (Transform child in transform) {
			Slider firstSlider = child.GetComponent<Slider>();
			if (firstSlider != null){
				firstSlider.Select();
				activeSlider = firstSlider;
				break;
			}
		}
	}

	public void SelectUp(){
		if (activeSlider != null) {
			Selectable sel = activeSlider.FindSelectableOnUp ();
			if (sel != null){
				Slider sliderUp = sel.GetComponent<Slider>();
				if (sliderUp != null){
					sliderUp.Select();
					activeSlider = sliderUp;
				}
			}
		}
	}

	public void SelectDown(){
		if (activeSlider != null) {
			Selectable sel = activeSlider.FindSelectableOnDown ();
			if (sel != null){
				Slider sliderDown = sel.GetComponent<Slider>();
				if (sliderDown != null){
					sliderDown.Select();
					activeSlider = sliderDown;
				}
			}
		}
	}

	public void SelectLeft(){
		if (activeSlider != null)
			activeSlider.value -= 1;
	}

	public void SelectRight(){
		if (activeSlider != null)
			activeSlider.value += 1;
	}

	public void Activate(){
		gameObject.SetActive (true);
		SetFirstSliderActive ();
	}

	public void Deactivate(){
		gameObject.SetActive (false);
	}
}
