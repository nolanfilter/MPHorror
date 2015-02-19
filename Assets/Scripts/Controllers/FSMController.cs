using UnityEngine;
using System.Collections;

public class FSMController : MonoBehaviour {

	private PlayMakerFSM FSM;

	void Start () {

		FSM = GetComponent<PlayMakerFSM>();

		if( FSM )
			FSMAgent.RegisterFSM( FSM );
	}
	
	void OnDestroy()
	{
		if( FSM )
			FSMAgent.UnregisterFSM( FSM );
	}
}
