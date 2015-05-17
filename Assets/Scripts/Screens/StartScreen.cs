using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StartScreen : MonoBehaviour {

	public Image playCursor;
	public Image creditsCursor;

	private static StartScreen mInstance = null;
	public static StartScreen instance
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
			Debug.LogError( string.Format( "Only one instance of StartScreen allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;

		HighlightPlay();
	}

	public static void HighlightPlay()
	{
		if( instance )
			instance.internalHighlightPlay();
	}

	private void internalHighlightPlay()
	{
		if( playCursor )
			playCursor.enabled = true;

		if( creditsCursor )
			creditsCursor.enabled = false;
	}

	public static void HighlightCredits()
	{
		if( instance )
			instance.internalHighlightCredits();
	}

	private void internalHighlightCredits()
	{
		if( playCursor )
			playCursor.enabled = false;
		
		if( creditsCursor )
			creditsCursor.enabled = true;
	}

	public static void Activate()
	{
		if( instance )
			instance.internalActivate();
	}

	private void internalActivate()
	{
		if( playCursor && playCursor.enabled )
		{
			NetworkAgent.SetSelectionIndex( 0 );
			GameAgent.ChangeGameState( GameAgent.GameState.Outro );
			return;
		}

		if( creditsCursor && creditsCursor.enabled )
		{
			GameAgent.ChangeGameState( GameAgent.GameState.Credits );
			return;
		}
	}
}
