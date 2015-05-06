using UnityEngine;
using System.Collections;

public class PlayDestroyEffect : MonoBehaviour {

	public GameObject particleEffect;
	
	void Start ()
	{

	}

	void Update ()
	{
	
	}

	public void PlayEffect()
	{
		//Debug.Log("Particles!");
		GameObject particle = Instantiate(particleEffect, transform.position, Quaternion.identity) as GameObject;
		particle.GetComponent<ParticleSystem>().Play();
	}
}
