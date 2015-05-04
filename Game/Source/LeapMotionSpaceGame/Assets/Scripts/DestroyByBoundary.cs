using UnityEngine;
using System.Collections;

public class DestroyByBoundary : MonoBehaviour {
	//*zničení objektu v případě překročení hranic*//
	void OnTriggerExit(Collider other) {
		Destroy (other.gameObject);
	}
}
