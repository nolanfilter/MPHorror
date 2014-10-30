﻿using UnityEngine;
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

	//random between 120 and 240 seconds
	private float sanityDecreaseRate = Random.Range( 1f/120f, 1f/240f );

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
	private Vector2 viewChangeVector;
	private Vector2 oldViewChangeVector;
	private float viewChangeRate = 2.5f;
	private float timeViewChangeStatic;
	private float timeViewChangeStaticThreshold = 1f;

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
	private float timeZoomedInThreshold = 0.5f;

	private float lifeLength = 120f;
	private float currentTimeLived = 0f;
	private Rect messageRect;
	private string messageString = "";
	private GUIStyle textStyle;
	private float messageDisplayDuration = 7.5f;

	private NetworkView networkView;

	public Renderer modelRenderer;

	public Light flashlight;
	
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

		if( modelRenderer == null )
			modelRenderer = GetComponentInChildren<Renderer>();

		if( flashlight == null )
			flashlight = GetComponentInChildren<Light>();
	}
	
	void Start()
	{
		messageRect = new Rect( 0f, 0f, Screen.width, Screen.height * 0.1f );

		textStyle = new GUIStyle();
		textStyle.font = FontAgent.GetFont();
		textStyle.normal.textColor = Color.white;
		textStyle.alignment = TextAnchor.MiddleCenter;

		viewChangeVector = Vector2.zero;
		oldViewChangeVector = viewChangeVector;

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

		float fearFactor = Mathf.Clamp01( currentFear );
		float zoomFactor = Mathf.Lerp( 1f, zoomSpeedScale, zoomProgress );

		movementVector = movementVector.normalized * speed * fearFactor * zoomFactor * Time.deltaTime;

		characterController.Move( movementVector );
		
		if( movementVector != Vector3.zero )
		{
			Quaternion movementVectorRotation = Quaternion.LookRotation( movementVector );

			if( Quaternion.Angle( movementVectorRotation, transform.rotation ) <= 150f )
				transform.rotation = Quaternion.Lerp( transform.rotation, movementVectorRotation, cameraRotationRate * fearFactor * zoomFactor );
		}

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

			if( viewChangeVector != Vector2.zero )
			{
				cameraTransform.eulerAngles = new Vector3( cameraTransform.eulerAngles.x - viewChangeVector.y * 15f, cameraTransform.eulerAngles.y + viewChangeVector.x * 30f, 0f );		
			}

			if( viewChangeVector == oldViewChangeVector )
				timeViewChangeStatic += Time.deltaTime;
			else
				timeViewChangeStatic = 0f;               

			if( viewChangeVector != Vector2.zero && timeViewChangeStatic > timeViewChangeStaticThreshold )
			{
				if( viewChangeVector.x < 0f )
					viewChangeVector = new Vector2( Mathf.Clamp( viewChangeVector.x + viewChangeRate * 0.5f * Time.deltaTime, -1f, 0f ), viewChangeVector.y );

				if( viewChangeVector.x > 0f )
					viewChangeVector = new Vector2( Mathf.Clamp( viewChangeVector.x - viewChangeRate * 0.5f * Time.deltaTime, 0f, 1f ), viewChangeVector.y );

				if( viewChangeVector.y < 0f )
					viewChangeVector = new Vector2( viewChangeVector.x, Mathf.Clamp( viewChangeVector.y + viewChangeRate * 0.5f * Time.deltaTime, -1f, 0f ) );

				if( viewChangeVector.y > 0f )
					viewChangeVector = new Vector2( viewChangeVector.x, Mathf.Clamp( viewChangeVector.y - viewChangeRate * 0.5f * Time.deltaTime, 0f, 1f ) );
			}

			if( flashlight != null )
				flashlight.transform.rotation = cameraTransform.rotation;

			oldZoomProgress = zoomProgress;
			oldViewChangeVector = viewChangeVector;
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

	private void EvaluateMovement( InputController.ButtonType button )
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

	private void EvaluateViewChange( InputController.ButtonType button )
	{
		switch( button )
		{
		case InputController.ButtonType.RLeft:
		{	
			viewChangeVector = new Vector2( Mathf.Clamp( viewChangeVector.x - viewChangeRate * Time.deltaTime, -1f, 1f ), viewChangeVector.y );
		} break;
			
		case InputController.ButtonType.RRight: 
		{
			viewChangeVector = new Vector2( Mathf.Clamp( viewChangeVector.x + viewChangeRate * Time.deltaTime, -1f, 1f ), viewChangeVector.y );
		} break;
			
		case InputController.ButtonType.RUp: 
		{
			viewChangeVector = new Vector2( viewChangeVector.x, Mathf.Clamp( viewChangeVector.y + viewChangeRate * Time.deltaTime, -1f, 1f ) );
		} break;
			
		case InputController.ButtonType.RDown: 
		{
			viewChangeVector = new Vector2( viewChangeVector.x, Mathf.Clamp( viewChangeVector.y - viewChangeRate * Time.deltaTime, -1f, 1f ) );
		} break;
		}
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
		EvaluateMovement( button );
		EvaluateViewChange( button );

		switch( button )
		{
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
		EvaluateMovement( button );
		EvaluateViewChange( button );

		switch( button )
		{
		
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

	//has the player been fully zoomed in for at least timeZoomedInThreshold seconds
	public bool IsZoomedIn()
	{
		return ( timeZoomedIn > timeZoomedInThreshold );
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
			if( CanIncreaseFear() )
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

	public bool CanIncreaseFear()
	{
		return ( Time.time - fearAttackLastTime > fearAttackTimeBuffer );
	}
	//end public functions
}
