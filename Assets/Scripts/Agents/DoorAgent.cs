using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoorAgent : MonoBehaviour {

	public enum RoomType
	{
		Invalid,
	}

	private static Dictionary<int, DoorController> doorsById = new Dictionary<int, DoorController>();
	private static Dictionary<int, int> doorConnections = new Dictionary<int, int>();
	private static int IDIndex = 0;

	private static DoorAgent mInstance = null;
	public static DoorAgent instance
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
			Debug.LogError( string.Format( "Only one instance of DoorAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;
	}

	public static int RegisterDoor( DoorController doorController )
	{
		if( instance )
			return instance.internalRegisterDoor( doorController );

		return -1;
	}

	private int internalRegisterDoor( DoorController doorController )
	{
		if( doorController.getUniqueID() == -1 )
		{
			int uniqueID = IDIndex;
			IDIndex++;

			doorsById.Add( uniqueID, doorController );

			return uniqueID;
		}

		return doorController.getUniqueID();
	}

	public static Transform GetToDoorTransform( int fromDoorID )
	{
		if( instance )
			return instance.internalGetToDoorTransform( fromDoorID );

		return null;
	}

	private Transform internalGetToDoorTransform( int fromDoorID )
	{
		int toDoorId = -1;

		if( doorConnections.ContainsKey( fromDoorID ) )
			toDoorId = doorConnections[ fromDoorID ];

		if( doorsById.ContainsKey( toDoorId ) )
			return doorsById[ toDoorId ].transform;

		return null;
	}

	public static void RandomizeDoorConnections()
	{
		if( instance )
			instance.internalRandomizeDoorConnections();
	}

	private void internalRandomizeDoorConnections()
	{
		doorConnections.Clear();
		
		List<int> doorIDs = new List<int>( doorsById.Keys );
		
		for( int i = 0; i < doorIDs.Count; i++ )
			doorConnections.Add( doorIDs[i], doorIDs[ Random.Range( 0, doorIDs.Count ) ] );
	}
}
