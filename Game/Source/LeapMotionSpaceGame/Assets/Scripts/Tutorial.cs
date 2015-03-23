using UnityEngine;
using System.Collections;

public class Tutorial : MonoBehaviour {
	public FadeText tutorialText;
	public GameObject skipTutorial;
	public TutorialPlayerController tutorialPlayer;
	public GameObject handAnimations;
	public GameObject pointingButton;
	public GameObject clickingButton;
	public GameObject clickFireAnimation;
	public GameObject thumbAnimation;
	public GameObject planeScan;

	public bool shipTried = false;
	bool buttonPointed = false;
	bool planeScanned = false;
	bool clickOk = false;
	bool scanAgain = false;

	int shotCount = 0;
	bool clickShot = false;
	bool pointShot = false;

	public bool isTutorial = false;

	private GameController gameController;
	
	void Awake(){
		GameObject gameControllerObject = GameObject.FindWithTag ("GameController");
		if (gameControllerObject != null) {
			gameController = gameControllerObject.GetComponent<GameController>();
		}				
		if (gameController == null) {
			Debug.Log("Cannot find GameController script/object");
		}	
	}
	
	void Start () {
		isTutorial = true;
		StartCoroutine ("TutorialController");
	}

	IEnumerator TutorialController() {
		skipTutorial.SetActive (true);
		tutorialText.ChangeText("Welcome! If you already know the basis of the game, don't hesitate to skip tutorial.");
		tutorialText.FadeIn ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		yield return new WaitForSeconds (6);
		tutorialText.FadeOut ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);

		tutorialText.ChangeText("Game is controlled by hand movement and gestures, particulary with index finger. Default controlling hand is the right hand, that can be changed later in menu.");
		tutorialText.FadeIn ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		yield return new WaitForSeconds (9);
		tutorialText.FadeOut ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);

		skipTutorial.SetActive (false);
		
		tutorialText.ChangeText ("Let's explain two differnt kinds of control. You can either CLICK or POINT as is shown in animations.");
		tutorialText.FadeIn ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		handAnimations.SetActive (true);
		yield return new WaitForSeconds (7);
		tutorialText.FadeOut ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		
		tutorialText.ChangeText ("Try pointing on and hence activating this button. You have to hold your finger for a little while.");
		tutorialText.FadeIn ();
		pointingButton.SetActive (true);
		while (!buttonPointed) {
			yield return null;
		}
		tutorialText.FadeOut ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		
		tutorialText.ChangeText ("Very well!");
		tutorialText.FadeIn ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		yield return new WaitForSeconds (2);
		tutorialText.FadeOut ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		
	scan:
			tutorialText.ChangeText ("A little harder task now. Point at these three buttons. But this time you have to touch the plane you are projecting on. It is important for the second control type to work correctly.");
		tutorialText.FadeIn ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		planeScan.SetActive (true);
		for (int i = 0; i < planeScan.transform.childCount; i++) {
			planeScan.transform.GetChild(i).gameObject.SetActive(true);
		}
		while (!planeScanned) {
			yield return null;
		}
		tutorialText.FadeOut ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		
		tutorialText.ChangeText ("Second control method is CLICKING. Try clicking on the test button. If it works well you can continue with OK button. Otherwise point at the scan again button.");
		tutorialText.FadeIn ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		clickingButton.SetActive (true);
		
		while (!scanAgain && !clickOk) {
			yield return null;
		}
		clickingButton.SetActive (false);
		tutorialText.FadeOut ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		if (scanAgain) {
			scanAgain = false;
			goto scan;
		}

		handAnimations.SetActive (false);
		tutorialText.ChangeText("Thats it for the two control methods. How to control the ship? The ship moves just in front of your index finger of your controlling hand. Again it is the right hand and you can change it in menu later. Try moving with the ship a little bit.");
		tutorialText.FadeIn ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		yield return new WaitForSeconds (3);
		tutorialPlayer.gameObject.SetActive (true);

		while (!shipTried) {
			yield return new WaitForSeconds (11);
		}

		tutorialText.FadeOut ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		tutorialText.ChangeText ("Excellent!");
		tutorialText.FadeIn ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		yield return new WaitForSeconds (2);
		tutorialText.FadeOut ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);

		gameController.SelectClickingMethod ();
		tutorialPlayer.ActivateShooting ();
		shotCount = 0;
		clickShot = false;
		tutorialText.ChangeText ("Firing depends on the selected method. When Clicking is selected, you have to click to fire. Fire a few times");
		tutorialText.FadeIn ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);
		clickFireAnimation.SetActive (true);
		while (!clickShot){
			yield return new WaitForSeconds (2);
		}
		tutorialPlayer.DeactivateShooting ();
		tutorialText.FadeOut ();
		clickFireAnimation.SetActive (false);
		yield return new WaitForSeconds (tutorialText.fadeDuration);

		gameController.SelectHoveringMethod ();
		tutorialPlayer.ActivateShooting ();
		shotCount = 0;
		pointShot = false;
		tutorialText.ChangeText ("When Pointing is selected, you have to put your thumb near index finger just like the animation shows. The ship fires as long as you have ammo or put the thumb away.");
		tutorialText.FadeIn ();
		yield return new WaitForSeconds (2);
		thumbAnimation.SetActive (true);
		while (!pointShot){
			yield return new WaitForSeconds (2);
		}
		tutorialText.FadeOut ();
		thumbAnimation.SetActive (false);
		yield return new WaitForSeconds (tutorialText.fadeDuration);


		tutorialPlayer.DeactivateShooting ();
		tutorialText.ChangeText ("Great! One last thing, the game stops it self if you take your hand away or put it in a fist. But you can try that out inside the game. Good luck!");
		tutorialText.FadeIn ();
		tutorialPlayer.gameObject.SetActive (false);
		yield return new WaitForSeconds (7);
		tutorialText.FadeOut ();
		yield return new WaitForSeconds (tutorialText.fadeDuration);

		isTutorial = false;//konec tutorialu
		gameController.TutorialEnd ();
		yield return null;
	}

	public void ClickShot(){
		shotCount += 1;
		if (shotCount > 7){
			clickShot = true;
			shotCount = 0;
		}
	}

	public void PointShot(){
		shotCount += 1;
		if (shotCount > 7){
			pointShot = true;
			shotCount = 0;
		}
	}

	public void PlaneScanned(){
		planeScanned = true;
	}
	

	public void ScanAgain(){
		scanAgain = true;
		planeScanned = false;
	}

	public void OkClicked(){
		clickOk = true;
	}

	public void ButtonPointed(){
		buttonPointed = true;
	}

	public void SkipTutorial(){
		StopCoroutine ("TutorialController");
		isTutorial = false;
		gameObject.SetActive (false);
	}


}
