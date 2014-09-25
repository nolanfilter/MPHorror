using UnityEngine;
using System.Collections;

public class NetworkAgent : MonoBehaviour {

	public GameObject playerPrefab;
	public GameObject networkBackgroundPrefab;

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
		if( Input.GetKeyDown( KeyCode.Return ) )
		{
			LeaveRoom();
		}

		if( networkBackground != null )
			networkBackground.renderer.enabled = ( PhotonNetwork.room == null );
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
					if( GUI.Button( new Rect( 250, 100 + ( 110 * i ), 250, 100 ), "Join " + roomsList[i].name ) )
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

	public static void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
	}	
}
