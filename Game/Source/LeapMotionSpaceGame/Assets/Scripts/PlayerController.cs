using UnityEngine;
using System.Collections;

using UnityEngine.EventSystems;
using UnityEngine.UI;
//using Leap;

public class PlayerController : MonoBehaviour {

	public float speed;
	public Boundary boundary; //sou nastaveny boundary rucne
	public float tilt;
	public bool screenBoundary; //muze se hybat do kraju obrazovky
	
	public Transform shotSpawn;
	public GameObject shot;
	public float fireRate;
	public float playerShift;
	private float nextFire = 0.0f;
	public int fireAngle;
	
	public LeapController leapController;
	public AmmoSlider ammoSliderPrefab;
	AmmoSlider ammoSlider = null;

	private GameController gameController;
	

	void Awake(){
		GameObject gameControllerObject = GameObject.FindWithTag ("GameController");
		if (gameControllerObject != null) {
			gameController = gameControllerObject.GetComponent<GameController>();
		}				
		if (gameController == null) {
			Debug.Log("Cannot find GameController script/object");
		}	

		leapController.OnSwipe += OnSwipe;
		leapController.OnFingerClick += OnFingerClick;

		gameObject.SetActive (false); //ve vysledku to tady chci, aby to nepauzovalo kdyz nema, ale musi byt na zacatku videt aby se spustil awake
	}

	void OnSwipe(Vector3 direction){
		if (gameController.UsingLeap () && gameController.IsColorPicking()) {
			if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)){ //doprava doleva/nahoru dolu
				//doprava/doleva
				if(direction.x > 0){
					print ("doleva");
					gameController.colorPicker.SelectLeft();
				}
				else{
					print ("doprava");
					gameController.colorPicker.SelectRight();
				}
			}
			else{
				if (direction.y > 0){ //nahoru dolu
					print ("dolu");
					gameController.colorPicker.SelectDown();
				}
				else{
					print ("nahoru");
					gameController.colorPicker.SelectUp();
				}
			}
		}
	}

	void FireShot(){
		if (!gameController.AmmoEmpty() && !gameController.IsPause() && !gameController.IsColorPicking()){
			Instantiate (shot, shotSpawn.position, shotSpawn.rotation); 
			nextFire = (Time.time + fireRate);
			gameController.SpendAmmo();
		}
	}

	void Update()
	{	
		///////--------------------------------------------poloha/pohyb
		Vector3 position = new Vector3(0, 0, 0);
		
		if (!gameController.UsingLeap ()){
			float moveHorizontal = Input.GetAxis ("Horizontal");
			float moveVertical = Input.GetAxis ("Vertical");
			
			Vector3 movement = new Vector3(moveHorizontal,0.0f,moveVertical);
			
			rigidbody.velocity = movement * speed;
			
			rigidbody.rotation = Quaternion.Euler (rigidbody.velocity.z * tilt, 0.0f, rigidbody.velocity.x * -tilt);
			position = rigidbody.position;
		}
		else if(gameController.UsingLeap ()){
			Vector3 controlPosition = Camera.main.ScreenToWorldPoint(leapController.GetControlScreenPosition());
			
			rigidbody.rotation = leapController.GetControlRotation();
			transform.rotation = leapController.GetControlRotation();
			
			//position = new Vector3 (controlPosition.x, controlPosition.y, controlPosition.z + playerShift); //aby lod byla kus pred prstem, TAKHLE NE
			position = controlPosition + transform.forward * playerShift;
			
			Vector3 ammoPosition = Camera.main.ScreenToWorldPoint(leapController.GetControlJointPosition());
			
			if (ammoSlider != null){
				ammoSlider.SetPosRot(ammoPosition, leapController.GetControlRotation());
				ammoSlider.SetValues();
			}
		}
		
		//nastaveni pozice lodicky, nesmi se dostat mimo boundaries nebo mimo viewport(to co je videt)
		if (!screenBoundary){
			rigidbody.position = new Vector3
				(
					Mathf.Clamp(position.x, boundary.xMin, boundary.xMax),
					0.0f,
					Mathf.Clamp(position.z, boundary.zMin, boundary.zMax)
					);
		}
		else{
			rigidbody.position = new Vector3
				( //ziskaji se okraje obrazovky podle viewportu
				 Mathf.Clamp(position.x,  Camera.main.ViewportToWorldPoint(new Vector3(0.0f, 0.0f, 0.0f)).x+1, Camera.main.ViewportToWorldPoint(new Vector3(1.0f, 0.0f, 0.0f)).x-1),
				 0.0f,
				 Mathf.Clamp(position.z, Camera.main.ViewportToWorldPoint(new Vector3(0.0f, 0.0f, 0.0f)).z+1, Camera.main.ViewportToWorldPoint(new Vector3(0.0f, 1.0f, 0.0f)).z-1)
				 );
			
			transform.position = rigidbody.position;
		}



		///////-------------------------------strelba a pauzovani hry
		if (!gameController.UsingLeap ()){
			if (Input.GetButton("Fire1") && (Time.time > nextFire)){
				FireShot();
			}
			
			if (Input.GetKeyDown(KeyCode.Space) && !gameController.IsColorPicking())
				gameController.PauseGame ();
			/*
			if (Input.GetKeyDown (KeyCode.Escape) && !gameController.IsPause())
				gameController.ColorPicking ();
			
			if (Input.GetKeyDown (KeyCode.G)) {
				gameController.colorPicker.SelectDown();
			}*/
		}
		else if(gameController.UsingLeap ()){
			if (!leapController.ControllingHandInView() && !leapController.SecondaryHandInView()){
				//provadet to jenom jednou tzn pokud jeste neni pauza, odpauzovat..etc
				gameController.PauseOnlyGame();
				gameController.ColorPickingOff();

			}
			//TODO: COLORPICKING ASI ZRUSIT
			/*
			else if (leapController.SecondaryHandInView()){
				//gameController.UnpauseOnlyGame();
				gameController.PauseOnlyGame();
				GameObject colorpicker = gameController.ColorPickingOn();	
				leapController.DrawOnPalm(leapController.GetSecondaryHand(), colorpicker);					
			}
			*/
			else if(leapController.ControllingHandInView()){
				//gameController.UnpauseOnlyGame();
				//gameController.ColorPickingOff();
				if (leapController.ControllingHandGrab() == 1.0f){
					gameController.PauseOnlyGame();
				}

				///verze hry hovering
				if (gameController.IsHoveringMethod() && (Time.time > nextFire)){
					float angle = leapController.AngleBetweenFingers(leapController.GetControllingHand());
					if (angle < fireAngle){
						FireShot();
					}
				}
			}
		}

	}

	///verze hry s clickanim
	void OnFingerClick(){
		if (gameController.IsClickingMethod () && gameObject.activeSelf) {
			FireShot();
		}
	}
	

	void FixedUpdate()
	{

	}

	public void Activate(){
		//zobrazi hrace a vyrobi mu ammoslider
		if(gameController.UsingLeap() && ammoSlider == null)
			ammoSlider = (AmmoSlider) Instantiate (ammoSliderPrefab);
		gameObject.SetActive (true);
	}

	public void Deactivate(){
		if(gameController.UsingLeap() && ammoSlider != null)
			Destroy (ammoSlider.gameObject);
		gameObject.SetActive (false);
	}
}
