using UnityEngine;
using System.Collections;

public class PlayDestroyEffect : MonoBehaviour {

	public GameObject particleEffect;
	public GameObject SoulParticle1;
	public GameObject SoulParticle2;
	
	void Awake ()
	{

	}

	void Update ()
	{
	
	}

	public void PlayEffect()
	{
		//Debug.Log("Particles!");
		GameObject particle = Instantiate(particleEffect, transform.position, Quaternion.identity) as GameObject;
		GameObject particle1 = Instantiate(SoulParticle1, new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), Quaternion.identity) as GameObject;
		GameObject particle2 = Instantiate(SoulParticle2, new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), Quaternion.identity) as GameObject;
	}
}
