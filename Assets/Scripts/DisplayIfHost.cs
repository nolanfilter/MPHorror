using UnityEngine;
using System.Collections;

public class DisplayIfHost : MonoBehaviour {

	void Start ()
	{
		renderer.enabled = NetworkAgent.GetIsHost();
	}
}
