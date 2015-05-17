﻿using UnityEngine;
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
		Room = 8,
		Intro = 9,
		Outro = 10,
		BackToStart = 11,
		BackToLobby = 12,
		Invalid = 11,
	}
	private List<GameState> gameStateStack = new List<GameState>();

	public GameObject[] gameStatePrefabs = new GameObject[ Enum.GetNames( typeof( GameState ) ).Length - 1 ];
	private List<GameObject> gameStateObjectStack = new List<GameObject>();

	public GameObject menuControllerPrefab = null;
	public GameObject darkQuadPrefab = null;

	private GameObject menuController = null;
	private GameObject darkQuad = null;

	private bool isPendingChange = false;

	private float resetTime;
	private float resetDuration = 2f;

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

		//Application.targetFrameRate = 1;
	}

	void Start()
	{
		//Cursor.visible does nothing, this is a unity bug
		//TODO activate when unity gets its shit together
		/*
		if( !Application.isEditor )
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		else
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
		*/

		Camera.main.gameObject.AddComponent<ScreenshotAgent>();

		if( menuControllerPrefab )
			menuController = Instantiate( menuControllerPrefab ) as GameObject;

		if( menuController )
			menuController.SetActive( false );

		if( darkQuadPrefab )
			darkQuad = Instantiate( darkQuadPrefab ) as GameObject; 

		PushGameState( GameState.Intro );
	}

	void Update()
	{
		if( Input.GetKeyDown( KeyCode.Escape ) )
			Application.Quit();

		if( GetCurrentGameState() == GameState.Game )
		{
			if( Input.GetKeyDown( KeyCode.R ) )
				resetTime = 0f;

			if( Input.GetKey( KeyCode.R ) )
				resetTime += Time.deltaTime;

			if( resetTime > resetDuration )
			{
				resetTime = 0f;

				NegativeEffect negativeEffect = Camera.main.gameObject.GetComponent<NegativeEffect>();
				
				if( negativeEffect )
					Destroy( negativeEffect );
				
				StunController stunController = gameObject.GetComponent<StunController>();
				
				if( stunController )
					Destroy( stunController );
				
				MotionBlur motionBlur = Camera.main.gameObject.GetComponent<MotionBlur>();
				
				if( motionBlur )
					motionBlur.enabled = false;

				NetworkAgent.LeaveRoom();
			}
		}
	}

	/*
	void OnGUI()
	{
		GUI.Label( new Rect( 10f, 10f, 1000f, 1000f ), "" + GetCurrentGameState() );
	}
	*/

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

		if( gameStateObjectStack.Count > 0 )
		{
			Destroy( gameStateObjectStack[0] );
			gameStateObjectStack.RemoveAt( 0 );
		}

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
