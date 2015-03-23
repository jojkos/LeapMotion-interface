using UnityEngine;
using System.Collections;

public class LoadingImageTutorial : MonoBehaviour {
	public float duration;
	float startingTime;
	float endingTime;
	public float respawnTime;
	// Use this for initialization
	void Start () {
		renderer.material.SetFloat("_Cutoff",1);//vynulovani
		Initiate ();
	}
	
	// Update is called once per frame
	void Update () {
		float value = renderer.material.GetFloat ("_Cutoff");
		if (value != 0){
			renderer.material.SetFloat("_Cutoff",1 - Mathf.InverseLerp(startingTime, endingTime, Time.realtimeSinceStartup));
		}
		else if (value == 0){
			gameObject.SetActive(false);
		}	

		if (Time.realtimeSinceStartup > endingTime + respawnTime){
			Initiate ();
		}
	}

	public void Initiate(){	
		startingTime = Time.realtimeSinceStartup;
		endingTime = startingTime + duration;

		renderer.material.SetFloat("_Cutoff",1);
		gameObject.SetActive (true);
	}
}
