using UnityEngine;

public class GameplayUIRoot : MonoBehaviour {
	[Header("Systems")]
	[SerializeField] private EconomyManager economyManager;
	[SerializeField] private GameScoreManager gameScoreManager;
	[SerializeField] private RestaurantOperationsController operationsController;
	[SerializeField] private PhaseStateMachine phaseStateMachine;
	[SerializeField] private ShopManager shopManager;
	[SerializeField] private BuildInventoryManager buildInventoryManager;
	[SerializeField] private PlacementController placementController;
	[SerializeField] private GameCodeRunner gameCodeRunner;
	[SerializeField] private RobotSpawnManager robotSpawnManager;
	[SerializeField] private RobotCodeRegistry robotCodeRegistry;

	[Header("Window Area")]
	[SerializeField] private WindowAreaUI windowArea;
	[SerializeField] private ShopWindowUI shopWindowContentPrefab;
	[SerializeField] private DocumentationWindowUI documentationWindowContentPrefab;

	[Header("UI Controllers")]
	[SerializeField] private GameplayHudUI hudUI;
	[SerializeField] private GameplayActionPanelUI actionPanelUI;
	[SerializeField] private BuildPaletteUI buildPaletteUI;
	[SerializeField] private CodeEditorWindowUI codeEditorWindowUI;

	[Header("Responsive Layout")]
	[SerializeField] private RectTransform[] rightStretchTargets;
	[SerializeField] private float actionPanelExpandedInset = 360f;
	[SerializeField] private float actionPanelCollapsedInset = 92f;

	private ShopWindowUI activeShopWindow;
	private DocumentationWindowUI activeDocumentationWindow;

	private void Awake() {
		ResolveReferences();
		InitializeUi();
	}

	private void OnEnable() {
		Subscribe();
		RefreshUiState();
	}

	private void OnDisable() {
		Unsubscribe();
	}

	private void ResolveReferences() {
		if (economyManager == null) economyManager = FindAnyObjectByType<EconomyManager>();
		if (gameScoreManager == null) gameScoreManager = FindAnyObjectByType<GameScoreManager>();
		if (operationsController == null) operationsController = FindAnyObjectByType<RestaurantOperationsController>();
		if (phaseStateMachine == null) phaseStateMachine = FindAnyObjectByType<PhaseStateMachine>();
		if (shopManager == null) shopManager = FindAnyObjectByType<ShopManager>();
		if (buildInventoryManager == null) buildInventoryManager = FindAnyObjectByType<BuildInventoryManager>();
		if (placementController == null) placementController = FindAnyObjectByType<PlacementController>();
		if (gameCodeRunner == null) gameCodeRunner = FindAnyObjectByType<GameCodeRunner>();
		if (robotSpawnManager == null) robotSpawnManager = FindAnyObjectByType<RobotSpawnManager>();
		if (robotCodeRegistry == null) robotCodeRegistry = FindAnyObjectByType<RobotCodeRegistry>();
		if (windowArea == null) windowArea = FindAnyObjectByType<WindowAreaUI>();
	}

	private void InitializeUi() {
		if (hudUI != null) {
         hudUI.SetReferences(economyManager, gameScoreManager, phaseStateMachine);
		}

		if (buildPaletteUI != null) {
			buildPaletteUI.SetReferences(buildInventoryManager, placementController);
           buildPaletteUI.SetVisible(false);
		}

		if (codeEditorWindowUI != null) {
			codeEditorWindowUI.SetReferences(gameCodeRunner, robotSpawnManager, operationsController, robotCodeRegistry, windowArea);
		}

		if (actionPanelUI != null) {
			actionPanelUI.SetReferences(robotCodeRegistry, robotSpawnManager, codeEditorWindowUI, operationsController);
		}
	}

	private void Subscribe() {
		if (actionPanelUI != null) {
			actionPanelUI.ActionInvoked -= HandleActionInvoked;
			actionPanelUI.ActionInvoked += HandleActionInvoked;
			actionPanelUI.CollapseChanged -= HandleActionPanelCollapseChanged;
			actionPanelUI.CollapseChanged += HandleActionPanelCollapseChanged;
		}

		if (operationsController != null) {
			operationsController.ModeChanged -= HandleModeChanged;
			operationsController.ModeChanged += HandleModeChanged;
		}

		if (phaseStateMachine != null) {
			phaseStateMachine.PhaseChanged -= HandlePhaseChanged;
			phaseStateMachine.PhaseChanged += HandlePhaseChanged;
		}

		if (buildInventoryManager != null) {
			buildInventoryManager.InventoryChanged -= HandleBuildInventoryChanged;
			buildInventoryManager.InventoryChanged += HandleBuildInventoryChanged;
		}
	}

	private void Unsubscribe() {
		if (actionPanelUI != null) {
			actionPanelUI.ActionInvoked -= HandleActionInvoked;
			actionPanelUI.CollapseChanged -= HandleActionPanelCollapseChanged;
		}

		if (operationsController != null) {
			operationsController.ModeChanged -= HandleModeChanged;
		}

		if (phaseStateMachine != null) {
			phaseStateMachine.PhaseChanged -= HandlePhaseChanged;
		}

		if (buildInventoryManager != null) {
			buildInventoryManager.InventoryChanged -= HandleBuildInventoryChanged;
		}
	}

	private void HandleActionInvoked(GameplayUIActionType actionType) {
		switch (actionType) {
			case GameplayUIActionType.OpenShop:
				OpenShopWindow();
				break;
			case GameplayUIActionType.ToggleBuildMode:
				ToggleBuildMode();
				break;
			case GameplayUIActionType.OpenDocumentation:
				OpenDocumentationWindow();
				break;
			case GameplayUIActionType.ToggleRestaurantMode:
				ToggleRestaurantMode();
				break;
		}
	}

	private void OpenShopWindow() {
		if (operationsController != null && operationsController.IsRestaurantOpen) {
			return;
		}

		if (windowArea == null || shopWindowContentPrefab == null) {
			return;
		}

		activeShopWindow = windowArea.OpenSingletonWindow("shop", "Shop", shopWindowContentPrefab);
		if (activeShopWindow != null) {
			activeShopWindow.SetReference(shopManager);
			activeShopWindow.RefreshAll();
		}
	}

	private void ToggleBuildMode() {
		if (operationsController != null && operationsController.IsRestaurantOpen) {
			return;
		}

		if (placementController == null) {
			return;
		}

		bool nextState = !placementController.editMode;
		placementController.SetEditMode(nextState);
		if (buildPaletteUI != null) {
			buildPaletteUI.SetVisible(nextState);
		}
	}

	private void OpenDocumentationWindow() {
		if (windowArea == null || documentationWindowContentPrefab == null) {
			return;
		}

		activeDocumentationWindow = windowArea.OpenSingletonWindow("documentation", "Dokümantasyon", documentationWindowContentPrefab);
	}

	private void ToggleRestaurantMode() {
		if (operationsController != null) {
			operationsController.ToggleRestaurantMode();
		}
	}

	private void HandleModeChanged(RestaurantOperationMode mode) {
		RefreshUiState();
	}

	private void HandlePhaseChanged(PhaseStateMachine.Phase phase) {
		RefreshUiState();
	}

	private void HandleBuildInventoryChanged() {
		if (buildPaletteUI != null && buildPaletteUI.gameObject.activeSelf) {
			buildPaletteUI.Refresh();
		}
	}

	private void HandleActionPanelCollapseChanged(bool isCollapsed, float currentWidth) {
		RefreshResponsiveLayout(isCollapsed);
	}

	private void RefreshUiState() {
		RestaurantOperationMode mode = operationsController != null ? operationsController.CurrentMode : RestaurantOperationMode.Closed;
		bool isService = operationsController != null && operationsController.IsRestaurantOpen;
		bool showBuildPalette = !isService && placementController != null && placementController.editMode;

		if (actionPanelUI != null) {
			actionPanelUI.Refresh(mode);
		}

		if (buildPaletteUI != null) {
			buildPaletteUI.SetVisible(showBuildPalette);
		}

		if (activeShopWindow != null && isService) {
			if (activeShopWindow.Window != null) {
				activeShopWindow.Window.CloseWindow();
			}

			activeShopWindow = null;
		}

		RefreshResponsiveLayout(actionPanelUI != null && actionPanelUI.IsCollapsed);
	}

	private void RefreshResponsiveLayout(bool isCollapsed) {
		float inset = isCollapsed ? actionPanelCollapsedInset : actionPanelExpandedInset;
		if (rightStretchTargets == null) {
			return;
		}

		for (int i = 0; i < rightStretchTargets.Length; i++) {
			RectTransform target = rightStretchTargets[i];
			if (target == null) {
				continue;
			}

			Vector2 offsetMax = target.offsetMax;
			offsetMax.x = -inset;
			target.offsetMax = offsetMax;
		}
	}
}
