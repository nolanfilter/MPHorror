﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAgent : MonoBehaviour {

	private List<PlayerController> playerControllers;

	public Shader monsterShader;

	public bool monsterize = true;
	public bool monsterizeMaster = false;

	private int monsterID = -1;

	public float waitTime = 25f;

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

		if( monsterize )
		{
			if( monsterizeMaster )
			{
				StartCoroutine( "WaitAndMonsterizeMaster" );
			}
			else
			{
				if( playerControllers.Count == 1 )
					StartCoroutine( "WaitAndMonsterizeRandom" );
			}
		}
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

	public static Shader GetMonsterShader()
	{
		if( instance )
			return instance.monsterShader;

		return null;
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

	public void GlobalMessageDisplay( string messageToDisplay )
	{
		for( int i = 0; i < playerControllers.Count; i++ )
			playerControllers[i].DisplayMessage( messageToDisplay );
	}

	public void AllButMonsterMessageDisplay( string messageToDisplay )
	{
		for( int i = 0; i < playerControllers.Count; i++ )
			if( i != monsterID )
				playerControllers[i].DisplayMessage( messageToDisplay );
	}

	private IEnumerator WaitAndMonsterizeMaster()
	{
		if( !monsterize || playerControllers.Count == 0 )
			yield break;
		
		yield return new WaitForSeconds( waitTime );

		monsterID = 0;
		
		playerControllers[ monsterID ].Monsterize();
	}

	private IEnumerator WaitAndMonsterizeRandom()
	{
		if( !monsterize || playerControllers.Count == 0 )
			yield break;

		yield return new WaitForSeconds( waitTime );

		int seed = Utilities.HexToInt( PhotonNetwork.room.name[ PhotonNetwork.room.name.Length - 1 ] );

		Random.seed = seed;

		monsterID = Mathf.FloorToInt( Random.value * playerControllers.Count );
	
		playerControllers[ monsterID ].Monsterize();
	}
}
