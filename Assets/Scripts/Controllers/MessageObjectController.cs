using UnityEngine;
using System.Collections;

public class MessageObjectController : MonoBehaviour {

	public string message = "";

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
			
			if( playerController == null )
				return;

			playerController.DisplayMessage( message );

			enabled = false;
		}
	}
}
