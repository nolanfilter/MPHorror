using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OutroScreen : MonoBehaviour {

	public Image black;

	private static OutroScreen mInstance = null;
	public static OutroScreen instance
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
			Debug.LogError( string.Format( "Only one instance of OutroScreen allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;

		StartCoroutine( "DoOutro" );
	}

	private IEnumerator DoOutro()
	{
		float duration = 0.3f;
		float currentTime = 0f;
		float lerp;

		float fromY = 470f;
		float toY = 1350f;

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

		GameAgent.ChangeGameState( GameAgent.GameState.Lobby );
	}
}
