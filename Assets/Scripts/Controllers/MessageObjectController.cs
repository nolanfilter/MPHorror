using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MessageObjectController : MonoBehaviour {

	public string message = "";

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

			playerController.DisplayMessage( message );

			encounteredPlayers.Add( playerController );

			if( encounteredPlayers.Count >= NetworkAgent.GetNumPlayers() )
				Destroy( gameObject );
		}
	}
}
