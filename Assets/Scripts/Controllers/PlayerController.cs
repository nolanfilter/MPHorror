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

	//random between 120 and 240 seconds
	private float sanityDecreaseRate = Random.Range( 1f/120f, 1f/240f );

	private float fearThreshold = 1f;

	private Rect fearMeterRect;
	private Rect sanityMeterRect;

	private Color fearColor = new Color( 1f, 0.5f, 0f, 1f );
	private Color sanityColor = new Color( 0f, 0.5f, 1f, 1f );

	private bool hasReRandomized = false;

	private float speed = Random.Range( 4.5f, 6.5f );
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
	private Quaternion cameraRotationOffset = Quaternion.Euler( new Vector3( 0f, 0f, 0f ) );
	private float cameraRotationRate = 0.025f;

	private Vector3 clippingOffset = Vector3.zero;
	private Vector3 zoomOffset = Vector3.zero;

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
	
	//new movement variables
	private Vector3 moveDirection = Vector3.zero;
	private bool movingBack = false;
	private bool isMoving = false;
	private float lockCameraTimer = 0.0f;
	private float walkSpeed = 0f;
	private float trotSpeed = 0f;
	private float runSpeed = 0f;
	private float speedSmoothing = 10f;
	private float rotateSpeed = 500f;
	private float trotAfterSeconds = 3f;
	private float moveSpeed = 0f;
	private float walkTimeStart = 0f;
	private float lockCameraTimeout = 2f;
	private float angularSmoothLag = 0.3f;
	private float angularMaxSpeed = 15.0f;
	private float snapSmoothLag = 0.2f;
	private float snapMaxSpeed = 720.0f;
	private float clampHeadPositionScreenSpace = 0.75f;
	private Vector3 headOffset = Vector3.zero;
	private Vector3 centerOffset = Vector3.zero;
	private float angleVelocity = 0.0f;
	private bool snap = false;
	private float distance = 1.25f;

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

		moveDirection = transform.TransformDirection(Vector3.forward);
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

		walkSpeed = speed * 0.5f;
		trotSpeed = speed;
		runSpeed = speed * 1.5f;

		SnapCamera();

		PlayerAgent.RegisterPlayer( this );
	}
	
	void OnEnable()
	{
		inputController.OnButtonDown += OnButtonDown;
		inputController.OnButtonHeld += OnButtonHeld;
		inputController.OnButtonUp += OnButtonUp;

		centerOffset = Vector3.up * 1.5f;
		headOffset = centerOffset;
		//headOffset.y = characterController.bounds.max.y - transform.position.y;
		
		
		Cut(transform, centerOffset);
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

	void LateUpdate()
	{
		Apply(transform, Vector3.zero);
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


		// Forward vector relative to the camera along the x-z plane	
		Vector3 forward = cameraTransform.TransformDirection(Vector3.forward);
		forward.y = 0;
		forward = forward.normalized;
		
		// Right vector relative to the camera
		// Always orthogonal to the forward vector
		Vector3 right = new Vector3(forward.z, 0, -forward.x);

		movementVector = inputController.getRawAxes();

		// Are we moving backwards or looking backwards
		if (movementVector.y < -0.2f)
			movingBack = true;
		else
			movingBack = false;
		
		bool wasMoving = isMoving;
		isMoving = Mathf.Abs(movementVector.x) > 0.1f || Mathf.Abs(movementVector.y) > 0.1f;
		
		// Target direction relative to the camera
		Vector3 targetDirection = movementVector.x * right + movementVector.y * forward;

		// Lock camera for short period when transitioning moving & standing still
		lockCameraTimer += Time.deltaTime;
		if (isMoving != wasMoving)
			lockCameraTimer = 0.0f;
		
		// We store speed and direction seperately,
		// so that when the character stands still we still have a valid forward direction
		// moveDirection is always normalized, and we only update it if there is user input.
		if (targetDirection != Vector3.zero)
		{
			// If we are really slow, just snap to the target direction
			if (moveSpeed < walkSpeed * 0.9f)
			{
				moveDirection = targetDirection.normalized;
			}
			// Otherwise smoothly turn towards it
			else
			{
				moveDirection = Vector3.RotateTowards(moveDirection, targetDirection, rotateSpeed * Mathf.Deg2Rad * Time.deltaTime, 1000);
				
				moveDirection = moveDirection.normalized;
			}
		}

		// Smooth the speed based on the current target direction
		float curSmooth = speedSmoothing * Time.deltaTime;
		
		// Choose target speed
		//* We want to support analog input but make sure you cant walk faster diagonally than just forward or sideways
		float targetSpeed = Mathf.Min(targetDirection.magnitude, 1.0f);

		// Pick speed modifier
		if (Input.GetKey(KeyCode.LeftShift) | Input.GetKey(KeyCode.RightShift))
		{
			targetSpeed *= runSpeed;
		}
		else if (Time.time - trotAfterSeconds > walkTimeStart)
		{
			targetSpeed *= trotSpeed;
		}
		else
		{
			targetSpeed *= walkSpeed;
		}
		
		moveSpeed = Mathf.Lerp(moveSpeed, targetSpeed, curSmooth);
		
		// Reset walk time start when we slow down
		if (moveSpeed < walkSpeed * 0.3f)
			walkTimeStart = Time.time;

		Vector3 movement = moveDirection * moveSpeed;
		movement *= Time.deltaTime;
		
		characterController.Move(movement);

		if( zoomProgress == 1f )
			moveDirection = zoomOffset.normalized;

		transform.rotation = Quaternion.LookRotation(moveDirection);

		//nolan code
		/*movementVector = movementVector.normalized * speed * fearFactor * zoomFactor * Time.deltaTime;

		characterController.Move( movementVector );
		
		if( movementVector != Vector3.zero )
		{
			Quaternion movementVectorRotation = Quaternion.LookRotation( movementVector );

			if( Quaternion.Angle( movementVectorRotation, transform.rotation ) <= 150f )
				transform.rotation = Quaternion.Lerp( transform.rotation, movementVectorRotation, cameraRotationRate * fearFactor * zoomFactor );
		}
		*/

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

			if( zoomProgress != 0f )
				lockCameraTimer = 0f;

			zoomOffset = Vector3.Lerp( Vector3.zero, Quaternion.AngleAxis( cameraTransform.eulerAngles.y, Vector3.up ) * Vector3.forward * 1.25f, zoomProgress );

			cameraTransform.position += zoomOffset;
			cameraTransform.camera.fieldOfView = Mathf.RoundToInt( Mathf.Lerp( cameraFoV, cameraZoomFoV, zoomProgress ) );

			if( flashlight != null )
			{
				RaycastHit hit;

				Quaternion newFlashlightRotation = cameraTransform.rotation;

				if( Physics.Raycast( cameraTransform.position + cameraTransform.forward * 1.5f, cameraTransform.forward, out hit ) )
				{
					Vector3 hitPosition = cameraTransform.position + cameraTransform.forward * ( hit.distance + 1.5f );

					newFlashlightRotation = Quaternion.LookRotation( hitPosition - flashlight.transform.position );
				}

				float percent = Mathf.Clamp01( Quaternion.Angle( flashlight.transform.rotation, newFlashlightRotation ) / 90f );

				flashlight.transform.rotation = Quaternion.Lerp( flashlight.transform.rotation, newFlashlightRotation, percent );
			}
			
			oldZoomProgress = zoomProgress;
			oldViewChangeVector = viewChangeVector;

			return;

			//if( )
			//{
				if( !movingBack )
					clippingOffset = transform.TransformPoint( cameraPositionOffset );
				else
					clippingOffset = transform.TransformPoint( new Vector3( cameraPositionOffset.x, cameraPositionOffset.y, cameraPositionOffset.z * -1f ) );

				RaycastHit[] hits;

				float longestDistance = 0f;

				hits = Physics.RaycastAll( clippingOffset, cameraTransform.forward, Mathf.Abs( cameraPositionOffset.z ) );

				for( int i = 0; i < hits.Length; i++ )
					if( hits[i].distance > longestDistance )
						longestDistance = hits[i].distance;
		
				clippingOffset += cameraTransform.forward * longestDistance;

				if( !movingBack )
					zoomOffset = transform.TransformPoint( cameraPositionZoomOffset );
				else
					zoomOffset = transform.TransformPoint( new Vector3( cameraPositionZoomOffset.x, cameraPositionZoomOffset.y, cameraPositionZoomOffset.z * -1f ) );
			//}
					                                   
			cameraTransform.position = Vector3.Lerp( clippingOffset, zoomOffset, zoomProgress );
			cameraTransform.camera.fieldOfView = Mathf.RoundToInt( Mathf.Lerp( cameraFoV, cameraZoomFoV, zoomProgress ) );

			if( !movingBack )
				cameraTransform.rotation = transform.rotation * cameraRotationOffset;

			if( viewChangeVector != Vector2.zero )
			{
				//cameraTransform.eulerAngles = new Vector3( cameraTransform.eulerAngles.x - viewChangeVector.y * 15f, cameraTransform.eulerAngles.y + viewChangeVector.x * 30f, 0f );	
				//cameraTransform.eulerAngles = new Vector3( cameraTransform.eulerAngles.x - viewChangeVector.y * 30f, cameraTransform.eulerAngles.y + viewChangeVector.x * 180f, 0f );	
			}

			/*
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
			*/
		}
	}

	private float AngleDistance(float a, float b)
	{
		a = Mathf.Repeat(a, 360);
		b = Mathf.Repeat(b, 360);
		
		return Mathf.Abs(b - a);
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
		return;

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

	//camera junk
	void Apply(Transform dummyTarget, Vector3 dummyCenter)
	{
		Vector3 targetCenter = transform.position + centerOffset;
		Vector3 targetHead = transform.position + headOffset;
		
		//	DebugDrawStuff();
		
		// Calculate the current & target rotation angles
		float originalTargetAngle = transform.eulerAngles.y;
		float currentAngle = cameraTransform.eulerAngles.y;
		
		// Adjust real target angle when camera is locked
		float targetAngle = originalTargetAngle;
		
		if (snap)
		{
			// We are close to the target, so we can stop snapping now!
			if (AngleDistance(currentAngle, originalTargetAngle) < 3.0f)
				snap = false;
			
			currentAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref angleVelocity, snapSmoothLag, snapMaxSpeed);
		}
		// Normal camera motion
		else
		{
			if (lockCameraTimer < lockCameraTimeout)
			{
				targetAngle = currentAngle;
			}
			
			// Lock the camera when moving backwards!
			// * It is really confusing to do 180 degree spins when turning around.
			if (AngleDistance(currentAngle, targetAngle) > 160 && movingBack)
				targetAngle += 180;
			
			currentAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref angleVelocity, angularSmoothLag, angularMaxSpeed);
		}
		

		// Damp the height
		float currentHeight = transform.position.y + height;
		
		// Convert the angle into a rotation, by which we then reposition the camera
		Quaternion currentRotation = Quaternion.Euler(0, currentAngle, 0);
		
		// Set the position of the camera on the x-z plane to:
		// distance meters behind the target
		cameraTransform.position = targetCenter;
		cameraTransform.position += currentRotation * Vector3.back * distance;
		
		// Set the height of the camera
		cameraTransform.position = new Vector3(cameraTransform.position.x, currentHeight, cameraTransform.position.z);

		SnapCamera();
		
		// Always look at the target	
		SetUpRotation(targetCenter, targetHead);
	}

	void Cut(Transform dummyTarget, Vector3 dummyCenter)
	{
		float oldSnapMaxSpeed = snapMaxSpeed;
		float oldSnapSmooth = snapSmoothLag;
		
		snapMaxSpeed = 10000;
		snapSmoothLag = 0.001f;

		snap = true;
		Apply(transform, Vector3.zero);
		
		snapMaxSpeed = oldSnapMaxSpeed;
		snapSmoothLag = oldSnapSmooth;
	}
	
	void SetUpRotation(Vector3 centerPos, Vector3 headPos)
	{
		// Now it's getting hairy. The devil is in the details here, the big issue is jumping of course.
		// * When jumping up and down we don't want to center the guy in screen space.
		//  This is important to give a feel for how high you jump and avoiding large camera movements.
		//   
		// * At the same time we dont want him to ever go out of screen and we want all rotations to be totally smooth.
		//
		// So here is what we will do:
		//
		// 1. We first find the rotation around the y axis. Thus he is always centered on the y-axis
		// 2. When grounded we make him be centered
		// 3. When jumping we keep the camera rotation but rotate the camera to get him back into view if his head is above some threshold
		// 4. When landing we smoothly interpolate towards centering him on screen
		Vector3 cameraPos = cameraTransform.position - zoomOffset;
		Vector3 offsetToCenter = centerPos - cameraPos;
		
		// Generate base rotation only around y-axis
		Quaternion yRotation = Quaternion.LookRotation(new Vector3(offsetToCenter.x, 0, offsetToCenter.z));
		
		Vector3 relativeOffset = Vector3.forward * distance + Vector3.down * height;
		cameraTransform.rotation = yRotation * Quaternion.LookRotation(relativeOffset);
		
		// Calculate the projected center position and top position in world space
		Ray centerRay = cameraTransform.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1));
		Ray topRay = cameraTransform.camera.ViewportPointToRay(new Vector3(0.5f, clampHeadPositionScreenSpace, 1));
		
		Vector3 centerRayPos = centerRay.GetPoint(distance);
		Vector3 topRayPos = topRay.GetPoint(distance);
		
		float centerToTopAngle = Vector3.Angle(centerRay.direction, topRay.direction);
		
		float heightToAngle = centerToTopAngle / (centerRayPos.y - topRayPos.y);
		
		float extraLookAngle = heightToAngle * (centerRayPos.y - centerPos.y);
		if (extraLookAngle < centerToTopAngle)
		{
			extraLookAngle = 0;
		}
		else
		{
			extraLookAngle = extraLookAngle - centerToTopAngle;
			cameraTransform.rotation *= Quaternion.Euler(-extraLookAngle, 0, 0);
		}

		float xAngleOffset = cameraRotationOffset.eulerAngles.x;

		//nolan junk
		if( viewChangeVector != Vector2.zero )
		{
			if( zoomProgress == 0f )
			{
				cameraRotationOffset *= Quaternion.AngleAxis( viewChangeVector.y * -30f, Vector3.right );
				cameraTransform.rotation *= Quaternion.AngleAxis( viewChangeVector.x * 90f, Vector3.up );
			}
			else
			{
				cameraRotationOffset *= Quaternion.AngleAxis( viewChangeVector.y * -15f, Vector3.right );
				cameraTransform.rotation *= Quaternion.AngleAxis( viewChangeVector.x * 45f, Vector3.up );
			}

			xAngleOffset = cameraRotationOffset.eulerAngles.x;

			//cameraTransform.eulerAngles = new Vector3( cameraTransform.eulerAngles.x - viewChangeVector.y * 30f, cameraTransform.eulerAngles.y + viewChangeVector.x * 30f, 0f );	
			viewChangeVector = Vector2.zero;

			lockCameraTimer = 0f;
		}

		if( zoomProgress == 0f )
		{
			if( xAngleOffset > 30f && xAngleOffset < 180f )
				xAngleOffset = 30f;
			
			if( xAngleOffset > 180f && xAngleOffset < 330f )
				xAngleOffset = 330f;
		}
		else
		{
			if( xAngleOffset > 40f && xAngleOffset < 180f )
				xAngleOffset = 40f;

			if( xAngleOffset > 180f && xAngleOffset < 355f )
				xAngleOffset = 355f;
		}

		cameraRotationOffset.eulerAngles = new Vector3( xAngleOffset, cameraRotationOffset.eulerAngles.y, cameraRotationOffset.eulerAngles.z );
		cameraTransform.rotation *= cameraRotationOffset;

		cameraTransform.eulerAngles = new Vector3 (cameraTransform.eulerAngles.x, cameraTransform.eulerAngles.y, 0f);

	}
	
	Vector3 GetCenterOffset()
	{
		return centerOffset;
	}
	//end camera junk
}
