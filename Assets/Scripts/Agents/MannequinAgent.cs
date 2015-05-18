using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MannequinAgent : MonoBehaviour {

	public GameObject mannequinPrefab;

	public int numMannequinsToGenerate;
	public float mannequinHeight = 0.96f;
	public float minMannequinDistance = 1f;
	public Transform mannequinAreaRoot;

	public int maxMonsterMannequins = int.MaxValue;

	private string[] possiblePoses = { "Pose 2", "Pose 3", "Pose 4" };

	private List<GameObject> mannequins;
	private List<GameObject> randomMannequins;
	private List<GameObject> monsterMannequins;
	private List<Rect> mannequinAreas;

	private static MannequinAgent mInstance = null;
	public static MannequinAgent instance
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
			Debug.LogError( string.Format( "Only one instance of MannequinAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;
		
		mannequins = new List<GameObject>();
		randomMannequins = new List<GameObject>();
		monsterMannequins = new List<GameObject>();

		mannequinAreas = new List<Rect>();

		if( mannequinAreaRoot )
		{
			mannequinAreaRoot.gameObject.SetActive( true );

			foreach( Transform childTransform in mannequinAreaRoot )
				mannequinAreas.Add( new Rect( childTransform.transform.position.x - childTransform.transform.localScale.x * 0.5f, childTransform.transform.position.z - childTransform.transform.localScale.y * 0.5f, childTransform.transform.localScale.x, childTransform.transform.localScale.y ) );
		
			mannequinAreaRoot.gameObject.SetActive( false );
		}
	}

	/*
	void OnGUI()
	{
		if( GameAgent.GetCurrentGameState() != GameAgent.GameState.Game )
			return;

		int numActiveMannequins = 0;
		
		for( int i = 0; i < mannequins.Count; i++ )
			if( mannequins[i].activeSelf )
				numActiveMannequins++;

		GUI.Label( new Rect( 10f, 10f, 1000f, 1000f ), "" + numActiveMannequins + "/" + mannequins.Count );
	}
	*/

	public static void RegisterMannequin( GameObject mannequin )
	{
		if( instance )
			instance.internalRegisterMannequin( mannequin );
	}
	
	private void internalRegisterMannequin( GameObject mannequin )
	{
		if( !mannequins.Contains( mannequin ) )
			mannequins.Add( mannequin );
	}

	public static void UnregisterMannequin( GameObject mannequin )
	{
		if( instance )
			instance.internalUnregisterMannequin( mannequin );
	}
	
	private void internalUnregisterMannequin( GameObject mannequin )
	{
		if( mannequins.Contains( mannequin ) )
			mannequins.Remove( mannequin );
	}

	public static void Reset()
	{
		if( instance )
			instance.internalReset();
	}

	private void internalReset()
	{
		if( randomMannequins.Count > 0 )
		{
			for( int i = 0; i < randomMannequins.Count; i++ )
			{
				randomMannequins[i].GetComponent<MannequinController>().SetShouldCount( false );

				mannequins.Remove( randomMannequins[i] );
				Destroy( randomMannequins[i] );
			}

			randomMannequins.Clear();
		}

		if( monsterMannequins.Count > 0 )
		{
			for( int i = 0; i < monsterMannequins.Count; i++ )
			{
				monsterMannequins[i].GetComponent<MannequinController>().SetShouldCount( false );

				mannequins.Remove( monsterMannequins[i] );
				Destroy( monsterMannequins[i] );
			}

			monsterMannequins.Clear();
		}

		int seed = Utilities.HexToInt( PhotonNetwork.room.name[ PhotonNetwork.room.name.Length - 2 ] );
		
		Random.seed = seed;

		if( mannequins.Count > 0 )
		{
			for( int i = 0; i < mannequins.Count; i++ )
			{
				mannequins[i].transform.localScale = Vector3.one;
				mannequins[i].SetActive( true );
				mannequins[i].GetComponent<MannequinController>().SetPose( possiblePoses[ Random.Range( 0, possiblePoses.Length ) ] );
				//mannequins[i].tag = "Activatable";
			}
		}

		if( mannequinPrefab == null || mannequinAreaRoot == null )
			return;

		int attempts = 0;
		int maxAttempts = numMannequinsToGenerate * 3;
		int randomAreaIndex;
		Vector3 randomPosition;
		Collider[] colliders;
		bool isViablePosition;

		while( attempts < maxAttempts && randomMannequins.Count < numMannequinsToGenerate )
		{
			attempts++;

			randomAreaIndex = Random.Range( 0, mannequinAreas.Count );

			randomPosition = new Vector3( Random.Range( mannequinAreas[randomAreaIndex].xMin, mannequinAreas[randomAreaIndex].xMax ), mannequinHeight, Random.Range( mannequinAreas[randomAreaIndex].yMin, mannequinAreas[randomAreaIndex].yMax ) );

			colliders = Physics.OverlapSphere( randomPosition, minMannequinDistance );

			isViablePosition = true;

			for( int i = 0; i < colliders.Length; i++ )
				if( colliders[i].GetComponent<MannequinController>() )
					isViablePosition = false;

			if( isViablePosition )
			{
				randomMannequins.Add( Instantiate( mannequinPrefab, randomPosition, Quaternion.AngleAxis( Random.Range( 0, 360f ), Vector3.up ) ) as GameObject );
			}
		}

		for( int i = 0; i < randomMannequins.Count; i++ )
			randomMannequins[i].GetComponent<MannequinController>().SetPose( possiblePoses[ Random.Range( 0, possiblePoses.Length ) ] );
	}

	public static bool GetAllMannequinsDisabled()
	{
		if( instance )
			return instance.internalGetAllMannequinsDisabled();

		return false;
	}

	private bool internalGetAllMannequinsDisabled()
	{
		if( mannequins.Count == 0 )
			return true;

		int numActiveMannequins = 0;

		for( int i = 0; i < mannequins.Count; i++ )
			if( mannequins[i].activeSelf )
				numActiveMannequins++;

		return ( numActiveMannequins == 0 );
	}

	public static Vector3 GetClosestMannequin( Vector3 position )
	{
		if( instance )
			return instance.internalGetClosestMannequin( position );

		return Vector3.zero;
	}

	private Vector3 internalGetClosestMannequin( Vector3 position )
	{
		float closestDistance = Mathf.Infinity;
		Vector3 closestPosition = Vector3.zero;
		float distance;
		Vector3 adjustedPosition;

		for( int i = 0; i < mannequins.Count; i++ )
		{
			if( mannequins[i].activeSelf )
			{
				adjustedPosition = new Vector3( mannequins[i].transform.position.x, position.y, mannequins[i].transform.position.z );

				distance = Vector3.Distance( position, adjustedPosition );

				if( distance < closestDistance )
				{
					closestDistance = distance;
					closestPosition = adjustedPosition;
				}
			}
		}

		List<Vector3> frozenPlayerPositions = PlayerAgent.GetFrozenPlayerPositions();

		for( int i = 0; i < frozenPlayerPositions.Count; i++ )
		{
			distance = Vector3.Distance( position, frozenPlayerPositions[i] );
			
			if( distance < closestDistance )
			{
				closestDistance = distance;
				closestPosition = frozenPlayerPositions[i];
			}
		}

		return closestPosition;
	}

	public static int GetNumActiveMannequins()
	{
		if( instance )
			return instance.internalGetNumActiveMannequins();

		return -1;
	}

	private int internalGetNumActiveMannequins()
	{
		int numActiveMannequins = 0;
		
		for( int i = 0; i < mannequins.Count; i++ )
			if( mannequins[i].activeSelf )
				numActiveMannequins++;

		return numActiveMannequins;
	}

	public static bool GetShouldMonsterize()
	{
		if( instance )
			return instance.internalGetShouldMonsterize();
		
		return false;
	}

	private bool internalGetShouldMonsterize()
	{
		if( mannequins.Count == 0 )
			return true;
		
		int numActiveMannequins = 0;
		
		for( int i = 0; i < mannequins.Count; i++ )
			if( mannequins[i].activeSelf )
				numActiveMannequins++;

		return ( mannequins.Count - numActiveMannequins >= PlayerAgent.GetMonsterizingMannequinNumber() );
	}

	public static void CreateMonsterMannequin( Vector3 position )
	{
		if( instance )
			instance.internalCreateMonsterMannequin( position );
	}

	private void internalCreateMonsterMannequin( Vector3 position )
	{
		monsterMannequins.Insert( 0, Instantiate( mannequinPrefab, position, Quaternion.AngleAxis( Random.Range( 0, 360f ), Vector3.up ) ) as GameObject );

		monsterMannequins[0].GetComponent<MannequinController>().SetPose( possiblePoses[ Random.Range( 0, possiblePoses.Length ) ] );

		while( monsterMannequins.Count > maxMonsterMannequins )
		{
			monsterMannequins[ maxMonsterMannequins ].GetComponent<MannequinController>().SetShouldCount( false );
			Destroy( monsterMannequins[ maxMonsterMannequins ] );
			monsterMannequins.RemoveAt( maxMonsterMannequins );
		}
	}

	public static int GetIDByMannequin( GameObject mannequin )
	{
		if( instance )
			return instance.internalGetIDByMannequin( mannequin );

		return -1;
	}

	private int internalGetIDByMannequin( GameObject mannequin )
	{
		int ID = mannequins.IndexOf( mannequin );

		if( ID == -1 )
		{
			ID = randomMannequins.IndexOf( mannequin );

			if( ID != -1 )
				ID += mannequins.Count;
		}

		return ID;
	}

	public static void DestroyMannequin( int ID )
	{
		if( instance )
			instance.internalDestroyMannequin( ID );
	}

	private void internalDestroyMannequin( int ID )
	{
		if( ID == -1 )
			return;

		GameObject mannequin = null;

		if( ID < mannequins.Count )
		{
			mannequin = mannequins[ ID ];
		}
		else
		{
			ID -= mannequins.Count;

			if( ID < randomMannequins.Count )
				mannequin = randomMannequins[ ID ];
		}

		if( mannequin )
		{
			PlayMakerFSM fsm = mannequin.GetComponent<PlayMakerFSM>();
			
			if( fsm )
				fsm.SendEvent( "ObjectSeen" );
		}
	}
}
