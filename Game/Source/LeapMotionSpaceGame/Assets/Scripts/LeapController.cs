using UnityEngine;
using System.Collections;

using UnityEngine.EventSystems;
using UnityEngine.UI;

using OpenCvSharp;
using SimpleJSON;
using Leap;
using System.Runtime.InteropServices;

//TODO nejak vypsat kdyz je vyplej leap, stejne jako kdyz neni inputs soubor a tak!! automaticky zmenit na mys? pridat do settings mys?
//TODO: LEAP NENI PRIPOJENEJ TAK NEPUSTIT HRU NEBO NVM


//BEZ LISTENERU NEFUNGUJI DOBRE GESTA, unity Update neni tak casto jako novy Frame z leapu
public class MyLeapListener : Listener{
	LeapController myLeapController;

	public MyLeapListener(LeapController controller){
		myLeapController = controller;
	}

	public override void OnFrame (Controller controller){
		GestureList gestures = controller.Frame().Gestures();
		if (!gestures.IsEmpty) {
			SwipeGesture gesture = new SwipeGesture(gestures[0]);
			if (gesture.State == Gesture.GestureState.STATE_STOP && gesture.IsValid){
				Vector3 direction = new Vector3(gesture.Direction.x, gesture.Direction.y, gesture.Direction.z);
				myLeapController.ShouldSwipe(direction);
			}

			KeyTapGesture tap = new KeyTapGesture(gestures[0]);
			if (tap.State == Gesture.GestureState.STATE_STOP && tap.IsValid){
				myLeapController.Tap(tap);
			}			
		}
	}




}

public class LeapController : MonoBehaviour {
	public enum HandType{RightHand, LeftHand};
	public HandType controllingHandType;

	public float clickSensitivity;

	EventSystem eventSystem;

	Plane clickPlane;
	bool clickPlaneSet = false;
	
	//LEAP
	Controller controller;
	private Frame frame;
	private Frame lastSeenControlHandFrame;
	
	CvMat rvec = new CvMat (1, 3, MatrixType.F64C1);
	CvMat tvec = new CvMat (1, 3, MatrixType.F64C1);
	CvMat mtx = new CvMat (3, 3, MatrixType.F64C1);
	CvMat dist = new CvMat (1, 5, MatrixType.F64C1);
	CvMat imagePoints = new CvMat (1, 2, MatrixType.F64C1);
	
	CvMat objectPoints = new CvMat (1, 3, MatrixType.F64C1);
	
	int calwidth; //sirka pouzita pri kalibraci
	int calheight; //vyska pouzita pri kalibraci

	// Event Handler
	//pro vysilinani eventu kdyz nastane swipegesto
	public delegate void OnSwipeEvent(Vector3 direction);
	public event OnSwipeEvent OnSwipe;

	public delegate void OnFingerClickEvent();
	public event OnFingerClickEvent OnFingerClick;
	public delegate void OnFingerReleaseEvent();
	public event OnFingerClickEvent OnFingerRelease;


	MyLeapListener leapListener;
	bool shouldSwipe = false;
	bool shouldClick = false;
	Vector3 swipeDirection;

	bool isFile = false;

	bool pressed = false;

	public void Tap(KeyTapGesture gesture){
		Hand hand = gesture.Hands [0];
		if (controllingHandType == HandType.RightHand && hand.IsRight || controllingHandType == HandType.LeftHand && hand.IsLeft) {
			if (gesture.Pointable.IsFinger){
				Finger finger = new Finger(gesture.Pointable);
				if (finger.Type() == Finger.FingerType.TYPE_INDEX){
					shouldClick = true;
				}
			}
		}
		
	}

	public void ShouldSwipe(Vector3 direction){
		if (shouldSwipe == false){
			shouldSwipe = true;
			swipeDirection = direction;
		}
	}

	////ze stranek leapu, pro zruseni zaseknuti na konci hry
	#if UNITY_STANDALONE_WIN
	[DllImport("mono", SetLastError=true)]
	static extern void mono_thread_exit();
	#endif		
	void OnApplicationQuit() {
		controller.RemoveListener (leapListener);
		controller.Dispose();
		leapListener.Dispose();   					//&& UNITY_3_5 //keca to, potreubju to i tady
		#if UNITY_STANDALONE_WIN && !UNITY_EDITOR  
		mono_thread_exit ();
		#endif
	}
	

	void Awake () {
		controller = new Controller ();
		leapListener = new MyLeapListener (this); //listener zpusobuje ze hra nejde zavrit
		controller.AddListener (leapListener);

		string inputs = "";
		//TRY EXCEPT NAKA CHYBA KDYZ NENI SPRAVNY INPUTS
		//TODO nebo to takhle staci?
		try{
			inputs = System.IO.File.ReadAllText(Application.dataPath + "/inputs.txt");
		}
		catch(System.IO.IOException e){

			//Time.timeScale = 0.0f;
			return;
		}
		isFile = true;

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

		
		eventSystem = EventSystem.current; //pro ovladani slideru a jinych selecatables
		frame = controller.Frame ();//poprve tady a pak v kazdym updatu
		lastSeenControlHandFrame = frame;
		
		controller.SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD|Controller.PolicyFlag.POLICY_BACKGROUND_FRAMES);
		/*controller.EnableGesture (Gesture.GestureType.TYPESWIPE); //mozna to potom presunout do update, jen kdyz je potreba aby bylo zaply	
		controller.Config.SetFloat("Gesture.Swipe.MinLength", 60.0f);
		controller.Config.SetFloat("Gesture.Swipe.MinVelocity", 700f);*/
		controller.EnableGesture (Gesture.GestureType.TYPEKEYTAP);
		controller.Config.SetFloat("Gesture.KeyTap.MinDownVelocity", 30.0f);
		controller.Config.SetFloat("Gesture.KeyTap.HistorySeconds", .1f);
		controller.Config.SetFloat("Gesture.KeyTap.MinDistance", 0.5f);
		controller.Config.Save();
	}

	void Update () {
		frame = controller.Frame (); //globalni pro vsechny, jeden update, jeden frame		
		if (shouldSwipe && OnSwipe != null) {
			shouldSwipe = false;
			OnSwipe(swipeDirection);
		}
		if (shouldClick){
			shouldClick = false;
			//OnFingerClick();
		}
		if (clickPlaneSet)
			CheckForClick ();
	}


	public bool IsFile(){
		return isFile;
	}
	
	Finger GetFinger(Hand hand, Finger.FingerType fingerType = Finger.FingerType.TYPE_INDEX){
		foreach (Finger finger in hand.Fingers) {
			if (finger.Type() == fingerType){
				return finger;
			}
		}	
		return null;
	}
	
	public Vector GetFingerJointPosition(Hand hand, Finger.FingerType fingerType = Finger.FingerType.TYPE_INDEX, Bone.BoneType boneType = Bone.BoneType.TYPE_INTERMEDIATE){		
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
		objectPoints [1] = position.y;
		objectPoints [2] = position.z;
		
		Cv.ProjectPoints2 (this.objectPoints, this.rvec, this.tvec, this.mtx, this.dist, this.imagePoints);
		
		float x = (float)imagePoints [0];
		float y = (float)imagePoints [1];
		
		y = this.calheight - y; //jiny pocatek (0,0)
		
		x = x * UnityEngine.Screen.width / this.calwidth;
		y = y * UnityEngine.Screen.height / this.calheight;
		
		return new Vector3 (x, y, 10); //JAKE Z? zalezi na tom?
	}
	
	Vector3 ProjectFingerTipToGame(Hand hand, Finger.FingerType fingerType = Finger.FingerType.TYPE_INDEX){
		//prevede pozici konecku prstu ze souradnic leapu do svetovych souradnich hry
		Vector position = GetFingerTipPosition (hand, fingerType);
		Vector3 screenpos = ProjectPointsToScreen (position);
		Vector3 retpos = Camera.main.ScreenToWorldPoint(screenpos);
		return retpos;
		
	}
	
	
	
	public void DrawOnPalm(Hand hand, GameObject gameobject){
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
		//RectTransform rt = gameobject.GetComponent<RectTransform>();
		
		
		//float scaling = resWidth/rt.rect.width;
		//gameobject.transform.localScale = new Vector3(scaling, scaling, 0);
		
		//gameobject.transform.localEulerAngles = new Vector3 (90,90+ 360 - ((float)rvec[1]*180/Mathf.PI + hand.Direction.Yaw*180/Mathf.PI), 0);
		//gameobject.transform.localEulerAngles = new Vector3 ((float)rvec[0] * hand.Direction.Pitch * 180 / Mathf.PI, 360 - ((float)rvec[1]*180/Mathf.PI + hand.Direction.Yaw*180/Mathf.PI), 0);
		//gameobject.transform.localEulerAngles = new Vector3 (90 + hand.Direction.y*180/Mathf.PI, 0, 0);
		//gameobject.transform.localEulerAngles = new Vector3 ((float)rvec[0] * hand.Direction.Pitch * 180 / Mathf.PI, 0, 0);
	}
	
	public float AngleBetweenFingers(Hand hand, Finger.FingerType fingerType1 = Finger.FingerType.TYPE_THUMB, Finger.FingerType fingerType2 = Finger.FingerType.TYPE_INDEX){
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

	public Hand GetControllingHand(){
		Hand controllingHand = null;
		foreach (Hand hand in frame.Hands) {
			if(controllingHandType == HandType.RightHand && hand.IsRight)
				controllingHand = hand;
			else if(controllingHandType == HandType.LeftHand && hand.IsLeft)
				controllingHand = hand;
		}
		//ovladaci ruka neni v obraze
		if (controllingHand == null){ 
			return null;
		}
		else{
			lastSeenControlHandFrame = frame;
			return controllingHand;
		}
	}

	public Hand GetSecondaryHand(){
		Hand secondaryHand = null;
		foreach (Hand hand in frame.Hands) {
			if(controllingHandType == HandType.RightHand && hand.IsLeft)
				secondaryHand = hand;
			else if(controllingHandType == HandType.LeftHand && hand.IsRight)
				secondaryHand = hand;
		}
		//ovladaci ruka neni v obraze
		if (secondaryHand == null) 
			return null;
		else
			return secondaryHand;
	}

	public bool ControllingHandInView(){
		if(GetControllingHand() == null)
			return false;
		else
			return true;
	}

	public bool SecondaryHandInView(){
		if(GetSecondaryHand() == null)
			return false;
		else
			return true;
	}
	

	public void ChangeControllingHand(string handtype){
		switch (handtype) {
		case "right": 
			controllingHandType = HandType.RightHand;
			break;
		case "left": 
			controllingHandType = HandType.LeftHand;
			break;
		default:
			controllingHandType = HandType.RightHand;
			break;
		}
	}

	//chovani jako pozice mysi, pro ovladani menu a jinych prvku - ukazovacek pravek ruky (melo by jit nastavit jestli je ovladaci prava nebo leva TODO do settings)
	public Vector3 GetControlScreenPosition(){
		Hand controllingHand = GetControllingHand();

		//ovladaci ruka neni v obraze
		if (controllingHand == null) {
			return Camera.main.WorldToScreenPoint(new Vector3(0, 0, 0)); //prostredek, ale lepsi by bylo aby se to drzelo tam odkud vyjela ruka
		}
		
		Vector position = GetFingerTipPosition(controllingHand);
		Vector3 screenpos = ProjectPointsToScreen(position);
		screenpos = new Vector3(screenpos.x, screenpos.y, 0);
		return screenpos;
	}

	public Vector3 GetControlLeapWorldPosition(){
		Hand controllingHand = GetControllingHand ();
		
		//ovladaci ruka neni v obraze
		if (controllingHand == null) {
			return Vector3.zero;
		}
		
		Vector3 points;
		points = VectorToVector3(GetFingerTipPosition(controllingHand));

		return points;
	}

	public Vector3 GetControlJointPosition(){
		Hand controllingHand = GetControllingHand();
		
		//ovladaci ruka neni v obraze
		if (controllingHand == null) {
			return new Vector3(-100, -100, 0);
		}
		
		Vector position = GetFingerJointPosition(controllingHand);
		Vector3 screenpos = ProjectPointsToScreen(position);
		screenpos = new Vector3(screenpos.x, screenpos.y, 0);
		return screenpos;
	}

	public float ControllingHandGrab(){
		Hand controllingHand = GetControllingHand();
		
		//ovladaci ruka neni v obraze
		if (controllingHand == null) {
			return 0.0f;
		}

		return controllingHand.GrabStrength;
	}
	

	public Quaternion GetControlRotation(){
		//natoceni ukazovacku ovladaci ruky prepocitane podle RVEC
		Hand controllingHand = GetControllingHand();

		//ovladaci ruka neni v obraze
		if (controllingHand == null) {
			return Quaternion.Euler(0, 0, 0);
		}

		Finger finger = GetFinger(controllingHand, Finger.FingerType.TYPE_INDEX);
		//return Quaternion.Euler(0.0f, 360 - ((float)rvec[1]*180/Mathf.PI + finger.Direction.Yaw*180/Mathf.PI), 0.0f);
		//zmenil sem to ze leap haze normalne svoje x,y,z coz zmeni rvec pro me asi
		return Quaternion.Euler(0.0f,360 - ((float)rvec[0]*180/Mathf.PI + finger.Direction.Yaw*180/Mathf.PI), 0.0f);
	}

	Vector3 VectorToVector3(Vector vector){
		return new Vector3 (vector.x, vector.y, vector.z);
	}

	public Plane ControllingHandPlane(){
		Hand controllingHand = GetControllingHand ();

		//ovladaci ruka neni v obraze
		if (controllingHand == null) {
			return new Plane(Vector3.zero, Vector3.zero);
		}

		Vector3 a, b, c;
		a = VectorToVector3(GetFingerJointPosition (controllingHand, Finger.FingerType.TYPE_INDEX, Bone.BoneType.TYPE_DISTAL));
		b = VectorToVector3(GetFingerJointPosition (controllingHand, Finger.FingerType.TYPE_MIDDLE, Bone.BoneType.TYPE_DISTAL));
		c = VectorToVector3(GetFingerJointPosition (controllingHand, Finger.FingerType.TYPE_PINKY, Bone.BoneType.TYPE_DISTAL));

		var dir = Vector3.Cross(a - b, c - b);
		var norm = Vector3.Normalize(dir);
		//print (norm.x+","+norm.y+","+norm.z);

		return new Plane(norm, b);
	}

	public void SetClickPlane(Plane plane){
		clickPlane = plane;
		clickPlaneSet = true;
	}

	void CheckForClick(){
		//zjistuje jestli se kliklo prstem v nastavene klikaci rovine
		Hand controllingHand = GetControllingHand ();
		
		//ovladaci ruka neni v obraze
		if (controllingHand == null) {
			return;
		}
		
		Vector3 position = VectorToVector3(GetFingerTipPosition(controllingHand));


		if (Mathf.Abs(clickPlane.GetDistanceToPoint(position)) < clickSensitivity){
			if (!pressed){
				OnFingerClick();
			}
			pressed = true;
		}
		else{
			if (pressed){
				OnFingerRelease();
			}
			pressed = false;
		}
	}


}
