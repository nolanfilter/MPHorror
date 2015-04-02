using UnityEngine;
using System.Collections;

public class MannequinController : MonoBehaviour {

	void Start()
	{
		MannequinAgent.RegisterMannequin( gameObject );
	}

	void OnDisable()
	{
		if( !PlayerAgent.GetIsMonsterSet() && MannequinAgent.GetShouldMonsterize() )
			PlayerAgent.MonsterizeNearestPlayer( transform.position );

		PlayerAgent.CheckForEnd();
	}

	void OnDestroy()
	{
		MannequinAgent.UnregisterMannequin( gameObject );
	}
}
