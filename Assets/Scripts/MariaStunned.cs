using UnityEngine;
using System.Collections;

public class MariaStunned : Photon.MonoBehaviour {


	public static float StunDuration = 5f;

	//Bloom (STUN)
	public static float BloomDuration = 1.5f;
	private float fromThreshhold = 0f;
	private float toThreshhold = .25f;
	private float fromIntensity = 4f;
	private float toIntensity = .5f;

	//Blur (STUN)
	private int fromDownsample = 1;
	private int toDownsample = 0;
	private float fromBlurSize = 5f;
	private float toBlurSize = 0f;
	private int fromBlurIterations = 2;
	private int toBlurIterations = 0;

	//FoV (field of view) (ZOOM IN MONSTER)
	private float cameraFoV = 40f;
	private float cameraZoomFoV = 20f;
	public float smooth = 2.0F;

	//TiltShift HDR -- blur from the edges (ALWAYS BUT ENHANCED WHEN ZOOM IN MONSTER)
	private float fromBlurArea = 7f;
	private float toBlurArea = 1.5f;

	//Vignetting (ZOOM IN MONSTER)
	private float fromChromAberration = 24f;
	private float toChromAberration = 0f;



	void Start () {

	}
	

	void Update () {

	}


	//NOISE for proximity with another player
	void NoiseToggle (int changeNoise) {

		int _changeNoise = changeNoise;
		NoiseEffect _noise = transform.GetComponent<NoiseEffect>();
		if(_changeNoise == 1){
			_noise.grainIntensityMin = 0f;
			_noise.grainIntensityMax = .1f;
			_noise.scratchIntensityMin = .01f;
			_noise.scratchIntensityMax = .03f;
			//_noise.enabled = true;  
		} else {
			_noise.grainIntensityMin = .3f;
			_noise.grainIntensityMax = .5f;
			_noise.scratchIntensityMin = .1f;
			_noise.scratchIntensityMax = .5f;
			//_noise.enabled = false;
		}
	}

	// STUN for all players
	private IEnumerator Stun (){

		FastBloom _fastBloom = transform.GetComponent<FastBloom>();
		_fastBloom.threshhold = fromThreshhold;
		_fastBloom.intensity = fromIntensity;

		Blur _blur = transform.GetComponent<Blur>();
		_blur.downsample = fromDownsample;
		_blur.blurSize = fromBlurSize;
		_blur.blurIterations = fromBlurIterations;
		
		float currentTime = 0f;
		float lerp;
		float lerpBloom;
		
		yield return null;
		
		do
		{
			currentTime += Time.deltaTime;
			lerp = currentTime / StunDuration;
			lerpBloom = currentTime / BloomDuration;
			
			_fastBloom.threshhold = Mathf.Lerp( fromThreshhold, toThreshhold, lerpBloom );
			_fastBloom.intensity = Mathf.Lerp (fromIntensity, toIntensity, lerpBloom);

			_blur.enabled = true;
			//_blur.downsample = Mathf.Lerp( fromDownsample, toDownsample, lerp );
			_blur.blurSize =  Mathf.Lerp( fromBlurSize, toBlurSize, lerp );
			//_blur.blurIterations = Mathf.Lerp( fromBlurIterations, toBlurIterations, lerp );
			
			yield return null;
			
		} while ( currentTime < StunDuration );


		_fastBloom.threshhold = toThreshhold;
		_fastBloom.intensity = toIntensity;

		_blur.downsample = toDownsample;
		_blur.blurSize = toBlurSize;
		_blur.blurIterations = toBlurIterations;

		_blur.enabled = false;

	}

	//ZOOM FOR MONSTER
	private IEnumerator Zoom (bool state){
		bool _state = state;
		//float _currentFieldOfView = /*cameraTransform*/this.camera.fieldOfView;
		TiltShiftHdr _tiltShiftHdr = transform.GetComponent<TiltShiftHdr>();
		Vignetting _vignetting = transform.GetComponent<Vignetting>();
		//_tiltShiftHdr.blurArea = fromBlurArea;


		//if zooming in
		if (state){
			this.camera.fieldOfView = Mathf.Lerp(this.camera.fieldOfView, cameraZoomFoV, Time.deltaTime * smooth);
			_tiltShiftHdr.blurArea = Mathf.Lerp (_tiltShiftHdr.blurArea, fromBlurArea, Time.deltaTime * smooth);
			_vignetting.chromaticAberration = Mathf.Lerp (_vignetting.chromaticAberration, fromChromAberration, Time.deltaTime * smooth);

			//POUNCING: chromatic aberration when pouncing 100
			//COOLING DOWN MONSTER: chromatic aberration when cooling down from 200


		}
		// if zooming out
		else {
			this.camera.fieldOfView = Mathf.Lerp(this.camera.fieldOfView, cameraFoV, Time.deltaTime * smooth);
			_tiltShiftHdr.blurArea = Mathf.Lerp (_tiltShiftHdr.blurArea, toBlurArea, Time.deltaTime * smooth);
			_vignetting.chromaticAberration = Mathf.Lerp (_vignetting.chromaticAberration, toChromAberration, Time.deltaTime * smooth);
		}

		yield return null;
	}
}