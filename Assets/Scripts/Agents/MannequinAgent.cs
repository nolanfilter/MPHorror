using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MannequinAgent : MonoBehaviour {

	private List<GameObject> mannequins;
	
	private static MannequinAgent mInstance = null;
	public static MannequinAgent instance
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
			Debug.LogError( string.Format( "Only one instance of MannequinAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;
		
		mannequins = new List<GameObject>();
	}

	public static void RegisterMannequin( GameObject mannequin )
	{
		if( instance )
			instance.internalRegisterMannequin( mannequin );
	}
	
	private void internalRegisterMannequin( GameObject mannequin )
	{
		if( !mannequins.Contains( mannequin ) )
			mannequins.Add( mannequin );
	}

	public static void UnregisterMannequin( GameObject mannequin )
	{
		if( instance )
			instance.internalUnregisterMannequin( mannequin );
	}
	
	private void internalUnregisterMannequin( GameObject mannequin )
	{
		if( mannequins.Contains( mannequin ) )
			mannequins.Remove( mannequin );
	}

	public static void SetKeys()
	{
		if( instance )
			instance.internalSetKeys();
	}

	private void internalSetKeys()
	{
		return;

		if( mannequins.Count == 0 )
			return;

		for (int i = 0; i < mannequins.Count; i++)
		{
			mannequins[i].SetActive( true );
			mannequins[i].tag = "Activatable";
		}

		int seed = Utilities.HexToInt( PhotonNetwork.room.name[ PhotonNetwork.room.name.Length - 2 ] );
		
		Random.seed = seed;

		int keyIndex = Random.Range( 0, mannequins.Count );
		int key2Index = Random.Range( 0, mannequins.Count );
		
		if( key2Index == keyIndex )
			key2Index = ( key2Index + 1 )%mannequins.Count;
		
		if( mannequins.Count > 0 )
			mannequins[ keyIndex ].tag = "Key";
		
		if( mannequins.Count > 1 )
			mannequins[ key2Index ].tag = "Key2";
	}


	public static bool GetAllMannequinsDisabled()
	{
		if( instance )
			return instance.internalGetAllMannequinsDisabled();

		return false;
	}

	private bool internalGetAllMannequinsDisabled()
	{
		if( mannequins.Count == 0 )
			return true;

		int numActiveMannequins = 0;

		for( int i = 0; i < mannequins.Count; i++ )
			if( mannequins[i].activeSelf )
				numActiveMannequins++;

		return ( numActiveMannequins == 0 );
	}

	public static bool GetShouldMonsterize()
	{
		if( instance )
			return instance.internalGetShouldMonsterize();
		
		return false;
	}

	private bool internalGetShouldMonsterize()
	{
		if( mannequins.Count == 0 )
			return true;
		
		int numActiveMannequins = 0;
		
		for( int i = 0; i < mannequins.Count; i++ )
			if( mannequins[i].activeSelf )
				numActiveMannequins++;

		return ( mannequins.Count - numActiveMannequins >= PlayerAgent.GetMonsterizingMannequinNumber() );
	}
}
