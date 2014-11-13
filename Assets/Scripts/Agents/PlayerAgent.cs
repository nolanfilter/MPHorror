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
		{
			int viewID = playerController.photonView.viewID;

			int index = 0;

			while( index < playerControllers.Count && viewID > playerControllers[index].photonView.viewID )
				index++;

			playerControllers.Insert( index, playerController );
		}

		for( int i = 0; i < playerControllers.Count; i++ )
			Debug.Log( "" + i + " = " + playerControllers[i].photonView.viewID );

		if( playerControllers.Count == 1 )
			StartCoroutine( "WaitAndMonsterize" );
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
			playerControllers[i].SetFlashlightTo( on );
	}

	public void TeleportAllTo( string coordinates )
	{
		string[] splitCoordinates = coordinates.Split( ',' );

		for( int i = 0; i < playerControllers.Count; i++ )
			playerControllers[i].TeleportTo( new Vector3( float.Parse( splitCoordinates[ i * 2 ] ), 0f, float.Parse( splitCoordinates[ i * 2 + 1 ] ) ) );
	}

	private IEnumerator WaitAndMonsterize()
	{
		yield return new WaitForSeconds( 5f );

		Debug.Log( PhotonNetwork.room.name );

		int seed = HexToInt( PhotonNetwork.room.name[ PhotonNetwork.room.name.Length - 1 ] );

		Debug.Log( seed );

		Random.seed = seed;

		int seededRandomValue = Mathf.FloorToInt (Random.value * playerControllers.Count);

		Debug.Log( seededRandomValue );
		Debug.Log( playerControllers[ seededRandomValue ].photonView.viewID );

		playerControllers[ seededRandomValue ].Monsterize();
	}

	private int HexToInt( char hex )
	{
		switch( hex )
		{
			case 'a': return 10;
			case 'b': return 11;
			case 'c': return 12;
			case 'd': return 13;
			case 'e': return 14;
			case 'f': return 15;

			default: return (int)(hex - '0');
		}
	}
}
