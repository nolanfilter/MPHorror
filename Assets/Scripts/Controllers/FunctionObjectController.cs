using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FunctionObjectController : MonoBehaviour {

	public enum FunctionMode
	{
		AllPlayers = 0,
		EachPlayer = 1,
		FirstPlayer = 2,
	}
	public FunctionMode functionMode = FunctionMode.AllPlayers;

	public enum FunctionName
	{
		None = 0,
		ChangeSanity = 1,
		ChangeFear = 2,
		DecreaseSanity = 3,
		IncreaseSanity = 4,
		IncreaseFear = 5,
		SetFlashlightTo = 6,
		TeleportTo = 7,
		DisplayMessage = 8,
		Escape = 9,
	}
	public FunctionName functionName = FunctionName.None;

	//function specific variables
	public float sanityChange;
	public bool sanityDamageOverTime;
	public float fearChange;
	public bool fearDamageOverTime;
	public bool flashlightOn;
	public Vector3 coordinate;
	public string message;
	//end function specific variables

	public string eventName = "";

	private List<PlayerController> encounteredPlayers;

	void Start()
	{
		encounteredPlayers = new List<PlayerController>();
	}

	void OnTriggerEnter( Collider collider )
	{
		EvaluateCollider( collider );
	}
	
	void OnTiggerStay( Collider collider )
	{
		EvaluateCollider( collider );
	}
	
	private void EvaluateCollider( Collider collider )
	{
		if( collider.tag == "Player" )
		{
			PlayerController playerController = collider.GetComponent<PlayerController>();

			if( playerController == null || encounteredPlayers.Contains( playerController ) )
				return;

			switch( functionName )
			{
				case FunctionObjectController.FunctionName.ChangeSanity:
				{
					if( sanityDamageOverTime )
						playerController.ChangeFear( sanityChange * Time.deltaTime );
					else
						playerController.ChangeFear( sanityChange );
				} break;
					
				case FunctionObjectController.FunctionName.ChangeFear:
				{
					if( fearDamageOverTime )
						playerController.ChangeFear( fearChange * Time.deltaTime );
					else
						playerController.ChangeFear( fearChange );
				} break;

				case FunctionObjectController.FunctionName.DecreaseSanity:
				{
					playerController.DecreaseSanity();
				} break;

				case FunctionObjectController.FunctionName.IncreaseSanity:
				{
					playerController.IncreaseSanity();
				} break;

				case FunctionObjectController.FunctionName.IncreaseFear:
				{
					playerController.IncreaseFear();
				} break;

				case FunctionObjectController.FunctionName.SetFlashlightTo:
				{
					playerController.SetFlashlightTo( flashlightOn );
				} break;
					
				case FunctionObjectController.FunctionName.TeleportTo:
				{
					playerController.TeleportTo( coordinate );
				} break;
					
				case FunctionObjectController.FunctionName.DisplayMessage:
				{
					playerController.DisplayMessage( message );
				} break;

				case FunctionObjectController.FunctionName.Escape:
				{
					playerController.Escape();
				} break;
			}

			PlayMakerFSM fsm = GetComponent<PlayMakerFSM>();
			
			if( fsm != null && eventName != "" )
				fsm.SendEvent( eventName );

			if( functionMode == FunctionMode.EachPlayer )
				encounteredPlayers.Add( playerController );
			
			if( ( functionMode == FunctionMode.FirstPlayer || encounteredPlayers.Count >= NetworkAgent.GetNumPlayers() ) )
				Destroy( gameObject );
		}
	}

}
