using UnityEngine;
using System.Collections;

public class ShakeEffect : MonoBehaviour {

	private float shakeDuration = 0.6f;
	private float currentTime = 0f;

	void Start()
	{
		if( gameObject.GetComponents<ShakeEffect>().Length > 1 )
			Destroy( this );
	}

	void LateUpdate()
	{
		transform.position += Vector3.up * Random.Range( -0.05f, 0.05f );

		currentTime += Time.deltaTime;

		if( currentTime > shakeDuration )
			Destroy( this );
	}
}
