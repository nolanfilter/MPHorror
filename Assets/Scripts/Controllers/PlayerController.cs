using UnityEngine;
using UnityEngine.UI;
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
		Frozen = 7,
		Invalid = 8,
	}
	private State currentState = State.None;

	private bool isZoomingIn = false;
	private bool hasPhoto = false;
	private bool isOpeningDoor = false;
	private bool isTutorializing = false;

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
	private float viewChangeRate = 1f;
	private float timeViewChangeStatic;
	private float timeViewChangeStaticThreshold = 1f;

	private float height = 0.96f;
	
 	private Transform cameraTransform;
	private float cameraFoV = 40f;
	private float cameraZoomFoV = 20f;
	private Quaternion cameraRotationOffset = Quaternion.Euler( new Vector3( 4f, 0f, 0f ) );
	private float cameraRotationRate = 0.025f;

	private Vector3 clippingOffset = Vector3.zero;
	private Vector3 zoomOffset = Vector3.zero;

	private float zoomDuration = 0.1f;
	private float zoomProgress = 0f;
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
	
	public string[] movementPoses;
	public string picturePose;
	public string pouncePose;
	public string stunPose;
	private List<string> poseDeck = new List<string>();

	public Animation animation;

	public GameObject deathModel;

	public ParticleSystem movementParticleSystem;
	public ParticleSystem wireframeParticleSystem;

	public GameObject UIRootObject;
	public Image flashUI;
	public RawImage screenshotUI;
	public Image photoFrameUI;
	public Image camUI;
	public Image rageUI;
	public Image compassUI;
	public Image motionBlindUI;
	public Image tutorialUI;
	public Image demonizedUI;
	public Image demon1toGoUI;
	public Image demon2toGoUI;
	public Image gatherer2LeftUI;
	public Image gatherer1LeftUI;
	public Image gathererTrappedForever;

	public PlayMakerFSM uiFSM;

	private Renderer[] modelRenderers = null;

	public Light flashlight;
	public Light flashBulb;

	private Color whiteClear = new Color( 1f, 1f, 1f, 0f );

	private bool isWaitingForPhotoFinish = false;
	private bool isTakingPhoto = false;

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
	private Vector3 centerOffset = new Vector3( 0f, 0.6f, 0f );//new Vector3( 0f, 0.6125f, 0f );
	private float shoulderOffset = 0.4f;//0.325f;
	private float angleVelocity = 0.0f;
	private bool snap = false;
	private float distance = 1.125f;//1f;

	private float maxRotationSpeed = 5f;
	private bool invertY = false;

	private float movementRefocusAmount = 1.125f;
	private float movementRefocusDuration = 0.25f;
	private bool isRefocusing = false;

	private List<GameObject> photographedObjects = new List<GameObject>();

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

	private bool didMove = false;

	private float jumpDistance = 2f;

	private float freezeDuration = 20f;

	private int tutorialIndex;
	private float gifTime = 0.6f;

	public Sprite[] everyoneTutorialImages;
	public Sprite[] demonTutorialImages;
	public Sprite[] gathererTutorialImages;
	
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
			Color color = new Color( 1f, 0.95f, 0.95f, 1f );
			
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

		if( UIRootObject )
		{
			if( photonView.isMine )
			{
				UIRootObject.SetActive( false );
				UIRootObject.transform.SetParent( null, false );
			}
			else
			{
				Destroy( UIRootObject );
			}
		}

		DisableUI();

		viewChangeVector = Vector2.zero;

		speed = Random.Range( 4.5f, 6.5f );

		walkSpeed = speed * 0.5f;
		trotSpeed = walkSpeed;
		//trotSpeed = speed;

		fearAttackLastTime = Time.time;

		//random between 120 and 240 seconds
		sanityDecreaseRate = Random.Range( 1f/120f, 1f/240f );

		SnapCamera();

		PlayerAgent.RegisterPlayer( this, photonView.isMine );

		SetPose( NextRandomPose() );
	}
	
	void OnEnable()
	{
		inputController.OnButtonDown += OnButtonDown;
		inputController.OnButtonHeld += OnButtonHeld;
		inputController.OnButtonUp += OnButtonUp;

		Cut();
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

		Destroy( UIRootObject );

		Destroy( deathModel );
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

		if( transform.position.y != height )
			transform.position = new Vector3( transform.position.x, height, transform.position.z );

		UpdateCompass();

		//if( photonView.isMine && Input.GetKeyDown( KeyCode.K ) )
		//	KillPlayer();
	}

	void LateUpdate()
	{
		if( photonView.isMine )
			Apply();
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

				snap = true;

				SnapCamera();

				StartCoroutine( "DoMotionBlind" );
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
			if( currentState == State.Voyeur )
			{
				movementVector = inputController.getRawAxes();
			}
			else if( currentState != State.Frozen && !isWaitingForPhotoFinish )
			{
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

				if( didMove )
				{
					movementVector = Vector3.up;
					joystickValues.Clear();
					
					StopCoroutine( "DoMovementRefocus" );
					StartCoroutine( "DoMovementRefocus" );
					
					DrawParticles();

					if( ( currentState == State.None || currentState == State.Monster ) && zoomProgress == 0f )
						SetPose( NextRandomPose() );

					didMove = false;
				}
				else
				{
					movementVector = Vector3.zero;
				}

				/*
				if( joystickValues.Count > 0 && joystickValues[0].xy.magnitude > flickThreshold && ( joystickValues[0].xy.magnitude - joystickValues[ joystickValues.Count - 1 ].xy.magnitude > flickMinDelta ) )
				{
					movementVector = joystickValues[0].xy;
					joystickValues.Clear();
				
					StopCoroutine( "DoMovementRefocus" );
					StartCoroutine( "DoMovementRefocus" );

					if( movementParticleSystem )
						movementParticleSystem.Play();

					if( wireframeParticleSystem )
						wireframeParticleSystem.Play();
				}
				else
				{
					movementVector = Vector3.zero;
				}
				*/

				/*
				Debug.Log( "Joystick Values" );

				for( int i = 0; i < joystickValues.Count; i++ )
					Debug.Log( joystickValues[i].timeStamp );
				*/
			}
		}

		movingBack = ( movementVector.y < -0.2f );
		
		bool wasMoving = isMoving;
		isMoving = ( movementVector.magnitude > 0.1f );
		
		Vector3 targetDirection = movementVector.x * right + movementVector.y * forward;

		/*
		lockCameraTimer += Time.deltaTime;
		if (isMoving != wasMoving )
			lockCameraTimer = 0.0f;
		*/

		Vector3 movement = Vector3.zero;

		if (currentState == State.Voyeur)
		{
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

			movement = moveDirection * moveSpeed * Time.deltaTime;
		}
		else
		{
			moveDirection = targetDirection.normalized;
			movement = moveDirection * jumpDistance;
		}

		if( currentState != State.Raging )
		{
			Vector3 oldPosition = transform.position;

			characterController.Move( movement );

			Vector3 deltaPosition = transform.position - oldPosition;

			cameraTransform.position += deltaPosition;
		}

		if( zoomProgress == 1f )
		{
			transform.rotation = Quaternion.LookRotation( zoomOffset.normalized );

			if( rageUI && currentState == State.Monster )
				rageUI.enabled = true;

			if( camUI && ( currentState == State.Normal || currentState == State.None ) )
				camUI.enabled = ( !isWaitingForPhotoFinish && !hasPhoto );
		}
		else
		{
			Vector3 axes = inputController.getRawAxes();
			axes = Vector3.up;

			axes = axes.x * right + axes.y * forward;

			if( axes != Vector3.zero )
			{
				if( joystickValues.Count == 0 )
					transform.rotation = Quaternion.LookRotation( axes );
				//else
				//	transform.rotation = Quaternion.RotateTowards( transform.rotation, Quaternion.LookRotation( axes ), maxRotationSpeed );
			}

			if( rageUI )
				rageUI.enabled = false;
			
			if( camUI )
				camUI.enabled = false;
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

	private void UpdateCompass()
	{
		if( !photonView.isMine || GameAgent.GetCurrentGameState() != GameAgent.GameState.Game || compassUI == null || MannequinAgent.GetNumActiveMannequins() > PlayerAgent.GetCompassActivationNumber() )
			return;

		Vector3 closestMannequinVector = MannequinAgent.GetClosestMannequin( transform.position ) - transform.position;

		float angle = Mathf.DeltaAngle( Mathf.Atan2( cameraTransform.forward.x, cameraTransform.forward.z ) * Mathf.Rad2Deg,
		                               Mathf.Atan2( closestMannequinVector.x, closestMannequinVector.z ) * Mathf.Rad2Deg );

		compassUI.rectTransform.localRotation = Quaternion.AngleAxis( angle, Vector3.back );
	}

	private void SnapCamera()
	{
		if( photonView.isMine )
		{
			float oldZoomProgress = zoomProgress;

			if( ( isZoomingIn && currentState != State.Stunned && currentState != State.Frozen ) || currentState == State.Raging || isWaitingForPhotoFinish )
				zoomProgress = Mathf.Clamp01( zoomProgress + Time.deltaTime / zoomDuration );
			else
				zoomProgress = Mathf.Clamp01( zoomProgress - Time.deltaTime / zoomDuration );

			if( zoomProgress == 1f )
			{
				if( oldZoomProgress != 1f )
					SetPose( picturePose );

				timeZoomedIn += Time.deltaTime;
				Camera.main.gameObject.GetComponent<DepthOfField34>().enabled = ( currentState != State.Monster );
			}
			else
			{
				if( zoomProgress == 0f && oldZoomProgress != 0f && ( currentState == State.None || currentState == State.Monster ) )
					SetPose( NextRandomPose() );

				timeZoomedIn = 0f;
				Camera.main.gameObject.GetComponent<DepthOfField34>().enabled = false;
			}

			if( zoomProgress != 0f )
				lockCameraTimer = 0f;

			Vector3 directionVector = cameraTransform.forward;
			directionVector = new Vector3( directionVector.x, 0f, directionVector.z ).normalized;

			float raycastDistance = Vector3.Distance( cameraTransform.position, transform.position );

			Debug.DrawRay( cameraTransform.position + cameraTransform.right * 0.1f + directionVector * raycastDistance * 1.25f,  directionVector * -1.5f, Color.green, 0.5f );
			Debug.DrawRay( cameraTransform.position - cameraTransform.right * 0.1f + directionVector * raycastDistance * 1.25f,  directionVector * -1.5f, Color.green, 0.5f );
			
			float shortestDistance = Mathf.Infinity;
			RaycastHit[] hits;

			hits = Physics.RaycastAll( cameraTransform.position + cameraTransform.right * 0.1f + directionVector * raycastDistance * 1.25f, directionVector * -1f, raycastDistance * 1.5f );

			for( int i = 0; i < hits.Length; i++ )
				if( hits[i].transform != transform && hits[i].normal != Vector3.up && hits[i].distance < shortestDistance )
					shortestDistance = hits[i].distance;

			hits = Physics.RaycastAll( cameraTransform.position - cameraTransform.right * 0.1f + directionVector * raycastDistance * 1.25f,  directionVector * -1f, raycastDistance * 1.5f );

			for( int i = 0; i < hits.Length; i++ )
				if( hits[i].transform != transform && hits[i].normal != Vector3.up && hits[i].distance < shortestDistance )
					shortestDistance = hits[i].distance;

			clippingOffset = directionVector * Mathf.Clamp( ( raycastDistance * 1.15f - shortestDistance ), 0f, raycastDistance * 1.15f );

			zoomOffset = Vector3.Lerp( clippingOffset, Quaternion.AngleAxis( cameraTransform.eulerAngles.y, Vector3.up ) * Vector3.forward, zoomProgress );
			cameraTransform.position += zoomOffset;

			if( !isRefocusing )
				cameraTransform.GetComponent<Camera>().fieldOfView = Mathf.Lerp( cameraFoV, cameraZoomFoV, zoomProgress );

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
		}
	}

	private void Apply()
	{
		if( photonView.isMine )
		{
			Vector3 targetCenter = transform.position + centerOffset;

			SetUpRotation( targetCenter );

			float originalTargetAngle = transform.eulerAngles.y;
			float currentAngle = cameraTransform.eulerAngles.y;
			
			float targetAngle = originalTargetAngle;
			
			if (snap)
			{
				if (AngleDistance(currentAngle, originalTargetAngle) < 3.0f)
					snap = false;

				currentAngle = targetAngle;

				//currentAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref angleVelocity, snapSmoothLag, snapMaxSpeed);
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


			Vector3 temp = targetCenter + currentRotation * Vector3.back * distance;
			temp = new Vector3( temp.x, currentHeight, temp.z );

			//cameraTransform.position = targetCenter;
			//cameraTransform.position += currentRotation * Vector3.back * distance;
			
			//cameraTransform.position = new Vector3(cameraTransform.position.x, currentHeight, cameraTransform.position.z);

			Vector3 shoulderOffsetVector = cameraTransform.right * shoulderOffset;

			//temp += shoulderOffsetVector;

			cameraTransform.position = temp;

			cameraTransform.position += shoulderOffsetVector;

			SnapCamera();
		}
	}
	
	private void Cut()
	{
		if( photonView.isMine )
		{
			float oldSnapMaxSpeed = snapMaxSpeed;
			float oldSnapSmooth = snapSmoothLag;
			
			snapMaxSpeed = 10000f;
			snapSmoothLag = 0.001f;

			snap = true;
			Apply();
			
			snapMaxSpeed = oldSnapMaxSpeed;
			snapSmoothLag = oldSnapSmooth;
		}
	}
	
	private void SetUpRotation( Vector3 centerPos )
	{
		if( photonView.isMine )
		{
			Vector3 cameraPos = cameraTransform.position - zoomOffset - cameraTransform.right * shoulderOffset;
			Vector3 offsetToCenter = centerPos - cameraPos;
			
			Quaternion yRotation = Quaternion.LookRotation( new Vector3( offsetToCenter.x, 0f, offsetToCenter.z ) );
			
			//Vector3 relativeOffset = Vector3.forward * distance + Vector3.down * height;
			//cameraTransform.rotation = yRotation * Quaternion.LookRotation( relativeOffset );

			cameraTransform.rotation = yRotation;
			
			float xAngleOffset = cameraRotationOffset.eulerAngles.x;

			bool viewInputThisFrame = false;

			if( viewChangeVector != Vector2.zero )
			{
				viewInputThisFrame = true;

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

			if( zoomProgress < 0.5f )
			{
				if( currentState == State.Voyeur )
				{
					if( xAngleOffset > 40f && xAngleOffset < 180f )
						xAngleOffset = 40f;
						
					if( xAngleOffset > 180f && xAngleOffset < 300f )
						xAngleOffset = 300f;
				}
				else
				{
					if( xAngleOffset > 13f && xAngleOffset < 180f )
						xAngleOffset = 13f;
					
					if( xAngleOffset > 180f && xAngleOffset < 355f )
						xAngleOffset = 355f;
				}
			}
			else
			{
				if( xAngleOffset > 60f && xAngleOffset < 180f )
					xAngleOffset = 60f;
				
				if( xAngleOffset > 180f && xAngleOffset < 290f )
					xAngleOffset = 290f;
			}

			if( !viewInputThisFrame )
				xAngleOffset = Mathf.MoveTowardsAngle( xAngleOffset, 4f, 37.5f * Time.deltaTime );

			cameraRotationOffset.eulerAngles = new Vector3( xAngleOffset, cameraRotationOffset.eulerAngles.y, cameraRotationOffset.eulerAngles.z );
			cameraTransform.rotation *= cameraRotationOffset;
			
			cameraTransform.eulerAngles = new Vector3( cameraTransform.eulerAngles.x, cameraTransform.eulerAngles.y, 0f );
		}
	}

	private float AngleDistance(float a, float b)
	{
		a = Mathf.Repeat( a, 360 );
		b = Mathf.Repeat( b, 360 );
		
		return Mathf.Abs( b - a );
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
		if( GameAgent.GetCurrentGameState() != GameAgent.GameState.Game || isTutorializing )
			return;

		if( currentState == State.Voyeur )
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
					viewChangeVector = new Vector2( viewChangeVector.x, Mathf.Clamp( viewChangeVector.y + viewChangeRate * Time.deltaTime * ( invertY ? -1f : 1f ), -1f, 1f ) );
				} break;
					
				case InputController.ButtonType.RDown: 
				{
					viewChangeVector = new Vector2( viewChangeVector.x, Mathf.Clamp( viewChangeVector.y - viewChangeRate * Time.deltaTime * ( invertY ? -1f : 1f ), -1f, 1f ) );
				} break;
			}
		}
		else
		{
			switch( button )
			{
					case InputController.ButtonType.Left:
					{	
						viewChangeVector = new Vector2( Mathf.Clamp( viewChangeVector.x - viewChangeRate * Time.deltaTime, -1f, 1f ), viewChangeVector.y );
					} break;
						
					case InputController.ButtonType.Right: 
					{
						viewChangeVector = new Vector2( Mathf.Clamp( viewChangeVector.x + viewChangeRate * Time.deltaTime, -1f, 1f ), viewChangeVector.y );
					} break;
						
					case InputController.ButtonType.Up: 
					{
						viewChangeVector = new Vector2( viewChangeVector.x, Mathf.Clamp( viewChangeVector.y + viewChangeRate * Time.deltaTime * ( invertY ? -1f : 1f ), -1f, 1f ) );
					} break;
						
					case InputController.ButtonType.Down: 
					{
						viewChangeVector = new Vector2( viewChangeVector.x, Mathf.Clamp( viewChangeVector.y - viewChangeRate * Time.deltaTime * ( invertY ? -1f : 1f ), -1f, 1f ) );
					} break;
			}
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

	private void DisableUI()
	{
		if( flashUI )
		{
			flashUI.enabled = false;
			flashUI.color = whiteClear;
		}
		if( screenshotUI )
		{
			screenshotUI.enabled = false;
			screenshotUI.color = whiteClear;
		}

		if( photoFrameUI )
		{
			photoFrameUI.enabled = false;
		}
		
		if( camUI )
		{
			camUI.enabled = false;
		}
		
		if( rageUI )
		{
			rageUI.enabled = false;
		}
		
		if( compassUI )
		{
			compassUI.enabled = false;
		}

		if( motionBlindUI )
		{
			motionBlindUI.enabled = false;
		}

		if( tutorialUI )
		{
			tutorialUI.enabled = false;
		}
	}

	private void KillPlayer()
	{
		FallApart();
		StopStatuses();
		ChangeState( (int)State.Voyeur );
		DisplayMessage( "Your friend destroyed you" );
		ChangeColor( new Quaternion( 0f, 0f, 0f, 0f ) );

		if( uiFSM )
			uiFSM.SendEvent( "UI_People_TrappedFoever" );

		PlayerAgent.MessagePlayersLeft();
	}

	private string NextRandomPose()
	{
		if( poseDeck.Count < 2 )
		{
			for( int i = 0; i < movementPoses.Length; i++ )
			{
				if( !poseDeck.Contains( movementPoses[i] ) )
					poseDeck.Add( movementPoses[i] );
			}

			int randomValue;
			string temp;

			for( int i = 1; i < poseDeck.Count; i++ )
			{
				randomValue = Random.Range( 1, poseDeck.Count );
				temp = poseDeck[i];
				poseDeck[i] = poseDeck[randomValue];
				poseDeck[randomValue] = temp;
			}
		}

		string randomPose = poseDeck[0];

		poseDeck.RemoveAt( 0 );

		return randomPose;
	}

	private void SetPose( string pose )
	{
		if( animation )
			animation.Play( pose );

		photonView.RPC( "RPCSetPose", PhotonTargets.OthersBuffered, pose );
	}

	private void PhotographMannequin( int ID )
	{
		MannequinAgent.DestroyMannequin( ID );

		photonView.RPC( "RPCPhotographMannequin", PhotonTargets.OthersBuffered, ID );
	}

	private void CreateMannequin( Vector3 position )
	{
		MannequinAgent.CreateMonsterMannequin( position );

		photonView.RPC( "RPCCreateMannequin", PhotonTargets.OthersBuffered, position );
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
		if( ( zoomProgress != 1f ) || hasPhoto )
			yield break;

		hasPhoto = true;

		SetFlashBulbTo( true );

		if( flashUI )
		{
			flashUI.color = Color.white;
			flashUI.enabled = true;
		}

		StartCoroutine( "PlayCameraCooldown" );

		bool oldFlashlightState = ( flashlight && flashlight.enabled );

		SetFlashlightTo( false );


		photographedObjects.Clear();

		isTakingPhoto = true;

		yield return new WaitForSeconds( 0.1f );

		isTakingPhoto = false;

		if( flashUI )
			StartCoroutine( DoImageColorFade( flashUI, Color.white, whiteClear, 0.25f ) );

		for( int i = 0; i < photographedObjects.Count; i++ )
		{
			if( photographedObjects[i].tag == "Player" )
			{
				PlayerController otherPlayerController = photographedObjects[i].GetComponent<PlayerController>();

				if( otherPlayerController )
				{
					if( PlayerAgent.GetIsPlayerMonster( otherPlayerController ) )
						otherPlayerController.ChangeColor(  new Quaternion( 0.75f, 0f, 0f, 1f ), false );
					else
						otherPlayerController.ChangeColor( new Quaternion( 0f, 0.75f, 0f, 1f ), false );
				}
			}
			else if( photographedObjects[i].tag == "Activatable" )
			{
				PlayMakerFSM fsm = photographedObjects[i].GetComponent<PlayMakerFSM>();
				
				if( fsm )
					fsm.SendEvent( "CameraReveal" );
			}
		}

		isWaitingForPhotoFinish = true;
		
		ScreenshotAgent.instance.OnPostRenderFinish += OnScreenshotFinish;
		ScreenshotAgent.Enable();
		
		while( isWaitingForPhotoFinish )
			yield return null;

		SetFlashBulbTo( false );

		for( int i = 0; i < photographedObjects.Count; i++ )
		{
			if( photographedObjects[i].tag == "Player" )
			{
				PlayerController otherPlayerController = photographedObjects[i].GetComponent<PlayerController>();
				
				if( otherPlayerController )
				{
					if( otherPlayerController.GetCurrentState() == State.Monster || otherPlayerController.GetCurrentState() == State.Raging )
					{
						otherPlayerController.ChangeColor( new Quaternion( 1f, 0.95f, 0.95f, 1f ), false );
						otherPlayerController.MonsterReveal();
					}
					else if( otherPlayerController.GetCurrentState() == State.Normal || otherPlayerController.GetCurrentState() == State.None || otherPlayerController.GetCurrentState() == State.Frozen || otherPlayerController.GetCurrentState() == State.Stunned )
					{
						otherPlayerController.ChangeColor( new Quaternion( 1f, 0.95f, 0.95f, 1f ), false );
						otherPlayerController.SurvivorReveal();
					}
				}
			}
			else if( photographedObjects[i].tag == "Activatable" )
			{
				PhotographMannequin( MannequinAgent.GetIDByMannequin( photographedObjects[i] ) );
			}
		}

		float currentTime = 0f;
		float lerp;
		float duration = 0.5f;

		Vector3 fromPosition = new Vector3( -596f, -654f, 0f );
		Vector3 toPosition = new Vector3( -537f, -207f, 0f );

		if( photoFrameUI )
		{
			photoFrameUI.color = Color.white;
			photoFrameUI.enabled = true;
		}

		if( screenshotUI )
		{
			screenshotUI.enabled = true;

			StartCoroutine( DoRawImageColorFade( screenshotUI, Color.black, Color.white, 1f ) );
		}

		if( uiFSM )
			uiFSM.SendEvent( "UI_Screenshot_in" );

		//yield return new WaitForSeconds( 0.75f );

		yield return new WaitForSeconds( 7f );

		if( flashUI )
			flashUI.enabled = false;

		//if( photoFrameUI )
		//	StartCoroutine( DoColorFade( photoFrameUI, Color.white, whiteClear, 0.5f ) );

		//if( screenshotUI )
		//	StartCoroutine( DoColorFade( screenshotUI, Color.white, whiteClear, 0.5f ) );

		if( uiFSM )
			uiFSM.SendEvent( "UI_Screenshot_out" );

		yield return new WaitForSeconds( 1f );

		if( photoFrameUI )
			photoFrameUI.enabled = false;

		if( screenshotUI )
			screenshotUI.enabled = false;

		SetFlashlightTo( oldFlashlightState );

		hasPhoto = false;

		if( compassUI && MannequinAgent.GetNumActiveMannequins() <= PlayerAgent.GetCompassActivationNumber() )
			compassUI.enabled = true;
	}

	private IEnumerator RageMode()
	{
		if( ( zoomProgress != 1f ) )
			yield break;

		ChangeState( (int)State.Raging );
		//ChangeColor(  new Quaternion( 0.75f, 0f, 0f, 1f ) );

		SetPose( pouncePose );

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

		SetPose( stunPose );

		//MonsterReveal();

		/*
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
		*/

		if( rageController )
			Destroy( rageController );

		ChangeColor( new Quaternion( 1f, 0.95f, 0.95f, 1f ) );
		ChangeState( (int)State.Monster );

		SetPose( NextRandomPose() );
	}

	private IEnumerator DoStun()
	{
		if( currentState == State.Stunned || currentState == State.Dead || currentState == State.Voyeur )
			yield break;

		State originalState = currentState;

		ChangeState( (int)State.Stunned );

		SetPose( stunPose );

		if( photonView.isMine )
			gameObject.AddComponent<StunController>();

		yield return new WaitForSeconds( StunController.StunDuration );

		ChangeColor( new Quaternion( 1f, 0.95f, 0.95f, 1f ) );

		RemoveStunEffect();

		ChangeState( (int)originalState );

		SetPose( NextRandomPose() );
	}

	private IEnumerator DoFreeze()
	{
		float beginTime = Time.time;

		DisplayMessage( "You've been frozen in place" );

		yield return new WaitForSeconds( messageDisplayDuration );

		int timeLeft;

		while( Time.time - beginTime < freezeDuration )
		{
			timeLeft = Mathf.RoundToInt( freezeDuration - ( Time.time - beginTime ) ) + 1;

			DisplayMessage( "" + timeLeft );

			yield return new WaitForSeconds( 1f );
		}

		DisplayMessage( "" );

		if( currentState == State.Frozen )
			ChangeState( (int)State.None );
	}

	private IEnumerator DoRawImageColorFade( RawImage image, Color fromColor, Color toColor, float duration )
	{
		image.color = fromColor;
		
		float beginTime = Time.time;
		float currentTime = 0f;
		float lerp = 0f;
		
		do
		{
			currentTime += Time.deltaTime;
			lerp = currentTime / duration;
			
			image.color = Color.Lerp( fromColor, toColor, lerp );
			
			yield return null;
			
		} while ( currentTime < duration );
		
		image.color = toColor;
	}

	private IEnumerator DoImageColorFade( Image image, Color fromColor, Color toColor, float duration )
	{
		image.color = fromColor;
		
		float beginTime = Time.time;
		float currentTime = 0f;
		float lerp = 0f;
		
		do
		{
			currentTime += Time.deltaTime;
			lerp = currentTime / duration;
			
			image.color = Color.Lerp( fromColor, toColor, lerp );
			
			yield return null;
			
		} while ( currentTime < duration );
		
		image.color = toColor;
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

	private IEnumerator DoMovementRefocus()
	{
		isRefocusing = true;

		float beginTime = Time.time;
		float currentTime = 0f;
		float lerp;

		do
		{
			currentTime += Time.deltaTime;
			lerp = Mathf.Clamp01( Mathf.Pow( currentTime / movementRefocusDuration, 0.5f ) );

			cameraTransform.GetComponent<Camera>().fieldOfView = Mathf.Lerp( cameraFoV, cameraZoomFoV, zoomProgress ) * Mathf.Lerp( movementRefocusAmount, 1f, lerp );

			yield return null;

		} while( currentTime < movementRefocusDuration );

		isRefocusing = false;
	}

	private IEnumerator DoMotionBlind()
	{
		if( motionBlindUI == null )
			yield break;

		motionBlindUI.enabled = true;

		yield return new WaitForSeconds( 0.1f );

		motionBlindUI.enabled = false;
	}

	private IEnumerator DoTutorial( Sprite[] images )
	{
		if( tutorialUI == null || isTutorializing )
			yield break;

		tutorialUI.enabled = true;

		yield return null;

		isTutorializing = true;

		tutorialIndex = 0;
		int oldTutorialIndex = -1;

		List<Sprite> subImages = new List<Sprite>();
		int subTutorialIndex = -1;

		float currentTime = 0f;

		while( tutorialIndex < images.Length )
		{
			if( tutorialIndex != oldTutorialIndex )
				tutorialIndex = Mathf.Clamp( tutorialIndex, 0, images.Length );

			if( tutorialIndex > oldTutorialIndex )
			{
				subImages.Clear();
				subTutorialIndex = -1;

				string name = images[ tutorialIndex ].name;

				if( name.Substring( name.Length - 2, 1 ) == "-" )
				{
					string subname = name.Substring( 0, name.Length - 1 );

					while( tutorialIndex < images.Length && name.Substring( 0, name.Length - 1 ) == subname )
					{
						subImages.Add( images[ tutorialIndex ] );

						tutorialIndex++;

						if( tutorialIndex < images.Length )
							name = images[ tutorialIndex ].name;
					}

					tutorialIndex--;
				}
				else
				{
					subImages.Add( images[ tutorialIndex ] );
				}

				currentTime = gifTime;
			}
			else if( tutorialIndex < oldTutorialIndex )
			{
				subImages.Clear();
				subTutorialIndex = -1;

				string name = images[ tutorialIndex ].name;
				string oldName = images[ oldTutorialIndex ].name;

				while( tutorialIndex > 0 && name.Substring( 0, name.Length - 1 ) == oldName.Substring( 0, oldName.Length - 1 ) )
				{
					tutorialIndex--;
					name = images[ tutorialIndex ].name;
				}

				if( name.Substring( name.Length - 2, 1 ) == "-" )
				{
					string subname = name.Substring( 0, name.Length - 1 );

					int newTutorialIndex = tutorialIndex;

					while( tutorialIndex >= 0 && name.Substring( 0, name.Length - 1 ) == subname )
					{
						subImages.Insert( 0, images[ tutorialIndex ] );
						
						tutorialIndex--;
						name = images[ tutorialIndex ].name;
					}
					
					tutorialIndex = newTutorialIndex;
				}
				else
				{
					subImages.Add( images[ tutorialIndex ] );
				}
				
				currentTime = gifTime;
			}

			currentTime += Time.deltaTime;

			if( currentTime >= gifTime && subImages.Count > 0 )
			{
				subTutorialIndex = ( subTutorialIndex + 1 )%subImages.Count;

				tutorialUI.sprite = subImages[ subTutorialIndex ];

				while( currentTime > gifTime )
					currentTime -= gifTime;
			}

			oldTutorialIndex = tutorialIndex;

			yield return null;
		}

		tutorialUI.enabled = false;
		isTutorializing = false;
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
		DisableUI();

		if( UIRootObject )
			UIRootObject.SetActive( true );

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

		if( wireframeParticleSystem )
			wireframeParticleSystem.Stop();

		GameAgent.ChangeGameState( GameAgent.GameState.Game );

		StartCoroutine( "DoTutorial", everyoneTutorialImages );
	}

	[RPC]
	public void RPCEndGame()
	{
		StopStatuses();
		
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

		messageString = "";

		DisableUI();
		
		if( UIRootObject )
			UIRootObject.SetActive( false );

		GameAgent.ChangeGameState( GameAgent.GameState.End );
	}

	[RPC]
	public void RPCStun()
	{
		StartCoroutine( "DoStun" );
	}

	[RPC]
	public void RPCFreeze()
	{
		StartCoroutine( "DoFreeze" );
	}

	[RPC]
	public void RPCStopStatuses()
	{
		StopCoroutine( "DoFreeze" );
		StopCoroutine( "DoStun" );
		StopCoroutine( "RageMode" );
	}

	[RPC]
	public void RPCFallApart()
	{
		if( deathModel )
		{
			deathModel.transform.parent = null;
			deathModel.SetActive( true );
		}
	}

	[RPC]
	public void RPCDrawParticles()
	{
		if( movementParticleSystem )
			movementParticleSystem.Play();
		
		if( wireframeParticleSystem )
			wireframeParticleSystem.Play();
	}

	[RPC]
	public void RPCSetPose( string pose )
	{
		if( animation )
			animation.Play( pose );
	}

	[RPC]
	public void RPCPhotographMannequin( int ID )
	{
		MannequinAgent.DestroyMannequin( ID );
	}

	[RPC]
	private void RPCCreateMannequin( Vector3 position )
	{
		MannequinAgent.CreateMonsterMannequin( position );		
	}

	[RPC]
	private void RPCDisplayPlayersLeftForGatherers( int playersLeft )
	{
		if( uiFSM == null )
			return;

		switch( playersLeft )
		{
			case 1: uiFSM.SendEvent( "UI_People_1PeopleLeft" ); break;
			case 2: uiFSM.SendEvent( "UI_People_2PeopleLeft" ); break;
		}
	}

	[RPC]
	private void RPCDisplayPlayersLeftForDemon( int playersLeft )
	{
		if( uiFSM == null )
			return;
		
		switch( playersLeft )
		{
			case 1: uiFSM.SendEvent( "UI_Demon_1toGo" ); break;
			case 2: uiFSM.SendEvent( "UI_Demon_2toGo" ); break;
		}
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

		/*
		case InputController.ButtonType.LeftShoulder: 
		{
			SetShoulderOffset( -1f );
		} break;

		case InputController.ButtonType.RightShoulder: 
		{
			SetShoulderOffset( 1f );
		} break;
		*/
		
		case InputController.ButtonType.Start: 
		{
			if( isTutorializing )
			{
				StopCoroutine( "DoTutorial" );
				tutorialUI.enabled = false;
				isTutorializing = false;
			}
		} break;

		case InputController.ButtonType.X:
		{
			if( currentState == State.Monster )
				CreateMannequin( transform.position );
		} break;

		case InputController.ButtonType.A:
		{
			if( isTutorializing )
				tutorialIndex++;
			else
				didMove = true;
		} break;

		case InputController.ButtonType.Y:
		{
			invertY = !invertY;
		} break;

		case InputController.ButtonType.B:
		{
			if( isTutorializing )
				tutorialIndex--;
		} break;
		}
	}
	
	private void OnButtonHeld( InputController.ButtonType button )
	{	
		EvaluateViewChange( button );
	}
	
	private void OnButtonUp( InputController.ButtonType button )
	{
		EvaluateViewChange( button );

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

		if( screenshotUI )
			screenshotUI.texture = ScreenshotAgent.GetTexture();

		isWaitingForPhotoFinish = false;
	}
	//end event handlers

	//public functions
	public void ChangeColider( int state )
	{
		if( characterController )
			characterController.enabled = ( state == 1 );

		photonView.RPC( "RPCChangeColider", PhotonTargets.OthersBuffered, state );
	}

	public void ChangeColor( Quaternion colorVector4, bool sync = true )
	{
		if( modelRenderers != null )
		{
			Color color = new Color( colorVector4.x, colorVector4.y, colorVector4.z, colorVector4.w );
			
			for( int i = 0; i < modelRenderers.Length; i++ )
				modelRenderers[i].material.color = color;
		}

		if( sync )
			photonView.RPC( "RPCChangeColor", PhotonTargets.OthersBuffered, colorVector4 );
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

	public void IncreaseFear()
	{
		if( currentState == State.None )
		{
			ChangeState( (int)State.Frozen );
			Freeze();
		}
		else if( currentState == State.Stunned )
		{
			KillPlayer();
		}

		PlayerAgent.CheckForEnd();
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
		return isTakingPhoto || currentState == State.Raging;
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

		if( camUI )
			camUI.enabled = false;

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

		if( uiFSM )
			uiFSM.SendEvent( "UI_Demonized" );

		StartCoroutine( "DoTutorial", demonTutorialImages );
	}

	public void ShowMonsterTutorial()
	{
		StartCoroutine( "DoTutorial", gathererTutorialImages );
	}

	public void RageHit()
	{
		StopCoroutine( "RageMode" );
		ChangeState( (int)State.Monster );
		DisplayMessage( "Trapped your friend" );
		ChangeColor( new Quaternion( 1f, 0.95f, 0.95f, 1f ) );
	}

	public void MonsterReveal()
	{
		StopCoroutine( "RageMode" );

		RageController rageController = gameObject.GetComponent<RageController>();
		
		if( rageController )
			Destroy( rageController );

		ChangeState( (int)State.Monster );

		//ChangeColor( new Quaternion( 1f, 0.5f, 0.75f, 1f ) );
		Stun();
	}

	public void SurvivorReveal()
	{
		if( currentState == State.Frozen )
		{
			KillPlayer();
			
			PlayerAgent.CheckForEnd();
		}
		else
		{
			//ChangeColor( new Quaternion( 0f, 0.75f, 0f, 1f ), false );
			Stun();
		}
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
		DisableUI();

		if( UIRootObject )
			UIRootObject.SetActive( true );

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

		StartCoroutine( "DoTutorial", everyoneTutorialImages );

		photonView.RPC( "RPCStartGame", PhotonTargets.OthersBuffered );
	}

	public void EndGame()
	{
		StopStatuses();
		
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

		messageString = "";
		
		DisableUI();
		
		if( UIRootObject )
			UIRootObject.SetActive( false );

		GameAgent.ChangeGameState( GameAgent.GameState.End );

		photonView.RPC( "RPCEndGame", PhotonTargets.OthersBuffered );
	}

	public void Stun()
	{
		photonView.RPC( "RPCStun", PhotonTargets.OthersBuffered );

		StartCoroutine( "DoStun" );
	}

	public void Freeze()
	{
		photonView.RPC( "RPCFreeze", PhotonTargets.OthersBuffered );
		
		StartCoroutine( "DoFreeze" );
	}

	public void StopStatuses()
	{
		photonView.RPC( "RPCStopStatuses", PhotonTargets.OthersBuffered );
		
		StopCoroutine( "DoFreeze" );
		StopCoroutine( "DoStun" );
		StopCoroutine( "RageMode" );
	}

	public void FallApart()
	{
		photonView.RPC( "RPCFallApart", PhotonTargets.OthersBuffered );

		if( deathModel )
		{
			deathModel.transform.parent = null;
			deathModel.SetActive( true );
		}
	}

	public void DrawParticles()
	{
		photonView.RPC( "RPCDrawParticles", PhotonTargets.OthersBuffered );

		if( movementParticleSystem )
			movementParticleSystem.Play();
		
		if( wireframeParticleSystem )
			wireframeParticleSystem.Play();
	}

	public void TeleportTo( Vector3 coordinate )
	{
		transform.position = coordinate;
		transform.position = new Vector3( transform.position.x, height, transform.position.z );
	}

	public void AddPhotographedObject( GameObject photographedObject )
	{
		if( !photographedObjects.Contains( photographedObject ) )
			photographedObjects.Add( photographedObject );
	}

	public void DisplayPlayersLeftForGatherers( int playersLeft )
	{
		photonView.RPC( "RPCDisplayPlayersLeftForGatherers", PhotonTargets.OthersBuffered, playersLeft );
		
		if( uiFSM == null )
			return;
		
		switch( playersLeft )
		{
			case 1: uiFSM.SendEvent( "UI_People_1PeopleLeft" ); break;
			case 2: uiFSM.SendEvent( "UI_People_2PeopleLeft" ); break;
		}
	}
	
	public void DisplayPlayersLeftForDemon( int playersLeft )
	{
		photonView.RPC( "RPCDisplayPlayersLeftForDemon", PhotonTargets.OthersBuffered, playersLeft );
		
		if( uiFSM == null )
			return;
		
		switch( playersLeft )
		{
			case 1: uiFSM.SendEvent( "UI_Demon_1toGo" ); break;
			case 2: uiFSM.SendEvent( "UI_Demon_2toGo" ); break;
		}
	}
	//end public functions
}
