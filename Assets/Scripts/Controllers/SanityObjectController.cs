using UnityEngine;
using System.Collections;

public class SanityObjectController : MonoBehaviour {

	public float sanityChange = 0.016f;
	public bool damageOverTime = true;

	void Start()
	{
		sanityChange = Mathf.Clamp( sanityChange, -1f, 1f );
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
			
			if( playerController == null || playerController.GetCurrentState() != PlayerController.State.Normal )
				return;

			if( damageOverTime )
				playerController.ChangeFear( sanityChange * Time.deltaTime );
			else
				playerController.ChangeFear( sanityChange );
		}
	}
}
