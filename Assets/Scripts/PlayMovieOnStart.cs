using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayMovieOnStart : MonoBehaviour {

	public float delay = 0.1f;

	public bool loop = false;

	public GameAgent.GameState nextGameState = GameAgent.GameState.Invalid;

	private MovieTexture movieTexture;
	private AudioSource audioSource;

	void Start()
	{
		Renderer renderer = GetComponent<Renderer>();

		if( renderer )
			movieTexture = renderer.material.mainTexture as MovieTexture;

		if( movieTexture == null )
		{
			RawImage rawImage = GetComponent<RawImage>();

			if( rawImage )
				movieTexture = rawImage.texture as MovieTexture;
		}

		movieTexture.loop = loop;

		audioSource = gameObject.AddComponent<AudioSource>();

		audioSource.clip = movieTexture.audioClip;
		audioSource.loop = loop;

		StartCoroutine( "WaitAndPlay" );
	}

	private IEnumerator WaitAndPlay()
	{
		yield return new WaitForSeconds( delay );

		movieTexture.Play();
		audioSource.Play();

		if( !loop && nextGameState != GameAgent.GameState.Invalid )
			StartCoroutine( "ChangeToNextStateOnFinish" );
	}

	private IEnumerator ChangeToNextStateOnFinish()
	{
		while( movieTexture.isPlaying )
			yield return null;

		GameAgent.ChangeGameState( GameAgent.GameState.Start );
	}
}
