using UnityEngine;
using System.Collections;

public class CopyMainCamera : MonoBehaviour {

	void Awake()
	{
		if( camera == null )
			enabled = false;
	}


	void LateUpdate () {
	
		transform.position = Camera.main.transform.position;
		transform.rotation = Camera.main.transform.rotation;

		camera.fieldOfView = Camera.main.fieldOfView;
	}
}
