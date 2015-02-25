using UnityEngine;
using System.Collections;

public class NegativeEffect : MonoBehaviour {

	private Shader shader;
	private Material material;

	private float negativeAmount;
	private float fromNegativeAmount = 0f;
	private float toNegativeAmount = 0.9f;

	private float duration = 1.5f;

	void Start()
	{
		if( GetComponents<NegativeEffect>().Length > 1 )
			Destroy( this );

		shader = PlayerAgent.GetMonsterShader();

		if( shader == null )
		{
			enabled = false;
			return;
		}

		material = new Material( shader );

		StartCoroutine( "DoNegativeFade" );
	}

	void Update()
	{
		negativeAmount = Mathf.Clamp01( negativeAmount );
	}

	void OnRenderImage( RenderTexture source, RenderTexture destination )
	{
		if( material == null )
			return;

		material.SetFloat( "_NegativeAmount", negativeAmount );
		Graphics.Blit( source, destination, material );
	}

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
}