using UnityEngine;
using System.Collections;

public class DestroyByContact : MonoBehaviour {

	public GameObject explosion;
	public GameObject playerExplosion;

	public int scoreValue;
	private GameController gameController;

	void Start(){
		GameObject gameControllerObject = GameObject.FindWithTag ("GameController");
		if (gameControllerObject != null) {
			gameController = gameControllerObject.GetComponent<GameController>();
		}				
		if (gameController == null) {
			Debug.Log("Cannot find GameController script/object");
		}
	}
		
	void OnTriggerEnter(Collider other) {
		//print (this.tag + "," + other.tag );
		//vnejsi hranice nevybouchne pri srazce, stejne tak ostatni hazardy taky ne
		if (other.tag == "Boundary" || other.tag == "Hazard" || other.tag == "AmmoCrate" || other.tag == "EnemyBolt") {
			return;
		}

		Destroy (this.gameObject);


		//hazardy nici hrace, bonusy ne
		if (this.tag == "Hazard" && other.tag == "Bolt"){
			Destroy(other.gameObject);
			gameController.AddScore (scoreValue);
			Instantiate (explosion, transform.position, transform.rotation); 
		}

		if ((this.tag == "Hazard" || this.tag == "EnemyBolt") && other.tag == "Player") {
			Instantiate (playerExplosion, other.transform.position, other.transform.rotation); 
			gameController.GameOver ();
		}

		if (this.tag == "AmmoCrate" && other.tag == "Bolt") {			
			Instantiate (explosion, transform.position, transform.rotation); 
			Destroy(other.gameObject);
		}
		if (this.tag == "AmmoCrate" && other.tag == "Player") {
			gameController.AddAmmo(scoreValue);
			Instantiate (playerExplosion, other.transform.position, other.transform.rotation); 
		}

	}
	
}
