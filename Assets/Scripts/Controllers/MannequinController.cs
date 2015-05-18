using UnityEngine;
using System.Collections;

public class MannequinController : MonoBehaviour {

	public Animation animation;

	private bool shouldCount = true;

	void Start()
	{
		MannequinAgent.RegisterMannequin( gameObject );

		StartCoroutine( "WaitAndEnableCollider" );
	}

	void OnDisable()
	{
		if( !shouldCount )
			return;

		if( !PlayerAgent.GetIsMonsterSet() && MannequinAgent.GetShouldMonsterize() )
			PlayerAgent.MonsterizeNearestPlayer( transform.position );

		PlayerAgent.CheckForEnd();
	}

	void OnDestroy()
	{
		MannequinAgent.UnregisterMannequin( gameObject );
	}

	public void SetPose( string pose )
	{
		if( animation )
			animation.Play( pose );
	}

	public void SetShouldCount( bool newShouldCount )
	{
		shouldCount = newShouldCount;
	}

	private IEnumerator WaitAndEnableCollider()
	{
		CapsuleCollider collider = GetComponent<CapsuleCollider>();

		if( collider == null )
			yield break;

		collider.enabled = false;

		Vector3 start = transform.position - Vector3.up * collider.height * 0.5f;
		Vector3 end = transform.position + Vector3.up * collider.height * 0.5f;
		float radius = collider.radius;

		while( !collider.enabled )
		{
			collider.enabled = !Physics.CheckCapsule( start, end, radius );

			yield return null;
		}
	}
}
