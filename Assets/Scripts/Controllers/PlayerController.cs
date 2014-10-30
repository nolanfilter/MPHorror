using UnityEngine;
using System.Collections;

public class PlayerController : Photon.MonoBehaviour {

	public enum State
	{
		Normal = 0,
		Monster = 1,
		Dead = 2,
		Voyeur = 3,
	}
	private State currentState = State.Normal;

	private bool isZoomingIn = false;

	private float currentFear = 1f;
	private float currentSanity = 1f;

	private float fearIncreaseRate = 0.005f;
	private float fearAttack = 0.3f;
	private float fearAttackTimeBuffer = 2f;
	private float fearAttackLastTime = Time.time;

	//random between 60 and 180 seconds
	private float sanityDecreaseRate = Random.Range( 0.016f, 0.0055f );

	private float fearThreshold = 1f;

	private Rect fearMeterRect;
	private Rect sanityMeterRect;

	private Color fearColor = new Color( 1f, 0.5f, 0f, 1f );
	private Color sanityColor = new Color( 0f, 0.5f, 1f, 1f );

	private bool hasReRandomized = false;

	private float speed = Random.Range( 4f, 6f );
	public Texture2D meterTexture;
	
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
	public float height = 0.925f;
	
 	private Transform cameraTransform;
	private Vector3 cameraPositionOffset = new Vector3( 0f, 1.5f, -1.75f );
	private Vector3 cameraPositionZoomOffset = new Vector3( 0f, 1.5f, -1.5f );
	private float cameraFoV = 60f;
	private float cameraZoomFoV = 30f;
	private Quaternion cameraRotationOffset = Quaternion.Euler( new Vector3( 10f, 0f, 0f ) );
	private float cameraRotationRate = 0.025f;

	private float zoomDuration = 0.1f;
	private float zoomProgress = 0f;
	private float oldZoomProgress;
	private float zoomSpeedScale = 0.5f;
	private float timeZoomedIn = 0f;
	private float timeZoomedThreshold = 0.5f;

	private float lifeLength = 120f;
	private float currentTimeLived = 0f;
	private Rect messageRect;
	private string messageString = "";
	private GUIStyle textStyle;
	private float messageDisplayDuration = 7.5f;

	private NetworkView networkView;

	private Renderer modelRenderer;

	private Light flashlight;
	
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

		modelRenderer = GetComponentInChildren<Renderer>();

		flashlight = GetComponentInChildren<Light>();
	}
	
	void Start()
	{
		messageRect = new Rect( 0f, 0f, Screen.width, Screen.height * 0.1f );

		textStyle = new GUIStyle();
		textStyle.font = FontAgent.GetFont();
		textStyle.normal.textColor = Color.white;
		textStyle.alignment = TextAnchor.MiddleCenter;

		SnapCamera();

		PlayerAgent.RegisterPlayer( this );
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

	void OnDestroy()
	{
		PlayerAgent.UnregisterPlayer( this );
	}
	
	void Update()
	{
		if( photonView.isMine )
		{
			InputMovement();
		}
		else
		{
			SyncedMovement();
		}
	
		SnapCamera();

		if (transform.position.y != height)
			transform.position = new Vector3( transform.position.x, height, transform.position.z );

		if( currentState == State.Normal )
		{
			currentSanity -= sanityDecreaseRate * Time.deltaTime;

			if( currentSanity < 0.25f && !hasReRandomized )
			{
				DoorAgent.RandomizeDoorConnections();
				hasReRandomized = true;
			}

			if( currentSanity < 0f )
				currentSanity = 0f;

			if( currentSanity == 0f )
			{
				ChangeState( (int)State.Monster );
				DisplayMessage( "Kill players to get points!" );
				ChangeColor( new Vector3( 1f, 0f, 0f ) );
				currentFear = 1.25f;
			}

			float deltaFear = fearIncreaseRate * Time.deltaTime;

			if( currentFear < fearThreshold - deltaFear )
				ChangeFear( deltaFear * -1f );
			else if( currentFear < fearThreshold )
				currentFear = fearThreshold;
			
			if( currentFear < 0f )
			{
				ChangeState( (int)State.Dead );
				DisplayMessage( "You're paralyzed with fear!" );
				ChangeColor( new Vector3( 0.5f, 0.5f, 0.5f ) );
				currentFear = 0f;
			}
		}

		currentTimeLived += Time.deltaTime;

		//if( currentTimeLived > lifeLength )
		//	Destroy( gameObject );
	}

	void OnGUI()
	{
		if( photonView.isMine )
		{
			DisplayGUI();
		}
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
			Debug.Log( collider.name );

			DoorController doorController = collider.GetComponent<DoorController>();
			
			Transform fromDoorTransform = null;
			
			if( doorController )
			{
				fromDoorTransform = DoorAgent.GetToDoorTransform( doorController.getUniqueID() );
			}
			
			if( fromDoorTransform )
			{
				TeleportTo( fromDoorTransform.position + fromDoorTransform.forward * 1.25f );
				transform.LookAt( transform.position + fromDoorTransform.forward, Vector3.up );

				SnapCamera();
			}
		}
	}
	
	private void InputMovement()
	{
		if( currentState == State.Dead )
			return;

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
			float fearFactor = Mathf.Clamp01( currentFear );
			float zoomFactor = Mathf.Lerp( 1f, zoomSpeedScale, zoomProgress );

			movementVector = movementVector.normalized * speed * fearFactor * zoomFactor * Time.deltaTime;

			//update player
			characterController.Move( movementVector );
		
			Quaternion movementVectorRotation = Quaternion.LookRotation( movementVector );
			if( Quaternion.Angle( movementVectorRotation, transform.rotation ) <= 120f )
				transform.rotation = Quaternion.Lerp( transform.rotation, movementVectorRotation, cameraRotationRate * fearFactor * zoomFactor );


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
		if( photonView.isMine )
		{
			if( isZoomingIn )
				zoomProgress = Mathf.Clamp01( zoomProgress + Time.deltaTime / zoomDuration );
			else
				zoomProgress = Mathf.Clamp01( zoomProgress - Time.deltaTime / zoomDuration );

			//check for jumps
			if( Mathf.Abs( oldZoomProgress - zoomProgress ) > 0.5f )
				zoomProgress = oldZoomProgress + 0.1f * Mathf.Sign( oldZoomProgress - zoomProgress );

			if( zoomProgress == 1f )
			{
				timeZoomedIn += Time.deltaTime;
			}
			else
			{
				timeZoomedIn = 0f;
			}

			Vector3 clippingOffset = transform.TransformPoint( cameraPositionOffset );

			RaycastHit[] hits;

			float longestDistance = 0f;

			hits = Physics.RaycastAll( clippingOffset, cameraTransform.forward, Mathf.Abs( cameraPositionOffset.z ) );

			for( int i = 0; i < hits.Length; i++ )
				if( hits[i].distance > longestDistance )
					longestDistance = hits[i].distance;
	
			clippingOffset += cameraTransform.forward * longestDistance;

			cameraTransform.position = Vector3.Lerp( clippingOffset, transform.TransformPoint( cameraPositionZoomOffset ), zoomProgress );
			cameraTransform.camera.fieldOfView = Mathf.RoundToInt( Mathf.Lerp( cameraFoV, cameraZoomFoV, zoomProgress ) );
			cameraTransform.rotation = transform.rotation * cameraRotationOffset;

			oldZoomProgress = zoomProgress;
		}
	}

	private void DisplayGUI()
	{
		if( currentState == State.Normal )
		{
			GUI.color = fearColor;
			GUI.DrawTexture( new Rect( 0f, Screen.height * currentFear, Screen.width * 0.05f, Screen.height * ( 1f- currentFear ) ), meterTexture );

			GUI.color = sanityColor;
			GUI.DrawTexture( new Rect( Screen.width * 0.95f, Screen.height * ( 1f - currentSanity ), Screen.width * 0.05f, Screen.height * currentSanity  ), meterTexture );
		}

		GUI.color = Color.white;
		GUI.Label( messageRect, messageString, textStyle );
	}

	private void ToggleFlashlight()
	{
		if( flashlight == null )
			ChangeFlashlight( 0 );
		else
			ChangeFlashlight( flashlight.enabled ? 0 : 1 );
	}

	//coroutines
	private IEnumerator DoDisplayMessage( string messageToDisplay )
	{
		messageString = messageToDisplay;

		yield return new WaitForSeconds( messageDisplayDuration );

		messageString = "";
	}

	//end coroutines

	//server calls
	[RPC] void ChangeColor( Vector3 color )
	{
		if( modelRenderer != null )
			modelRenderer.material.color = new Color( color.x, color.y, color.z, 1f );
		
		if( photonView.isMine )
			photonView.RPC( "ChangeColor", PhotonTargets.OthersBuffered, color );
	}

	[RPC] void ChangeState( int state )
	{
		currentState = (State)state;
		
		if( photonView.isMine )
			photonView.RPC( "ChangeState", PhotonTargets.OthersBuffered, state );
	}

	[RPC] void ChangeZoom( int zoom )
	{
		isZoomingIn = ( zoom == 1 );
		
		if( photonView.isMine )
			photonView.RPC( "ChangeZoom", PhotonTargets.OthersBuffered, zoom );
	}

	[RPC] void ChangeFlashlight( int state )
	{
		if( flashlight )
			flashlight.enabled = ( state == 1 );

		if( photonView.isMine )
			photonView.RPC( "ChangeFlashlight", PhotonTargets.OthersBuffered, state );
	}
	// end server calls

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

		case InputController.ButtonType.Zoom:
		{
			ChangeZoom( 1 );
		} break;

		case InputController.ButtonType.Flashlight: 
		{
			ToggleFlashlight();
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
		switch( button )
		{
			case InputController.ButtonType.Zoom:
			{
				ChangeZoom( 0 );
			} break;
		}

	}
	//end event handlers

	//public functions
	public State GetCurrentState()
	{
		return currentState;
	}

	//has the player been fully zoomed in for at least timeZoomedThreshold seconds
	public bool IsZoomedIn()
	{
		return ( timeZoomedIn > timeZoomedThreshold );
	}

	public void ChangeSanity( float amount )
	{
		currentSanity += amount;
	}

	public bool ChangeFear( float amount )
	{
		bool wasAlive = ( currentFear > 0f );

		if( amount > 0f )
		{
			if( Time.time - fearAttackLastTime > fearAttackTimeBuffer )
			{
				currentFear -= amount;
				fearAttackLastTime = Time.time;
			}
		}
		else
		{
			currentFear -= amount;
		}

		return wasAlive && ( currentFear <= 0f );
	}

	public void DecreaseSanity()
	{
		ChangeSanity( sanityDecreaseRate * -2f * Time.deltaTime );
	}

	public void IncreaseSanity()
	{
		ChangeSanity( sanityDecreaseRate * 0.3f * Time.deltaTime );
	}

	public bool IncreaseFear()
	{
		return ChangeFear( fearAttack );
	}

	public void SetFlashlightTo( bool on )
	{
		if( flashlight == null )
			ChangeFlashlight( 0 );
		else
			ChangeFlashlight( ( on ? 1 : 0 ) );
	}

	public void TeleportTo( Vector3 coordinate )
	{
		transform.position = coordinate;
		transform.position = new Vector3( transform.position.x, height, transform.position.z );
	}

	public void IncrementPoint()
	{
		DisplayMessage( "You got 1 point!" );
	}

	public void DisplayMessage( string messageToDisplay )
	{
		StopCoroutine( "DoDisplayMessage" );
		StartCoroutine( "DoDisplayMessage", messageToDisplay );
	}

	public void Escape()
	{
		ChangeState( (int)State.Voyeur );
		DisplayMessage( "Escape! You got 5 points!" );

		foreach( Transform child in transform )
			child.gameObject.SetActive( false );
	}
	//end public functions
}
