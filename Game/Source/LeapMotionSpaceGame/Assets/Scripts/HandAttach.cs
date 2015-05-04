using UnityEngine;
using System.Collections;

using Leap;

public class HandAttach : MonoBehaviour {
	//*depreciated*//

	public LeapController leapController;

//	Controller controller = new Controller ();
	

	// Update is called once per frame
	void Update () {

		leapController.DrawOnPalm (leapController.GetControllingHand (), gameObject);

		/*
		Frame frame = controller.Frame ();

		if (!frame.Hands.IsEmpty){
			Hand hand = frame.Hands.Frontmost;

			//transform.localEulerAngles = new Vector3 (90, -(((hand.Direction.Yaw * 180) / Mathf.PI) + 90), 0);
			//transform.localEulerAngles = new Vector3 ((((hand.Direction.Pitch * 180) / Mathf.PI)), 0, 0);
			//transform.localEulerAngles = new Vector3 (0, 0, (((hand.PalmNormal.Roll * 180) / Mathf.PI)));
			transform.localEulerAngles = new Vector3 ((((hand.Direction.Pitch * 180) / Mathf.PI)), -(((hand.Direction.Yaw * 180) / Mathf.PI)), (((hand.PalmNormal.Roll * 180) / Mathf.PI)));
		}
		*/
	
	}
}
