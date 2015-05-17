using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RoomScreen : MonoBehaviour {

	public Image hostCursor;
	public Image joinCursor;
	public Text numPlayersText;
	public Text roomText;
	public Image aButton;
	public Text startText;

	private bool isHost;

	private static RoomScreen mInstance = null;
	public static RoomScreen instance
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
			Debug.LogError( string.Format( "Only one instance of RoomScreen allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;

		isHost = NetworkAgent.GetIsHost();

		if( isHost )
			HighlightHost();
		else
			HighlightJoin();

		if( numPlayersText )
			numPlayersText.text = "1";
		
		if( roomText )
			roomText.text = "player\ncursed";
		
		if( aButton )
			aButton.enabled = false;
		
		if( startText )
			startText.enabled = false;
	}

	void Update()
	{
		UpdateText();
	}

	public static void HighlightHost()
	{
		if( instance )
			instance.internalHighlightHost();
	}

	private void internalHighlightHost()
	{
		if( hostCursor )
			hostCursor.enabled = true;

		if( joinCursor )
			joinCursor.enabled = false;
	}

	public static void HighlightJoin()
	{
		if( instance )
			instance.internalHighlightJoin();
	}

	private void internalHighlightJoin()
	{
		if( hostCursor )
			hostCursor.enabled = false;
		
		if( joinCursor )
			joinCursor.enabled = true;
	}

	public static void Activate()
	{
		if( instance )
			instance.internalActivate();
	}

	private void internalActivate()
	{
		if( isHost )// && aButton && aButton.enabled )
			PlayerAgent.StartGame();
	}

	private void UpdateText()
	{
		int connectedPlayers = PhotonNetwork.playerList.Length;

		switch( connectedPlayers )
		{
			case 1:
			{
				if( numPlayersText )
					numPlayersText.text = "1";

				if( roomText )
					roomText.text = "player\ncursed";

				if( aButton )
					aButton.enabled = false;

				if( startText )
					startText.enabled = false;
			} break;

			case 2:
			{
				if( numPlayersText )
					numPlayersText.text = "11";
				
				if( roomText )
					roomText.text = "players\ncursed";

				if( aButton )
					aButton.enabled = isHost;
				
				if( startText )
					startText.enabled = isHost;
			} break;

			case 3:
			{
				if( numPlayersText )
					numPlayersText.text = "111";
				
				if( roomText )
					roomText.text = "players\ncursed";

				if( aButton )
					aButton.enabled = isHost;
				
				if( startText )
					startText.enabled = isHost;
			} break;

			case 4:
			{
				if( numPlayersText )
					numPlayersText.text = "4";
				
				if( roomText )
					roomText.text = "players\ncursed";

				if( aButton )
					aButton.enabled = isHost;
				
				if( startText )
					startText.enabled = isHost;
			} break;
		}
	}
}
