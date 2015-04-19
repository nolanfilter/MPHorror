using UnityEngine;
using System.Collections;

public class ScreenshotAgent : MonoBehaviour {

	public delegate void PostRenderFinish();
	public event PostRenderFinish OnPostRenderFinish;

	private int adjustedWidth;
	private int adjustedX;

	private Texture2D texture;

	private static ScreenshotAgent mInstance = null;
	public static ScreenshotAgent instance
	{
		get
		{
			return mInstance;
		}
	}
	
	void Awake()
	{
		if( mInstance != null )
		{
			Debug.LogError( string.Format( "Only one instance of ScreenshotAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;

		adjustedWidth = Mathf.RoundToInt( Screen.height / 3f * 4f );
		adjustedX = Mathf.RoundToInt( ( Screen.width - adjustedWidth ) / 2f );

		texture = new Texture2D( adjustedWidth, Screen.height, TextureFormat.RGB24, false );
		Disable();
	}

	void OnPostRender()
	{
		texture.ReadPixels( new Rect( adjustedX, 0, adjustedWidth, Screen.height ), 0, 0 );
		texture.Apply();
		Disable();
		
		if( OnPostRenderFinish != null )
			OnPostRenderFinish();
	}	

	public static void Enable()
	{
		if( instance )
			instance.enabled = true;
	}

	public static void Disable()
	{
		if( instance )
			instance.enabled = false;
	}

	public static Texture2D GetTexture()
	{
		if( instance )
			return instance.texture;

		return null;
	}


}
