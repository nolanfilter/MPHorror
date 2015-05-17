using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WaitingScreen : MonoBehaviour {

	public Image black;
	public Image hostCursor;
	public Image joinCursor;

	private bool canCancel = false;

	private static WaitingScreen mInstance = null;
	public static WaitingScreen instance
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
			Debug.LogError( string.Format( "Only one instance of WaitingScreen allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;

		if( NetworkAgent.GetIsHost() )
			HighlightHost();
		else
			HighlightJoin();

		StartCoroutine( "WaitForRoom" );
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

	public static bool GetCanCancel()
	{
		if( instance )
			return instance.canCancel;

		return true;
	}

	private IEnumerator WaitForRoom()
	{
		canCancel = false;

		float duration = 0.3f;
		float currentTime = 0f;
		float lerp;

		float fromY = 1350f;
		float toY = 470f;

		if( black )
			black.rectTransform.localPosition = Vector3.up * fromY;

		do
		{
			currentTime += Time.deltaTime;
			lerp = Mathf.Clamp01( currentTime / duration );

			lerp = Mathf.Pow( lerp, 2f ) * 3f - Mathf.Pow( lerp, 3f ) * 2f;

			if( black )
				black.rectTransform.localPosition = Vector3.up * Mathf.Lerp( fromY, toY, lerp );

			yield return null;

		} while( currentTime < duration );

		if( black )
			black.rectTransform.localPosition = Vector3.up * toY;

		canCancel = true;

		while( PhotonNetwork.room == null )
			yield return null;

		GameAgent.ChangeGameState( GameAgent.GameState.Room );
	}
}
