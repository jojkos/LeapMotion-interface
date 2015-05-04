using UnityEngine;
using System.Collections;

public class TutorialPlayerController : MonoBehaviour {
	/*ovladač tutorialové lodičky*/
	
	public Transform shotSpawn;
	public GameObject shot;
	public float playerShift;
	public float fireRate;
	private float nextFire = 0.0f;
	public int fireAngle;

	public LeapController leapController;

	public Tutorial tutorial;	

	bool canShoot = false;

	private GameController gameController;

	void Awake(){
		GameObject gameControllerObject = GameObject.FindWithTag ("GameController");
		if (gameControllerObject != null) {
			gameController = gameControllerObject.GetComponent<GameController>();
		}				
		if (gameController == null) {
			Debug.Log("Cannot find GameController script/object");
		}	
		leapController.OnFingerClick += OnFingerClick;
	}

	void FireShot(){
		if(canShoot){
			Instantiate (shot, shotSpawn.position, shotSpawn.rotation); 
			nextFire = (Time.time + fireRate);
		}
	}

	void OnFingerClick(){
		if (gameController.IsClickingMethod () && gameObject.activeSelf) {
			FireShot();
			tutorial.ClickShot();
		}
	}

	public void ActivateShooting(){
		canShoot = true;
	}

	public void DeactivateShooting(){
		canShoot = false;
	}

	void Update()
	{	
		if (leapController.ControllingHandInView() && !tutorial.shipTried) {
			tutorial.shipTried = true;
		}

		///////--------------------------------------------poloha/pohyb
		Vector3 position = new Vector3(0, 0, 0);


		Vector3 controlPosition = Camera.main.ScreenToWorldPoint(leapController.GetControlScreenPosition());
		
		rigidbody.rotation = leapController.GetControlRotation();
		transform.rotation = leapController.GetControlRotation();
		
		//position = new Vector3 (controlPosition.x, controlPosition.y, controlPosition.z + playerShift); //aby lod byla kus pred prstem, TAKHLE NE
		position = controlPosition + transform.forward * playerShift;

		rigidbody.position = new Vector3
			( //ziskaji se okraje obrazovky podle viewportu
			 Mathf.Clamp(position.x,  Camera.main.ViewportToWorldPoint(new Vector3(0.0f, 0.0f, 0.0f)).x+1, Camera.main.ViewportToWorldPoint(new Vector3(1.0f, 0.0f, 0.0f)).x-1),
			 0.0f,
			 Mathf.Clamp(position.z, Camera.main.ViewportToWorldPoint(new Vector3(0.0f, 0.0f, 0.0f)).z+1, Camera.main.ViewportToWorldPoint(new Vector3(0.0f, 1.0f, 0.0f)).z-1)
			 );
		
		transform.position = rigidbody.position;
	

					
		///verze hry hovering
		if (leapController.ControllingHandInView() && gameController.IsHoveringMethod() && (Time.time > nextFire)){
			float angle = leapController.AngleBetweenFingers(leapController.GetControllingHand());
			if (angle < fireAngle){
				FireShot();
				tutorial.PointShot();
			}
		}

	
		
	}

}
