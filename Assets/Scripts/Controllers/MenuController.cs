using UnityEngine;
using System.Collections;

public class MenuController : MonoBehaviour {

	private InputController inputController;
	private float buttonBuffer = 1f;
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
			case GameAgent.GameState.Start:
			{
				if( button == InputController.ButtonType.Start || button == InputController.ButtonType.A )
				{
					NetworkAgent.SetSelectionIndex( 0 );
					GameAgent.ChangeGameState( GameAgent.GameState.Lobby );
					lastButtonTime = Time.time;
				}
			} break;

			case GameAgent.GameState.Lobby:
			{
				if( button == InputController.ButtonType.Start || button == InputController.ButtonType.A )
				{
					NetworkAgent.ActivateSelected();
					lastButtonTime = Time.time;
				}
				else if( button == InputController.ButtonType.Left )
				{
					if( NetworkAgent.GetSelectionIndex() != 0 )
					{
						NetworkAgent.SetSelectionIndex( 0 );
						lastButtonTime = Time.time;
					}
				}
				else if( button == InputController.ButtonType.Right )
				{
					if( NetworkAgent.GetSelectionIndex() == 0 )
					{
						NetworkAgent.SetSelectionIndex( 1 );
						lastButtonTime = Time.time;
					}
				}
				else if( button == InputController.ButtonType.Up )
				{
					if( NetworkAgent.GetSelectionIndex() > 1 )
					{
						NetworkAgent.SetSelectionIndex( NetworkAgent.GetSelectionIndex() - 1 );
						lastButtonTime = Time.time;
					}
				}
				else if( button == InputController.ButtonType.Down )
				{
					if( NetworkAgent.GetSelectionIndex() > 0 )
					{
						NetworkAgent.SetSelectionIndex( NetworkAgent.GetSelectionIndex() + 1 );
						lastButtonTime = Time.time;
					}
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
