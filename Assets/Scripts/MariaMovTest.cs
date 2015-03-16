using UnityEngine;
using System.Collections;

public class MariaMovTest : Photon.MonoBehaviour {

	public float rotationSpeed;
	public float playerXspeed;
	Vector3 pos;
	
	public float moveZ;
	public float moveX;
	
	public GameObject mannequin;
	public GameObject monsterCamera;

	int noise = 1;
	
	
	
	
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
		moveZ = Input.GetAxis("Horizontal") * playerXspeed;
		moveX = Input.GetAxis("Vertical") * playerXspeed;
		
		moveZ *= Time.deltaTime;
		moveX *= Time.deltaTime;
		transform.Translate(moveX, 0, moveZ*-1);
		
		float rotationY = Input.GetAxis("RHorizontal") * rotationSpeed;
		rotationY *= Time.deltaTime;
		transform.Rotate (0, rotationY, 0);


		if (Input.GetButtonDown("X_Button")){
			//Debug.Log("x");
			if (noise == 1){noise = -1;} else {noise = 1;}
			monsterCamera.SendMessage("NoiseToggle",noise);
		}

		if (Input.GetButtonDown("B_Button")){
			//Debug.Log("b");
			monsterCamera.SendMessage("Stun");
		}

		if (Input.GetButton("A_Button")){
			Debug.Log("a down");
			monsterCamera.SendMessage("Zoom",true);
		} else {
			Debug.Log("a up");
			monsterCamera.SendMessage("Zoom",false);
		}



		
	}
}