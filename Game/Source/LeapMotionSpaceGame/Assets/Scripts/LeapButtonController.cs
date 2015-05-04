using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using UnityEngine.EventSystems;

public class LeapButtonController : MonoBehaviour {
	//*tlačítka menu ovladatelné pomocí gest a pohybu Leap Motionu*//

	RectTransform rt;
	Rect rect;
	Rect worldRect;
	bool hovering = false;
	Button thisButton;
	float afterSelectDelay = 1; //cas po jak dlouhe dobe se tlacitko muze znova zacit vybirat ukazovanim po poslednim vybrani nejakeho tlacitka
	public LoadingImage loadingCircle;
	public LeapController leapController;
	private GameController gameController;


	public enum PermanentSelectMethod{ChosenFromMenu, Hovering, Clicking};
	public PermanentSelectMethod selectMethod;

	static bool someClicked = false; //zamek, at se neklikne dvakrat zaraz;
	

	Vector3 leftTopCorner;
	Canvas canvas;
	

	void RefreshWorldRect(){
		/*pri zmeneni velikosti okna se zmeni pozice a velikost tlacitek*/

		//ziskam jeho souradnice ve worldPoints
		Vector3[] corners = new Vector3[4];
		//rt.GetWorldCorners (corners);
		rt.GetWorldCorners (corners);
		//pro zjisteni sirky a vysky tlacitka
		rect = rt.rect;
				
		if (canvas.renderMode == UnityEngine.RenderMode.ScreenSpaceCamera)
			leftTopCorner = Camera.main.WorldToScreenPoint (corners [0]);//pro screen space -camera
		
		//z nich potrebuju pouze levy horni roh pro vytvoreni Rect
		worldRect = new Rect (leftTopCorner[0], leftTopCorner[1], rect.width * canvas.scaleFactor, rect.height * canvas.scaleFactor);
	}
	
	
	void Start () {	

		//zapisu se do odberu kliknuti prstem do ovladaci rovinny
		leapController.OnFingerClick += OnFingerClick;
		//kdyz se prst zvedne (prestane click/pressed)
		leapController.OnFingerRelease += OnFingerRelease;

		//ziskam RectTransform objektu
		rt = GetComponent<RectTransform> ();

		//ziskam nadrazeny canvas
		Transform parent = transform.parent;
		while (true){
			canvas = parent.GetComponent<Canvas> ();
			if (canvas != null)
				break;
			parent = parent.parent;
		}

		RefreshWorldRect ();

		//ziskam odkaz na tlacitko
		thisButton = GetComponent<Button>();

		GameObject gameControllerObject = GameObject.FindWithTag ("GameController");
		if (gameControllerObject != null) {
			gameController = gameControllerObject.GetComponent<GameController>();
		}				
		if (gameController == null) {
			Debug.Log("Cannot find GameController script/object");
		}


	}

	void CheckHovering(){
		/*zjisteni jestli je prst nad tlacitkem (ma se pustit timer pro vyber tlacitka pri ukazovani)*/

		Vector3 controllerPos = leapController.GetControlScreenPosition ();
		Vector3 pos = Camera.main.ScreenToWorldPoint(controllerPos);

		var pointer = new PointerEventData(EventSystem.current); // pointer event for Execute
		
		RefreshWorldRect ();
		if (worldRect.Contains (controllerPos) && !hovering) {
			if (Mathf.Abs((loadingCircle.LastSelected() - Time.realtimeSinceStartup)) >= afterSelectDelay){ //aby se omylem nevybiralo v menu dalsi polozka hned po tom co se neco vybere
				hovering = true;
				loadingCircle.Initiate(pos);
			}
		}
		else if(!worldRect.Contains (controllerPos) && hovering){
			hovering = false;
			loadingCircle.EarlyStop();
			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.deselectHandler); //odoznaci kdyz se miri jinam
		}
		if (worldRect.Contains (controllerPos)){
			loadingCircle.ChangePosition(pos);
		}
		
		if (worldRect.Contains (controllerPos) && loadingCircle.HasFinished()){
			//thisButton.Select();
			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.selectHandler); //oznaci kdyz je kliknuto
			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler); //nasledne klikne
		}	
	}


	void OnFingerClick(){
		/*pri kliknuti se provede klik, pokud uzivatel kliknul do prostoru tlacitka*/

		if(selectMethod == PermanentSelectMethod.Clicking || (gameController.IsClickingMethod() && selectMethod == PermanentSelectMethod.ChosenFromMenu) && !gameController.IsColorPicking()){
			var pointer = new PointerEventData(EventSystem.current); // pointer event for Execute
			Vector2 controllerPos = leapController.GetControlScreenPosition ();

			if (worldRect.Contains (controllerPos) && transform.parent.gameObject.activeSelf) {//jen pokud je zobrazene
				//thisButton.Select();
				someClicked = true;
				ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.selectHandler); //oznaci kdyz je kliknuto
				ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler); //oznaci kdyz je kliknuto
	
			}					
		}
	}
	
	void OnFingerRelease(){
		/*ukonceni kliknuti - odznaceni tlacitka*/

		if(selectMethod == PermanentSelectMethod.Clicking || gameController.IsClickingMethod() && !gameController.IsColorPicking()){
			var pointer = new PointerEventData(EventSystem.current); // pointer event for Execute

			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.deselectHandler); //odznaci kdyz se zvedne prst

		}
	}


	// Update is called once per frame
	void Update () {
		if (gameController.UsingLeap() && !gameController.IsColorPicking() && leapController.ControllingHandInView()) {//PlayerLeapController je active
			if (selectMethod == PermanentSelectMethod.Hovering)
				CheckHovering();
			else if(gameController.IsHoveringMethod() && selectMethod == PermanentSelectMethod.ChosenFromMenu)
				CheckHovering();
		}

	}

}
