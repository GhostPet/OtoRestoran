using UnityEngine;
using System.Collections.Generic;

public class Customer : MonoBehaviour {
	[Header("Timings")]
	[SerializeField] private float thinkDuration = 2.0f;   // Oturduktan sonra düşünme
	[SerializeField] private float eatDuration = 6.0f;   // Yeme süresi

	[Header("Movement")]
	[SerializeField] private float moveSpeed = 2.5f;
	[SerializeField] private float rotateSpeed = 10f;
	[SerializeField] private float seatSnapDistance = 0.05f;
	[SerializeField] private List<string> defaultOrderItems = new() { "Meal" };

	private ChairBehavior chair;
	private TableBehavior table;

	private CustomerState state = CustomerState.Seating;
	private float stateTimer;

	private Vector3 moveTarget;
	private bool hasMoveTarget;

	// Dış sistemler için flag’ler
	private bool orderReady;   // Thinking süresi doldu, robot sipariş alabilir
	private bool orderTaken;   // Robot siparişi aldı
	private bool orderServed;  // Robot siparişi teslim etti
	private Order currentOrder;

	public CustomerState State => state;
	public TableBehavior Table => table;
	public bool IsOrderReady => orderReady;
	public bool IsOrderTaken => orderTaken;
	public bool IsOrderServed => orderServed;

	private void Update() {
		TickMovement();
		TickState();
	}


	// Spawner -> Assign'ten sonra çağırılmalı
	public void SetSeat(ChairBehavior chair) {
		this.chair = chair;
		this.table = chair != null ? chair.Table : null;

		state = CustomerState.Seating;
		if (chair != null)
			SetMoveTarget(chair.transform.position);
		Debug.Log($"[Customer] {name} spawned -> Seating");
	}


	private void TickState() {
		switch (state) {
			case CustomerState.Seating:
				// Koltuğa varınca Thinking'e geç
				if (!hasMoveTarget && chair != null) {
					SnapToSeat();
					EnterThinking();
				}
				break;

			case CustomerState.Thinking:
				// Önce düşünme süresi dolmalı
				if (!orderReady) {
					stateTimer -= Time.deltaTime;
					if (stateTimer <= 0f) {
						EnterOrdering();
					}
				}
				break;

			case CustomerState.Ordering:
				if (orderTaken) {
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
		currentOrder = null;
		Debug.Log($"[Customer] {name} -> Thinking");
	}

	private void EnterOrdering() {
		state = CustomerState.Ordering;
		orderReady = true;
		Debug.Log($"[Customer] {name} -> Ordering");
	}

	private void EnterWaiting() {
		state = CustomerState.Waiting;
		orderReady = false;
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
		return GetOrder() != null;
	}

	public Order GetOrder() {
		if (state != CustomerState.Ordering || !orderReady)
			return null;

		if (orderTaken && currentOrder != null)
			return currentOrder;

		currentOrder = new Order {
			Customer = this
		};

		if (defaultOrderItems != null && defaultOrderItems.Count > 0) {
			for (int i = 0; i < defaultOrderItems.Count; i++) {
				var item = defaultOrderItems[i];
				if (!string.IsNullOrWhiteSpace(item)) currentOrder.Items.Add(item);
			}
		}

		if (currentOrder.Items.Count == 0) {
			currentOrder.Items.Add("Meal");
		}

		orderTaken = true;
		ActiveOrders.Remember(currentOrder);
		EnterWaiting();

		Debug.Log($"[Customer] {name} order taken by robot.");
		return currentOrder;
	}

	// Robot siparişi teslim ettiğinde çağır
	public bool TryServeOrder() {
		if (state != CustomerState.Waiting)
			return false;

		orderServed = true;
		if (currentOrder != null) ActiveOrders.Remove(currentOrder);
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
		transform.position += moveSpeed * Time.deltaTime * dir;

		if (dir.sqrMagnitude > 0.0001f) {
			Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
		}
	}

	private void SnapToSeat() {
		if (chair != null)
			transform.SetPositionAndRotation(chair.transform.position, chair.transform.rotation);
	}


	private void MakeTableDirty() {
		if (table != null) {
			table.SetDirty(true);
			Debug.Log($"[Customer] {name} dirtied table {table.name}");
		}
	}

	private void CleanupAndDestroy() {
		if (currentOrder != null) ActiveOrders.Remove(currentOrder);
		else ActiveOrders.RemoveByCustomer(this);

		if (chair != null) {
			chair.Clear();
			chair = null;
			table = null;
		}

		Destroy(gameObject);
	}

}
