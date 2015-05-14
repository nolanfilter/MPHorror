using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlayerAgent : MonoBehaviour {

	private List<PlayerController> playerControllers;
	
	public Shader stunShader;
	public Shader monsterShader;
	public Shader fastBloomShader;
	public Shader blurShader;
	public Shader rgbShader;
	public Shader yuvShader;
	public Shader brushEffectShader;

	public Texture grainTexture;
	public Texture scratchTexture;

	public AudioClip cameraCooldownClip;

	public bool monsterize = true;
	public bool monsterizeMaster = false;
	public bool checkForEnd = true;

	private bool isEnding = false;

	private PlayerController client;

	private int monsterID = -1;

	public float waitTime = 25f;
	public float endBuffer = 8f;
	public int monsterizingMannequinNumber = 13;
	public int compassActivationNumber = 25;

	private Dictionary<int, float> potentialMonstersByTime;
	private float monsterizeBuffer = 0.5f;

	private static PlayerAgent mInstance = null;
	public static PlayerAgent instance
	{
		get
		{
			return mInstance;
		}
	}
	
	void Awake()
	{
		if( mInstance != null )
		{
			Debug.LogError( string.Format( "Only one instance of PlayerAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;

		playerControllers = new List<PlayerController>();
		potentialMonstersByTime = new Dictionary<int, float>();
	}

	void Start()
	{
		//turn off camera effects to start
		//js
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

		//add new effects
		//js
		FastBloom fastBloom = Camera.main.gameObject.AddComponent<FastBloom>();

		fastBloom.enabled = false;
		fastBloom.fastBloomShader = fastBloomShader;

		//js
		Blur blur = Camera.main.gameObject.AddComponent<Blur>();

		blur.enabled = false;
		blur.blurShader = blurShader;

		BrushEffect brushEffect = Camera.main.gameObject.AddComponent<BrushEffect>();

		brushEffect.enabled = false;
		brushEffect.BrushEffectShader = brushEffectShader;

		NoiseEffect noiseEffect = Camera.main.gameObject.AddComponent<NoiseEffect>();

		noiseEffect.enabled = false;
		noiseEffect.grainTexture = grainTexture;
		noiseEffect.scratchTexture = scratchTexture;
		noiseEffect.shaderRGB = rgbShader;
		noiseEffect.shaderYUV = yuvShader;
	}

	public static void RegisterPlayer( PlayerController playerController, bool isClient )
	{
		if( instance )
			instance.internalRegisterPlayer( playerController, isClient );
	}

	private void internalRegisterPlayer( PlayerController playerController, bool isClient )
	{
		if( !playerControllers.Contains( playerController ) )
		{
			int viewID = playerController.photonView.viewID;

			int index = 0;

			while( index < playerControllers.Count && viewID > playerControllers[index].photonView.viewID )
				index++;

			playerControllers.Insert( index, playerController );
		}

		if( isClient )
			client = playerController;
	}

	public static void UnregisterPlayer( PlayerController playerController )
	{
		if( instance )
			instance.internalUnregisterPlayer( playerController );
	}

	private void internalUnregisterPlayer( PlayerController playerController )
	{
		if( playerControllers.Contains( playerController ) )
			playerControllers.Remove( playerController );

		if( client == playerController )
			client = null;

		if( playerControllers.Count == 0 )
			monsterID = -1;
	}

	public static int GetMonsterizingMannequinNumber()
	{
		if( instance )
			return instance.monsterizingMannequinNumber;

		return -1;
	}

	public static int GetCompassActivationNumber()
	{
		if( instance )
			return instance.compassActivationNumber;
		
		return -1;
	}

	public static bool GetIsMonsterSet()
	{
		if( instance )
			return instance.monsterID != -1;

		return false;
	}

	public static void CheckForEnd()
	{
		if( instance )
			instance.internalCheckForEnd();
	}

	private void internalCheckForEnd()
	{
		if( !checkForEnd || GameAgent.GetCurrentGameState() != GameAgent.GameState.Game )
			return;

		bool isOver = true;

		PlayerController.State state;

		for( int i = 0; i < playerControllers.Count; i++ )
		{
			state = playerControllers[i].GetCurrentState();

			if( i != monsterID && state != PlayerController.State.Dead && state != PlayerController.State.Voyeur && state != PlayerController.State.Frozen )
				isOver = false;
		}

		if( !isOver )
			isOver = MannequinAgent.GetAllMannequinsDisabled();

		if( isOver )
			StartCoroutine( "WaitAndEnd" );
	}

	public static PlayerController.State GetClientState()
	{
		if( instance )
			return instance.internalGetClientState();

		return PlayerController.State.Invalid;
	}

	private PlayerController.State internalGetClientState()
	{
		if( client != null && monsterID >= 0 && monsterID < playerControllers.Count )
		{
			if( client == playerControllers[ monsterID ] )
				return PlayerController.State.Monster;

			return client.GetCurrentState();
		}

		return PlayerController.State.Invalid;
	}

	public static Shader GetMonsterShader()
	{
		if( instance )
			return instance.monsterShader;

		return null;
	}

	public static Shader GetStunShader()
	{
		if( instance )
			return instance.stunShader;

		return null;
	}

	//TODO move to AudioAgent
	public static AudioClip GetCameraCooldownClip()
	{
		if( instance )
			return instance.cameraCooldownClip;

		return null;
	}

	public static void StartGame()
	{
		if( instance )
			instance.internalStartGame();
	}

	private void internalStartGame()
	{
		NetworkAgent.LockRoom();
		potentialMonstersByTime.Clear();
		monsterID = -1;

		for( int i = 0; i < playerControllers.Count; i++ )
			playerControllers[i].StartGame();
	}
	
	public static void EndGame()
	{
		if( instance )
			instance.internalEndGame();
	}

	private void internalEndGame()
	{
		for( int i = 0; i < playerControllers.Count; i++ )
			playerControllers[i].EndGame();
	}

	public static void MonsterizeNearestPlayer( Vector3 position )
	{
		if( instance )
			instance.internalMonsterizeNearestPlayer( position );
	}

	private void internalMonsterizeNearestPlayer( Vector3 position )
	{
		if( !monsterize || monsterID != -1 )
			return;

		float closestDistance = Mathf.Infinity;
		float distance;
		int closestPlayerID = -1;

		for( int i = 0; i < playerControllers.Count; i++ )
		{
			distance = Vector3.Distance( playerControllers[i].transform.position, position );
				
			if( distance < closestDistance )
			{
				closestDistance = distance;
				closestPlayerID = i;
			}
		}

		if( potentialMonstersByTime.Count == 0 )
			StartCoroutine( "DoMonsterizeBuffer" );

		if( potentialMonstersByTime.ContainsKey( closestPlayerID ) )
			potentialMonstersByTime[ closestPlayerID ] = Mathf.Min( potentialMonstersByTime[ closestPlayerID ], Time.time );
		else
			potentialMonstersByTime.Add( closestPlayerID, Time.time );
	}

	public static void SetMonster()
	{
		if( instance )
			instance.internalSetMonster();
	}

	private void internalSetMonster()
	{
		return;

		if( monsterize )
		{
			if( monsterizeMaster )
				StartCoroutine( "WaitAndMonsterizeMaster" );
			else
				StartCoroutine( "WaitAndMonsterizeRandom" );
		}
	}

	public static bool GetIsPlayerMonster( PlayerController playerController )
	{
		if( instance )
			return instance.internalGetIsPlayerMonster( playerController );

		return false;
	}

	private bool internalGetIsPlayerMonster( PlayerController playerController )
	{
		int playerID = playerControllers.IndexOf( playerController );

		if( playerID != -1 )
			return ( playerID == monsterID );

		return false;
	}

	public static float GetClosestPlayerPosition( Vector3 currentPosition )
	{
		if( instance )
			return instance.internalGetClosestPlayerPosition( currentPosition );

		return Mathf.Infinity;
	}

	private float internalGetClosestPlayerPosition( Vector3 currentPosition )
	{
		float distance;
		float closestDistance = Mathf.Infinity;

		for( int i = 0; i < playerControllers.Count; i++ )
		{
			if( playerControllers[i].transform.position != currentPosition && playerControllers[i].GetCurrentState() != PlayerController.State.Dead )
			{
				distance = Vector3.Distance( playerControllers[i].transform.position, currentPosition );

				if( distance < closestDistance )
					closestDistance = distance;
			}
		}

		return closestDistance;
	}

	public void SetAllFlashlightsTo( bool on )
	{
		for( int i = 0; i < playerControllers.Count; i++ )
			playerControllers[i].SetFlashlightTo( on );
	}

	public void TeleportAllTo( string coordinates )
	{
		string[] splitCoordinates = coordinates.Split( ',' );

		for( int i = 0; i < playerControllers.Count; i++ )
			playerControllers[i].TeleportTo( new Vector3( float.Parse( splitCoordinates[ i * 2 ] ), 0f, float.Parse( splitCoordinates[ i * 2 + 1 ] ) ) );
	}

	public void GlobalMessageDisplay( string messageToDisplay )
	{
		for( int i = 0; i < playerControllers.Count; i++ )
			playerControllers[i].DisplayMessage( messageToDisplay );
	}

	public void AllButMonsterMessageDisplay( string messageToDisplay )
	{
		for( int i = 0; i < playerControllers.Count; i++ )
			if( i != monsterID )
				playerControllers[i].DisplayMessage( messageToDisplay );
	}

	private IEnumerator WaitAndMonsterizeMaster()
	{
		monsterID = 0;
		
		yield return new WaitForSeconds( waitTime );

		if( monsterID < playerControllers.Count )
			playerControllers[ monsterID ].Monsterize();
	}

	private IEnumerator WaitAndMonsterizeRandom()
	{
		int seed = Utilities.HexToInt( PhotonNetwork.room.name[ PhotonNetwork.room.name.Length - 1 ] );
		
		Random.seed = seed;
		
		monsterID = Mathf.FloorToInt( Random.value * playerControllers.Count );

		yield return new WaitForSeconds( waitTime );
	
		if( monsterID < playerControllers.Count )
			playerControllers[ monsterID ].Monsterize();
	}

	private IEnumerator WaitAndEnd()
	{
		if( isEnding )
			yield break;

		isEnding = true;

		if( MannequinAgent.GetAllMannequinsDisabled() )
			GlobalMessageDisplay( "The healing is complete" );
		else
			GlobalMessageDisplay( "There are no heroes left" );

		yield return new WaitForSeconds( endBuffer );

		EndGame();

		isEnding = false;
	}

	private IEnumerator DoMonsterizeBuffer()
	{
		yield return new WaitForSeconds( monsterizeBuffer );

		float earliestTime = Mathf.Infinity;
		int earliestPlayerID = -1;

		foreach( KeyValuePair<int, float> kvp in potentialMonstersByTime )
		{
			if( kvp.Value < earliestTime )
			{
				earliestTime = kvp.Value;
				earliestPlayerID = kvp.Key;
			}
		}

		if( earliestPlayerID == -1 )
			yield break;

		monsterID = earliestPlayerID;

		if( monsterID >= 0 && monsterID < playerControllers.Count )
			playerControllers[ monsterID ].Monsterize();

		for( int i = 0; i < playerControllers.Count; i++ )
			if( i != monsterID )
				playerControllers[i].ShowMonsterTutorial();

		potentialMonstersByTime.Clear();
	}
}
