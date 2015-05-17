using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LobbyScreen : MonoBehaviour {

	public Image hostCursor;
	public Image joinCursor;

	private static LobbyScreen mInstance = null;
	public static LobbyScreen instance
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
			Debug.LogError( string.Format( "Only one instance of LobbyScreen allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;

		HighlightHost();
	}

	public static void HighlightHost()
	{
		if( instance )
			instance.internalHighlightHost();
	}

	private void internalHighlightHost()
	{
		if( hostCursor )
			hostCursor.enabled = true;

		if( joinCursor )
			joinCursor.enabled = false;
	}

	public static void HighlightJoin()
	{
		if( instance )
			instance.internalHighlightJoin();
	}

	private void internalHighlightJoin()
	{
		if( hostCursor )
			hostCursor.enabled = false;
		
		if( joinCursor )
			joinCursor.enabled = true;
	}

	public static void Activate()
	{
		if( instance )
			instance.internalActivate();
	}

	private void internalActivate()
	{
		if( hostCursor && hostCursor.enabled )
		{
			NetworkAgent.SetSelectionIndex( 0 );
			NetworkAgent.ActivateSelected();
			return;
		}

		if( joinCursor && joinCursor.enabled )
		{
			NetworkAgent.SetSelectionIndex( 1 );
			NetworkAgent.ActivateSelected();
			return;
		}
	}
}
