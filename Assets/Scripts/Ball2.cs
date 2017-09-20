using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball2 : MonoBehaviour {

	Animator anim;
	Animator ragdollAnim;
	Animator placeHolderAnim;
	public Camera mainCamera;


	private float thrust;
	public GameObject ragdoll;
	public GameObject player;
	public GameObject placeHolderPrefab;
	GameObject placeHolder;
	public GameObject ball;
	GameObject positionHolder;
	Rigidbody ballrb;

	public Component[] phRigidbodies;
	public Component[] rdRigidbodies;
	public MeshRenderer[] renderers;


	Transform playerHips;
	Transform ragdollHips;
	Transform placeHolderHips;
	Transform lookPoint;

	Vector3 ragdollDirection;
	Vector3 deltaHipsPosition; 
	Vector3 oldHipsPosition = new Vector3 (0f, 0f, 0f);

	float speed = 0f;
	float turnSpeed = 60f;

	
	void Start () {
		ragdoll.SetActive (true);
		anim = player.GetComponent<Animator>();
		ragdollAnim = ragdoll.GetComponent<Animator>();

		rdRigidbodies = ragdoll.GetComponentsInChildren<Rigidbody>();
		renderers = player.GetComponentsInChildren<MeshRenderer>();

		playerHips = anim.GetBoneTransform (HumanBodyBones.Hips);
		ragdollHips = ragdollAnim.GetBoneTransform (HumanBodyBones.Hips);


		lookPoint = playerHips;

		thrust = 500f;
		ballrb = ball.GetComponent<Rigidbody>();
		ragdoll.SetActive (false);

	}

	//-------------------------------------------------------


	void Update(){


		Vector3 temp = player.transform.position;
		temp.y = 0;
		player.transform.position = temp;

		if (Input.GetKeyDown ("space")) {
			
			Vector2 newPosition = Random.insideUnitCircle * 8 + new Vector2(player.transform.position.x, player.transform.position.z);
			ballrb.transform.position = new Vector3(newPosition.x, 3, newPosition.y);
			ballrb.transform.LookAt (new Vector3(player.transform.position.x, 5, player.transform.position.z));
			ballrb.AddForce(ballrb.transform.forward * thrust, ForceMode.Impulse);

			
		}

		if (Input.GetKeyDown("up")) {
			anim.SetInteger ("WalkParam", 1);
			speed = 4;
		} 

		if (Input.GetKeyUp ("up")) {
			anim.SetInteger ("WalkParam", 0);
			speed = 0;

		} 	

		float turn = Input.GetAxis ("Horizontal");
		player.transform.Rotate (0, turn * turnSpeed * Time.deltaTime, 0);
		player.transform.Translate (0, -player.transform.position.y, speed * Time.deltaTime);

		Vector3 camDistance1 = lookPoint.position - mainCamera.transform.position;
		camDistance1.y = 0;
        mainCamera.transform.position += camDistance1 / 1000;
        mainCamera.transform.LookAt(lookPoint.position);

	}
	//-----------------------------------------------------------

	void PlayerUnvisible(){
		
		Collider coll = player.GetComponent<Collider> ();
		coll.enabled = false;


		foreach (Renderer rend in renderers) {
			rend.enabled = false;
		}

		
		lookPoint = ragdollHips;
	}

	//-----------------------------------------------------------

	void PlayerVisible(){
		
		Collider coll = player.GetComponent<Collider> ();
		coll.enabled = true;


		foreach (Renderer rend in renderers) {
			rend.enabled = true;
		}

		
		lookPoint = playerHips;
	}

	//------------------------------------------------------------

	void OnTriggerEnter(Collider other) {

		CopyPosition(player.transform, ragdoll.transform);
		ragdoll.SetActive(true);
		lookPoint = ragdollHips;
		PlayerUnvisible ();

		StartCoroutine ("CheckRagdollMotion");
	}

	//--------------------------------------------------------


	void CopyPosition(Transform reference, Transform ragdollPart)
	{
		ragdollPart.transform.position = reference.transform.position;
		ragdollPart.transform.rotation = reference.transform.rotation;

		for (int i = 0; i < reference.childCount; i++) 
		{
			Transform ref_t = reference.GetChild(i);
			Transform rag_t = ragdollPart.GetChild(i);

			if(ref_t != null && rag_t != null)
				CopyPosition(ref_t, rag_t);
		}
	}

	//-------------------------------------------------------------------



	void PlayerPositionToRagdoll(){

		player.SetActive(true);
		//anim.speed = 0;
		Vector3 rootToRagdoll = ragdollHips.position - playerHips.position;
		//rootToRagdoll.y = 0;
		Vector3 newRootPosition = player.transform.position + rootToRagdoll;
		//newRootPosition.y = 0;
		player.transform.position = newRootPosition;

		Vector3 hipsPlanePosition = ragdollHips.position;
		hipsPlanePosition.y = 0;
		Vector3 headPlanePosition = ragdollAnim.GetBoneTransform(HumanBodyBones.Head).position;
		headPlanePosition.y = 0;

		if (ragdollHips.forward.y > 0) {

			ragdollDirection = hipsPlanePosition - headPlanePosition;
			anim.SetInteger("WalkParam",2);

		} else {
			ragdollDirection = headPlanePosition - hipsPlanePosition;
			anim.SetInteger("WalkParam",3);

		}
		anim.speed = 0;
		Vector3 playerDirection = player.transform.forward;
		player.transform.rotation*=Quaternion.FromToRotation(playerDirection.normalized, ragdollDirection.normalized);
	}


	//---------------------------------------------------------------------------

	void LerpPosition(Transform reference, Transform ragdollPart, Transform origin, float f= 0.1f)
	{

		ragdollPart.transform.rotation = Quaternion.Lerp(origin.transform.rotation, reference.transform.rotation, f);
		ragdollPart.transform.position = Vector3.Lerp(origin.transform.position, reference.transform.position, f);


		for (int i = 0; i < reference.childCount; i++) 
		{
			Transform ref_t = reference.GetChild(i);
			Transform rag_t = ragdollPart.GetChild(i);
			Transform ori_t = origin.GetChild(i);

			if (ref_t != null && rag_t != null && ori_t != null)
				LerpPosition (ref_t, rag_t, ori_t, f);

		}

	}

	//--------------------------------------------------------------------------------

	IEnumerator CheckRagdollMotion(){
		for(int f = 1000; f > 0; f--){

			deltaHipsPosition = oldHipsPosition - ragdollHips.position;
			oldHipsPosition = ragdollHips.position;

			if (Mathf.Round(deltaHipsPosition.x*100) == 0 && Mathf.Round(deltaHipsPosition.y*100) == 0 && Mathf.Round(deltaHipsPosition.z*100) == 0) {
				RagdollBlendState ();
				PlayerPositionToRagdoll();
				StartCoroutine ("FadeToPosition");
				StopCoroutine ("CheckRagdollMotion");
			}

			yield return new WaitForSeconds (0.1f);
		}
	}
	//--------------------------------------------------------------


	IEnumerator FadeToPosition(){
		
		for(float f = 0f; f < 1f; f += 0.05f){

			LerpPosition (player.transform, ragdoll.transform, placeHolder.transform, f);

			//yield return new WaitForSeconds (0.5f);
			yield return Time.deltaTime;
		}

		foreach(Rigidbody rb in rdRigidbodies){
			rb.isKinematic = false;
		}
		Destroy (placeHolder);
		PlayerVisible ();
		ragdoll.SetActive (false);
		anim.speed = 1;
	}
	//-------------------------------------------------------------

	void RagdollBlendState(){
		foreach(Rigidbody rb in rdRigidbodies){
			rb.isKinematic = true;
		}

		
		placeHolder = Instantiate (placeHolderPrefab);
		placeHolderAnim = placeHolder.GetComponent<Animator>();
		placeHolderHips = placeHolderAnim.GetBoneTransform (HumanBodyBones.Hips);
		CopyPosition (ragdoll.transform, placeHolder.transform);
		placeHolder.SetActive (false);

		
	}


}




