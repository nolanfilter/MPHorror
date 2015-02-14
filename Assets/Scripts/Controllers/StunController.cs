using UnityEngine;
using System.Collections;

public class StunController : MonoBehaviour {

	public static float StunDuration = 7.5f;

	private GrayscaleEffect grayscaleEffect;
	private GlowEffect glowEffect;

	private float fromGlow = 10f;
	private float toGlow = 0f;

	void Start ()
	{
		grayscaleEffect = Camera.main.gameObject.AddComponent<GrayscaleEffect>();
		glowEffect = Camera.main.gameObject.GetComponent<GlowEffect>();

		if( glowEffect )
		{
			glowEffect.enabled = true;
			StartCoroutine( "DoGlowFade" );
		}
	}

	void OnDestroy()
	{
		if( grayscaleEffect )
			Destroy( grayscaleEffect );

		if( glowEffect )
		{
			glowEffect.glowIntensity = fromGlow;
			glowEffect.enabled = false;
		}
	}

	private IEnumerator DoGlowFade()
	{
		glowEffect.glowIntensity = fromGlow;

		float currentTime = 0f;
		float lerp;

		yield return null;

		do
		{
			currentTime += Time.deltaTime;
			lerp = currentTime / StunDuration;

			glowEffect.glowIntensity = Mathf.Lerp( fromGlow, toGlow, lerp );

			yield return null;

		} while( currentTime < StunDuration );

		glowEffect.glowIntensity = toGlow;
	}
}
