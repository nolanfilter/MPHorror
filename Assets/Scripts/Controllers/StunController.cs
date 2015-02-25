using UnityEngine;
using System.Collections;

public class StunController : MonoBehaviour {

	public static float StunDuration = 5f;
	
	private FastBloom fastBloom;
	private Blur blur;

	private float fromGlow = 10f;
	private float toGlow = 0f;

	private float bloomDuration = 1.5f;
	private float fromBloomThreshhold = 0f;
	private float toBloomThreshhold = 0.25f;
	private float fromBloomIntensity = 4f;
	private float toBloomIntensity = 0.5f;

	private float fromBlurDownsample = 1f;
	private float toBlurDownsample = 0f;
	private float fromBlurSize = 5f;
	private float toBlurSize = 0f;
	private float fromBlurIterations = 2f;
	private float toBlurIterations = 0f;

	void Start ()
	{
		fastBloom = Camera.main.gameObject.GetComponent<FastBloom>();
		
		if( fastBloom )
		{
			fastBloom.enabled = true;
			StartCoroutine( "DoBloomFade" );
		}

		blur = Camera.main.gameObject.GetComponent<Blur> ();

		if( blur )
		{
			blur.enabled = true;
			StartCoroutine( "DoBlurFade" );
		}
	}

	void OnDestroy()
	{
		if( fastBloom )
		{
			fastBloom.threshhold = toBloomThreshhold;
			fastBloom.intensity = toBloomIntensity;
		}

		if( blur )
		{
			blur.enabled = false;
			blur.downsample = Mathf.RoundToInt( fromBlurDownsample );
			blur.blurSize = fromBlurSize;
			blur.blurIterations = Mathf.RoundToInt( fromBlurIterations );
		}
	}

	private IEnumerator DoBloomFade()
	{
		fastBloom.threshhold = fromBloomThreshhold;
		fastBloom.intensity = fromBloomIntensity;
		
		float currentTime = 0f;
		float lerp;
		
		yield return null;
		
		do
		{
			currentTime += Time.deltaTime;
			lerp = currentTime / bloomDuration;

			fastBloom.threshhold = Mathf.Lerp( fromBloomThreshhold, toBloomThreshhold, lerp );
			fastBloom.intensity = Mathf.Lerp( fromBloomIntensity, toBloomIntensity, lerp );
			
			yield return null;
			
		} while( currentTime < bloomDuration );
		
		fastBloom.threshhold = toBloomThreshhold;
		fastBloom.intensity = toBloomIntensity;
	}

	private IEnumerator DoBlurFade()
	{
		blur.downsample = Mathf.RoundToInt( fromBlurDownsample );
		blur.blurSize = fromBlurSize;
		blur.blurIterations = Mathf.RoundToInt( fromBlurIterations );
		
		float currentTime = 0f;
		float lerp;
		
		yield return null;
		
		do
		{
			currentTime += Time.deltaTime;
			lerp = currentTime / StunDuration;
			
			blur.downsample = Mathf.RoundToInt( Mathf.Lerp( fromBlurDownsample, toBlurDownsample, lerp ) );
			blur.blurSize =  Mathf.Lerp( fromBlurSize, toBlurSize, lerp );
			blur.blurIterations = Mathf.RoundToInt( Mathf.Lerp( fromBlurIterations, toBlurIterations, lerp ) );
			
			yield return null;
			
		} while( currentTime < StunDuration );
		
		blur.downsample = Mathf.RoundToInt( toBlurDownsample );
		blur.blurSize = toBlurSize;
		blur.blurIterations = Mathf.RoundToInt( toBlurIterations );
	}

}
