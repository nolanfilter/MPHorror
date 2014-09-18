using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public float speed;

	void Update()
	{
		Vector3 movement = Vector2.zero;

		if( Input.GetKey( KeyCode.RightArrow ) )
			movement += Vector3.right;

		if( Input.GetKey( KeyCode.LeftArrow ) )
			movement += Vector3.left;

		if( Input.GetKey( KeyCode.UpArrow ) )
			movement += Vector3.forward;

		if( Input.GetKey( KeyCode.DownArrow ) )
			movement += Vector3.back;

		movement = movement.normalized * speed * Time.deltaTime;

		transform.position += movement;
	}

	void OnTriggerEnter( Collider collider )
	{
		if( collider.tag == "Door" )
		{
			DoorController doorController = collider.GetComponent<DoorController>();

			Transform fromDoorTransform = null;

			if( doorController )
			{
				fromDoorTransform = DoorAgent.GetToDoorTransform( doorController.getUniqueID() );
			}

			if( fromDoorTransform )
			{
				transform.position = fromDoorTransform.position + fromDoorTransform.forward * 1.5f;
				transform.LookAt( transform.position + fromDoorTransform.forward, Vector3.up );
			}
		}
	}

	/*
	public float speed = 10f;
	
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;

	void Awake()
	{
		lastSynchronizationTime = Time.time;
	}

	void Update()
	{
		if( networkView.isMine )
		{
			InputMovement();
			InputColorChange();
		}
		else
		{
			SyncedMovement();
		}
	}

	private void InputMovement()
	{
		if( Input.GetKey( KeyCode.UpArrow ) )
			rigidbody.MovePosition( rigidbody.position + Vector3.forward * speed * Time.deltaTime );

		if( Input.GetKey( KeyCode.DownArrow ) )
			rigidbody.MovePosition( rigidbody.position - Vector3.forward * speed * Time.deltaTime );

		if( Input.GetKey( KeyCode.RightArrow ) )
			rigidbody.MovePosition( rigidbody.position + Vector3.right * speed * Time.deltaTime );

		if( Input.GetKey( KeyCode.LeftArrow ) )
			rigidbody.MovePosition( rigidbody.position - Vector3.right * speed * Time.deltaTime );
	}

	void OnSerializeNetworkView( BitStream stream, NetworkMessageInfo info )
	{
		Vector3 syncPosition = Vector3.zero;
		Vector3 syncVelocity = Vector3.zero;
		if (stream.isWriting)
		{
			syncPosition = rigidbody.position;
			stream.Serialize(ref syncPosition);
			
			syncPosition = rigidbody.velocity;
			stream.Serialize(ref syncVelocity);
		}
		else
		{
			stream.Serialize(ref syncPosition);
			stream.Serialize(ref syncVelocity);
			
			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;
			
			syncEndPosition = syncPosition + syncVelocity * syncDelay;
			syncStartPosition = rigidbody.position;
		}
	}

	private void SyncedMovement()
	{
		syncTime += Time.deltaTime;
		rigidbody.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
	}

	private void InputColorChange()
	{
		if( Input.GetKeyDown( KeyCode.C ) )
			ChangeColorTo( new Vector3( Random.value, Random.value, Random.value ) );
	}

	[RPC] void ChangeColorTo( Vector3 color )
	{
		renderer.material.color = new Color( color.x, color.y, color.z, 1f );

		if( networkView.isMine )
			networkView.RPC( "ChangeColorTo", RPCMode.Others, color );
	}
	*/
}
