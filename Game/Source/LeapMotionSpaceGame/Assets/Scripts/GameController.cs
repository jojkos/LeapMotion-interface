using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour {

	public GameObject[] hazards;
	public GameObject[] bonuses;
	public LoadingImage loadingHand;
	public PlayerController player;
	public LeapController leapController;
	public bool useLeap;
	public ColorPicker colorPicker;
	public MenuController menu;
	public Tutorial tutorial;

	public Vector3 spawnValues;
	public bool screenBoundary; //muze se spawnovat a pohybovat do kraju obrazovky

	public float waveWait;
	public float spawnWait;
	public float spawnCount;
	public int bonusModulo;
	public int startingAmmo;

	public GUIText scoreText;
	public GUIText ammoText;
	public GUIText gamestatusText;
	private bool gameover;
	private int score;
	private int lvl;
	public int maxAmmo;
	private int ammo;
	private bool isPause; //to stejny co menu

	private bool colorPicking;

	Vector3 [] planePoints = new Vector3[3];
	int planePointsCount = 0;

	enum SelectMethod{Hovering, Clicking};
	SelectMethod selectMethod = SelectMethod.Hovering;

	void Awake(){
		//Application.runInBackground = true; //bezi i na pozadi
		scoreText.text = "";
		ammoText.text = "";
		gamestatusText.text = "";
	}

	void Start(){
		if (!leapController.IsFile()) {
			NoInputsFile();
			return;
		}
		Initiate ();
		//ScanPalmPlane (); //staci zmenit tady a v tlacitku scan again
		//menu.OpenPlaneScanChoices ();
		tutorial.gameObject.SetActive (true);
	}


	public void TutorialEnd(){
		menu.OpenInputSettings ();
	}

	public void TutorialSkip(){
		menu.OpenPlaneScanChoices ();
	}

	public void AddPlanePoint(){
		//prida bod pro vypocet klikaci roviny. jsou potreba tri
		Vector3 point = leapController.GetControlLeapWorldPosition ();
		if (planePointsCount == 3){
			planePointsCount = 0;
		}
		planePoints [planePointsCount] = point;
		planePointsCount += 1;

		if (planePointsCount == 3){
			menu.ClosePlaneScanChoices();
			var dir = Vector3.Cross(planePoints[0] - planePoints[1], planePoints[2] - planePoints[1]);
			var norm = Vector3.Normalize(dir);
			
			ClickPlaneScanned(new Plane(norm, planePoints[2]));
		}
	}

	public void ScanPalmPlane(){
		loadingHand.Initiate (new Vector3(0, 0, 5));
		StartCoroutine ("GetPalmPlane");
	}

	//TODO pridat vsude hromadu komentaru
	IEnumerator GetPalmPlane (){ //nepouzivam kvuli spatne efektivite
		Plane plane;
		Plane firstPlane;

		firstPlane = leapController.ControllingHandPlane();

		while (true) {
			plane = leapController.ControllingHandPlane();


			if(!leapController.ControllingHandInView() || (plane.normal - firstPlane.normal).magnitude > 0.1){
				loadingHand.Restart();
				firstPlane = plane;
			}			

			if (loadingHand.HasFinished()){
				//print (leapController.ControllingHandInView());
				plane = leapController.ControllingHandPlane();
				//print (plane.distance);
				//print (plane.normal);
				ClickPlaneScanned(plane);
				break;
			}

			yield return null;
		}
	}

	void ClickPlaneScanned(Plane plane){
		leapController.SetClickPlane (plane);
		if (tutorial.isTutorial)
			tutorial.PlaneScanned();
		else
			menu.OpenInputSettings ();
	}

	public bool IsHoveringMethod(){
		if (selectMethod == SelectMethod.Hovering)
			return true;
		return false;
	}
	
	public bool IsClickingMethod(){
		if (selectMethod == SelectMethod.Clicking)
			return true;
		return false;
	}
	
	public void SelectHoveringMethod(){
		selectMethod = SelectMethod.Hovering;
	}
	
	public void SelectClickingMethod(){
		selectMethod = SelectMethod.Clicking;
	}

	public void Initiate(){
		score = 0;
		scoreText.text = "";
		lvl = 1;
		ammo = startingAmmo;
		ammoText.text = "";
		gameover = false;
		gamestatusText.text = "";

		isPause = false;
		colorPicking = colorPicker.gameObject.activeSelf;
		//OpenMenu ();
		//player.SetActive (false);
		player.gameObject.transform.position = new Vector3 (0, 0, 0);
	}

	void ClearSpace(){ //smaze objekty co zbyly z predchozi hry
		DestroyObjectWithTag ("Hazard");
		DestroyObjectWithTag ("AmmoCrate");
		DestroyObjectWithTag ("Bolt");
		DestroyObjectWithTag ("EnemyBolt");
	}

	void DestroyObjectWithTag(string tag){
		GameObject [] gameObjects;
		gameObjects =  GameObject.FindGameObjectsWithTag (tag);		
		for(var i = 0 ; i < gameObjects.Length ; i ++)
			Destroy(gameObjects[i]);
	}

	public void OpenMenu(){
		menu.Open ();

	}

	public void CloseMenu(){
		menu.Close ();
	}



	public void StartGame(){
		ClearSpace ();
		Time.timeScale = 1.0f;
		UpdateScore ();
		UpdateAmmo ();
		StartCoroutine ("SpawnWaves");	
		isPause = false;
		player.Activate ();
		//CloseMenu ();
	}

	void Update(){
	}

	public void RestartGame(){
		//Application.LoadLevel(Application.loadedLevel);
		Initiate ();
		StartGame ();
		CloseMenu ();
	}

	Vector3 RandomSpawnPosition(){
		Vector3 spawnPosition;
		if(!screenBoundary){
			spawnPosition = new Vector3 (Random.Range (-spawnValues.x, spawnValues.x), 0.0f, spawnValues.z);
		}
		else{
			spawnPosition = new Vector3 (
				Random.Range (
								Camera.main.ViewportToWorldPoint(new Vector3(0.0f, 0.0f, 0.0f)).x+1, //aby objekt nebyl pres okraj +-1
								Camera.main.ViewportToWorldPoint(new Vector3(1.0f, 0.0f, 0.0f)).x-1
							 ),
	        	0.0f,
	            Camera.main.ViewportToWorldPoint(new Vector3(0.0f, 1.0f, 0.0f)).z
			);
		}
		return spawnPosition;
	}

	IEnumerator SpawnWaves (){
		yield return new WaitForSeconds (waveWait);

		while (true) {
			for (int i = 0; i < spawnCount * lvl; i++) {

				int whichObject = Random.Range(0, hazards.Length);
				Quaternion spawnTransformation = hazards[whichObject].transform.rotation;
				GameObject hazard = Instantiate (hazards[whichObject], RandomSpawnPosition(), spawnTransformation) as GameObject; 
				if (lvl > 1)
					hazard.GetComponent<Mover>().speed *= 1.2f; //zvysovani rychlosti po kolech

				if (i%bonusModulo == 0){
					spawnTransformation = Quaternion.identity;
					whichObject = Random.Range(0, bonuses.Length);
					Instantiate (bonuses[whichObject], RandomSpawnPosition(), spawnTransformation); 
				}

				yield return new WaitForSeconds (spawnWait);
			}

			if (gameover) {
				GameOver();
				break;
			}

			if (!gameover){
				//ceka nez ze sceny zmizi vsichni nepratale
				while(GameObject.FindGameObjectsWithTag("Hazard").Length > 0){
					yield return new WaitForSeconds (1);
				}
				yield return new WaitForSeconds (1);

				if (gameover) {
					GameOver();
					break;
				}

				gamestatusText.text = "Level "+lvl+" ended. Score + "+lvl*100+", \nnext level..";
				AddScore(lvl*100);

				yield return new WaitForSeconds (waveWait);
				gamestatusText.text = "";
				lvl += 1;
				yield return new WaitForSeconds (1);
			}

		}

	}

	public void AddScore(int count){
		score += count;
		UpdateScore ();
	}

	void UpdateScore(){
		scoreText.text = "Score: " + score;
	}

	void SaveHighScore(){
		PlayerPrefs.SetInt ("High Score", score);
	}

	public int GetHighScore(){
		return PlayerPrefs.GetInt ("High Score");
	}

	public void ShowHighScore(){
		gamestatusText.text = "Highscore is "+GetHighScore();
	}

	public void ClearHighscore(){
		PlayerPrefs.SetInt ("High Score", 0);
	}

	public void Endgame(){
		if (!gameover) {
			player.Deactivate ();
			StopCoroutine ("SpawnWaves");
		}
	}

	public void GameOver(){
		player.Deactivate();
		
		StopCoroutine ("SpawnWaves");
		
		gameover = true;
		gamestatusText.text = "Game Over!";
		
		if (GetHighScore () < score) {
			SaveHighScore();
			gamestatusText.text += "\n new Highscore: "+GetHighScore();
		}
		else{
			gamestatusText.text += "\n score: "+ score;
		}
		
		menu.GameOver ();
	}

	public int AmmoCount(){
		return ammo;
	}

	public bool AmmoEmpty(){
		return ammo == 0;
	}

	public void SpendAmmo(){
		ammo -= 1;
		UpdateAmmo ();
	}

	public void AddAmmo(int count){
		ammo += count;
		if (ammo > maxAmmo)
			ammo = maxAmmo;
		UpdateAmmo ();
	}

	void UpdateAmmo(){
		ammoText.text = "Ammo: " + ammo;
	}

	public void NoInputsFile(){
		Time.timeScale = 0.0f;
		gamestatusText.text = "inputs.txt missing, get file and restart game";
	}

	public void PauseGame(){
		if (isPause == false) {
			isPause = true;
			gamestatusText.text = "Pause";
			Time.timeScale = 0.0f;
		}
		else{
			isPause = false;
			gamestatusText.text = "";
			Time.timeScale = 1.0f;
		}
	}

	public void PauseOnlyGame(){
		if (isPause == false) {
			isPause = true;
			//gamestatusText.text = "Pause";
			Time.timeScale = 0.0f;
			menu.Pause();
			player.Deactivate();
		}
	}

	public void UnpauseOnlyGame(){
		if (isPause == true) {
			isPause = false;
			gamestatusText.text = "";
			Time.timeScale = 1.0f;
			player.Activate();
		}
	}

	public bool IsPause(){
		return isPause;
	}

	public void ColorPicking(){
		if (colorPicking == false) {
			colorPicking = true;
			Time.timeScale = 0.0f;
		}
		else{
			colorPicking = false;
			Time.timeScale = 1.0f;
		}
		GameObject CPobject = colorPicker.gameObject;
		if (CPobject.activeSelf)
			colorPicker.Deactivate();
		else
			colorPicker.Activate();
	}

	public GameObject ColorPickingOn(){
		if (colorPicking == false){
			colorPicking = true;
			//Time.timeScale = 0.0f;
			colorPicker.Activate ();
		}
		return colorPicker.gameObject;
	}

	public void ColorPickingOff(){
		if (colorPicking == true) {
			colorPicking = false;
			//Time.timeScale = 1.0f;
			colorPicker.Deactivate();
		}
	}

	public bool IsColorPicking(){
		return colorPicking;
	}

	public void ExitGame(){
		Application.Quit ();
		//System.Diagnostics.Process.GetCurrentProcess().Kill();
	}

	public bool UsingLeap(){
		return useLeap;
	}

	public void UseLeap(bool use){
		useLeap = use;
	}
}
