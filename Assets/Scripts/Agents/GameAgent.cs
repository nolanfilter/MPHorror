using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

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
		Options = 7,
		Invalid = 8,
	}
	private List<GameState> gameStateStack = new List<GameState>();

	public GameObject[] gameStatePrefabs = new GameObject[ Enum.GetNames( typeof( GameState ) ).Length - 1 ];
	private List<GameObject> gameStateObjectStack = new List<GameObject>();

	public GameObject menuControllerPrefab = null;
	public GameObject darkQuadPrefab = null;

	private GameObject menuController = null;
	private GameObject darkQuad = null;

	private bool isPendingChange = false;

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

		PushGameState( GameState.Start );
	}

	void Update()
	{
		if( Input.GetKeyDown( KeyCode.Escape ) )
			Application.Quit();
	}

	public static GameState GetCurrentGameState()
	{
		if( instance )
			return instance.internalGetCurrentGameState();

		return GameState.Invalid;
	}

	private GameState internalGetCurrentGameState()
	{
		if( gameStateStack.Count > 0 )
			return gameStateStack[0];

		return GameState.Invalid;
	}

	public static void ChangeGameState( GameState newGameState )
	{
		if( instance )
			instance.internalChangeGameState( newGameState );
	}

	private void internalChangeGameState( GameState newGameState )
	{
		if( GetCurrentGameState() == newGameState )
			return;

		isPendingChange = true;

		PopGameState();
		PushGameState( newGameState );

		isPendingChange = false;

		EvaluateCurrentState();
	}

	public static void PushGameState( GameState newGameState )
	{
		if( instance )
			instance.internalPushGameState( newGameState );
	}

	private void internalPushGameState( GameState newGameState )
	{
		if( GetCurrentGameState() == newGameState )
			return;

		if( gameStateObjectStack.Count > 0 && gameStateObjectStack[0] != null )
			gameStateObjectStack[0].SetActive( false );

		gameStateStack.Insert( 0, newGameState );

		if( gameStatePrefabs[ (int)newGameState ] != null )
			gameStateObjectStack.Insert( 0, Instantiate( gameStatePrefabs[ (int)newGameState ] ) as GameObject );

		if( !isPendingChange )
			EvaluateCurrentState();
	}

	public static void PopGameState()
	{
		if( instance )
			instance.internalPopGameState();
	}

	private void internalPopGameState()
	{
		if( gameStateStack.Count == 0 )
			return;

		gameStateStack.RemoveAt( 0 ) ;

		Destroy( gameStateObjectStack[0] );
		gameStateObjectStack.RemoveAt( 0 );

		if( !isPendingChange )
		{
			if( gameStateObjectStack.Count > 0 && gameStateObjectStack[0] != null )
				gameStateObjectStack[0].SetActive( true );

			EvaluateCurrentState();
		}
	}

	private void EvaluateCurrentState()
	{
		GameState currentGameState = GetCurrentGameState();

		if( menuController )
			menuController.SetActive( !( currentGameState == GameState.Game || currentGameState == GameState.Invalid ) );
	}
}
