using UnityEngine;
using System.Collections;

public class MannequinController : MonoBehaviour {

	void Start () {
		MannequinAgent.RegisterMannequin( gameObject );
	}
	
	void OnDestroy()
	{
		MannequinAgent.UnregisterMannequin( gameObject );
	}
}
