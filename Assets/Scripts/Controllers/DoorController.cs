using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour {

	public DoorAgent.RoomType roomType;

	private int uniqueID = -1;

	void Awake()
	{
		uniqueID = DoorAgent.RegisterDoor( this );
	}

	public int getUniqueID()
	{
		return uniqueID;
	}
}
