using UnityEngine;
using System.Collections;

public class CopyMainCamera : MonoBehaviour {

	void Awake()
	{
		if( GetComponent<Camera>() == null )
			enabled = false;
	}


	void LateUpdate () {
	
		transform.position = Camera.main.transform.position;
		transform.rotation = Camera.main.transform.rotation;

		GetComponent<Camera>().fieldOfView = Camera.main.fieldOfView;
	}
}
