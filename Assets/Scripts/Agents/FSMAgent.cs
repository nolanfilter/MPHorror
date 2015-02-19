using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FSMAgent : MonoBehaviour {

	private List<PlayMakerFSM> FSMs;
	
	private static FSMAgent mInstance = null;
	public static FSMAgent instance
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
			Debug.LogError( string.Format( "Only one instance of FSMAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;
		
		FSMs = new List<PlayMakerFSM>();
	}
	
	public static void RegisterFSM( PlayMakerFSM FSM )
	{
		if( instance )
			instance.internalRegisterFSM( FSM );
	}
	
	private void internalRegisterFSM( PlayMakerFSM FSM )
	{
		if( !FSMs.Contains( FSM ) )
			FSMs.Add( FSM );
	}
	
	public static void UnregisterFSM( PlayMakerFSM FSM )
	{
		if( instance )
			instance.internalUnregisterFSM( FSM );
	}
	
	private void internalUnregisterFSM( PlayMakerFSM FSM )
	{
		if( FSMs.Contains( FSM ) )
			FSMs.Remove( FSM );
	}

	public static void Reset()
	{
		if( instance )
			instance.internalReset();
	}

	private void internalReset()
	{
		for( int i = 0; i < FSMs.Count; i++ )
			FSMs[i].SendEvent( "Reset" );
	}
}
