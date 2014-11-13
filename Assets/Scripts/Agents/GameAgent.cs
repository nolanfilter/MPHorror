using UnityEngine;
using System.Collections;

public class GameAgent : MonoBehaviour {

	public enum GameState
	{
		Start = 0,
		Lobby = 1,
		Waiting = 2,
		Game = 3,
		End = 4,
		Settings = 5,
		Credits = 6,
		Invalid = 7,
	}
	private GameState currentGameState = GameState.Invalid;

	public GameObject menuControllerPrefab = null;
	public GameObject darkQuadPrefab = null;

	private GameObject menuController = null;
	private GameObject darkQuad = null;

	private static GameAgent mInstance = null;
	public static GameAgent instance
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
			Debug.LogError( string.Format( "Only one instance of GameAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;
	}

	void Start()
	{
		Camera.main.gameObject.AddComponent<ScreenshotAgent>();

		if( menuControllerPrefab )
			menuController = Instantiate( menuControllerPrefab ) as GameObject;

		if( menuController )
			menuController.SetActive( false );

		if( darkQuadPrefab )
			darkQuad = Instantiate( darkQuadPrefab ) as GameObject; 
	}

	public static void ChangeGameState( GameState newGameState )
	{
		if( instance )
			instance.internalChangeGameState( newGameState );
	}

	private void internalChangeGameState( GameState newGameState )
	{
		if( currentGameState == newGameState )
			return;

		currentGameState = newGameState;
	}
}
