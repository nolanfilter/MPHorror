using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : Photon.MonoBehaviour {

	public enum State
	{
		Normal = 0,
		Monster = 1,
		Dead = 2,
		Voyeur = 3,
		Stunned = 4,
		Raging = 5,
		None = 6,
		Invalid = 7,
	}
	private State currentState = State.None;

	private bool isZoomingIn = false;
	private bool hasPhoto = false;
	private bool isOpeningDoor = false;

	private float currentFear = 1f;
	private float currentSanity = 1f;

	private float fearIncreaseRate = 0.005f;
	private float fearAttack = 0.3f;
	private float fearAttackTimeBuffer = 2f;
	private float fearAttackLastTime = 0f;

	private float sanityDecreaseRate;

	private float fearThreshold = 1f;

	private Rect fearMeterRect;
	private Rect sanityMeterRect;

	private Color fearColor = new Color( 1f, 0.5f, 0f, 1f );
	private Color sanityColor = new Color( 0f, 0.5f, 1f, 1f );

	private bool hasReRandomized = false;

	private float speed;
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
	private float viewChangeRate = 1.75f;
	private float timeViewChangeStatic;
	private float timeViewChangeStaticThreshold = 1f;

	private float height = 0.96f;
	
 	private Transform cameraTransform;
	private float cameraFoV = 40f;
	private float cameraZoomFoV = 20f;
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
	public GameObject camQuadPrefab;
	public GameObject rageQuadPrefab;

	private Renderer[] modelRenderers = null;

	public Light flashlight;
	public Light flashBulb;

	private GameObject flashQuad;
	private GameObject screenshotQuad;
	private GameObject camQuad;
	private GameObject rageQuad;

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
	private float lockCameraTimeout = 15f;
	private float angularSmoothLag = 0.3f;
	private float angularMaxSpeed = 15.0f;
	private float snapSmoothLag = 0.2f;
	private float snapMaxSpeed = 720.0f;
	private float clampHeadPositionScreenSpace = 0.75f;
	private Vector3 headOffset = new Vector3( 0f, 0f, 0f );
	private Vector3 centerOffset = new Vector3( 0f, 0.6125f, 0f );
	private float shoulderOffset = 0.325f;
	private float angleVelocity = 0.0f;
	private bool snap = false;
	private float distance = 1f;

	private struct JoystickValue
	{
		public Vector2 xy { get; private set; }
		public float timeStamp { get; private set; }

		public JoystickValue( Vector2 newXY, float newTimeStamp )
		{
			xy = newXY;
			timeStamp = newTimeStamp;
		}
	}

	private List<JoystickValue> joystickValues;
	private float flickThreshold = 0.75f;
	private float flickMinDelta = 0.5f;
	private float flickWindow = 0.1f;

	private float jumpDistance = 2f;
	
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
		{
			Light[] lights = GetComponentsInChildren<Light>();

			for( int i = 0; i < lights.Length; i++ )
				if( lights[i].gameObject.name == "Flashlight" )
					flashlight = lights[i];
		}

		if( flashBulb == null )
		{
			Light[] lights = GetComponentsInChildren<Light>();
			
			for( int i = 0; i < lights.Length; i++ )
				if( lights[i].gameObject.name == "Flash Bulb" )
					flashBulb = lights[i];
		}

		if( flashBulb )
			flashBulb.enabled = false;

		moveDirection = transform.TransformDirection(Vector3.forward);

		gameObject.name = "Player " + photonView.viewID;

		if( photonView.isMine )
			gameObject.name += "(Client)";



		if( modelRenderers != null )
		{
			Color color = new Color( 1f, 0.9f, 0.9f, 1f );
			
			for( int i = 0; i < modelRenderers.Length; i++ )
				modelRenderers[i].material.color = color;
		}

		joystickValues = new List<JoystickValue>();
	}
	
	void Start()
	{
		messageRect = new Rect( 0f, 0f, Screen.width, Screen.height * 0.1f );

		textStyle = new GUIStyle();
		textStyle.font = FontAgent.GetNotificationFont();
		textStyle.normal.textColor = Color.white;
		textStyle.alignment = TextAnchor.MiddleCenter;

		if( flashQuadPrefab )
		{
			flashQuad = Instantiate( flashQuadPrefab ) as GameObject;
			flashQuad.GetComponent<Renderer>().material.color = whiteClear;
		}

		if( screenshotQuadPrefab )
		{
			screenshotQuad = Instantiate( screenshotQuadPrefab ) as GameObject;
			screenshotQuad.GetComponent<Renderer>().material.color = whiteClear;
		}

		if( camQuadPrefab )
		{
			camQuad = Instantiate( camQuadPrefab ) as GameObject;
			camQuad.GetComponent<Renderer>().enabled = false;
		}

		if( rageQuadPrefab )
		{
			rageQuad = Instantiate( rageQuadPrefab ) as GameObject;
			rageQuad.GetComponent<Renderer>().enabled = false;
		}

		viewChangeVector = Vector2.zero;
		oldViewChangeVector = viewChangeVector;

		speed = Random.Range( 4.5f, 6.5f );

		walkSpeed = speed * 0.5f;
		trotSpeed = walkSpeed;
		//trotSpeed = speed;

		fearAttackLastTime = Time.time;

		//random between 120 and 240 seconds
		sanityDecreaseRate = Random.Range( 1f/120f, 1f/240f );

		SnapCamera();

		PlayerAgent.RegisterPlayer( this, photonView.isMine );
	}
	
	void OnEnable()
	{
		inputController.OnButtonDown += OnButtonDown;
		inputController.OnButtonHeld += OnButtonHeld;
		inputController.OnButtonUp += OnButtonUp;

		//centerOffset = Vector3.up * 0.25f;
		//headOffset = centerOffset + Vector3.up * 0.25f;
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
		if( GameAgent.GetCurrentGameState() != GameAgent.GameState.Game )
			return;

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
		{
			//movementVector = inputController.getRawAxes();

			joystickValues.Insert( 0, new JoystickValue( inputController.getRawAxes(), Time.time ) );
			
			float timeDifference;
			int index = joystickValues.Count - 1;
			
			do
			{
				timeDifference = joystickValues[0].timeStamp - joystickValues[ index ].timeStamp;
				
				//Debug.Log( timeDifference );
				
				if( timeDifference > flickWindow )
					joystickValues.RemoveAt( index );
				
				index--;
				
			} while( index > 0 && timeDifference > flickWindow );
			
			if( joystickValues.Count > 0 && joystickValues[0].xy.magnitude > flickThreshold && ( joystickValues[0].xy.magnitude - joystickValues[ joystickValues.Count - 1 ].xy.magnitude > flickMinDelta ) )
			{
				movementVector = joystickValues[0].xy;
				joystickValues.Clear();
			}
			else
			{
				movementVector = Vector3.zero;
			}

			/*
			Debug.Log( "Joystick Values" );

			for( int i = 0; i < joystickValues.Count; i++ )
				Debug.Log( joystickValues[i].timeStamp );
			*/
		}

		movingBack = ( movementVector.y < -0.2f );
		
		bool wasMoving = isMoving;
		isMoving = ( movementVector.magnitude > 0.1f );
		
		Vector3 targetDirection = movementVector.x * right + movementVector.y * forward;

		lockCameraTimer += Time.deltaTime;
		if (isMoving != wasMoving )
			lockCameraTimer = 0.0f;

		//if( targetDirection != Vector3.zero )
		//{
			//if( moveSpeed < walkSpeed * 0.9f )
			//{
				moveDirection = targetDirection.normalized;
			//}
			//else
			//{
				//moveDirection = Vector3.RotateTowards(moveDirection, targetDirection, rotateSpeed * Mathf.Deg2Rad * Time.deltaTime, 1000);
				
				//moveDirection = moveDirection.normalized;
			//}
		//}

		/*
		float curSmooth = speedSmoothing * Time.deltaTime;
		float targetSpeed = Mathf.Min( targetDirection.magnitude, 1.0f );

		if( Time.time - trotAfterSeconds > walkTimeStart )
			targetSpeed *= trotSpeed;
		else
			targetSpeed *= walkSpeed;

		moveSpeed = Mathf.Lerp( moveSpeed, targetSpeed, curSmooth );
		
		if( moveSpeed < walkSpeed * 0.3f )
			walkTimeStart = Time.time;
		*/

		//Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;
		Vector3 movement = moveDirection * jumpDistance;

		if( currentState != State.Raging )
			characterController.Move( movement );

		if( zoomProgress == 1f )
		{
			transform.rotation = Quaternion.LookRotation( zoomOffset.normalized );

			if( rageQuad && currentState == State.Monster )
				rageQuad.GetComponent<Renderer>().enabled = true;

			if( camQuad && ( currentState == State.Normal || currentState == State.None ) )
				camQuad.GetComponent<Renderer>().enabled = !isWaitingForPhotoFinish && !hasPhoto;
		}
		else
		{
			Vector3 axes = inputController.getRawAxes();

			axes = axes.x * right + axes.y * forward;

			if( axes != Vector3.zero )
				transform.rotation = Quaternion.LookRotation( axes );

			if( rageQuad )
				rageQuad.GetComponent<Renderer>().enabled = false;
			
			if( camQuad )
				camQuad.GetComponent<Renderer>().enabled = false;
		}

		movementVector = Vector3.zero;
	}
	
	private void SyncedMovement()
	{
		/*
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
		*/

		transform.position = syncEndPosition;
		transform.rotation = syncEndRotation;
	}

	private void SnapCamera()
	{
		if( photonView.isMine )
		{
			if( ( isZoomingIn && currentState != State.Stunned ) || currentState == State.Raging )
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

			float longestDistance = 0f;
			RaycastHit[] hits = Physics.RaycastAll( cameraTransform.position, cameraTransform.forward, Vector3.Distance( cameraTransform.position, transform.position + headOffset ) );
			
			for( int i = 0; i < hits.Length; i++ )
				if( hits[i].transform != transform && hits[i].normal != Vector3.up && hits[i].distance > longestDistance )
					longestDistance = hits[i].distance;
			
			clippingOffset = cameraTransform.forward * longestDistance;

			zoomOffset = Vector3.Lerp( clippingOffset, Quaternion.AngleAxis( cameraTransform.eulerAngles.y, Vector3.up ) * Vector3.forward * 0.75f, zoomProgress );
			cameraTransform.position += zoomOffset;

			cameraTransform.GetComponent<Camera>().fieldOfView = Mathf.RoundToInt( Mathf.Lerp( cameraFoV, cameraZoomFoV, zoomProgress ) );

			if( flashlight != null && currentState != State.Dead )
			{
				RaycastHit hit;

				Quaternion newFlashlightRotation = cameraTransform.rotation;

				if( Physics.Raycast( cameraTransform.position + cameraTransform.forward * 1.5f, cameraTransform.forward, out hit ) )
				{
					if( hit.collider.transform != transform )
					{
						Vector3 hitPosition = cameraTransform.position + cameraTransform.forward * ( hit.distance + 1.5f );

						newFlashlightRotation = Quaternion.LookRotation( hitPosition - flashlight.transform.position );
					}
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
			
			float currentHeight = transform.position.y + centerOffset.y;
			
			Quaternion currentRotation = Quaternion.Euler(0, currentAngle, 0);

			Vector3 oldCameraPosition = cameraTransform.position;

			cameraTransform.position = targetCenter;
			cameraTransform.position += currentRotation * Vector3.back * distance;
			
			cameraTransform.position = new Vector3(cameraTransform.position.x, currentHeight, cameraTransform.position.z);

			Vector3 shoulderOffsetVector = cameraTransform.right * shoulderOffset;

			cameraTransform.position += shoulderOffsetVector;

			//cameraTransform.position = Vector3.MoveTowards( oldCameraPosition, cameraTransform.position, 0.25f );

			SnapCamera();
			
			SetUpRotation(targetCenter + shoulderOffsetVector, targetHead + shoulderOffsetVector);
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
			
			Ray centerRay = cameraTransform.GetComponent<Camera>().ViewportPointToRay( new Vector3( 0.5f, 0.5f, 1f ) );
			Ray topRay = cameraTransform.GetComponent<Camera>().ViewportPointToRay( new Vector3( 0.5f, clampHeadPositionScreenSpace, 1f ) );
			
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
					
				if( xAngleOffset > 180f && xAngleOffset < 300f )
					xAngleOffset = 300f;
			}
			else
			{
				if( xAngleOffset > 60f && xAngleOffset < 180f )
					xAngleOffset = 60f;
				
				if( xAngleOffset > 180f && xAngleOffset < 290f )
					xAngleOffset = 290f;
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
			SetFlashlightTo( false );
		else
			SetFlashlightTo( !flashlight.enabled );
	}

	private void EvaluateViewChange( InputController.ButtonType button )
	{
		if( GameAgent.GetCurrentGameState() != GameAgent.GameState.Game )
			return;

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

	private void SetShoulderOffset( float sign )
	{
		shoulderOffset = Mathf.Abs( shoulderOffset ) * sign;
	}

	private void RemoveStunEffect()
	{
		StunController stunController = gameObject.GetComponent<StunController>();
	
		if( stunController )
			Destroy( stunController );
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

		isWaitingForPhotoFinish = true;

		ScreenshotAgent.instance.OnPostRenderFinish += OnScreenshotFinish;
		ScreenshotAgent.Enable();

		while( isWaitingForPhotoFinish )
			yield return null;

		bool oldFlashlightState = ( flashlight && flashlight.enabled );

		SetFlashlightTo( false );

		hasPhoto = true;

		StartCoroutine( "PlayCameraCooldown" );

		SetFlashBulbTo( true );

		if( flashQuad )
		{
			yield return StartCoroutine( DoColorFade( flashQuad.GetComponent<Renderer>().material, Color.white, whiteClear, 0.5f ) );
		}

		SetFlashBulbTo( false );

		if( screenshotQuad )
		{
			yield return StartCoroutine( DoColorFade( screenshotQuad.GetComponent<Renderer>().material, whiteClear, Color.white, 0.25f ) );

			yield return new WaitForSeconds( 1.25f );

			yield return StartCoroutine( DoColorFade( screenshotQuad.GetComponent<Renderer>().material, Color.white, whiteClear, 0.45f ) );
		}

		hasPhoto = false;

		SetFlashlightTo( oldFlashlightState );
	}

	private IEnumerator RageMode()
	{
		if( ( zoomProgress != 1f ) )
			yield break;

		ChangeState( (int)State.Raging );
		ChangeColor(  new Quaternion( 0.75f, 0f, 0f, 1f ) );

		RageController rageController = null;

		if( photonView.isMine )
			rageController = gameObject.AddComponent<RageController>();

		float speed = 12.5f;

		float distance = speed * RageController.RageDuration;
		float currentDistance = 0f;
	
		float deltaDistance;

		Vector3 direction = transform.forward;

		yield return null;

		do
		{
			deltaDistance = speed * Time.deltaTime;
			currentDistance += speed * Time.deltaTime;

			characterController.Move( direction * speed * Time.deltaTime );

			yield return null;

		} while( currentDistance < distance );

		ChangeState( (int)State.Stunned );
		//MonsterReveal();

		speed = 0.5f;
		
		distance = speed * RageController.RageCooldown;
		currentDistance = 0f;

		yield return null;
		
		do
		{
			deltaDistance = speed * Time.deltaTime;
			currentDistance += speed * Time.deltaTime;
			
			characterController.Move( direction * speed * Time.deltaTime );
			
			yield return null;
			
		} while( currentDistance < distance );


		if( rageController )
			Destroy( rageController );

		ChangeColor( new Quaternion( 1f, 0.9f, 0.9f, 1f ) );
		ChangeState( (int)State.Monster );
	}

	private IEnumerator DoStun()
	{
		if( currentState == State.Stunned || currentState == State.Dead || currentState == State.Voyeur )
			yield break;

		State originalState = currentState;

		ChangeState( (int)State.Stunned );

		if( photonView.isMine )
			gameObject.AddComponent<StunController>();

		yield return new WaitForSeconds( StunController.StunDuration );

		ChangeColor( new Quaternion( 1f, 0.9f, 0.9f, 1f ) );

		RemoveStunEffect();

		ChangeState( (int)originalState );
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

	//TODO generalize
	private IEnumerator PlayCameraCooldown()
	{
		if( !photonView.isMine )
			yield break;

		AudioClip cameraCooldownClip = PlayerAgent.GetCameraCooldownClip();
			
		if( cameraCooldownClip )
		{
			AudioSource source = Camera.main.gameObject.AddComponent<AudioSource>();
			source.clip = cameraCooldownClip;
			source.loop = false;
			source.volume = 1f;
			source.Play();
				
			float duration = 2.5f;
			float lerp;

			do
			{
				lerp = Mathf.Clamp01( source.time / duration );

				source.volume = Mathf.Lerp( 1f, 0f, lerp );

				yield return null;

			} while( source.time < duration );

			Destroy( source );
		}
	}
	//end coroutines

	//server calls
	[RPC] 
	public void RPCChangeColider( int state )
	{
		if( characterController )
			characterController.enabled = ( state == 1 );
	}

	[RPC] 
	public void RPCChangeColor( Quaternion colorVector4 )
	{
		if( modelRenderers != null )
		{
			Color color = new Color( colorVector4.x, colorVector4.y, colorVector4.z, colorVector4.w );

			for( int i = 0; i < modelRenderers.Length; i++ )
				modelRenderers[i].material.color = color;
		}
	}

	[RPC] 
	public void RPCChangeFlashBulb( int state )
	{
		if( flashBulb )
			flashBulb.enabled = ( state == 1 );
	}

	[RPC] 
	public void RPCChangeFlashlight( int state )
	{
		if( flashlight )
			flashlight.enabled = ( state == 1 );
	}

	[RPC]
	public void RPCChangeState( int state )
	{
		currentState = (State)state;
	}

	[RPC] 
	public void RPCChangeZoom( int zoom )
	{
		isZoomingIn = ( zoom == 1 );
	}

	[RPC]
	public void RPCDisplayMessage( string messageToDisplay )
	{
		StopCoroutine( "DoDisplayMessage" );
		StartCoroutine( "DoDisplayMessage", messageToDisplay );
	}

	[RPC]
	public void RPCStartGame()
	{
		FastBloom fastBloom = Camera.main.gameObject.GetComponent<FastBloom>();
			
		if( fastBloom )
			fastBloom.enabled = true;

		ColorCorrectionCurves colorCorrectionCurves = Camera.main.gameObject.GetComponent<ColorCorrectionCurves>();
			
		if( colorCorrectionCurves )
			colorCorrectionCurves.enabled = true;
			
		//js
		DepthOfField34 depthOfField34 = Camera.main.gameObject.GetComponent<DepthOfField34>();
			
		//if( depthOfField34 )
		//	depthOfField34.enabled = true;
			
		SSAOEffect ssaoEffect = Camera.main.gameObject.GetComponent<SSAOEffect>();
			
		if( ssaoEffect )
			ssaoEffect.enabled = true;
			
		//js
		Vignetting vignetting = Camera.main.GetComponent<Vignetting>();
			
		if( vignetting )
			vignetting.enabled = true;

		ZoomSurvivorController zoomSurvivorController = gameObject.AddComponent<ZoomSurvivorController>();
		
		zoomSurvivorController.playerController = this;

		gameObject.AddComponent<NoiseController>();
		
		GameAgent.ChangeGameState( GameAgent.GameState.Game );
		PlayerAgent.SetMonster();
	}

	[RPC]
	public void RPCEndGame()
	{
		StopCoroutine( "DoStun" );
		StopCoroutine( "RageMode" );
		
		NegativeEffect negativeEffect = Camera.main.gameObject.GetComponent<NegativeEffect>();
		
		if( negativeEffect )
			Destroy( negativeEffect );
		
		RemoveStunEffect();

		FastBloom fastBloom = Camera.main.gameObject.GetComponent<FastBloom>();
			
		if( fastBloom )
			fastBloom.enabled = false;

		ColorCorrectionCurves colorCorrectionCurves = Camera.main.gameObject.GetComponent<ColorCorrectionCurves>();
			
		if( colorCorrectionCurves )
			colorCorrectionCurves.enabled = false;
			
		//js
		DepthOfField34 depthOfField34 = Camera.main.gameObject.GetComponent<DepthOfField34>();
			
		if( depthOfField34 )
			depthOfField34.enabled = false;
			
		SSAOEffect ssaoEffect = Camera.main.gameObject.GetComponent<SSAOEffect>();
			
		if( ssaoEffect )
			ssaoEffect.enabled = false;
			
		//js
		Vignetting vignetting = Camera.main.GetComponent<Vignetting>();
			
		if( vignetting )
			vignetting.enabled = false;
		
		RageController rageController = gameObject.GetComponent<RageController>();
		
		if( rageController )
			Destroy( rageController );

		ZoomSurvivorController zoomSurvivorController = gameObject.GetComponent<ZoomSurvivorController>();
		
		if( zoomSurvivorController )
			Destroy( zoomSurvivorController );
		
		ZoomKillerController zoomKillerController = gameObject.GetComponent<ZoomKillerController>();
		
		if( zoomKillerController )
			Destroy( zoomKillerController );
		
		NoiseController noiseController = gameObject.GetComponent<NoiseController>();
		
		if( noiseController )
			Destroy( noiseController );
		
		GameAgent.ChangeGameState( GameAgent.GameState.End );
		
		messageString = "";
		
		TurnOffQuads();
	}

	[RPC]
	public void RPCStun()
	{
		StartCoroutine( "DoStun" );
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

		case InputController.ButtonType.Action: 
		{
			if( currentState == State.Normal || currentState == State.None )
				StartCoroutine( "TakePhoto" );
			else if( currentState == State.Monster )
				StartCoroutine( "RageMode" );
		} break;

		case InputController.ButtonType.LeftShoulder: 
		{
			SetShoulderOffset( -1f );
		} break;

		case InputController.ButtonType.RightShoulder: 
		{
			SetShoulderOffset( 1f );
		} break;
		
		case InputController.ButtonType.A: case InputController.ButtonType.X:
		{
			isOpeningDoor = true;
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

			case InputController.ButtonType.A: case InputController.ButtonType.X:
			{
				isOpeningDoor = false;
			} break;
		}

	}

	private void OnScreenshotFinish()
	{
		ScreenshotAgent.instance.OnPostRenderFinish -= OnScreenshotFinish;

		if( screenshotQuad )
			screenshotQuad.GetComponent<Renderer>().material.mainTexture = ScreenshotAgent.GetTexture();

		isWaitingForPhotoFinish = false;
	}
	//end event handlers

	//public functions
	public bool CanIncreaseFear()
	{
		//TODO tie into mechanic
		
		return true;
		//return ( Time.time - fearAttackLastTime > fearAttackTimeBuffer );
	}

	public void ChangeColider( int state )
	{
		if( characterController )
			characterController.enabled = ( state == 1 );

		photonView.RPC( "RPCChangeColider", PhotonTargets.OthersBuffered, state );
	}

	public void ChangeColor( Quaternion colorVector4 )
	{
		if( modelRenderers != null )
		{
			Color color = new Color( colorVector4.x, colorVector4.y, colorVector4.z, colorVector4.w );
			
			for( int i = 0; i < modelRenderers.Length; i++ )
				modelRenderers[i].material.color = color;
		}
		
		photonView.RPC( "RPCChangeColor", PhotonTargets.OthersBuffered, colorVector4 );
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

	public void ChangeFlashBulb( int state )
	{
		if( flashBulb )
			flashBulb.enabled = ( state == 1 );
		
		photonView.RPC( "RPCChangeFlashBulb", PhotonTargets.OthersBuffered, state );
	}

	public void ChangeFlashlight( int state )
	{
		if( flashlight )
			flashlight.enabled = ( state == 1 );
		
		photonView.RPC( "RPCChangeFlashlight", PhotonTargets.OthersBuffered, state );
	}

	public void ChangeSanity( float amount )
	{
		currentSanity += amount;
	}

	public void ChangeState( int state )
	{
		currentState = (State)state;
		photonView.RPC( "RPCChangeState", PhotonTargets.OthersBuffered, state );
	}

	public void ChangeZoom( int zoom )
	{
		isZoomingIn = ( zoom == 1 );
		
		photonView.RPC( "RPCChangeZoom", PhotonTargets.OthersBuffered, zoom );
	}

	public void DecreaseSanity()
	{
		ChangeSanity( sanityDecreaseRate * -2f * Time.deltaTime );
	}

	public void DisplayMessage( string messageToDisplay )
	{
		StopCoroutine( "DoDisplayMessage" );
		StartCoroutine( "DoDisplayMessage", messageToDisplay );
		
		photonView.RPC( "RPCDisplayMessage", PhotonTargets.OthersBuffered, messageToDisplay );
	}

	public void Escape()
	{
		if( currentState == State.Monster || currentState == State.Voyeur || currentState == State.Dead || currentState == State.Raging )
			return;

		ChangeState( (int)State.Voyeur );
		ChangeColor( new Quaternion( 0f, 0f, 0f, 0f ) );
		DisplayMessage( "You escaped!" );

		PlayerAgent.CheckForEnd();
	}

	public State GetCurrentState()
	{
		return currentState;
	}

	public bool IncreaseFear()
	{
		//TODO tie into mechanic
		
		ChangeState( (int)State.Dead );
		DisplayMessage( "Your soul was stolen" );
		ChangeColor( new Quaternion( 0.175f, 0.175f, 0.175f, 0.5f ) );
		ChangeColider( 0 );

		PlayerAgent.CheckForEnd();

		return true;
		//return ChangeFear( fearAttack );
	}
	
	public void IncreaseSanity()
	{
		ChangeSanity( sanityDecreaseRate * 0.3f * Time.deltaTime );
	}

	public void IncrementPoint()
	{
		//TODO tie into mechanic
		//DisplayMessage( "You got 1 point!" );
	}

	public bool IsZoomedIn()
	{
		return hasPhoto || currentState == State.Raging;
	}

	public float GetZoomProgress()
	{
		return zoomProgress;
	}

	public bool IsOpeningDoor()
	{
		return isOpeningDoor;
	}

	public void Monsterize()
	{
		if( !photonView.isMine || GameAgent.GetCurrentGameState() != GameAgent.GameState.Game || currentState == State.Monster || currentState == State.Dead )
			return;

		if( camQuad )
			camQuad.GetComponent<Renderer>().enabled = false;

		//cameraTransform.gameObject.AddComponent<NegativeEffect>();
		StopCoroutine( "DoStun" );
		RemoveStunEffect();
		ChangeState( (int)State.Monster );
		DisplayMessage( "Attack your friends" );

		ZoomSurvivorController zoomSurvivorController = gameObject.GetComponent<ZoomSurvivorController>();

		if( zoomSurvivorController )
			Destroy( zoomSurvivorController );

		ZoomKillerController zoomKillerController = gameObject.AddComponent<ZoomKillerController>();

		zoomKillerController.playerController = this;

		PlayerAgent.CheckForEnd();
	}

	public void RageHit()
	{
		StopCoroutine( "RageMode" );
		ChangeState( (int)State.Monster );
		DisplayMessage( "Killer" );
		ChangeColor( new Quaternion( 1f, 0.9f, 0.9f, 1f ) );
	}

	public void MonsterReveal()
	{
		StopCoroutine( "RageMode" );

		RageController rageController = gameObject.GetComponent<RageController>();
		
		if( rageController )
			Destroy( rageController );

		ChangeState( (int)State.Monster );

		ChangeColor( new Quaternion( 1f, 0.5f, 0.75f, 1f ) );
		Stun();
	}

	public void SurvivorReveal()
	{
		ChangeColor( new Quaternion( 0f, 0.75f, 0f, 1f ) );
		Stun();
	}

	public void SetFlashBulbTo( bool on )
	{
		if( flashBulb == null )
			ChangeFlashBulb( 0 );
		else
			ChangeFlashBulb( ( on ? 1 : 0 ) );
	}

	public void SetFlashlightTo( bool on )
	{
		if( flashlight == null || hasPhoto )
			ChangeFlashlight( 0 );
		else
			ChangeFlashlight( ( on ? 1 : 0 ) );
	}

	public void StartGame()
	{
		FastBloom fastBloom = Camera.main.gameObject.GetComponent<FastBloom>();
		
		if( fastBloom )
			fastBloom.enabled = true;
		
		ColorCorrectionCurves colorCorrectionCurves = Camera.main.gameObject.GetComponent<ColorCorrectionCurves>();
		
		if( colorCorrectionCurves )
			colorCorrectionCurves.enabled = true;
		
		//js
		DepthOfField34 depthOfField34 = Camera.main.gameObject.GetComponent<DepthOfField34>();
		
		//if( depthOfField34 )
		//	depthOfField34.enabled = true;
		
		SSAOEffect ssaoEffect = Camera.main.gameObject.GetComponent<SSAOEffect>();
		
		if( ssaoEffect )
			ssaoEffect.enabled = true;
		
		//js
		Vignetting vignetting = Camera.main.GetComponent<Vignetting>();
		
		if( vignetting )
			vignetting.enabled = true;
		
		ZoomSurvivorController zoomSurvivorController = gameObject.AddComponent<ZoomSurvivorController>();
		
		zoomSurvivorController.playerController = this;
		
		gameObject.AddComponent<NoiseController>();
		
		GameAgent.ChangeGameState( GameAgent.GameState.Game );
		PlayerAgent.SetMonster();

		photonView.RPC( "RPCStartGame", PhotonTargets.OthersBuffered );
	}

	public void EndGame()
	{
		StopCoroutine( "DoStun" );
		StopCoroutine( "RageMode" );
		
		NegativeEffect negativeEffect = Camera.main.gameObject.GetComponent<NegativeEffect>();
		
		if( negativeEffect )
			Destroy( negativeEffect );
		
		RemoveStunEffect();
		
		FastBloom fastBloom = Camera.main.gameObject.GetComponent<FastBloom>();
		
		if( fastBloom )
			fastBloom.enabled = false;
		
		ColorCorrectionCurves colorCorrectionCurves = Camera.main.gameObject.GetComponent<ColorCorrectionCurves>();
		
		if( colorCorrectionCurves )
			colorCorrectionCurves.enabled = false;
		
		//js
		DepthOfField34 depthOfField34 = Camera.main.gameObject.GetComponent<DepthOfField34>();
		
		if( depthOfField34 )
			depthOfField34.enabled = false;
		
		SSAOEffect ssaoEffect = Camera.main.gameObject.GetComponent<SSAOEffect>();
		
		if( ssaoEffect )
			ssaoEffect.enabled = false;
		
		//js
		Vignetting vignetting = Camera.main.GetComponent<Vignetting>();
		
		if( vignetting )
			vignetting.enabled = false;
		
		RageController rageController = gameObject.GetComponent<RageController>();
		
		if( rageController )
			Destroy( rageController );
		
		ZoomSurvivorController zoomSurvivorController = gameObject.GetComponent<ZoomSurvivorController>();
		
		if( zoomSurvivorController )
			Destroy( zoomSurvivorController );
		
		ZoomKillerController zoomKillerController = gameObject.GetComponent<ZoomKillerController>();
		
		if( zoomKillerController )
			Destroy( zoomKillerController );
		
		NoiseController noiseController = gameObject.GetComponent<NoiseController>();
		
		if( noiseController )
			Destroy( noiseController );
		
		GameAgent.ChangeGameState( GameAgent.GameState.End );
		
		messageString = "";
		
		TurnOffQuads();

		photonView.RPC( "RPCEndGame", PhotonTargets.OthersBuffered );
	}

	public void Stun()
	{
		photonView.RPC( "RPCStun", PhotonTargets.OthersBuffered );

		StartCoroutine( "DoStun" );
	}

	public void TurnOffQuads()
	{
		if( rageQuad )
			rageQuad.GetComponent<Renderer>().enabled = false;
		
		if( camQuad )
			camQuad.GetComponent<Renderer>().enabled = false;
	}

	public void TeleportTo( Vector3 coordinate )
	{
		transform.position = coordinate;
		transform.position = new Vector3( transform.position.x, height, transform.position.z );
	}
	//end public functions
}
