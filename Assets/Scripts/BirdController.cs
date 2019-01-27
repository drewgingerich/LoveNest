using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class BirdController : MonoBehaviour {

	bool animating;
	bool foraging = false;
	bool grounded;
	float speed = 1;
	NestItem chosenBit;
	[SerializeField]
	Transform forageScreenEntryPoint;
	[SerializeField]
	float forageEntraceTime;
	[SerializeField]
	float forageEntranceSpeed;
	[SerializeField]
	Transform nestScreenEntryPoint;

	// Animator parameter string keys
	const string stoppedBool = "Stopped";
	const string groundedBool = "Grounded";
	const string pickUpTrigger = "PickUp";
	const float pickUpAnimationTime = 0.2f;


	[System.NonSerialized]
	public static BirdController activeController;

	[SerializeField]
	HotBird hotBird;

	[SerializeField]
	float airSpeed = 5;
	[SerializeField]
	float groundSpeed = 2;
	[SerializeField]
	float minPickupDistance = 0.2f;

	[SerializeField]
	Transform beak;
	[SerializeField]
	Animator anim;

	[SerializeField]
	ForestFloor stage;
	[SerializeField]
	Camera forageCam;
	[SerializeField]
	CameraBounds forageCamBounds;
	[SerializeField]
	Camera nestCam;
	[SerializeField]
	CameraBounds nestCamBounds;

	CameraBounds activeCameraBounds;

	void Awake() {
		activeController = this;
	}

	void Start() {
		Fly();
		Nest();
	}

	public void InputMove(Vector2 direction) {
		if (animating) {
			return;
		}
		Move(direction);
	}

	public void Move(Vector2 direction) {
		if (direction == Vector2.zero) {
			anim.SetBool(stoppedBool, true);
			return;
		} else {
			anim.SetBool(stoppedBool, false);
		}

		Vector3 move = (Vector3)direction * Time.deltaTime;
		Vector3 newPosition = transform.position + move * speed;

		Bounds currentBounds = activeCameraBounds.GetBoundsWorldSpace();
		newPosition.x = Mathf.Clamp(newPosition.x, currentBounds.min.x, currentBounds.max.x);

		transform.position = newPosition;

		if (foraging) {
			HandleForaging();
		} else {
			HandleNesting();
		}
	}

	void HandleForaging() {
		Vector3 position = transform.position;

		if (position.y <= stage.groundLevel) {
			position.y = stage.groundLevel;
			Land();
		} else {
			Fly();
		}

		ConstrainToCameraHorizontal();

		if (position.y > activeCameraBounds.GetBoundsWorldSpace().max.y) {
			Nest();
		}
	}

	void Fly() {
		speed = airSpeed;
		grounded = false;
		anim.SetBool(groundedBool, false);
	}

	void Land() {
		speed = groundSpeed;
		grounded = true;
		anim.SetBool(groundedBool, true);
	}

	void ConstrainToCameraHorizontal() {
		Bounds bounds = activeCameraBounds.GetBoundsWorldSpace();
		if (!bounds.Contains(transform.position)) {
			Vector3 position = bounds.ClosestPoint(transform.position);
			position.z = 0;
			transform.position = position;
		}

	}

	void HandleNesting(){
		Bounds bounds = activeCameraBounds.GetBoundsWorldSpace();
		if (transform.position.y < bounds.min.y) {
			Forage();
			return;
		}
		ConstrainToCameraHorizontal();
	}

	void Forage() {
		nestCam.gameObject.SetActive(false);
		forageCam.gameObject.SetActive(true);

		transform.position = forageScreenEntryPoint.position;
		Vector3 forageCamPosition = forageCam.transform.position;
		forageCamPosition.y = transform.position.y;

		activeCameraBounds = forageCamBounds;
		StartCoroutine(ForageEntranceRoutine());
		foraging = true;
	}

	IEnumerator ForageEntranceRoutine() {
		animating = true;
		float timer = 0;
		while (timer < forageEntraceTime) {
			transform.position += Vector3.down * forageEntranceSpeed * Time.deltaTime;
			timer += Time.deltaTime;
			yield return null;
		}
		animating = false;
	}

	void Nest() {
		forageCam.gameObject.SetActive(false);
		nestCam.gameObject.SetActive(true);

		forageScreenEntryPoint.position = transform.position;

		activeCameraBounds = nestCamBounds;

		transform.position = nestScreenEntryPoint.position;
		foraging = false;
	}

	public void Interact() {
		if (null != chosenBit) {
			DropItem();
		} else {
			float distance = minPickupDistance;
			NestItem selectedBit = null;
			foreach (NestItem item in NestItem.ActiveItems) {
				Vector2 diff = (transform.position - item.transform.position);
				float newDistance = diff.magnitude;
				if (newDistance <= distance) {
					selectedBit = item;
					distance = newDistance;
				}
			}
			if (selectedBit != null) {
				Pickup(selectedBit);
				return;
			}
		}

		if (IsNearHotBird()) {
			hotBird.Sing();
		}
	}

	public void DropItem() {
		if (null == chosenBit) {
			return;
		}
		chosenBit.isHeld = false;
		if (grounded) {
			chosenBit.transform.localPosition = Vector3.forward * 0.5f;
		} 
		chosenBit.transform.SetParent(null);
		chosenBit.Fall();
		chosenBit = null;
	}

	private void Pickup(NestItem cruft) {
		chosenBit = cruft;
		chosenBit.isHeld = true;
		chosenBit.transform.SetParent(beak);
		chosenBit.transform.localPosition = Vector3.back;
	}

	IEnumerator PickUpRoutine(NestItem cruft) {
		if (grounded) {
			animating = true;
			anim.SetTrigger(pickUpTrigger);
			yield return new WaitForSeconds(pickUpAnimationTime);
			animating = false;
		}
		Pickup(cruft);
	}

	private bool IsNearHotBird(){
		return Vector3.Distance(hotBird.transform.position, transform.position) < minPickupDistance;
	}

	//HotBird will sing until player moves away
	// private IEnumerator ListenToFullSong(){
	// 	while (IsNearHotBird()) {
	// 		song.volume = 1;
	// 		yield return null;
	// 	}
	// 	song.volume = 0;
	// }

	void OnDrawGizmosSelected() {
		UnityEditor.Handles.color = Color.green;
		UnityEditor.Handles.DrawWireDisc(transform.position, transform.forward, minPickupDistance);
	}

}