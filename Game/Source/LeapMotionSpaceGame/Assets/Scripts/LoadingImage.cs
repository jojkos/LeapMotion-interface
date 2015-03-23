using UnityEngine;
using System.Collections;

public class LoadingImage : MonoBehaviour {
	public float duration;
	float startingTime;
	float endingTime;
	bool finished = false;
	
	// Use this for initialization
	void Start () {
		renderer.material.SetFloat("_Cutoff",1);//vynulovani
	}
	
	// Update is called once per frame
	void Update () {
		float value = renderer.material.GetFloat ("_Cutoff");
		if (value != 0){
			renderer.material.SetFloat("_Cutoff",1 - Mathf.InverseLerp(startingTime, endingTime, Time.realtimeSinceStartup));
		}
		else if (value == 0){
			finished = true;
			gameObject.SetActive(false);
		}
	}

	void OnApplicationFocus(bool focusStatus) { //diky tomuhle se nedokonci loading kdyz clovek vyjde na chvili ze hry, mezitim co tam neni
		if (focusStatus == true)
			Restart ();
	}

	public void EarlyStop(){
		gameObject.SetActive(false);
		//finished = true;
	}
	
	
	public bool HasFinished(){
		if (finished){
			finished = false;
			renderer.material.SetFloat("_Cutoff",1);
			return true;
		}
		else
			return false;
	}
	
	public void Initiate(Vector3 pos){	
		startingTime = Time.realtimeSinceStartup;
		endingTime = startingTime + duration;
		
		finished = false;
		renderer.material.SetFloat("_Cutoff",1);
		gameObject.SetActive (true);
		transform.position = new Vector3 (pos.x, 0, pos.z);
	}
	
	public void ChangePosition(Vector3 pos){
		transform.position = new Vector3 (pos.x, 0, pos.z);
	}

	public void Restart(){
		startingTime = Time.realtimeSinceStartup;
		endingTime = startingTime + duration;
	}
}
