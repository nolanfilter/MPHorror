using UnityEngine;
using System.Collections;

public class ZoomKillerController : MonoBehaviour {

	public PlayerController playerController;
	
	private Vignetting vignetting;

	private float fromBlur = 0.5f;
	private float toBlur = 2f;

	private float fromVignetting = 1f;
	private float toVignetting = 2f;
	
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
			vignetting.blur = fromBlur;
		}
	}
	
	private IEnumerator DoVignettingFade()
	{
		while( true )
		{
			if( playerController )
			{
				vignetting.intensity = Mathf.Lerp( fromVignetting, toVignetting, playerController.GetZoomProgress() );
				vignetting.blur = Mathf.Lerp( fromBlur, toBlur, playerController.GetZoomProgress() );
			}
			else
			{
				vignetting.intensity = fromVignetting;
				vignetting.blur = fromBlur;
			}
			
			yield return null;
		}
	}
}
