using UnityEngine;
using System.Collections;

public class NetworkAgent : MonoBehaviour {

	public Texture2D selectionCursor;
	public int numPlayers;
	public GameObject playerPrefab;
	public GameObject networkBackgroundPrefab;
	public Vector3[] playerStartPositions;
	public Vector3[] playerStartRotations;

	private const string roomName = "MPHorror_";
	private RoomInfo[] roomsList;

	private GameObject networkBackground;

	private int selectionIndex;
	
	private bool isHost = false;

	private GUIStyle textStyle;
	private GUIStyle shadowStyle;

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

		textStyle = new GUIStyle();
		textStyle.font = FontAgent.GetUIFont();
		textStyle.normal.textColor = Color.white;
		textStyle.alignment = TextAnchor.MiddleCenter;

		shadowStyle = new GUIStyle();
		shadowStyle.font = FontAgent.GetUIFont();
		shadowStyle.normal.textColor = Color.black;
		shadowStyle.alignment = TextAnchor.MiddleCenter;

		//if( networkBackgroundPrefab )
		//	networkBackground = Instantiate( networkBackgroundPrefab ) as GameObject;
	}

	void Update()
	{
		if( networkBackground != null )
		{
			networkBackground.GetComponent<Renderer>().enabled = ( PhotonNetwork.room == null );
		}

		if( GameAgent.GetCurrentGameState() == GameAgent.GameState.Waiting )
		{
			if( PhotonNetwork.room != null )
				GameAgent.ChangeGameState( GameAgent.GameState.Room );
		}

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
				Rect createRoomRect = new Rect( 100, 100, 300, 100 );

				if( selectionIndex == 0 )
					GUI.DrawTexture( new Rect( createRoomRect.center.x - 225, createRoomRect.center.y - 390, 512, 512 ), selectionCursor );
				
				GUI.Label( new Rect( createRoomRect.x + 3, createRoomRect.y, createRoomRect.width, createRoomRect.height ), "Start Server", shadowStyle );
				GUI.Label( createRoomRect, "Start Server", textStyle );
					
				// Join Room
				if( roomsList != null )
				{
					Rect roomRect;

					for (int i = 0; i < roomsList.Length; i++ )
					{
						roomRect = new Rect( Screen.width * 0.5f, 100 + ( 150 * i ), 300, 100 );

						if( selectionIndex == i + 1 )
							GUI.DrawTexture( new Rect( roomRect.center.x - 225, roomRect.center.y - 390, 512, 512 ), selectionCursor );

						GUI.Label( new Rect( roomRect.x + 3, roomRect.y, roomRect.width, roomRect.height ), "Join Room " + i, shadowStyle );
						GUI.Label( roomRect, "Join Room " + i, textStyle );
					}
				}
			}
		}
		else if( GameAgent.GetCurrentGameState() == GameAgent.GameState.Room )
		{
			if( isHost )
			{
				GUI.Label( new Rect( Screen.width * 0.1f + 3, 75, Screen.width * 0.8f, 100 ), "Hosting Room", shadowStyle );
				GUI.Label( new Rect( Screen.width * 0.1f, 75, Screen.width * 0.8f, 100 ), "Hosting Room", textStyle );
			}
			else
			{
				GUI.Label( new Rect( Screen.width * 0.1f + 3, 75, Screen.width * 0.8f, 100 ), "Joined Room", shadowStyle );
				GUI.Label( new Rect( Screen.width * 0.1f, 75, Screen.width * 0.8f, 100 ), "Joined Room", textStyle );
			}

			int playersConnected = PhotonNetwork.playerList.Length;
			
			string playersConnectedString = "";
			
			if( playersConnected == 1 )
				playersConnectedString += playersConnected + " player connected";
			else
				playersConnectedString += playersConnected + " players connected";

			GUI.Label( new Rect( Screen.width * 0.3f + 3, 225, Screen.width * 0.4f, 100 ), playersConnectedString, shadowStyle );
			GUI.Label( new Rect( Screen.width * 0.3f, 225, Screen.width * 0.4f, 100 ), playersConnectedString, textStyle );
		}
		else if( GameAgent.GetCurrentGameState() == GameAgent.GameState.End )
		{
			GUI.Label( new Rect( Screen.width * 0.1f + 3, 75, Screen.width * 0.8f, 100 ), "THE END", shadowStyle );
			GUI.Label( new Rect( Screen.width * 0.1f, 75, Screen.width * 0.8f, 100 ), "THE END", textStyle );

			string endStatusString = "";

			switch( PlayerAgent.GetClientState() )
			{
				case PlayerController.State.Dead: endStatusString = "Trapped forever"; break;
				case PlayerController.State.Voyeur: endStatusString = "Sweet freedom"; break;
				case PlayerController.State.Monster: endStatusString = "Betrayal suits you"; break;
			}

			GUI.Label( new Rect( Screen.width * 0.3f + 3, 225, Screen.width * 0.4f, 100 ), endStatusString, shadowStyle );
			GUI.Label( new Rect( Screen.width * 0.3f, 225, Screen.width * 0.4f, 100 ), endStatusString, textStyle );
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
			MannequinAgent.Reset();
			FSMAgent.Reset();
		}
	}

	public static void LeaveRoom()
	{
		if( PhotonNetwork.inRoom )
		{
			PhotonNetwork.LeaveRoom ();
			GameAgent.ChangeGameState( GameAgent.GameState.Lobby );
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
