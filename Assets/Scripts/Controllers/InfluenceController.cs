using UnityEngine;
using System.Collections;

public class InfluenceController : MonoBehaviour {

	public float radius = 2f;
	public float radiusZoom = 0.5f;
	public float zoomDistance = 2.5f;
	public bool showDebugRay = false;

	private PlayerController playerController = null;

	void Start()
	{
		if( transform.parent != null )
			playerController = transform.parent.GetComponent<PlayerController>();

		if( playerController == null )
		{
			Debug.LogError( "No player controller on Influence Controller's parent" );
			enabled = false;
			return;
		}
	}

	void FixedUpdate()
	{
		RaycastHit[] hits;

		Ray ray;

		float distance;

		if (playerController.IsZoomedIn ()) 
		{
			distance = radiusZoom + transform.root.localScale.z + zoomDistance;

			ray = new Ray( transform.position + transform.forward * distance, transform.forward * -1f );

			hits = Physics.SphereCastAll( ray, radiusZoom, distance );
		}
		else
		{
			distance = radius + transform.root.localScale.y;

			ray = new Ray( transform.position + Vector3.up * distance, Vector3.up * -1f );

			hits = Physics.SphereCastAll( ray, radius, distance );
		}

		if( showDebugRay )
			Debug.DrawRay( ray.origin, ray.direction * distance );

		foreach( RaycastHit hit in hits )
			EvaluateCollider( hit.collider );
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
			PlayerController otherPlayerController = collider.GetComponent<PlayerController>();

			if( otherPlayerController == null || otherPlayerController == playerController || otherPlayerController.GetCurrentState() != PlayerController.State.Normal )
				return;

			if( playerController.GetCurrentState() == PlayerController.State.Normal )
			{
				otherPlayerController.IncreaseSanity();
			}
			else if( playerController.GetCurrentState() == PlayerController.State.Monster )
			{
				bool killedByPlayer = otherPlayerController.IncreaseFear();

				if( killedByPlayer )
					otherPlayerController.IncrementPoint();
			}
		}
		else if( collider.tag == "Key" )
		{
			PlayMakerFSM fsm = collider.GetComponent<PlayMakerFSM>();

			if( fsm == null || playerController.GetCurrentState() != PlayerController.State.Normal || !playerController.IsZoomedIn() )
				return;

			fsm.SendEvent( "ObjectSeen" );
		}
		else if( collider.tag == "Activatable" )
		{
			PlayMakerFSM fsm = collider.GetComponent<PlayMakerFSM>();
			
			if( fsm == null || !playerController.IsZoomedIn() )
				return;
			
			fsm.SendEvent( "ObjectSeen" );
		}
	}
}
