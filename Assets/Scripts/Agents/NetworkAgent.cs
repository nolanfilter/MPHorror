using UnityEngine;
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

	private int selectionIndex;

	private bool wasInRoom = false;

	private bool isHost = false;

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

		//if( networkBackgroundPrefab )
		//	networkBackground = Instantiate( networkBackgroundPrefab ) as GameObject;
	}

	void Update()
	{
		if( networkBackground != null )
		{
			networkBackground.renderer.enabled = ( PhotonNetwork.room == null );
		}

		if( GameAgent.GetCurrentGameState() == GameAgent.GameState.Waiting )
		{
			if( PhotonNetwork.room != null  && !wasInRoom )
				GameAgent.ChangeGameState( GameAgent.GameState.Room );
		}

		wasInRoom = ( PhotonNetwork.room == null );

		SetSelectionIndex( GetSelectionIndex() );
	}
	
	void OnGUI()
	{
		if( GameAgent.GetCurrentGameState() == GameAgent.GameState.Lobby )
		{
			if( !PhotonNetwork.connected )
			{
				GUILayout.Label(PhotonNetwork.connectionStateDetailed.ToString());
			}
			else if( PhotonNetwork.room == null )
			{
				// Create Room
				Rect createRoomRect;

				if( selectionIndex == 0 )
					createRoomRect = new Rect( 75, 75, 350, 150 );
				else
					createRoomRect = new Rect( 100, 100, 300, 100 );

				GUI.Button( createRoomRect, "Start Server" );
					
				// Join Room
				if( roomsList != null )
				{
					Rect roomRect;

					for (int i = 0; i < roomsList.Length; i++ )
					{
						if( selectionIndex == i + 1 )
							roomRect = new Rect( Screen.width * 0.5f - 25, 75 + ( 150 * i ), 350, 150 );
						else
							roomRect = new Rect( Screen.width * 0.5f, 100 + ( 150 * i ), 300, 100 );

						GUI.Button( roomRect, "Join\nRoom " + i );
					}
				}
			}
		}
		else if( GameAgent.GetCurrentGameState() == GameAgent.GameState.Room )
		{
			if( isHost )
			{
				GUI.Button( new Rect( Screen.width * 0.1f, 75, Screen.width * 0.8f, 100 ), "Host of Room" );
				GUI.Button( new Rect( 100, 375, 300, 100 ), "A to Start Game" );
			}
			else
			{
				GUI.Button( new Rect( Screen.width * 0.1f, 75, Screen.width * 0.8f, 100 ), "Joined Room" );
			}

			int playersConnected = PhotonNetwork.playerList.Length;
			
			string playersConnectedString = "";
			
			if( playersConnected == 1 )
				playersConnectedString += playersConnected + " player connected";
			else
				playersConnectedString += playersConnected + " players connected";

			GUI.Button( new Rect( Screen.width * 0.3f, 225, Screen.width * 0.4f, 100 ), playersConnectedString );
			GUI.Button( new Rect( Screen.width * 0.9f - 300, 375, 300, 100 ), "B to Leave Room" );
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

			PhotonNetwork.Instantiate( playerPrefab.name, playerStartPositions[ playerNumber%playerStartPositions.Length ], Quaternion.Euler( playerStartRotations[ playerNumber%playerStartRotations.Length ] ), 0 );
			MannequinAgent.SetKeys();
		}
	}

	public static void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
		GameAgent.ChangeGameState( GameAgent.GameState.Lobby );
	}	

	public static int GetNumPlayers()
	{
		if( instance )
			return instance.numPlayers;

		return 0;
	}

	public static void SetSelectionIndex( int newSelectionIndex )
	{
		if( instance )
			instance.internalSetSelectionIndex( newSelectionIndex );
	}

	private void internalSetSelectionIndex( int newSelectionIndex )
	{
		if( newSelectionIndex > 0 )
		{
			if( roomsList != null )
			{
				if( newSelectionIndex > roomsList.Length )
				{
					newSelectionIndex = roomsList.Length;
				}
			}
			else
			{
				newSelectionIndex = 0;
			}
		}

		selectionIndex = newSelectionIndex;
	}

	public static int GetSelectionIndex()
	{
		if( instance )
			return instance.internalGetSelectionIndex();

		return -1;
	}

	private int internalGetSelectionIndex()
	{
		return selectionIndex;
	}

	public static bool GetIsHost()
	{
		if( instance )
			return instance.internalGetIsHost();

		return false;
	}

	private bool internalGetIsHost()
	{
		return isHost;
	}


	public static void ActivateSelected()
	{
		if( instance )
			instance.internalActivateSelected();
	}

	private void internalActivateSelected()
	{
		if( selectionIndex == 0 )
		{
			PhotonNetwork.CreateRoom( roomName + System.Guid.NewGuid().ToString( "N" ), true, true, numPlayers );
			isHost = true;
		}
		else
		{
			PhotonNetwork.JoinRoom( roomsList[ selectionIndex - 1 ].name );
			isHost = false;
		}

		GameAgent.ChangeGameState( GameAgent.GameState.Waiting );
	}
}
