using System;
using Unity.AI.Navigation;
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
	[SerializeField] private NavMeshSurface navigationSurface;

	public RestaurantOperationMode CurrentMode { get; private set; } = RestaurantOperationMode.Closed;
	public bool IsRestaurantOpen => CurrentMode == RestaurantOperationMode.Open;

	public event Action<RestaurantOperationMode> ModeChanged;

	private void Awake() {
		ResolveReferences();
	}

	private void OnEnable() {
		Subscribe();
	}

	private void OnDisable() {
		Unsubscribe();
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

	private void Subscribe() {
		ResolveReferences();
		if (!isActiveAndEnabled) {
			return;
		}

		if (phaseStateMachine != null) {
			phaseStateMachine.PhaseChanged -= HandlePhaseChanged;
			phaseStateMachine.PhaseChanged += HandlePhaseChanged;
		}
	}

	private void Unsubscribe() {
		if (phaseStateMachine != null) {
			phaseStateMachine.PhaseChanged -= HandlePhaseChanged;
		}
	}

	private void ApplyMode(RestaurantOperationMode mode, bool force) {
		ApplyMode(mode, force, true);
	}

	private void ApplyMode(RestaurantOperationMode mode, bool force, bool syncPhaseState) {
		if (!force && CurrentMode == mode) return;

		ResolveReferences();
		CurrentMode = mode;
		bool isOpen = mode == RestaurantOperationMode.Open;

		if (placementController != null) {
			placementController.SetEditMode(false);
		}

		if (syncPhaseState && phaseStateMachine != null) {
			if (isOpen) phaseStateMachine.StartService();
			else phaseStateMachine.StartPreparation();
		}

		if (gameCodeRunner != null) {
			gameCodeRunner.SetExecutionEnabled(isOpen);
		}

		if (robotSpawnManager != null) {
			robotSpawnManager.SyncSpawnedRobots();
			robotSpawnManager.SetRobotsVisible(isOpen);
		}

		if (gameCodeRunner != null && robotSpawnManager != null) {
			gameCodeRunner.SetExecutors(robotSpawnManager.GetActiveExecutors());
		}

		if (customerSpawner != null) {
			customerSpawner.SetRestaurantOpen(isOpen);
		}

		RebuildNavigation();

		ModeChanged?.Invoke(CurrentMode);
		Debug.Log($"[RestaurantOperations] Mode -> {CurrentMode}");
	}

	public void RequestNavigationRebuild() {
		RebuildNavigation();
	}

	private void HandlePhaseChanged(PhaseStateMachine.Phase phase) {
		switch (phase) {
			case PhaseStateMachine.Phase.Preparation:
				ApplyMode(RestaurantOperationMode.Closed, false, false);
				break;
			case PhaseStateMachine.Phase.Service:
				ApplyMode(RestaurantOperationMode.Open, false, false);
				break;
			case PhaseStateMachine.Phase.DayEnd:
				if (CurrentMode != RestaurantOperationMode.Closed) {
					ApplyMode(RestaurantOperationMode.Closed, true, false);
				}
				break;
		}
	}

	private void RebuildNavigation() {
		if (navigationSurface == null) {
			navigationSurface = FindAnyObjectByType<NavMeshSurface>();
		}

		if (navigationSurface == null) return;

		// Directly invoke the editor-equivalent bake
		navigationSurface.BuildNavMesh();
	}

}
