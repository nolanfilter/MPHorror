using UnityEngine;
using System.Collections;

public class NetworkAgent : MonoBehaviour {

	public int numPlayers;
	public GameObject playerPrefab;
	public Vector3[] playerStartPositions;
	public Vector3[] playerStartRotations;

	private const string roomName = "MPHorror_";
	private RoomInfo[] roomsList;

	private GameObject networkBackground;

	private int selectionIndex;
	
	private bool isHost = false;
	private bool isReady = false;

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
	}

	void OnReceivedRoomListUpdate()
	{
		roomsList = PhotonNetwork.GetRoomList();
	}

	void OnJoinedLobby()
	{
		isReady = true;
	}

	void OnConnectedToMaster()
	{
		isReady = true;
	}

	void OnJoinedRoom()
	{
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

			PhotonNetwork.Instantiate( playerPrefab.name, playerStartPositions[ playerNumber%playerStartPositions.Length ] + Vector3.up * 0.96f, Quaternion.Euler( playerStartRotations[ playerNumber%playerStartRotations.Length ] ), 0 );
			MannequinAgent.Reset();
			FSMAgent.Reset();
		}
	}

	public static void LeaveRoom()
	{
		if( PhotonNetwork.inRoom )
		{
			PhotonNetwork.LeaveRoom();
		}
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
		if( PhotonNetwork.connectionState != ConnectionState.Connected || !isReady )
			return;

		if( selectionIndex == 0 )
		{
			PhotonNetwork.CreateRoom( roomName + System.Guid.NewGuid().ToString( "N" ), true, true, numPlayers );
			isHost = true;
		}
		else
		{
			//PhotonNetwork.JoinRoom( roomsList[ selectionIndex - 1 ].name );
			PhotonNetwork.JoinRandomRoom();
			isHost = false;
		}

		GameAgent.ChangeGameState( GameAgent.GameState.Waiting );
	}

	public static void LockRoom()
	{
		if( instance )
			instance.internalLockRoom();
	}

	private void internalLockRoom()
	{
		Room room = PhotonNetwork.room;

		if( room != null )
		{
			room.open = false;
			room.visible = false;
		}
	}
}
