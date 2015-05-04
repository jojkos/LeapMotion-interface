using UnityEngine;
using System.Collections;

public class EnemyWeaponController : MonoBehaviour {
	//*třída ovládající střelbu nepřátelských lodí*//

	public GameObject shot;
	public Transform shotSpawn;
	public float fireRate;
	public float delay;
	
	void Start () {
		InvokeRepeating ("FireShot", delay, fireRate);
	}


	void FireShot(){
		Instantiate (shot, shotSpawn.position, shotSpawn.rotation); 
	}
}
