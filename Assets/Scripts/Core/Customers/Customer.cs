using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Customer : MonoBehaviour {
	[Header("Timings")]
	[SerializeField] private float thinkDuration = 2.0f;   // Oturduktan sonra düşünme
	[SerializeField] private float eatDuration = 6.0f;   // Yeme süresi

	[Header("Movement")]
	[SerializeField] private float moveSpeed = 2.5f;
	[SerializeField] private float rotateSpeed = 10f;
	[SerializeField] private float seatSnapDistance = 0.05f;
	[SerializeField] private float seatSnapTriggerDistance = 0.75f;
	[SerializeField] private List<OrderItem> defaultOrderItems = new();

	private NavMeshAgent agent;
	private ChairBehavior chair;
	private TableBehavior table;
	private Transform spawnPoint;

	private CustomerState state = CustomerState.Seating;
	private float stateTimer;

	private Vector3 moveTarget;
	private bool hasMoveTarget;

	// Dış sistemler için flag’ler
	private bool orderReady;   // Thinking süresi doldu, robot sipariş alabilir
	private bool orderTaken;   // Robot siparişi aldı
	private bool orderServed;  // Robot siparişi teslim etti
	private Order currentOrder;
	private bool isCleaningUp;

	public CustomerState State => state;
	public TableBehavior Table => table;
	public bool IsOrderReady => orderReady;
	public bool IsOrderTaken => orderTaken;
	public bool IsOrderServed => orderServed;
	public Order CurrentOrder => currentOrder;
	public bool IsSeating => state == CustomerState.Seating;
	public bool IsThinking => state == CustomerState.Thinking;
	public bool IsOrdering => state == CustomerState.Ordering;
	public bool IsWaiting => state == CustomerState.Waiting;
	public bool IsEating => state == CustomerState.Eating;
	public bool IsLeaving => state == CustomerState.Leaving;
	public bool IsMoving => hasMoveTarget;
	public bool IsApproachingSeat => state == CustomerState.Seating && hasMoveTarget && IsCloseEnoughToSeat();

	private void OnValidate() {
		if (defaultOrderItems == null) {
			return;
		}

		for (int i = 0; i < defaultOrderItems.Count; i++) {
			OrderItem orderItem = defaultOrderItems[i];
			if (orderItem == null) {
				continue;
			}

			orderItem.ClampQuantity();
		}
	}

	private void Awake() {
		agent = GetComponent<NavMeshAgent>();
		if (agent == null)
			agent = gameObject.AddComponent<NavMeshAgent>();

		agent.speed = moveSpeed;
		agent.angularSpeed = Mathf.Max(120f, rotateSpeed * 36f);
		agent.stoppingDistance = seatSnapDistance;
		agent.updateRotation = true;
		agent.updatePosition = true;
	}

	private void Update() {
		TickMovement();
		TickState();
	}

	public void SetSpawnPoint(Transform point) {
		spawnPoint = point;

		if (spawnPoint != null) {
			WarpTo(spawnPoint.position, spawnPoint.rotation);
		}
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

		if (spawnPoint != null)
			SetMoveTarget(spawnPoint.position);
		else
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

		Order nextOrder = new Order {
			Customer = this
		};

		if (!TryPopulateOrder(nextOrder)) {
			Debug.LogWarning($"[Customer] {name} için geçerli ve sipariş verilebilir item bulunamadı.");
			return null;
		}

		currentOrder = nextOrder;

		orderTaken = true;
		ActiveOrders.Remember(currentOrder);
		EnterWaiting();

		Debug.Log($"[Customer] {name} order taken by robot.");
		return currentOrder;
	}

	// Robot siparişi teslim ettiğinde çağır
	public bool TryServeOrder(RobotInventory inventory) {
		if (state != CustomerState.Waiting)
			return false;

		if (currentOrder == null || !currentOrder.TryConsumeFrom(inventory)) {
			return false;
		}

		CompleteOrder();
		return true;
	}

	public bool TryServeOrderItem(ItemSO item, int quantity, RobotInventory inventory) {
		if (state != CustomerState.Waiting || item == null || quantity <= 0 || inventory == null || currentOrder == null) {
			return false;
		}

		if (currentOrder.GetRemainingQuantity(item) < quantity) {
			return false;
		}

		if (!inventory.TryRemoveItem(item, quantity)) {
			return false;
		}

		if (!currentOrder.TryConsumeItem(item, quantity)) {
			inventory.TryAddItem(item, quantity);
			return false;
		}

		if (currentOrder.IsCompleted) {
			CompleteOrder();
		} else {
			Debug.Log($"[Customer] {name} partial order served: {item.DisplayName} x{quantity}");
		}

		return true;
	}

	public bool TryServeOrder() {
		return TryServeOrder(null);
	}

	public void DespawnImmediately() {
		CleanupAndDestroy();
	}


	private void SetMoveTarget(Vector3 worldPos) {
		moveTarget = worldPos;
		hasMoveTarget = true;

		if (CanUseNavMeshAgent()) {
			agent.stoppingDistance = seatSnapDistance;
			agent.SetDestination(worldPos);
		}
	}

	private void TickMovement() {
		if (!hasMoveTarget)
			return;

		if (CanUseNavMeshAgent()) {
			if (HasReachedDestination()) {
				hasMoveTarget = false;
				agent.ResetPath();
			}
			return;
		}

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
		if (chair != null) {
			WarpTo(chair.transform.position, chair.transform.rotation, true);
		}
	}

	private bool CanUseNavMeshAgent() {
		return agent != null && agent.enabled && agent.isOnNavMesh;
	}

	private bool HasReachedDestination() {
		if (state == CustomerState.Seating && IsCloseEnoughToSeat())
			return true;

		if (!CanUseNavMeshAgent())
			return false;

		if (agent.pathPending)
			return false;

		if (agent.remainingDistance > Mathf.Max(agent.stoppingDistance, seatSnapDistance))
			return false;

		if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f)
			return false;

		return true;
	}

	private bool IsCloseEnoughToSeat() {
		if (chair == null)
			return false;

		Vector3 offset = chair.transform.position - transform.position;
		offset.y = 0f;
		return offset.sqrMagnitude <= seatSnapTriggerDistance * seatSnapTriggerDistance;
	}

	private void WarpTo(Vector3 position, Quaternion rotation, bool disableAgentAfterWarp = false) {
		if (CanUseNavMeshAgent()) {
			if (disableAgentAfterWarp) {
				agent.ResetPath();
				agent.enabled = false;
			} else {
				agent.Warp(position);
				agent.ResetPath();
				agent.nextPosition = position;
			}
		}

		transform.SetPositionAndRotation(position, rotation);
	}


	private void MakeTableDirty() {
		if (table != null) {
			table.SetDirty(true);
			Debug.Log($"[Customer] {name} dirtied table {table.name}");
		}
	}

	private bool TryPopulateOrder(Order order) {
		if (order == null || defaultOrderItems == null) {
			return false;
		}

		for (int i = 0; i < defaultOrderItems.Count; i++) {
			OrderItem orderItem = defaultOrderItems[i];
			if (orderItem == null || !orderItem.IsValid) {
				continue;
			}

			if (!orderItem.Item.Orderable) {
				Debug.LogWarning($"[Customer] {name} için orderable olmayan item siparişe eklenemedi: {orderItem.Item.name}");
				continue;
			}

			order.AddItem(orderItem);
		}

		return order.Items != null && order.Items.Count > 0;
	}

	private void CompleteOrder() {
		orderServed = true;
		if (currentOrder != null) {
			ActiveOrders.Remove(currentOrder);
		}
		Debug.Log($"[Customer] {name} order served.");
	}

	private void CleanupAndDestroy() {
		if (isCleaningUp) {
			return;
		}

		isCleaningUp = true;

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
