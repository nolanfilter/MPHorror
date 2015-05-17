using UnityEngine;
using System.Collections;

public class MenuController : MonoBehaviour {

	private InputController inputController;
	private float buttonBuffer = 0.1f;
	private float lastButtonTime;

	void Awake()
	{
		inputController = GetComponent<InputController>();
		
		if( inputController == null )
		{
			Debug.LogError( "No input controller on " + gameObject.name );
			enabled = false;
		}
	}

	void Start()
	{
		lastButtonTime = buttonBuffer * -1f;
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

	private void EvaluateButton( InputController.ButtonType button )
	{
		if( Time.time - lastButtonTime < buttonBuffer )
			return;

		switch( GameAgent.GetCurrentGameState() )
		{
			case GameAgent.GameState.Intro:
			{
				if( button == InputController.ButtonType.Start || button == InputController.ButtonType.A )
				{
					GameAgent.ChangeGameState( GameAgent.GameState.Start );
					lastButtonTime = Time.time;
				}
			} break;

			case GameAgent.GameState.Start:
			{
				if( button == InputController.ButtonType.Start || button == InputController.ButtonType.A )
				{
					StartScreen.Activate();
					lastButtonTime = Time.time;
				}
				else if( button == InputController.ButtonType.Left || button == InputController.ButtonType.RLeft )
				{
					StartScreen.HighlightPlay();
					lastButtonTime = Time.time;
				}
				else if( button == InputController.ButtonType.Right || button == InputController.ButtonType.RRight )
				{
					StartScreen.HighlightCredits();
					lastButtonTime = Time.time;
				}
			} break;

			case GameAgent.GameState.Credits:
			{
				if( button == InputController.ButtonType.Start || button == InputController.ButtonType.A || button == InputController.ButtonType.B )
				{
					GameAgent.ChangeGameState( GameAgent.GameState.Start );
					lastButtonTime = Time.time;
				}
			} break;

			case GameAgent.GameState.Lobby:
			{
				if( button == InputController.ButtonType.Start || button == InputController.ButtonType.A )
				{
					LobbyScreen.Activate();
					lastButtonTime = Time.time;
				}
				else if( button == InputController.ButtonType.Up || button == InputController.ButtonType.RUp )
				{
					LobbyScreen.HighlightHost();
					lastButtonTime = Time.time;
				}
				else if( button == InputController.ButtonType.Down || button == InputController.ButtonType.RDown )
				{
					LobbyScreen.HighlightJoin();
					lastButtonTime = Time.time;
				}
				else if( button == InputController.ButtonType.B )
				{
					GameAgent.ChangeGameState( GameAgent.GameState.BackToStart );
					lastButtonTime = Time.time;
				}
			} break;

			case GameAgent.GameState.Waiting:
			{
				if( !WaitingScreen.GetCanCancel() )
					return;

				if( button == InputController.ButtonType.B )
				{
					GameAgent.ChangeGameState( GameAgent.GameState.Start );
					lastButtonTime = Time.time;
				}
			} break;

			case GameAgent.GameState.Room:
			{
				if( button == InputController.ButtonType.Start || button == InputController.ButtonType.A )
				{
					RoomScreen.Activate();
					lastButtonTime = Time.time;
				} else if( button == InputController.ButtonType.B )
				{
					NetworkAgent.LeaveRoom();
					GameAgent.ChangeGameState( GameAgent.GameState.BackToLobby );
					lastButtonTime = Time.time;
				}
			} break;

			case GameAgent.GameState.End:
			{
				if( button == InputController.ButtonType.Start || button == InputController.ButtonType.A )
				{
					NetworkAgent.LeaveRoom();
					GameAgent.ChangeGameState( GameAgent.GameState.Lobby );
					lastButtonTime = Time.time;
				}
			} break;
		}
	}

	//event handlers
	private void OnButtonDown( InputController.ButtonType button )
	{	
		EvaluateButton( button );
	}
	
	private void OnButtonHeld( InputController.ButtonType button )
	{	

	}
	
	private void OnButtonUp( InputController.ButtonType button )
	{
		
	}
	//end event handlers
}
