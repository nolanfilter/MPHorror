using UnityEngine;
using System.Collections;

public class RageController : MonoBehaviour {

	public static float RageDuration = 0.75f;
	public static float RageCooldown = 2.5f;

	private Vignetting vignetting;

	private float fromBlurArea = 7f;
	private float toBlurArea = 1.5f;

	private float rageChromAberration = 100f;
	private float fromCooldownChromAberration = 200f;
	private float toCooldownChromAberration = 0f;

	void Start()
	{
		vignetting = Camera.main.GetComponent<Vignetting>();

		if( vignetting )
		{
			vignetting.enabled = true;
			StartCoroutine( "DoChromaticAberrationFade" );
		}
	}

	void OnDestroy()
	{
		if( vignetting )
		{
			vignetting.chromaticAberration = toCooldownChromAberration;
		}
	}

	private IEnumerator DoChromaticAberrationFade()
	{
		vignetting.chromaticAberration = rageChromAberration;

		yield return new WaitForSeconds( RageDuration );

		vignetting.chromaticAberration = fromCooldownChromAberration;

		float currentTime = 0f;
		float lerp;

		yield return null;

		do
		{
			currentTime += Time.deltaTime;
			lerp = currentTime / RageCooldown;

			vignetting.chromaticAberration = Mathf.Lerp( fromCooldownChromAberration, toCooldownChromAberration, lerp );

			yield return null;

		} while( currentTime < RageCooldown );

		vignetting.chromaticAberration = toCooldownChromAberration;
	}
}
