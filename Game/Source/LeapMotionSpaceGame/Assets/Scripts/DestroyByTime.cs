using UnityEngine;
using System.Collections;

public class DestroyByTime : MonoBehaviour {
	//*zničení objektu po dané časové době*//

	private ParticleSystem ps;
	
	
	public void Start() 
	{
		ps = GetComponent<ParticleSystem>();
	}
	
	public void Update() 
	{
		if(ps)
		{
			if(!ps.IsAlive())
			{
				Destroy(gameObject);
			}
		}
	}
}
