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

	void Start()
	{
		/*
		if( doorPrefab )
		{
			Vector2 randomPosition;

			for( int i = 0; i < numDoors; i++ )
			{
				randomPosition = Random.insideUnitCircle * 10f;

				Instantiate( doorPrefab, new Vector3( randomPosition.x, 0f, randomPosition.y ), Quaternion.AngleAxis( Random.Range( 0f, 360f ), Vector3.up ) );
			}
		}
		*/


		DoorAgent.RandomizeDoorConnections();
	}

	void Update()
	{
		if( Input.GetKeyDown( KeyCode.Space ) )
		{
			DoorAgent.RandomizeDoorConnections();
			
			//foreach( KeyValuePair<int, int> connection in doorConnections )
			//	Debug.Log( "" + connection.Key + "->" + connection.Value );
		}
	}
}
