﻿using UnityEngine;
using System.Collections;

public class PlayerController : Photon.MonoBehaviour {

	public enum State
	{
		Normal = 0,
		Monster = 1,
		Dead = 2,
		Voyeur = 3,
		None = 4,
	}
	private State currentState = State.None;

	private bool isZoomingIn = false;
	private bool hasPhoto = false;

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

	private float height = 0.96f;
	
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

	public GameObject flashQuadPrefab;
	public GameObject screenshotQuadPrefab;

	private Renderer[] modelRenderers = null;

	public Light flashlight;

	private GameObject flashQuad;
	private GameObject screenshotQuad;
	private Color whiteClear = new Color( 1f, 1f, 1f, 0f );

	private bool isWaitingForPhotoFinish = false;

	//new movement variables
	private Vector3 moveDirection = Vector3.zero;
	private bool movingBack = false;
	private bool isMoving = false;
	private float lockCameraTimer = 0.0f;
	private float walkSpeed = 0f;
	private float trotSpeed = 0f;
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

		if( !photonView.isMine )
			inputController.enabled = false;

		characterController = GetComponent<CharacterController>();
		
		if( characterController == null )
		{
			Debug.LogError( "No character controller." );
			enabled = false;
		}
		
		cameraTransform = Camera.main.transform;
		
		networkView = GetComponent<NetworkView>();

		if( modelRenderers == null )
			modelRenderers = GetComponentsInChildren<Renderer>();

		if( flashlight == null )
			flashlight = GetComponentInChildren<Light>();

		moveDirection = transform.TransformDirection(Vector3.forward);

		gameObject.name = "Player " + photonView.viewID;

		if( photonView.isMine )
			gameObject.name += "(Client)";
	}
	
	void Start()
	{
		messageRect = new Rect( 0f, 0f, Screen.width, Screen.height * 0.1f );

		textStyle = new GUIStyle();
		textStyle.font = FontAgent.GetFont();
		textStyle.normal.textColor = Color.white;
		textStyle.alignment = TextAnchor.MiddleCenter;

		if( flashQuadPrefab )
		{
			flashQuad = Instantiate( flashQuadPrefab ) as GameObject;
			flashQuad.renderer.material.color = whiteClear;
		}

		if( screenshotQuadPrefab )
		{
			screenshotQuad = Instantiate( screenshotQuadPrefab ) as GameObject;
			screenshotQuad.renderer.material.color = whiteClear;
		}

		viewChangeVector = Vector2.zero;
		oldViewChangeVector = viewChangeVector;

		walkSpeed = speed * 0.5f;
		trotSpeed = walkSpeed;
		//trotSpeed = speed;

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

		/*
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
		*/
	}

	void LateUpdate()
	{
		if( photonView.isMine )
			Apply(cameraTransform, Vector3.zero);
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
			//Debug.Log( collider.name );

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

		Vector3 forward = cameraTransform.TransformDirection( Vector3.forward );
		forward.y = 0;
		forward = forward.normalized;

		Vector3 right = new Vector3( forward.z, 0, -forward.x );

		if( photonView.isMine )
			movementVector = inputController.getRawAxes();

		movingBack = ( movementVector.y < -0.2f );
		
		bool wasMoving = isMoving;
		isMoving = ( movementVector.magnitude > 0.1f );
		
		Vector3 targetDirection = movementVector.x * right + movementVector.y * forward;

		lockCameraTimer += Time.deltaTime;
		if (isMoving != wasMoving )
			lockCameraTimer = 0.0f;

		if( targetDirection != Vector3.zero )
		{
			if( moveSpeed < walkSpeed * 0.9f )
			{
				moveDirection = targetDirection.normalized;
			}
			else
			{
				moveDirection = Vector3.RotateTowards(moveDirection, targetDirection, rotateSpeed * Mathf.Deg2Rad * Time.deltaTime, 1000);
				
				moveDirection = moveDirection.normalized;
			}
		}

		float curSmooth = speedSmoothing * Time.deltaTime;
		float targetSpeed = Mathf.Min( targetDirection.magnitude, 1.0f );

		if( Time.time - trotAfterSeconds > walkTimeStart )
			targetSpeed *= trotSpeed;
		else
			targetSpeed *= walkSpeed;

		moveSpeed = Mathf.Lerp( moveSpeed, targetSpeed, curSmooth );
		
		if( moveSpeed < walkSpeed * 0.3f )
			walkTimeStart = Time.time;

		Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;

		characterController.Move( movement );

		if( zoomProgress == 1f )
			transform.rotation = Quaternion.LookRotation( zoomOffset.normalized );
		else
			transform.rotation = Quaternion.LookRotation( moveDirection );

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

			if( flashlight != null && currentState != State.Dead )
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
		}
	}

	private void Apply( Transform dummyTarget, Vector3 dummyCenter )
	{
		if( photonView.isMine )
		{
			Vector3 targetCenter = transform.position + centerOffset;
			Vector3 targetHead = transform.position + headOffset;
			
			float originalTargetAngle = transform.eulerAngles.y;
			float currentAngle = cameraTransform.eulerAngles.y;
			
			float targetAngle = originalTargetAngle;
			
			if (snap)
			{
				if (AngleDistance(currentAngle, originalTargetAngle) < 3.0f)
					snap = false;
				
				currentAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref angleVelocity, snapSmoothLag, snapMaxSpeed);
			}
			else
			{
				if (lockCameraTimer < lockCameraTimeout)
					targetAngle = currentAngle;
				
				// Lock the camera when moving backwards
				if (AngleDistance(currentAngle, targetAngle) > 160 && movingBack)
					targetAngle += 180;
				
				currentAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref angleVelocity, angularSmoothLag, angularMaxSpeed);
			}
			
			float currentHeight = transform.position.y + height;
			
			Quaternion currentRotation = Quaternion.Euler(0, currentAngle, 0);
			
			cameraTransform.position = targetCenter;
			cameraTransform.position += currentRotation * Vector3.back * distance;
			
			cameraTransform.position = new Vector3(cameraTransform.position.x, currentHeight, cameraTransform.position.z);
			
			SnapCamera();
			
			SetUpRotation(targetCenter, targetHead);
		}
	}
	
	private void Cut( Transform dummyTarget, Vector3 dummyCenter )
	{
		if( photonView.isMine )
		{
			float oldSnapMaxSpeed = snapMaxSpeed;
			float oldSnapSmooth = snapSmoothLag;
			
			snapMaxSpeed = 10000f;
			snapSmoothLag = 0.001f;
			
			snap = true;
			Apply(cameraTransform, Vector3.zero);
			
			snapMaxSpeed = oldSnapMaxSpeed;
			snapSmoothLag = oldSnapSmooth;
		}
	}
	
	private void SetUpRotation( Vector3 centerPos, Vector3 headPos )
	{
		if( photonView.isMine )
		{
			Vector3 cameraPos = cameraTransform.position - zoomOffset;
			Vector3 offsetToCenter = centerPos - cameraPos;
			
			Quaternion yRotation = Quaternion.LookRotation( new Vector3(offsetToCenter.x, 0f, offsetToCenter.z ) );
			
			Vector3 relativeOffset = Vector3.forward * distance + Vector3.down * height;
			cameraTransform.rotation = yRotation * Quaternion.LookRotation(relativeOffset);
			
			Ray centerRay = cameraTransform.camera.ViewportPointToRay( new Vector3( 0.5f, 0.5f, 1f ) );
			Ray topRay = cameraTransform.camera.ViewportPointToRay( new Vector3( 0.5f, clampHeadPositionScreenSpace, 1f ) );
			
			Vector3 centerRayPos = centerRay.GetPoint( distance );
			Vector3 topRayPos = topRay.GetPoint( distance );
			
			float centerToTopAngle = Vector3.Angle( centerRay.direction, topRay.direction );
			
			float heightToAngle = centerToTopAngle / ( centerRayPos.y - topRayPos.y );
			
			float extraLookAngle = heightToAngle * ( centerRayPos.y - centerPos.y);
			if( extraLookAngle < centerToTopAngle )
			{
				extraLookAngle = 0;
			}
			else
			{
				extraLookAngle = extraLookAngle - centerToTopAngle;
				cameraTransform.rotation *= Quaternion.Euler(-extraLookAngle, 0, 0);
			}
			
			float xAngleOffset = cameraRotationOffset.eulerAngles.x;
			
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
				
				viewChangeVector = Vector2.zero;
				
				lockCameraTimer = 0f;
			}

			if( zoomProgress == 0f )
			{
				if( xAngleOffset > 40f && xAngleOffset < 180f )
					xAngleOffset = 40f;
					
				if( xAngleOffset > 180f && xAngleOffset < 340f )
					xAngleOffset = 340f;
			}
			else
			{
				if( xAngleOffset > 60f && xAngleOffset < 180f )
					xAngleOffset = 60f;
				
				if( xAngleOffset > 180f && xAngleOffset < 320f )
					xAngleOffset = 320f;
			}
			
			cameraRotationOffset.eulerAngles = new Vector3( xAngleOffset, cameraRotationOffset.eulerAngles.y, cameraRotationOffset.eulerAngles.z );
			cameraTransform.rotation *= cameraRotationOffset;
			
			cameraTransform.eulerAngles = new Vector3 (cameraTransform.eulerAngles.x, cameraTransform.eulerAngles.y, 0f);
		}
	}

	private float AngleDistance(float a, float b)
	{
		a = Mathf.Repeat( a, 360 );
		b = Mathf.Repeat( b, 360 );
		
		return Mathf.Abs( b - a );
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

	private IEnumerator TakePhoto()
	{
		if( ( zoomProgress != 1f ) || isWaitingForPhotoFinish || hasPhoto )
			yield break;

		ScreenshotAgent.instance.OnPostRenderFinish += OnScreenshotFinish;
		ScreenshotAgent.Enable();

		isWaitingForPhotoFinish = true;

		while( isWaitingForPhotoFinish )
			yield return null;

		SetFlashlightTo( false );

		hasPhoto = true;

		if( flashQuad )
		{
			yield return StartCoroutine( DoColorFade( flashQuad.renderer.material, Color.white, whiteClear, 0.5f ) );
		}
	
		if( screenshotQuad )
		{
			yield return StartCoroutine( DoColorFade( screenshotQuad.renderer.material, whiteClear, Color.white, 0.25f ) );

			yield return new WaitForSeconds( 1.25f );
		}

		SetFlashlightTo( true );

		hasPhoto = false;

		if( screenshotQuad )
		{
			yield return StartCoroutine( DoColorFade( screenshotQuad.renderer.material, Color.white, whiteClear, 0.45f ) );
		}
	}

	private IEnumerator DoColorFade( Material material, Color fromColor, Color toColor, float duration )
	{
		material.color = fromColor;

		float beginTime = Time.time;
		float currentTime = 0f;
		float lerp = 0f;

		do
		{
			currentTime += Time.deltaTime;
			lerp = currentTime / duration;
			
			material.color = Color.Lerp( fromColor, toColor, lerp );
			
			yield return null;
			
		} while ( currentTime < duration );
		
		material.color = toColor;
	}
	//end coroutines

	//server calls
	[RPC] void ChangeColor( Vector3 colorVector )
	{
		if( modelRenderers != null )
		{
			Color color = new Color( colorVector.x, colorVector.y, colorVector.z, 1f );

			for( int i = 0; i < modelRenderers.Length; i++ )
				modelRenderers[i].material.color = color;
		}
		
		if( photonView.isMine )
			photonView.RPC( "ChangeColor", PhotonTargets.OthersBuffered, colorVector );
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

		case InputController.ButtonType.Photograph: 
		{
			StartCoroutine( "TakePhoto" );
		} break;
		}
	}
	
	private void OnButtonHeld( InputController.ButtonType button )
	{	
		EvaluateViewChange( button );
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

	private void OnScreenshotFinish()
	{
		ScreenshotAgent.instance.OnPostRenderFinish -= OnScreenshotFinish;

		if( screenshotQuad )
			screenshotQuad.renderer.material.mainTexture = ScreenshotAgent.GetTexture();

		isWaitingForPhotoFinish = false;
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
		return hasPhoto;
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
		//TODO tie into mechanic

		ChangeState( (int)State.Dead );
		DisplayMessage( "Your soul was stolen" );
		ChangeColor( new Vector3( 0.175f, 0.175f, 0.175f ) );

		return true;
		//return ChangeFear( fearAttack );
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
		//TODO tie into mechanic
		//DisplayMessage( "You got 1 point!" );
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
		//TODO tie into mechanic

		return true;
		//return ( Time.time - fearAttackLastTime > fearAttackTimeBuffer );
	}

	public void Monsterize()
	{
		ChangeState( (int)State.Monster );
		DisplayMessage( "(You're the Monster)" );
	}
	//end public functions
}
