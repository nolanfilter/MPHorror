using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAgent : MonoBehaviour {

	private List<PlayerController> playerControllers;

	private static PlayerAgent mInstance = null;
	public static PlayerAgent instance
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
			Debug.LogError( string.Format( "Only one instance of PlayerAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;

		playerControllers = new List<PlayerController>();
	}

	public static void RegisterPlayer( PlayerController playerController )
	{
		if( instance )
			instance.internalRegisterPlayer( playerController );
	}

	private void internalRegisterPlayer( PlayerController playerController )
	{
		if( !playerControllers.Contains( playerController ) )
			playerControllers.Add( playerController );
	}

	public static void UnregisterPlayer( PlayerController playerController )
	{
		if( instance )
			instance.internalUnregisterPlayer( playerController );
	}
	
	private void internalUnregisterPlayer( PlayerController playerController )
	{
		if( playerControllers.Contains( playerController ) )
			playerControllers.Remove( playerController );
	}

	public void SetAllFlashlightsTo( bool on )
	{
		for( int i = 0; i < playerControllers.Count; i++ )
			playerControllers[i].SetFlashlightOn( on );
	}

	public void TeleportAllTo( string coordinates )
	{
		string[] splitCoordinates = coordinates.Split( ',' );

		for( int i = 0; i < playerControllers.Count; i++ )
			playerControllers[i].TeleportTo( new Vector3( float.Parse( splitCoordinates[ i * 2 ] ), 0f, float.Parse( splitCoordinates[ i * 2 + 1 ] ) ) );
	}
}
