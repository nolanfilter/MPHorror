using UnityEngine;
using System.Collections;

public class ZoomSurvivorController : MonoBehaviour {

	public PlayerController playerController;

	private Vignetting vignetting;
	
	private float fromVignetting = 1f;
	private float toVignetting = 4f;
	
	void Start()
	{
		vignetting = Camera.main.GetComponent<Vignetting>();
		
		if( vignetting )
		{
			vignetting.enabled = true;
			StartCoroutine( "DoVignettingFade" );
		}
	}

	void OnDestroy()
	{
		if( vignetting )
		{
			vignetting.intensity = fromVignetting;
		}
	}
	
	private IEnumerator DoVignettingFade()
	{
		while( true )
		{
			if( playerController )
				vignetting.intensity = Mathf.Lerp( fromVignetting, toVignetting, playerController.GetZoomProgress() );
			else
				vignetting.intensity = fromVignetting;

			yield return null;
		}
	}
}
