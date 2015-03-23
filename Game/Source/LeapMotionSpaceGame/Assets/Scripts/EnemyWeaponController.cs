using UnityEngine;
using System.Collections;

public class EnemyWeaponController : MonoBehaviour {
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
