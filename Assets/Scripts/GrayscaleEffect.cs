using UnityEngine;
using System.Collections;

public class GrayscaleEffect : MonoBehaviour {

	private Shader shader;
	private Material material;

	private float grayscaleAmount = 1f;
	//private float fromNegativeAmount = 0f;
	//private float toNegativeAmount = 0.9f;

	//private float duration = 1.5f;

	void Start()
	{
		if( GetComponents<GrayscaleEffect>().Length > 1 )
			Destroy( this );

		shader = PlayerAgent.GetStunShader();

		if( shader == null )
		{
			enabled = false;
			return;
		}

		material = new Material( shader );

		//StartCoroutine( "DoNegativeFade" );
	}

	void Update()
	{
		grayscaleAmount = Mathf.Clamp01( grayscaleAmount );
	}

	void OnRenderImage( RenderTexture source, RenderTexture destination )
	{
		if( material == null )
			return;

		material.SetFloat( "_GrayscaleAmount", grayscaleAmount );
		Graphics.Blit( source, destination, material );
	}

	/*
	private IEnumerator DoNegativeFade()
	{
		negativeAmount = fromNegativeAmount;

		float lerp;
		float currentDuration = 0f;
		float beginTime = Time.time;

		do
		{
			currentDuration += Time.deltaTime;
			lerp = currentDuration / duration;

			negativeAmount = Mathf.Lerp( fromNegativeAmount, toNegativeAmount, lerp );

			yield return null;

		} while( currentDuration < duration );

		negativeAmount = toNegativeAmount;
	}
	*/
}