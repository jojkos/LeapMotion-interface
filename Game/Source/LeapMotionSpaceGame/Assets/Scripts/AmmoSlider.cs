using UnityEngine;
using System.Collections;

using UnityEngine.UI;
using System.Linq;

public class AmmoSlider : MonoBehaviour {
	//*zobrazovač naboju na prstu, umožnuje nastavit šířku, pozici a hodnotu, mění barvu podle plnosti*//
	public int ammoPerSlider;
	public GameObject slider;
	
	GameObject [] sliders;
	RectTransform [] rectTransforms;

	float ammoSliderShift = 10;

	GameController gameController;

	// Use this for initialization
	void Start () {
		GameObject gameControllerObject = GameObject.FindWithTag ("GameController");
		if (gameControllerObject != null) {
			gameController = gameControllerObject.GetComponent<GameController>();
		}				
		if (gameController == null) {
			Debug.Log("Cannot find GameController script/object");
		}	

		sliders = new GameObject[gameController.maxAmmo / ammoPerSlider];
		rectTransforms = new RectTransform[sliders.Length];

		for (int i = 0; i < sliders.Length; i++) {
			sliders[i] =(GameObject) Instantiate(slider);
			sliders[i].transform.SetParent(transform, true);
			sliders[i].GetComponent<Slider>().maxValue = ammoPerSlider;
			rectTransforms[i] = sliders[i].gameObject.GetComponent<RectTransform> ();
		}

		SetWidth(30);

	}
	
	public void SetWidth(int width){
		if (width == 0) {
			width = 30;
		}
		for (int i = 0; i < sliders.Length; i++) {
			rectTransforms[i].sizeDelta = new Vector2(width, rectTransforms[i].rect.height);
		}
	}

	public void SetPosRot(Vector3 pos, Quaternion rot){
		/*nastaveni pozice a rotace*/

		Vector3 euler = rot.eulerAngles;
		int mainShift = 20; //o kolik sou vsechny slidery posunute
		int shift = 20;   //mezera mezi kazdym dalsim sliderem

		for (int i = 0; i < sliders.Length; i++) {
			rectTransforms[i].rotation = Quaternion.Euler(0.0f, 0.0f, 360 - euler.y);

			Vector3 position = Camera.main.WorldToScreenPoint(pos);
			Vector3 ammoPos = position - rectTransforms[i].up * (mainShift + i*shift);
			///centrovani podle pocty zbyvajicich naboju
			ammoPos.x = ammoPos.x + (sliders[i].GetComponent<Slider>().maxValue - sliders[i].GetComponent<Slider>().value);
			rectTransforms[i].transform.position = ammoPos;
		}
	}

	public void SetRotation(Quaternion rotation){
		Vector3 euler = rotation.eulerAngles;
		for (int i = 0; i < sliders.Length; i++) {
			rectTransforms[i].rotation = Quaternion.Euler(0.0f, 0.0f, 360 - euler.y);
		}
	}

	public void SetValues(){
		for (int i = 0; i < sliders.Length; i++) {
			Slider s = sliders[i].GetComponent<Slider>();

			if (gameController.AmmoCount() > i*ammoPerSlider){
				sliders[i].SetActive(true);
				if (gameController.AmmoCount() >= (i+1)*ammoPerSlider)
					s.value = s.maxValue;
				else
					s.value = gameController.AmmoCount()%ammoPerSlider;			
			}
			else
				sliders[i].SetActive(false);

			var fill = sliders[i].GetComponentsInChildren<UnityEngine.UI.Image>()
				.FirstOrDefault(t => t.name == "Fill");
			if (fill != null)
			{
				fill.color = Color.Lerp(Color.red, Color.green, sliders[i].GetComponent<Slider>().value/sliders[i].GetComponent<Slider>().maxValue);
			}

		
		}
	}
}
