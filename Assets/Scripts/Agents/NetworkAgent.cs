using UnityEngine;
using System.Collections;

public class NetworkAgent : MonoBehaviour {

	public GameObject playerPrefab;

	private const string roomName = "MPHorror_";
	private RoomInfo[] roomsList;

	void Start()
	{
		PhotonNetwork.ConnectUsingSettings( "0.1" );
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
			if( GUI.Button( new Rect( 100, 100, 250, 100 ), "Start Server" ) )
				PhotonNetwork.CreateRoom( roomName + System.Guid.NewGuid().ToString( "N" ), true, true, 5 );
			
			// Join Room
			if( roomsList != null )
			{
				for (int i = 0; i < roomsList.Length; i++ )
				{
					if( GUI.Button( new Rect( 100, 250 + ( 110 * i ), 250, 100 ), "Join " + roomsList[i].name ) )
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
		// Spawn player
		if( playerPrefab != null )
			PhotonNetwork.Instantiate ( playerPrefab.name, Vector3.up * 5, Quaternion.identity, 0 );
	}

	/*
	private const string typeName = "UniqueGameName";
	private const string gameName = "MPHorror";

	public GameObject playerPrefab;

	private bool isRefreshingHostList = false;
	private HostData[] hostList;

	private int attemptedConnectionHostIndex = -1;

	void Awake()
	{
		//MasterServer.ipAddress = "127.0.0.1";
	}

	void OnGUI()
	{
		if( !Network.isClient && !Network.isServer )
		{
			if( GUI.Button( new Rect( 100, 100, 250, 100 ), "Start Server" ) )
				StartServer();
			
			if( GUI.Button( new Rect( 100, 250, 250, 100 ), "Refresh Hosts" ) )
				RefreshHostList();
			
			if( hostList != null )
			{
				for( int i = 0; i < hostList.Length; i++ )
				{
					if( attemptedConnectionHostIndex != i )
					{
						if( GUI.Button( new Rect( 400, 100 + (110 * i), 300, 100), hostList[i].gameName ) )
						{
							attemptedConnectionHostIndex = i;
							JoinServer(hostList[i]);
						}
					}
				}
			}
		}
	}
	
	private void StartServer()
	{
		Network.InitializeServer( 4, 25001, !Network.HavePublicAddress() );
		MasterServer.RegisterHost( typeName, gameName );
	}
	
	void OnServerInitialized()
	{
		SpawnPlayer();
	}
	
	
	void Update()
	{
		if( isRefreshingHostList && MasterServer.PollHostList().Length > 0 )
		{
			isRefreshingHostList = false;
			hostList = MasterServer.PollHostList();
		}
	}
	
	private void RefreshHostList()
	{
		if( !isRefreshingHostList )
		{
			isRefreshingHostList = true;
			MasterServer.RequestHostList( typeName );
		}
	}
	
	private void JoinServer( HostData hostData )
	{
		Network.Connect( hostData );
	}
	
	void OnConnectedToServer()
	{
		attemptedConnectionHostIndex = -1;
		SpawnPlayer();
	}

	void OnFailedToConnect()
	{
		attemptedConnectionHostIndex = -1;
	}
	
	private void SpawnPlayer()
	{
		Network.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity, 0);
	}
	*/
}
