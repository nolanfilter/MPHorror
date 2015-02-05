using UnityEngine;
using System.Collections;

public class InfluenceController : MonoBehaviour {

	private float radius = 2f;
	private float radiusZoom = 0.5f;
	private float zoomDistance = 2.5f;
	private float rageDistance = 0.5f;
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

		if( playerController.IsZoomedIn() ) 
		{
			if( playerController.GetCurrentState() == PlayerController.State.Raging )
				distance = radiusZoom + transform.root.localScale.z + rageDistance;
			else
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
			Debug.DrawRay( ray.origin, ray.direction * distance, Color.magenta );

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

			if( otherPlayerController == null || otherPlayerController == playerController )
				return;

			if( playerController.IsZoomedIn() )
			{
				if( playerController.GetCurrentState() == PlayerController.State.Raging && otherPlayerController.GetCurrentState() != PlayerController.State.Dead && otherPlayerController.GetCurrentState() != PlayerController.State.Voyeur )
				{
					playerController.RageHit();
					otherPlayerController.IncreaseFear();
				}

				if( playerController.GetCurrentState() == PlayerController.State.Normal || playerController.GetCurrentState() == PlayerController.State.None )
				{
					if( otherPlayerController.GetCurrentState() == PlayerController.State.Monster  )
						otherPlayerController.MonsterReveal();
					else if( otherPlayerController.GetCurrentState() == PlayerController.State.Normal || otherPlayerController.GetCurrentState() == PlayerController.State.None )
						otherPlayerController.SurvivorReveal();
				}
			}
		}
		else if( collider.tag == "Key" || collider.tag == "Key2" )
		{
			PlayMakerFSM fsm = collider.GetComponent<PlayMakerFSM>();

			if( fsm == null || !playerController.IsZoomedIn() )
				return;

			if( playerController.GetCurrentState() == PlayerController.State.Raging  )
			{
				playerController.MonsterReveal();
				return;
			}

			fsm.SendEvent( "ObjectSeen" );
		}
		else if( collider.tag == "Activatable" )
		{
			PlayMakerFSM fsm = collider.GetComponent<PlayMakerFSM>();
			
			if( fsm == null || !playerController.IsZoomedIn() )
				return;

			if( playerController.GetCurrentState() == PlayerController.State.Raging  )
			{
				playerController.MonsterReveal();
				return;
			}
			
			fsm.SendEvent( "ObjectSeen" );
		}
	}
}
