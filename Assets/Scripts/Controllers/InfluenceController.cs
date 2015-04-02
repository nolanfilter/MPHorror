using UnityEngine;
using System.Collections;

public class InfluenceController : MonoBehaviour {

	private float sphereRadius = 2f;
	private float zoomRadius = 0.5f;
	private float rageRadius = 0.25f;
	private float zoomDistance = 2f;
	private float rageDistance = 1f;
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
		if( GameAgent.GetCurrentGameState() != GameAgent.GameState.Game )
			return;

		Ray ray;

		float distance;
		float radius;

		if( playerController.IsZoomedIn() ) 
		{
			if( playerController.GetCurrentState() == PlayerController.State.Raging )
			{
				distance = rageDistance;
				radius = rageRadius;
			}
			else
			{
				distance = zoomDistance;
				radius = zoomRadius;
			}

			ray = new Ray( transform.position + transform.forward * distance, transform.forward * -1f );

			RaycastHit[] hits = Physics.SphereCastAll( ray, radius, distance );

			foreach( RaycastHit hit in hits )
				EvaluateCollider( hit.collider );
		}
		else
		{
			distance = sphereRadius + transform.root.localScale.y;

			ray = new Ray( transform.position + Vector3.up * distance, Vector3.up * -1f );

			Collider[] colliders = Physics.OverlapSphere( transform.position, distance );

			foreach( Collider collider in colliders )
				EvaluateCollider( collider );
		}

		if( showDebugRay )
			Debug.DrawRay( ray.origin, ray.direction * distance, Color.magenta );
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
				if( playerController.GetCurrentState() == PlayerController.State.Raging )
				{
					if( otherPlayerController.GetCurrentState() == PlayerController.State.Frozen )
					{
						playerController.MonsterReveal();
					}
					else if( otherPlayerController.GetCurrentState() != PlayerController.State.Dead && otherPlayerController.GetCurrentState() != PlayerController.State.Voyeur )
					{
						playerController.RageHit();
						otherPlayerController.IncreaseFear();
					}
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
			if( !playerController.IsZoomedIn() )
				return;

			if( playerController.GetCurrentState() == PlayerController.State.Raging  )
			{
				playerController.MonsterReveal();
				return;
			}

			PlayMakerFSM fsm = collider.GetComponent<PlayMakerFSM>();

			if( fsm != null )
				fsm.SendEvent( "ObjectSeen" );
		}
		else if( collider.tag == "Activatable" )
		{
			if( !playerController.IsZoomedIn() )
				return;

			if( playerController.GetCurrentState() == PlayerController.State.Raging  )
			{
				playerController.MonsterReveal();
				return;
			}

			PlayMakerFSM fsm = collider.GetComponent<PlayMakerFSM>();

			if( fsm != null )
				fsm.SendEvent( "ObjectSeen" );
		}
		else if( collider.tag == "Door" )
		{
			if( !playerController.IsOpeningDoor() )
				return;

			PlayMakerFSM fsm = collider.GetComponent<PlayMakerFSM>();
			
			if( fsm != null )
				fsm.SendEvent( "ObjectSeen" );
		}
	}
}
