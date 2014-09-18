using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public float speed;

	private InputController inputController;

	private Vector3 movementVector;

	private Transform cameraTransform;
	private Vector3 cameraPositionOffset = new Vector3( 0f, 1.5f, -2.5f );
	private Quaternion cameraRotationOffset = Quaternion.Euler( new Vector3( 15f, 0f, 0f ) );

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
		if( movementVector != Vector3.zero )
		{
			movementVector = movementVector.normalized * speed * Time.deltaTime;

			transform.position += movementVector;
			transform.rotation = Quaternion.Lerp( transform.rotation, Quaternion.LookRotation( movementVector ), 0.1f );

			cameraTransform.position = transform.TransformPoint( cameraPositionOffset );
			cameraTransform.rotation = transform.rotation * cameraRotationOffset;
		}

		movementVector = Vector3.zero;
	}

	void OnTriggerEnter( Collider collider )
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
				transform.position = fromDoorTransform.position + fromDoorTransform.forward * 1.5f;
				transform.LookAt( transform.position + fromDoorTransform.forward, Vector3.up );
			}
		}
	}

	//event handlers
	private void OnButtonDown( InputController.ButtonType button )
	{	
		switch( button )
		{
			case InputController.ButtonType.Left:
			{	
				movementVector += Vector3.left;
			} break;
				
			case InputController.ButtonType.Right: 
			{
				movementVector += Vector3.right;
			} break;

			case InputController.ButtonType.Up: 
			{
				movementVector += Vector3.forward;
			} break;

			case InputController.ButtonType.Down: 
			{
				movementVector += Vector3.back;
			} break;
		}
	}
	
	private void OnButtonHeld( InputController.ButtonType button )
	{	
		switch( button )
		{
			case InputController.ButtonType.Left:
			{	
				movementVector += Vector3.left;
			} break;
				
			case InputController.ButtonType.Right: 
			{
				movementVector += Vector3.right;
			} break;
				
			case InputController.ButtonType.Up: 
			{
				movementVector += Vector3.forward;
			} break;
				
			case InputController.ButtonType.Down: 
			{
				movementVector += Vector3.back;
			} break;
		}
	}
	
	private void OnButtonUp( InputController.ButtonType button )
	{

	}
	//end event handlers
	
	/*
	public float speed = 10f;
	
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;

	void Awake()
	{
		lastSynchronizationTime = Time.time;
	}

	void Update()
	{
		if( networkView.isMine )
		{
			InputMovement();
			InputColorChange();
		}
		else
		{
			SyncedMovement();
		}
	}

	private void InputMovement()
	{
		if( Input.GetKey( KeyCode.UpArrow ) )
			rigidbody.MovePosition( rigidbody.position + Vector3.forward * speed * Time.deltaTime );

		if( Input.GetKey( KeyCode.DownArrow ) )
			rigidbody.MovePosition( rigidbody.position - Vector3.forward * speed * Time.deltaTime );

		if( Input.GetKey( KeyCode.RightArrow ) )
			rigidbody.MovePosition( rigidbody.position + Vector3.right * speed * Time.deltaTime );

		if( Input.GetKey( KeyCode.LeftArrow ) )
			rigidbody.MovePosition( rigidbody.position - Vector3.right * speed * Time.deltaTime );
	}

	void OnSerializeNetworkView( BitStream stream, NetworkMessageInfo info )
	{
		Vector3 syncPosition = Vector3.zero;
		Vector3 syncVelocity = Vector3.zero;
		if (stream.isWriting)
		{
			syncPosition = rigidbody.position;
			stream.Serialize(ref syncPosition);
			
			syncPosition = rigidbody.velocity;
			stream.Serialize(ref syncVelocity);
		}
		else
		{
			stream.Serialize(ref syncPosition);
			stream.Serialize(ref syncVelocity);
			
			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;
			
			syncEndPosition = syncPosition + syncVelocity * syncDelay;
			syncStartPosition = rigidbody.position;
		}
	}

	private void SyncedMovement()
	{
		syncTime += Time.deltaTime;
		rigidbody.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
	}

	private void InputColorChange()
	{
		if( Input.GetKeyDown( KeyCode.C ) )
			ChangeColorTo( new Vector3( Random.value, Random.value, Random.value ) );
	}

	[RPC] void ChangeColorTo( Vector3 color )
	{
		renderer.material.color = new Color( color.x, color.y, color.z, 1f );

		if( networkView.isMine )
			networkView.RPC( "ChangeColorTo", RPCMode.Others, color );
	}
	*/
}
