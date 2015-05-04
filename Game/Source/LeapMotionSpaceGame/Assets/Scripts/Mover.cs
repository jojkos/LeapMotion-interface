using UnityEngine;
using System.Collections;

public class Mover : MonoBehaviour {	
	//*pohyb herních objektů*//

	public float speed;

	void Start()
	{
		transform.position = new Vector3 (transform.position.x, 0.0f, transform.position.z);

		rigidbody.rotation = Quaternion.Euler(0.0f, rigidbody.rotation.eulerAngles.y, 0.0f);
		transform.rotation = Quaternion.Euler(0.0f, rigidbody.rotation.eulerAngles.y, 0.0f);

		rigidbody.velocity = transform.forward  * speed;
	}
}
