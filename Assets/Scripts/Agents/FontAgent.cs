using UnityEngine;
using System.Collections;

public class FontAgent : MonoBehaviour {

	public Font font;

	private static FontAgent mInstance = null;
	public static FontAgent instance
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
			Debug.LogError( string.Format( "Only one instance of GameAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;
	}

	public static Font GetFont()
	{
		if( instance )
			return instance.internalGetFont();

		return null;
	}

	private Font internalGetFont()
	{
		return font;
	}
}
