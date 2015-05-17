using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BackToStartScreen : MonoBehaviour {

	public Image black;

	private static BackToStartScreen mInstance = null;
	public static BackToStartScreen instance
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
			Debug.LogError( string.Format( "Only one instance of BackToStartScreen allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;

		StartCoroutine( "DoBackToStart" );
	}

	private IEnumerator DoBackToStart()
	{
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

		GameAgent.ChangeGameState( GameAgent.GameState.Start );
	}
}
