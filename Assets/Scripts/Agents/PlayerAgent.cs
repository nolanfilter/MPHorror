using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAgent : MonoBehaviour {

	private List<PlayerController> playerControllers;

	public Shader monsterShader;
	public Shader stunShader;
	public Shader compositeShader;
	public Shader blurShader;
	public Shader downsampleShader;
	public Shader motionBlurShader;

	public bool monsterize = true;
	public bool monsterizeMaster = false;
	public bool checkForEnd = true;

	private bool isEnding = false;

	private PlayerController client;

	private int monsterID = -1;

	public float waitTime = 25f;
	public float endBuffer = 8f;

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
	}

	void Start()
	{
		GlowEffect glowEffect = Camera.main.gameObject.AddComponent<GlowEffect>();

		glowEffect.enabled = false;
		glowEffect.compositeShader = compositeShader;
		glowEffect.blurShader = blurShader;
		glowEffect.downsampleShader = downsampleShader;

		MotionBlur motionBlur = Camera.main.gameObject.AddComponent<MotionBlur>();

		motionBlur.enabled = false;
		motionBlur.shader = motionBlurShader;
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

	public static void CheckForEnd()
	{
		if( instance )
			instance.internalCheckForEnd();
	}

	private void internalCheckForEnd()
	{
		if( !checkForEnd )
			return;

		bool isOver = true;

		PlayerController.State state;

		for( int i = 0; i < playerControllers.Count; i++ )
		{
			state = playerControllers[i].GetCurrentState();

			if( i != monsterID && state != PlayerController.State.Dead && state != PlayerController.State.Voyeur )
				isOver = false;
		}

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
		if( client != null )
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

	public static void StartGame()
	{
		if( instance )
			instance.internalStartGame();
	}

	private void internalStartGame()
	{
		if( playerControllers.Count > 0 )
			playerControllers[ 0 ].StartGame();
	}

	public static void EndGame()
	{
		if( instance )
			instance.internalEndGame();
	}

	private void internalEndGame()
	{
		if( playerControllers.Count > 0 )
			playerControllers[ 0 ].EndGame();
	}

	public static void SetMonster()
	{
		if( instance )
			instance.internalSetMonster();
	}

	private void internalSetMonster()
	{
		if( monsterize )
		{
			if( monsterizeMaster )
				StartCoroutine( "WaitAndMonsterizeMaster" );
			else
				StartCoroutine( "WaitAndMonsterizeRandom" );
		}
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

		yield return new WaitForSeconds( endBuffer );

		EndGame();

		isEnding = false;
	}
}
