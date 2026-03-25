using System;
using UnityEngine;

public class RestaurantOperationsController : MonoBehaviour {
	[Header("Initial Mode")]
	[SerializeField] private RestaurantOperationMode startingMode = RestaurantOperationMode.Closed;

	[Header("References")]
	[SerializeField] private PhaseStateMachine phaseStateMachine;
	[SerializeField] private PlacementController placementController;
	[SerializeField] private CustomerSpawner customerSpawner;
	[SerializeField] private GameCodeRunner gameCodeRunner;
	[SerializeField] private RobotSpawnManager robotSpawnManager;

	public RestaurantOperationMode CurrentMode { get; private set; } = RestaurantOperationMode.Closed;
	public bool IsRestaurantOpen => CurrentMode == RestaurantOperationMode.Open;

	public event Action<RestaurantOperationMode> ModeChanged;

	private void Awake() {
		ResolveReferences();
	}

	private void Start() {
		ApplyMode(startingMode, true);
	}

	public void OpenRestaurant() {
		ApplyMode(RestaurantOperationMode.Open, false);
	}

	public void CloseRestaurant() {
		ApplyMode(RestaurantOperationMode.Closed, false);
	}

	public void ToggleRestaurantMode() {
		ApplyMode(IsRestaurantOpen ? RestaurantOperationMode.Closed : RestaurantOperationMode.Open, false);
	}

	public void ToggleRestaurantModeFromButton() {
		ToggleRestaurantMode();
	}

	public void SetRestaurantOpen(bool isOpen) {
		if (isOpen) OpenRestaurant();
		else CloseRestaurant();
	}

	private void ResolveReferences() {
		if (phaseStateMachine == null) phaseStateMachine = FindAnyObjectByType<PhaseStateMachine>();
		if (placementController == null) placementController = FindAnyObjectByType<PlacementController>();
		if (customerSpawner == null) customerSpawner = FindAnyObjectByType<CustomerSpawner>();
		if (gameCodeRunner == null) gameCodeRunner = FindAnyObjectByType<GameCodeRunner>();
		if (robotSpawnManager == null) robotSpawnManager = FindAnyObjectByType<RobotSpawnManager>();
	}

	private void ApplyMode(RestaurantOperationMode mode, bool force) {
		if (!force && CurrentMode == mode) return;

		ResolveReferences();
		CurrentMode = mode;
		bool isOpen = mode == RestaurantOperationMode.Open;

		if (placementController != null) {
			placementController.SetEditMode(!isOpen);
		}

		if (phaseStateMachine != null) {
			if (isOpen) phaseStateMachine.StartService();
			else phaseStateMachine.StartPreparation();
		}

		if (gameCodeRunner != null) {
			gameCodeRunner.SetExecutionEnabled(isOpen);
		}

		if (robotSpawnManager != null) {
			if (isOpen) robotSpawnManager.SpawnRobots();
			else robotSpawnManager.DespawnRobots();
		}

		if (isOpen && gameCodeRunner != null && robotSpawnManager != null) {
			if (!HasActiveExecutors(gameCodeRunner.executors)) {
				gameCodeRunner.SetExecutors(robotSpawnManager.GetActiveExecutors());
			}
		}

		if (customerSpawner != null) {
			customerSpawner.SetRestaurantOpen(isOpen);
		}

		ModeChanged?.Invoke(CurrentMode);
		Debug.Log($"[RestaurantOperations] Mode -> {CurrentMode}");
	}

	private bool HasActiveExecutors(System.Collections.Generic.List<RobotExecutor> executors) {
		if (executors == null) return false;

		for (int i = 0; i < executors.Count; i++) {
			if (executors[i] != null) return true;
		}

		return false;
	}
}
