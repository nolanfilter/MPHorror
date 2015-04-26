using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour {

	public DoorAgent.RoomType roomType;

	private int uniqueID = -1;

	void Awake()
	{
		uniqueID = DoorAgent.RegisterDoor( this );
	}

	void OnEnable()
	{
		DoorAgent.RandomizeDoorConnections();
	}

	public int getUniqueID()
	{
		return uniqueID;
	}
}
