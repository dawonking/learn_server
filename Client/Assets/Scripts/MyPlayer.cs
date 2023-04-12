using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyPlayer : Player
{
	NetworkManager _network;
	C_Move movePacket;

	float horizontalInput;
	float verticalInput;
	Rigidbody rb;
	Transform orientation;
	Vector3 moveDirection;

	private void OnEnable()
    {
		rb = GetComponent<Rigidbody>();
		rb.freezeRotation = true;
		orientation = this.gameObject.transform;
    }

    void Start()
    {	
		movePacket = new C_Move();
		_network = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
		//StartCoroutine("CoSendPacket");
	}

    private void FixedUpdate()
    {
		horizontalInput = Input.GetAxisRaw("Horizontal");
		verticalInput = Input.GetAxisRaw("Vertical");
		moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
		rb.AddForce(new Vector3(horizontalInput, 0, verticalInput), ForceMode.VelocityChange);
	}


	void Update()
    {
		
		movePacket.posX = transform.position.x;
		movePacket.posY = transform.position.y;
		movePacket.posZ = transform.position.z;

		_network.Send(movePacket.Write());

		

	}




	IEnumerator CoSendPacket()
	{
		while (true)
		{
			yield return new WaitForSeconds(0.25f);

			//C_Move movePacket = new C_Move();
			//movePacket.posX = UnityEngine.Random.Range(-50, 50);
			//movePacket.posY = 0;
			//movePacket.posZ = UnityEngine.Random.Range(-50, 50);

			movePacket.posX = transform.position.x;
			movePacket.posY = transform.position.y;
			movePacket.posZ = transform.position.z;

			_network.Send(movePacket.Write());
		}
	}
}
