using UnityEngine;
using System.Collections;

public class RageController : MonoBehaviour {

	public static float RageDuration = 0.75f;
	public static float RageCooldown = 2.5f;

	private TiltShiftHdr tiltShiftHdr;
	private Vignetting vignetting;

	private float fromBlurArea = 7f;
	private float toBlurArea = 1.5f;

	private float fromChromAberration = 24f;
	private float toChromAberration = 0f;

	void Start()
	{
		tiltShiftHdr = Camera.main.GetComponent<TiltShiftHdr>();

		if( tiltShiftHdr )
		{
			tiltShiftHdr.enabled = true;
			StartCoroutine( "DoBlurAreaFade" );
		}

		vignetting = Camera.main.GetComponent<Vignetting>();

		if( vignetting )
		{
			vignetting.enabled = true;
			StartCoroutine( "DoChromaticAberrationFade" );
		}
	}

	void OnDestroy()
	{
		if( tiltShiftHdr )
		{
			tiltShiftHdr.enabled = false;
			tiltShiftHdr.blurArea = toBlurArea;
		}

		if( vignetting )
		{
			vignetting.enabled = false;
			vignetting.chromaticAberration = toChromAberration;
		}
	}

	private IEnumerator DoBlurAreaFade()
	{
		tiltShiftHdr.blurArea = fromBlurArea;

		yield return new WaitForSeconds( RageDuration );
		
		float currentTime = 0f;
		float lerp;
		
		yield return null;
		
		do
		{
			currentTime += Time.deltaTime;
			lerp = currentTime / RageCooldown;
			
			tiltShiftHdr.blurArea = Mathf.Lerp( fromBlurArea, toBlurArea, lerp );
			
			yield return null;
			
		} while( currentTime < RageCooldown );
		
		tiltShiftHdr.blurArea = toBlurArea;
	}

	private IEnumerator DoChromaticAberrationFade()
	{
		vignetting.chromaticAberration = fromChromAberration;

		yield return new WaitForSeconds( RageDuration );

		float currentTime = 0f;
		float lerp;

		yield return null;

		do
		{
			currentTime += Time.deltaTime;
			lerp = currentTime / RageCooldown;

			vignetting.chromaticAberration = Mathf.Lerp( fromChromAberration, toChromAberration, lerp );

			yield return null;

		} while( currentTime < RageCooldown );

		vignetting.chromaticAberration = toChromAberration;
	}
}
