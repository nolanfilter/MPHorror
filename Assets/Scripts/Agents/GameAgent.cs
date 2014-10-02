using UnityEngine;
using System.Collections;

public class GameAgent : MonoBehaviour {

	//public GameObject doorPrefab;

	//private int numDoors = 7;

	private static GameAgent mInstance = null;
	public static GameAgent instance
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

	/*
	void Start()
	{
		DoorAgent.RandomizeDoorConnections();
	}
	
	void Update()
	{
		if( Input.GetKeyDown( KeyCode.Space ) )
		{
			DoorAgent.RandomizeDoorConnections();
		}
	}
	*/
}
