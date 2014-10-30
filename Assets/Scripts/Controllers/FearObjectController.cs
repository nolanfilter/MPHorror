using UnityEngine;
using System.Collections;

public class FearObjectController : MonoBehaviour {

	public float fearChange = 0.3f;
	public bool damageOverTime = false;
	public string eventName = "";

	void Start()
	{
		fearChange = Mathf.Clamp( fearChange, -1f, 1f );
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

			bool canIncreaseFear = playerController.CanIncreaseFear();

			if( playerController == null || playerController.GetCurrentState() != PlayerController.State.Normal || !canIncreaseFear )
				return;

			if( damageOverTime )
				playerController.ChangeFear( fearChange * Time.deltaTime );
			else
				playerController.ChangeFear( fearChange );

			PlayMakerFSM fsm = GetComponent<PlayMakerFSM>();
			
			if( fsm == null || eventName == "" )
				return;

			fsm.SendEvent( eventName );
		}
	}
}
