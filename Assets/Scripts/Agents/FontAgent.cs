using UnityEngine;
using System.Collections;

public class FontAgent : MonoBehaviour {

	public Font notificationFont;
	public Font uIFont;

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

	public static Font GetNotificationFont()
	{
		if( instance )
			return instance.internalGetNotificationFont();

		return null;
	}

	private Font internalGetNotificationFont()
	{
		return notificationFont;
	}

	public static Font GetUIFont()
	{
		if( instance )
			return instance.internalGetUIFont();

		return null;
	}

	private Font internalGetUIFont()
	{
		return uIFont;
	}
}
