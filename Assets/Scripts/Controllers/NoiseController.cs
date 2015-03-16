using UnityEngine;
using System.Collections;

public class NoiseController : MonoBehaviour {

	private NoiseEffect noiseEffect;

	private float maxDistance = 10f;

	private float fromGrainIntensityMin = 0f;
	private float toGrainIntensityMin = 0.3f;
	private float fromGrainIntensityMax = 0.1f;
	private float toGrainIntensityMax = 0.8f;
	private float fromScratchIntensityMin = 0.01f;
	private float toScratchIntensityMin = 0.1f;
	private float fromScratchIntensityMax = 0.03f;
	private float toScratchIntensityMax = 0.8f;

	void Start () 
	{
		noiseEffect = Camera.main.GetComponent<NoiseEffect>();

		if( noiseEffect )
		{
			noiseEffect.grainIntensityMin = fromGrainIntensityMin;
			noiseEffect.grainIntensityMax = fromGrainIntensityMax;
			noiseEffect.scratchIntensityMin = fromScratchIntensityMin;
			noiseEffect.scratchIntensityMax = fromScratchIntensityMax;

			noiseEffect.enabled = true;
			StartCoroutine( "DoNoiseFade" );
		}
	}

	void OnDestroy()
	{
		if( noiseEffect )
		{
			noiseEffect.enabled = false;

			noiseEffect.grainIntensityMin = fromGrainIntensityMin;
			noiseEffect.grainIntensityMax = fromGrainIntensityMax;
			noiseEffect.scratchIntensityMin = fromScratchIntensityMin;
			noiseEffect.scratchIntensityMax = fromScratchIntensityMax;
		}
	}

	private IEnumerator DoNoiseFade()
	{
		float lerp;

		while( true )
		{
			lerp = 1f - ( Mathf.Clamp( PlayerAgent.GetClosestPlayerPosition( transform.position ), 0f, maxDistance ) / maxDistance );

			noiseEffect.grainIntensityMin = Mathf.Lerp( fromGrainIntensityMin, toGrainIntensityMin, lerp );
			noiseEffect.grainIntensityMax = Mathf.Lerp( fromGrainIntensityMax, toGrainIntensityMax, lerp );
			noiseEffect.scratchIntensityMin = Mathf.Lerp( fromScratchIntensityMin, toScratchIntensityMin, lerp );
			noiseEffect.scratchIntensityMax = Mathf.Lerp( fromScratchIntensityMax, toScratchIntensityMax, lerp );

			/*
			noiseEffect.grainIntensityMin = toGrainIntensityMin;
			noiseEffect.grainIntensityMax = toGrainIntensityMax;
			noiseEffect.scratchIntensityMin = toScratchIntensityMin;
			noiseEffect.scratchIntensityMax = toScratchIntensityMax;
			*/

			yield return null;
		}
	}
}
