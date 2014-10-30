﻿using UnityEngine;
using System.Collections;

public class NetworkAgent : MonoBehaviour {

	public int numPlayers;
	public GameObject playerPrefab;
	public GameObject networkBackgroundPrefab;
	public Vector3[] playerStartPositions;
	public Vector3[] playerStartRotations;

	private const string roomName = "MPHorror_";
	private RoomInfo[] roomsList;

	private GameObject networkBackground;

	private static NetworkAgent mInstance = null;
	public static NetworkAgent instance
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
			Debug.LogError( string.Format( "Only one instance of NetworkAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;
	}

	void Start()
	{
		numPlayers = Mathf.RoundToInt( Mathf.Clamp( numPlayers, 0f, Mathf.Infinity ) );

		PhotonNetwork.ConnectUsingSettings( "0.1" );

		if( networkBackgroundPrefab )
		{
			networkBackground = Instantiate( networkBackgroundPrefab ) as GameObject;
			networkBackground.transform.parent = Camera.main.transform;
			networkBackground.transform.localPosition = new Vector3( 0f, 0f, Camera.main.nearClipPlane + 0.001f );
			networkBackground.transform.localRotation = Quaternion.identity;
		}
	}

	void Update()
	{
		if( Input.GetKeyDown( KeyCode.Alpha1 ) )
		{
			LeaveRoom();
		}

		if( networkBackground != null )
		{
			networkBackground.renderer.enabled = ( PhotonNetwork.room == null );
		}
	}

	void OnGUI()
	{
		if( !PhotonNetwork.connected )
		{
			GUILayout.Label(PhotonNetwork.connectionStateDetailed.ToString());
		}
		else if( PhotonNetwork.room == null )
		{
			// Create Room
			if( GUI.Button( new Rect( 100, 100, 300, 100 ), "Start Server" ) )
			{
				PhotonNetwork.CreateRoom( roomName + System.Guid.NewGuid().ToString( "N" ), true, true, numPlayers );
			}
				
			// Join Room
			if( roomsList != null )
			{
				for (int i = 0; i < roomsList.Length; i++ )
				{
					if( GUI.Button( new Rect( Screen.width * 0.5f, 100 + ( 110 * i ), 300, 100 ), "Join\n" + roomsList[i].name ) )
						PhotonNetwork.JoinRoom( roomsList[i].name );
				}
			}
		}
	}

	void OnReceivedRoomListUpdate()
	{
		roomsList = PhotonNetwork.GetRoomList();
	}

	void OnJoinedRoom()
	{
		DoorAgent.RandomizeDoorConnections();

		if( playerStartPositions.Length == 0 )
		{
			Debug.LogError( "No player start positions." );
			return;
		}

		if( playerStartRotations.Length == 0 )
		{
			Debug.LogError( "No player start rotations." );
			return;
		}

		// Spawn player
		if( playerPrefab != null )
		{
			int playerNumber = PhotonNetwork.otherPlayers.Length;

			PhotonNetwork.Instantiate ( playerPrefab.name, playerStartPositions[ playerNumber%playerStartPositions.Length ], Quaternion.Euler( playerStartRotations[ playerNumber%playerStartRotations.Length ] ), 0 );
		}
	}

	public static void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
	}	

	public static int GetNumPlayers()
	{
		if( instance )
			return instance.numPlayers;

		return 0;
	}

}
