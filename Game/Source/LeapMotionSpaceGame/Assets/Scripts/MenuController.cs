using UnityEngine;
using System.Collections;

using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {
	public GameObject menuChoices;
	public GameObject settingsChoices;
	public GameObject highscoreChoices;
	public GameObject inputChoices;
	public GameObject planeScanChoices;
	public Text highscore;

	public GameObject ingameChoices;
	public GameObject resume;
	private GameController gameController;

	EventSystem eventSystem;


	void Awake(){
		eventSystem = EventSystem.current; //pro ovladani slideru a jinych selecatables

		GameObject gameControllerObject = GameObject.FindWithTag ("GameController");
		if (gameControllerObject != null) {
			gameController = gameControllerObject.GetComponent<GameController>();
		}				
		if (gameController == null) {
			Debug.Log("Cannot find GameController script/object");
		}
	}

	public void Open(){
		//gameObject.SetActive (true);
		menuChoices.SetActive (true);
	}

	public void Close(){
		//gameObject.SetActive (false);
		menuChoices.SetActive (false);
		settingsChoices.SetActive (false);
		highscoreChoices.SetActive (false);
		inputChoices.SetActive (false);

		ingameChoices.SetActive (false);
		resume.SetActive (false);
	}

	public void OpenPlaneScanChoices(){
		for (int i = 0; i < planeScanChoices.transform.childCount; i++) {
			planeScanChoices.transform.GetChild(i).gameObject.SetActive(true);
		}
		planeScanChoices.SetActive(true);
	}

	public void ClosePlaneScanChoices(){
		planeScanChoices.SetActive (false);
	}

	public void OpenInputSettings(){
		settingsChoices.SetActive(false);
		inputChoices.SetActive(true);
		gameController.UseLeap(true);
	}

	public void CloseInputSettings(){
		settingsChoices.SetActive(true);
		inputChoices.SetActive(false);
	}

	public void OpenSettings(){
		menuChoices.SetActive (false);
		settingsChoices.SetActive (true);
	}

	public void CloseSettings(){
		menuChoices.SetActive (true);
		settingsChoices.SetActive (false);
	}

	public void OpenHighscore(){
		menuChoices.SetActive (false);
		highscoreChoices.SetActive (true);
		highscore.text = "Highscore: " + gameController.GetHighScore ().ToString();
	}

	public void ClearHighscore(){
		gameController.ClearHighscore ();
		highscore.text = "Highscore: " + gameController.GetHighScore ().ToString();
	}

	public void CloseHighscore(){
		menuChoices.SetActive (true);
		highscoreChoices.SetActive (false);
	}

	public void GameOver(){
		ingameChoices.SetActive (true);
	}

	public void Pause(){
		ingameChoices.SetActive (true);
		resume.SetActive (true);
	}

}
