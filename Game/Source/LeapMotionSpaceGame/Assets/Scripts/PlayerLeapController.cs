/*using UnityEngine;
using System.Collections;

using UnityEngine.EventSystems;
using UnityEngine.UI;

using OpenCvSharp;
using SimpleJSON;
using Leap;



public class PlayerLeapController : MonoBehaviour {
	
	public float speed;
	public Boundary boundary;
	public float tilt;

	public enum HandType{RightHand, LeftHand};
	public HandType controllingHand;
	
	public Transform shotSpawn;
	public GameObject shot;
	public float fireRate;
	private float nextFire = 0.0f;
	private GameController gameController;

	private Frame frame;

	EventSystem eventSystem;


	//LEAP
	Controller controller = new Controller ();
	
	CvMat rvec = new CvMat (1, 3, MatrixType.F64C1);
	CvMat tvec = new CvMat (1, 3, MatrixType.F64C1);
	CvMat mtx = new CvMat (3, 3, MatrixType.F64C1);
	CvMat dist = new CvMat (1, 5, MatrixType.F64C1);
	CvMat imagePoints = new CvMat (1, 2, MatrixType.F64C1);
	
	CvMat objectPoints = new CvMat (1, 3, MatrixType.F64C1);
	
	int calwidth; //sirka pouzita pri kalibraci
	int calheight; //vyska pouzita pri kalibraci
	

	void Start(){
		//TRY EXCEPT NAKA CHYBA KDYZ NENI SPRAVNY INPUTS
		string inputs = System.IO.File.ReadAllText(Application.dataPath + "/inputs.txt");
		var parsed = JSON.Parse (inputs);
		
		calwidth = parsed ["width"].AsInt;
		calheight = parsed ["height"].AsInt;
		
		rvec [0] = parsed ["rvecs"] [0][0].AsDouble;
		rvec [1] = parsed ["rvecs"] [1][0].AsDouble;
		rvec [2] = parsed ["rvecs"] [2][0].AsDouble;
		
		tvec [0] = parsed ["tvecs"] [0][0].AsDouble;
		tvec [1] = parsed ["tvecs"] [1][0].AsDouble;
		tvec [2] = parsed ["tvecs"] [2][0].AsDouble;
		
		
		mtx [0] = parsed ["mtx"] [0][0].AsDouble;
		mtx [1] = parsed ["mtx"] [0][1].AsDouble;
		mtx [2] = parsed ["mtx"] [0][2].AsDouble;
		mtx [3] = parsed ["mtx"] [1][0].AsDouble;
		mtx [4] = parsed ["mtx"] [1][1].AsDouble;
		mtx [5] = parsed ["mtx"] [1][2].AsDouble;
		mtx [6] = parsed ["mtx"] [2][0].AsDouble;
		mtx [7] = parsed ["mtx"] [2][1].AsDouble;
		mtx [8] = parsed ["mtx"] [2][2].AsDouble;
		
		dist [0] = parsed ["dist"] [0][0].AsDouble;
		dist [1] = parsed ["dist"] [0][1].AsDouble;
		dist [2] = parsed ["dist"] [0][2].AsDouble;
		dist [3] = parsed ["dist"] [0][3].AsDouble;
		dist [4] = parsed ["dist"] [0][4].AsDouble;

		GameObject gameControllerObject = GameObject.FindWithTag ("GameController");
		if (gameControllerObject != null) {
			gameController = gameControllerObject.GetComponent<GameController>();
		}				
		if (gameController == null) {
			Debug.Log("Cannot find GameController script/object");
		}

		eventSystem = EventSystem.current; //pro ovladani slideru a jinych selecatables
		frame = controller.Frame ();//poprve tady a pak v kazdym updatu

		controller.Config.SetFloat("Gesture.Swipe.MinLength", 100.0f);
		controller.Config.SetFloat("Gesture.Swipe.MinVelocity", 1000f);
		controller.Config.Save();
		controller.EnableGesture (Gesture.GestureType.TYPESWIPE); //mozna to potom presunout do update, jen kdyz je potreba aby bylo zaply
	}

	Finger GetFinger(Hand hand, Finger.FingerType fingerType = Finger.FingerType.TYPE_INDEX){
		foreach (Finger finger in hand.Fingers) {
			if (finger.Type() == fingerType){
				return finger;
			}
		}	
		return null;
	}

	Vector GetFingerJointPosition(Hand hand, Finger.FingerType fingerType = Finger.FingerType.TYPE_INDEX, Bone.BoneType boneType = Bone.BoneType.TYPE_PROXIMAL){		
		FingerList fingers = hand.Fingers;
		Finger resFinger;
		Bone resBone;
		Vector position = Vector.Zero;
		
		foreach (Finger finger in fingers) {
			if (finger.Type() == fingerType){
				resFinger = finger;
				resBone = finger.Bone(boneType);
				position = resBone.Center;
				break;
			}
		}
		
		return position;
	}

	Vector GetFingerTipPosition(Hand hand, Finger.FingerType fingerType = Finger.FingerType.TYPE_INDEX){		
		FingerList fingers = hand.Fingers;
		Finger indexFinger;
		Vector position = Vector.Zero;
		//Vector position = null;
		
		foreach (Finger finger in fingers) {
			if (finger.Type() == fingerType){
				indexFinger = finger;
				position = indexFinger.TipPosition;
				break;
			}
		}

		return position;
	}

	//body z prostoru leapu prevede do bodu v obrazovce
	Vector3 ProjectPointsToScreen(Vector position){
		//zezvrchu
		objectPoints [0] = position.x;
		objectPoints [1] = position.z;
		objectPoints [2] = position.y;

		Cv.ProjectPoints2 (this.objectPoints, this.rvec, this.tvec, this.mtx, this.dist, this.imagePoints);
		
		float x = (float)imagePoints [0];
		float y = (float)imagePoints [1];

		y = this.calheight - y; //jiny pocatek (0,0)
		
		x = x * UnityEngine.Screen.width / this.calwidth;
		y = y * UnityEngine.Screen.height / this.calheight;

		return new Vector3 (x, y, 10); //JAKE Z? zalezi na tom?
	}

	Vector3 ProjectFingerTipToGame(Hand hand, Finger.FingerType fingerType = Finger.FingerType.TYPE_INDEX){
		Vector position = GetFingerTipPosition (hand, fingerType);
		Vector3 screenpos = ProjectPointsToScreen (position);
		Vector3 retpos = Camera.main.ScreenToWorldPoint(screenpos);
		return retpos;

	}



	void DrawOnPalm(Hand hand, GameObject gameobject){
		Vector position = hand.PalmPosition;

		Vector3 screenpos = ProjectPointsToScreen (position);
		Vector3 pos = Camera.main.ScreenToWorldPoint(screenpos);
		gameobject.transform.position = new Vector3(pos.x, 0, pos.z);

		Vector positionLeft = GetFingerJointPosition (hand, Finger.FingerType.TYPE_INDEX, Bone.BoneType.TYPE_PROXIMAL);
		//positionLeft.x -= hand.PalmWidth/2;
		Vector3 screenposLeft = ProjectPointsToScreen (positionLeft);
		//Vector3 posLeft = Camera.main.ScreenToWorldPoint(screenposLeft);

		Vector positionRight = GetFingerJointPosition (hand, Finger.FingerType.TYPE_MIDDLE, Bone.BoneType.TYPE_PROXIMAL);

		Vector3 screenposRight = ProjectPointsToScreen (positionRight);
		Vector3 posRight = Camera.main.ScreenToWorldPoint(screenposRight);

		//print (position+","+positionLeft+","+positionRight);

		float resWidth = (pos-posRight).magnitude;
		RectTransform rt = gameobject.GetComponent<RectTransform>();


		float scaling = resWidth/rt.rect.width;
		gameobject.transform.localScale = new Vector3(scaling, scaling, 0);

		//gameobject.transform.localEulerAngles = new Vector3 (90, -(((hand.Direction.Yaw * 180) / Mathf.PI) + 90), 0);
	}

	float AngleBetweenFingers(Hand hand, Finger.FingerType fingerType1 = Finger.FingerType.TYPE_THUMB, Finger.FingerType fingerType2 = Finger.FingerType.TYPE_INDEX){
		Finger finger1 = null;
		Finger finger2 = null;

		foreach (Finger finger in hand.Fingers) {
			if(finger.Type() == fingerType1)
				finger1 = finger;
			else if(finger.Type() == fingerType2)
				finger2 = finger;
		}

		if(finger1 == null || finger2 == null)
			return -1;

		float angle = finger1.Direction.AngleTo(finger2.Direction);
		return angle*180/Mathf.PI;
	}

	void FireShot(){
		if ((Time.time > nextFire) && !gameController.IsPause() && !gameController.IsColorPicking()){
			if (!gameController.AmmoEmpty()){
				Instantiate (shot, shotSpawn.position, shotSpawn.rotation);
				nextFire = (Time.time + fireRate);
				gameController.SpendAmmo();
			}
		}
	}
	


	void Update()
	{
		frame = controller.Frame (); //globalni pro vsechny, jeden update, jeden frame
		//nejaka udalost kdyz odejdou ruce pryc je?
		if (frame.Hands.IsEmpty){
			//provadet to jenom jednou tzn pokud jeste neni pauza, odpauzovat..etc
			gameController.PauseOnlyGame();
			gameController.ColorPickingOff();
		}
		else if (frame.Hands.Count == 2 || frame.Hands[0].IsLeft){
			//gameController.UnpauseOnlyGame();
			GameObject colorpicker = gameController.ColorPickingOn();
			foreach(Hand hand in frame.Hands){
				if (hand.IsLeft)
					DrawOnPalm(hand, colorpicker);
			}

		}
		else if(frame.Hands[0].IsRight){
			gameController.UnpauseOnlyGame();
			float angle = AngleBetweenFingers(frame.Hands[0]);
			if (angle < 22){
				FireShot();
			}
		}



		GestureList gestures = frame.Gestures();
		if (!gestures.IsEmpty) {
			SwipeGesture gesture = new SwipeGesture(gestures[0]);
			if (gesture.State == Gesture.GestureState.STATESTOP && gesture.IsValid){
				Vector direction = gesture.Direction;
				//print (direction);
				if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)){ //doprava doleva/nahoru dolu
					//doprava/doleva
					if(direction.x > 0){
						print ("doleva");
						SelectLeft();
					}
					else{
						print ("doprava");
						SelectRight();
					}
				}
				else{
					if (direction.y > 0){ //nahoru dolu
						print ("dolu");
						SelectDown();
					}
					else{
						print ("nahoru");
						SelectUp();
					}
				}

			}
		}


	}

	Selectable CurrentSelectable(){
		return eventSystem.currentSelectedGameObject.GetComponent<Selectable>();
	}
	

	void SelectUp(){
		Selectable selected = CurrentSelectable ();
		if (selected == null)
			return;
		Selectable next = selected.FindSelectableOnUp();
		if (next == null) 
			return;
		next.Select ();
	}
	void SelectDown(){
		Selectable selected = CurrentSelectable ();
		if (selected == null)
			return;
		Selectable next = selected.FindSelectableOnDown();
		if (next == null) 
			return;
		next.Select ();
	}

	void SelectLeft(){
		Selectable selected = CurrentSelectable ();
		if (selected == null)
			return;
		Slider slider = selected.GetComponent<Slider> ();
		if (slider == null) 
			return;
		slider.value -= 1;
	}
	void SelectRight(){
		Selectable selected = CurrentSelectable ();
		if (selected == null)
			return;
		Slider slider = selected.GetComponent<Slider> ();
		if (slider == null) 
			return;
		slider.value += 1;
	}
	
	
	void FixedUpdate()
	{
		Hand hand = null;
		foreach (Hand h in frame.Hands) {
			if (h.IsRight)
				hand = h;
		}

		if (hand != null) {
			Vector3 position = ProjectFingerTipToGame (hand, Finger.FingerType.TYPE_INDEX);
			position = new Vector3 (position.x, position.y, position.z + 0); //aby lod byla kus pred prstem
			rigidbody.position = new Vector3
				(
					Mathf.Clamp(position.x, boundary.xMin, boundary.xMax),
					0.0f,
					Mathf.Clamp(position.z, boundary.zMin, boundary.zMax)
				);
			Finger finger = GetFinger(hand, Finger.FingerType.TYPE_INDEX);
			transform.rotation = Quaternion.Euler(0.0f, 360 - ((float)rvec[1]*180/Mathf.PI + finger.Direction.Yaw*180/Mathf.PI), 0.0f);//360- finger.Direction.Yaw*180/Mathf.PI

			//transform.localEulerAngles = new Vector3 ((((hand.Direction.Pitch * 180) / Mathf.PI)), -(((hand.Direction.Yaw * 180) / Mathf.PI)), (((hand.PalmNormal.Roll * 180) / Mathf.PI)));
		}


	}

	//chovani jako pozice mysi, pro ovladani menu a jinych prvku - ukazovacek pravek ruky (melo by jit nastavit jestli je ovladaci prava nebo leva TODO do settings)
	public Vector3 GetPosition(){
		Hand correctHand = null;
		foreach (Hand hand in frame.Hands) {
			if(controllingHand == HandType.RightHand && hand.IsRight)
				correctHand = hand;
			else if(controllingHand == HandType.LeftHand && hand.IsLeft)
				correctHand = hand;
		}
		//ovladaci ruka neni v obraze
		if (correctHand == null) {
			return new Vector3(0, 0, 0);
		}

		Vector position = GetFingerTipPosition(correctHand);
		Vector3 screenpos = ProjectPointsToScreen(position);
		screenpos = new Vector3(screenpos.x, screenpos.y, 0);
		return screenpos;
	}

	public void ChangeControllingHand(string handtype){
		switch (handtype) {
		case "right": 
			controllingHand = HandType.RightHand;
			break;
		case "left": 
			controllingHand = HandType.LeftHand;
			break;
		default:
			controllingHand = HandType.RightHand;
			break;
		}
	}

	public bool UsingLeap(){
		return enabled;
	}
	
}
*/