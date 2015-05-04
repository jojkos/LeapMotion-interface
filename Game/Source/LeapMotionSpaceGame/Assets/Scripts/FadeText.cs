using UnityEngine;
using System.Collections;

using UnityEngine.UI;

public class FadeText : MonoBehaviour {
	//*text který umožnuje provádět fadeIn a fadeOut*//

	public float fadeDuration;
	public Text text;
	bool fadeIn = false;
	bool fadeOut = false;
	float startingTime;
	float endingTime;

	void Update(){
		if (fadeIn) {
			if (Time.realtimeSinceStartup >= endingTime){
				fadeIn = false;
			}
			else{
				Color tmpColor = text.color;
				tmpColor.a = Mathf.InverseLerp(startingTime, endingTime, Time.realtimeSinceStartup);
				text.color = tmpColor;
			}
				
		}
		else if (fadeOut) {
			if (Time.realtimeSinceStartup >= endingTime){
				fadeIn = false;
			}
			else{
				Color tmpColor = text.color;
				tmpColor.a = 1 - Mathf.InverseLerp(startingTime, endingTime, Time.realtimeSinceStartup);
				text.color = tmpColor;
			}
			
		}
	}

	public void FadeIn() {
		startingTime = Time.realtimeSinceStartup;
		endingTime = startingTime + fadeDuration;
		fadeIn = true;
	}

	public void FadeOut() {
		startingTime = Time.realtimeSinceStartup;
		endingTime = startingTime + fadeDuration;
		fadeOut = true;
	}

	public void ChangeText(string newText){
		text.text = newText;
	}
}
