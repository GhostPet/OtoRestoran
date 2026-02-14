using UnityEngine;

public class Customer : MonoBehaviour {
	[Header("Timings")]
	[SerializeField] private float thinkDuration = 2.0f;   // Oturduktan sonra düşünme
	[SerializeField] private float eatDuration = 6.0f;   // Yeme süresi

	[Header("Movement")]
	[SerializeField] private float moveSpeed = 2.5f;
	[SerializeField] private float rotateSpeed = 10f;
	[SerializeField] private float seatSnapDistance = 0.05f;

	private Seat seat;
	private TableLogic table;

	private CustomerState state = CustomerState.Seating;
	private float stateTimer;

	private Vector3 moveTarget;
	private bool hasMoveTarget;

	// Dış sistemler için flag’ler
	private bool orderReady;   // Thinking süresi doldu, robot sipariş alabilir
	private bool orderTaken;   // Robot siparişi aldı
	private bool orderServed;  // Robot siparişi teslim etti

	public CustomerState State => state;
	public bool IsOrderReady => orderReady;
	public bool IsOrderTaken => orderTaken;
	public bool IsOrderServed => orderServed;

	private void Update() {
		TickMovement();
		TickState();
	}


	// Spawner -> Assign'ten sonra çağırılmalı
	public void SetSeat(Seat seat) {
		this.seat = seat;
		this.table = seat != null ? seat.Table : null;

		state = CustomerState.Seating;
		SetMoveTarget(seat.transform.position);
		Debug.Log($"[Customer] {name} spawned -> Seating");
	}


	private void TickState() {
		switch (state) {
			case CustomerState.Seating:
				// Koltuğa varınca Thinking'e geç
				if (!hasMoveTarget && seat != null) {
					SnapToSeat();
					EnterThinking();
				}
				break;

			case CustomerState.Thinking:
				// Önce düşünme süresi dolmalı
				if (!orderReady) {
					stateTimer -= Time.deltaTime;
					if (stateTimer <= 0f) {
						orderReady = true;
						Debug.Log($"[Customer] {name} is ready to order.");
						// Robot bu flag'i okuyup sipariş alabilir
					}
				}
				// Sipariş alındı mı?
				else if (orderTaken) {
					EnterWaiting();
				}
				break;

			case CustomerState.Waiting:
				// Sipariş teslim edilene kadar bekler
				if (orderServed) {
					EnterEating();
				}
				break;

			case CustomerState.Eating:
				stateTimer -= Time.deltaTime;
				if (stateTimer <= 0f) {
					MakeTableDirty();
					EnterLeaving();
				}
				break;

			case CustomerState.Leaving:
				// Restorandan çıkış hedefe varınca yok edilir
				if (!hasMoveTarget) {
					CleanupAndDestroy();
				}
				break;
		}
	}

	private void EnterThinking() {
		state = CustomerState.Thinking;
		stateTimer = thinkDuration;
		orderReady = false;
		orderTaken = false;
		orderServed = false;
		Debug.Log($"[Customer] {name} -> Thinking");
	}

	private void EnterWaiting() {
		state = CustomerState.Waiting;
		Debug.Log($"[Customer] {name} -> Waiting (order taken)");
	}

	private void EnterEating() {
		state = CustomerState.Eating;
		stateTimer = eatDuration;
		Debug.Log($"[Customer] {name} -> Eating");
	}

	private void EnterLeaving() {
		state = CustomerState.Leaving;
		Debug.Log($"[Customer] {name} -> Leaving");

		// Kapı / çıkış noktası daha sonra spawn manager tarafından verilebilir
		SetMoveTarget(transform.position + transform.forward * 4f);
	}


	// Robot siparişi aldığında çağır
	public bool TryTakeOrder() {
		if (state != CustomerState.Thinking || !orderReady)
			return false;

		orderTaken = true;
		Debug.Log($"[Customer] {name} order taken by robot.");
		return true;
	}

	// Robot siparişi teslim ettiğinde çağır
	public bool TryServeOrder() {
		if (state != CustomerState.Waiting)
			return false;

		orderServed = true;
		Debug.Log($"[Customer] {name} order served.");
		return true;
	}


	private void SetMoveTarget(Vector3 worldPos) {
		moveTarget = worldPos;
		hasMoveTarget = true;
	}

	private void TickMovement() {
		if (!hasMoveTarget)
			return;

		Vector3 to = moveTarget - transform.position;
		to.y = 0f;

		float dist = to.magnitude;
		if (dist <= seatSnapDistance) {
			hasMoveTarget = false;
			return;
		}

		Vector3 dir = to.normalized;
		transform.position += dir * moveSpeed * Time.deltaTime;

		if (dir.sqrMagnitude > 0.0001f) {
			Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
		}
	}

	private void SnapToSeat() {
		transform.position = seat.transform.position;
		transform.rotation = seat.transform.rotation;
	}


	private void MakeTableDirty() {
		if (table != null) {
			table.SetDirty(true);
			Debug.Log($"[Customer] {name} dirtied table {table.name}");
		}
	}

	private void CleanupAndDestroy() {
		if (seat != null) {
			seat.Clear();
			seat = null;
			table = null;
		}

		Destroy(gameObject);
	}

}
