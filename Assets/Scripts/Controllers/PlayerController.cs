using UnityEngine;
using System.Collections;

public class PlayerController : Photon.MonoBehaviour {
	
	public float speed;
	
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;
	private Quaternion syncStartRotation = Quaternion.identity;
	private Quaternion syncEndRotation = Quaternion.identity;
	
	private InputController inputController;
	private CharacterController characterController;
	
	private Vector3 movementVector;
	private float height = 0.68f;
	
 	private Transform cameraTransform;
	private Vector3 cameraPositionOffset = new Vector3( 0f, 1.5f, -2.5f );
	private Quaternion cameraRotationOffset = Quaternion.Euler( new Vector3( 10f, 0f, 0f ) );
	private float cameraRotationRate = 1f;

	private float lifeLength = 120f;
	private float currentTimeLived = 0f;
	private Rect timerRect;
	private GUIStyle textStyle;

	private NetworkView networkView;
	
	void Awake()
	{
		inputController = GetComponent<InputController>();
		
		if( inputController == null )
		{
			Debug.LogError( "No input controller." );
			enabled = false;
		}

		characterController = GetComponent<CharacterController>();
		
		if( characterController == null )
		{
			Debug.LogError( "No character controller." );
			enabled = false;
		}
		
		cameraTransform = Camera.main.transform;
		
		networkView = GetComponent<NetworkView>();
	}
	
	void Start()
	{
		timerRect = new Rect( 0f, 0f, Screen.width, Screen.height * 0.1f );

		textStyle = new GUIStyle();
		textStyle.font = FontAgent.GetFont();
		textStyle.normal.textColor = Color.white;
		textStyle.alignment = TextAnchor.MiddleCenter;

		SnapCamera();
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
		if( networkView != null && networkView.enabled )
		{
			if( photonView.isMine )
			{
				InputMovement();
			}
			else
			{
				SyncedMovement();
			}
		}
		else
		{
			InputMovement();
		}

		currentTimeLived += Time.deltaTime;

		if( currentTimeLived > lifeLength )
			Destroy( gameObject );
	}

	void OnGUI()
	{
		int timeLeft = Mathf.RoundToInt( lifeLength - currentTimeLived );

		GUI.Label( timerRect, "" + timeLeft, textStyle );
	}

	void OnTriggerEnter( Collider collider )
	{
		CheckForDoor( collider );
	}
	
	void OnTriggerStay( Collider collider )
	{
		CheckForDoor( collider );
	}

	void OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
	{
		if( stream.isWriting )
		{
			stream.SendNext( transform.position );
			stream.SendNext( transform.rotation );
		}
		else
		{
			syncEndPosition = (Vector3)stream.ReceiveNext();
			syncStartPosition = transform.position;
			
			syncEndRotation = (Quaternion)stream.ReceiveNext();
			syncStartRotation = transform.rotation;
			
			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;
		}
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

				SnapCamera();
			}
		}
	}
	
	private void InputMovement()
	{
		/*
		Vector3 adjustedPlayerPosition = new Vector3( transform.position.x, cameraTransform.position.y, transform.position.z );
		
		Vector3 projectedVector = Vector3.Project( adjustedPlayerPosition - cameraTransform.position, cameraTransform.right );
		
		float distanceBetweenPlayerAndCamera = Vector3.Distance( adjustedPlayerPosition, cameraTransform.position + projectedVector  );

		float playerScreenPositionX = cameraTransform.camera.WorldToScreenPoint( transform.position ).x / Screen.width;

		if( playerScreenPositionX < 0.3f )
			cameraTransform.RotateAround( cameraTransform.position, Vector3.up, cameraRotationRate * -1f );
		
		if( playerScreenPositionX > 0.7f )
			cameraTransform.RotateAround( cameraTransform.position, Vector3.up, cameraRotationRate );
		*/

		if( movementVector != Vector3.zero )
		{
			movementVector = movementVector.normalized * speed * Time.deltaTime;

			//update player
			characterController.Move( movementVector );

			Quaternion movementVectorRotation = Quaternion.LookRotation( movementVector );
			if( Quaternion.Angle( movementVectorRotation, transform.rotation ) <= 120f )
				transform.rotation = Quaternion.Lerp( transform.rotation, movementVectorRotation, 0.1f );


			//update camera		
			//if( distanceBetweenPlayerAndCamera > 5f || distanceBetweenPlayerAndCamera < 2.5f )
			//	cameraTransform.position += movementVector;
		}
		else
		{
			//if( distanceBetweenPlayerAndCamera > 5f )
			//	cameraTransform.position += ( adjustedPlayerPosition - cameraTransform.position ).normalized * speed * Time.deltaTime * 0.1f;

			//if( distanceBetweenPlayerAndCamera < 2.5f )
			//	cameraTransform.position -= ( adjustedPlayerPosition - cameraTransform.position ).normalized * speed * Time.deltaTime * 0.1f;
		}

		//cameraTransform.position = Vector3.Lerp( cameraTransform.position, transform.TransformPoint( cameraPositionOffset ), 0.1f );
		//cameraTransform.rotation = Quaternion.Lerp( cameraTransform.rotation, transform.rotation * cameraRotationOffset, 0.1f );

		SnapCamera();

		movementVector = Vector3.zero;
	}
	
	private void SyncedMovement()
	{
		syncTime += Time.deltaTime;
		
		float lerp = syncTime / syncDelay;
		
		if( Vector3.Distance( syncStartPosition, syncEndPosition ) < 2f )
		{
			transform.position = Vector3.Lerp( syncStartPosition, syncEndPosition, lerp );
		}
		else
		{
			transform.position = syncEndPosition;
			syncStartPosition = transform.position;
		}
		
		if( Quaternion.Angle( syncStartRotation, syncEndRotation ) < 90f )
		{
			transform.rotation = Quaternion.Lerp( syncStartRotation, syncEndRotation, lerp );
		}
		else
		{
			transform.rotation = syncEndRotation;
			syncStartRotation = transform.rotation;
		}
	}

	private void SnapCamera()
	{
		cameraTransform.position = transform.TransformPoint( cameraPositionOffset );
		cameraTransform.rotation = transform.rotation * cameraRotationOffset;
	}
	
	//event handlers
	private void OnButtonDown( InputController.ButtonType button )
	{	
		switch( button )
		{
		case InputController.ButtonType.Left:
		{	
			movementVector -= cameraTransform.right;
		} break;
			
		case InputController.ButtonType.Right: 
		{
			movementVector += cameraTransform.right;
		} break;
			
		case InputController.ButtonType.Up: 
		{
			movementVector += Quaternion.AngleAxis( cameraTransform.eulerAngles.x * -1f, cameraTransform.right ) * cameraTransform.forward;
		} break;
			
		case InputController.ButtonType.Down: 
		{
			movementVector -= Quaternion.AngleAxis( cameraTransform.eulerAngles.x * -1f, cameraTransform.right ) * cameraTransform.forward;
		} break;
		}
	}
	
	private void OnButtonHeld( InputController.ButtonType button )
	{	
		switch( button )
		{
		case InputController.ButtonType.Left:
		{	
			movementVector -= cameraTransform.right;
		} break;
			
		case InputController.ButtonType.Right: 
		{
			movementVector += cameraTransform.right;
		} break;
			
		case InputController.ButtonType.Up: 
		{
			movementVector += Quaternion.AngleAxis( cameraTransform.eulerAngles.x * -1f, cameraTransform.right ) * cameraTransform.forward;
		} break;
			
		case InputController.ButtonType.Down: 
		{
			movementVector -= Quaternion.AngleAxis( cameraTransform.eulerAngles.x * -1f, cameraTransform.right ) * cameraTransform.forward;
		} break;
		}
	}
	
	private void OnButtonUp( InputController.ButtonType button )
	{
		
	}
	//end event handlers
}
