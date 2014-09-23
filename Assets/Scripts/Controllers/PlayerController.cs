using UnityEngine;
using System.Collections;

public class PlayerController : Photon.MonoBehaviour {

	public float speed;

	private InputController inputController;

	private Vector3 movementVector;
	private float height = 0.5f;

	private Transform cameraTransform;
	private Vector3 cameraPositionOffset = new Vector3( 0f, 1.5f, -2.5f );
	private Quaternion cameraRotationOffset = Quaternion.Euler( new Vector3( 10f, 0f, 0f ) );

	void Awake()
	{
		inputController = GetComponent<InputController>();

		if( inputController == null )
		{
			Debug.LogError( "No input controller." );
			enabled = false;
		}

		cameraTransform = Camera.main.transform;
	}

	void OnEnable()
	{
		inputController.OnButtonDown += OnButtonDown;
		inputController.OnButtonHeld += OnButtonHeld;
		inputController.OnButtonUp += OnButtonUp;
	}
	
	void OnDisable()
	{
		inputController.OnButtonDown -= OnButtonDown;
		inputController.OnButtonHeld -= OnButtonHeld;
		inputController.OnButtonUp -= OnButtonUp;
	}

	void Update()
	{
		InputMovement();
	}

	void OnTriggerEnter( Collider collider )
	{
		CheckForDoor( collider );
	}

	void OnTriggerStay( Collider collider )
	{
		CheckForDoor( collider );
	}

	private void CheckForDoor( Collider collider )
	{
		if( collider.tag == "Door" )
		{
			DoorController doorController = collider.GetComponent<DoorController>();
			
			Transform fromDoorTransform = null;
			
			if( doorController )
			{
				fromDoorTransform = DoorAgent.GetToDoorTransform( doorController.getUniqueID() );
			}
			
			if( fromDoorTransform )
			{
				transform.position = fromDoorTransform.position + fromDoorTransform.forward * 2.5f;
				transform.position = new Vector3( transform.position.x, height, transform.position.z );
				transform.LookAt( transform.position + fromDoorTransform.forward, Vector3.up );
			}
		}
	}

	private void InputMovement()
	{
		if( movementVector != Vector3.zero )
		{
			movementVector = movementVector.normalized * speed * Time.deltaTime;
			
			transform.position += movementVector;

			transform.rotation = Quaternion.Lerp( transform.rotation, Quaternion.LookRotation( movementVector ), 0.1f );
		}

		cameraTransform.position = transform.TransformPoint( cameraPositionOffset );
		cameraTransform.rotation = transform.rotation * cameraRotationOffset;
		
		movementVector = Vector3.zero;
	}

	//event handlers
	private void OnButtonDown( InputController.ButtonType button )
	{	
		switch( button )
		{
			case InputController.ButtonType.Left:
			{	
				movementVector -= transform.right;
			} break;
				
			case InputController.ButtonType.Right: 
			{
				movementVector += transform.right;
			} break;
				
			case InputController.ButtonType.Up: 
			{
				movementVector += transform.forward;
			} break;
				
			case InputController.ButtonType.Down: 
			{
				movementVector -= transform.forward;
			} break;
		}
	}
	
	private void OnButtonHeld( InputController.ButtonType button )
	{	
		switch( button )
		{
			case InputController.ButtonType.Left:
			{	
				movementVector -= transform.right;
			} break;
				
			case InputController.ButtonType.Right: 
			{
				movementVector += transform.right;
			} break;
				
			case InputController.ButtonType.Up: 
			{
				movementVector += transform.forward;
			} break;
				
			case InputController.ButtonType.Down: 
			{
				movementVector -= transform.forward;
			} break;
		}
	}
	
	private void OnButtonUp( InputController.ButtonType button )
	{

	}
	//end event handlers
}
