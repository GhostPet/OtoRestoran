using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameplayUIActionType {
	None = 0,
	OpenShop = 1,
	ToggleBuildMode = 2,
	OpenDocumentation = 3,
	ToggleRestaurantMode = 4
}

[Serializable]
public class GameplayActionBinding {
	[SerializeField] private GameplayUIActionType actionType;
	[SerializeField] private Button button;
	[SerializeField] private TMP_Text labelText;
	[SerializeField] private string preparationLabel;
	[SerializeField] private string serviceLabel;
	[SerializeField] private bool visibleInPreparation = true;
	[SerializeField] private bool visibleInService = true;
	[SerializeField] private bool interactableInPreparation = true;
	[SerializeField] private bool interactableInService = true;

	public GameplayUIActionType ActionType => actionType;
	public Button Button => button;
	public TMP_Text LabelText => labelText;
	public string PreparationLabel => preparationLabel;
	public string ServiceLabel => serviceLabel;
	public bool VisibleInPreparation => visibleInPreparation;
	public bool VisibleInService => visibleInService;
	public bool InteractableInPreparation => interactableInPreparation;
	public bool InteractableInService => interactableInService;
}

public class GameplayActionPanelUI : MonoBehaviour {
	[SerializeField] private RectTransform panelRect;
	[SerializeField] private TMP_Text headerText;
	[SerializeField] private List<GameplayActionBinding> actions = new List<GameplayActionBinding>();
	[SerializeField] private Button collapseButton;
	[SerializeField] private TMP_Text collapseButtonText;
	[SerializeField] private GameObject expandedContentRoot;
	[SerializeField] private float expandedWidth = 340f;
	[SerializeField] private float collapsedWidth = 72f;
	[SerializeField] private bool startCollapsed;
	[SerializeField] private Button codingToggleButton;
	[SerializeField] private TMP_Text codingToggleButtonText;
	[SerializeField] private GameObject codingContentRoot;
	[SerializeField] private Transform robotItemContainer;
	[SerializeField] private CodingRobotActionItemUI robotItemPrefab;
	[SerializeField] private RobotCodeRegistry robotCodeRegistry;
	[SerializeField] private RobotSpawnManager robotSpawnManager;
	[SerializeField] private CodeEditorWindowUI codeEditorWindowUI;
	[SerializeField] private RestaurantOperationsController operationsController;

	private readonly List<CodingRobotActionItemUI> spawnedRobotItems = new List<CodingRobotActionItemUI>();
	private readonly HashSet<string> expandedRobotKeys = new HashSet<string>(StringComparer.Ordinal);
	private RestaurantOperationMode currentMode = RestaurantOperationMode.Closed;
	private bool collapsed;
	private bool codingSectionExpanded = true;

	public event Action<GameplayUIActionType> ActionInvoked;
	public event Action<bool, float> CollapseChanged;

	public float CurrentWidth => collapsed ? collapsedWidth : expandedWidth;
	public bool IsCollapsed => collapsed;

	private void Awake() {
		if (panelRect == null) {
			panelRect = GetComponent<RectTransform>();
		}

		collapsed = startCollapsed;
		RegisterButtons();
		ApplyCollapsedState(true);
	}

	private void OnEnable() {
		RegisterButtons();
		Subscribe();
		Refresh(operationsController != null ? operationsController.CurrentMode : currentMode);
	}

	private void OnDisable() {
		Unsubscribe();
	}

	public void SetReferences(RobotCodeRegistry codeRegistry, RobotSpawnManager spawnManager, CodeEditorWindowUI codeWindowManager, RestaurantOperationsController operations) {
		Unsubscribe();

		robotCodeRegistry = codeRegistry;
		robotSpawnManager = spawnManager;
		codeEditorWindowUI = codeWindowManager;
		operationsController = operations;

		Subscribe();
		Refresh(currentMode);
	}

	public void Refresh(RestaurantOperationMode mode) {
		currentMode = mode;
		bool isService = mode == RestaurantOperationMode.Open;

		if (headerText != null) {
			headerText.text = isService ? "Servis İşlemleri" : "Hazırlık İşlemleri";
		}

		RefreshMainActions(isService);
		RefreshCodingSection();
		ApplyCollapsedState(false);
	}

	public void ToggleRobotExpansion(RobotSpawnPoint spawnPoint) {
		if (spawnPoint == null) {
			return;
		}

		if (expandedRobotKeys.Contains(spawnPoint.ProgramBindingKey)) {
			expandedRobotKeys.Remove(spawnPoint.ProgramBindingKey);
		} else {
			expandedRobotKeys.Add(spawnPoint.ProgramBindingKey);
		}

		RebuildRobotList();
	}

	public void CreateCode(RobotSpawnPoint spawnPoint) {
		if (codeEditorWindowUI == null || spawnPoint == null) {
			return;
		}

		codeEditorWindowUI.CreateCode(spawnPoint);
		expandedRobotKeys.Add(spawnPoint.ProgramBindingKey);
		RebuildRobotList();
	}

	public void OpenCode(RobotSpawnPoint spawnPoint, string codeId) {
		if (codeEditorWindowUI == null || spawnPoint == null || string.IsNullOrWhiteSpace(codeId)) {
			return;
		}

		codeEditorWindowUI.OpenCodeWindow(spawnPoint, codeId);
	}

	public void RunCode(RobotSpawnPoint spawnPoint, string codeId) {
		if (codeEditorWindowUI == null || spawnPoint == null || string.IsNullOrWhiteSpace(codeId)) {
			return;
		}

		codeEditorWindowUI.RunCode(spawnPoint, codeId);
		RebuildRobotList();
	}

	public void DeleteCode(RobotSpawnPoint spawnPoint, string codeId) {
		if (codeEditorWindowUI == null || string.IsNullOrWhiteSpace(codeId)) {
			return;
		}

		codeEditorWindowUI.DeleteCode(codeId);
		if (spawnPoint != null) {
			expandedRobotKeys.Add(spawnPoint.ProgramBindingKey);
		}
		RebuildRobotList();
	}

	private void RegisterButtons() {
		for (int i = 0; i < actions.Count; i++) {
			GameplayActionBinding binding = actions[i];
			if (binding == null || binding.Button == null) {
				continue;
			}

			binding.Button.onClick.RemoveAllListeners();
			GameplayUIActionType action = binding.ActionType;
			binding.Button.onClick.AddListener(() => RaiseAction(action));
		}

		if (collapseButton != null) {
			collapseButton.onClick.RemoveListener(HandleCollapseClicked);
			collapseButton.onClick.AddListener(HandleCollapseClicked);
		}

		if (codingToggleButton != null) {
			codingToggleButton.onClick.RemoveListener(HandleCodingToggleClicked);
			codingToggleButton.onClick.AddListener(HandleCodingToggleClicked);
		}
	}

	private void Subscribe() {
		if (!isActiveAndEnabled) {
			return;
		}

		if (robotCodeRegistry != null) {
			robotCodeRegistry.CodesChanged -= HandleCodesChanged;
			robotCodeRegistry.CodesChanged += HandleCodesChanged;
		}

		if (robotSpawnManager != null) {
			robotSpawnManager.SpawnedRobotsChanged -= HandleSpawnedRobotsChanged;
			robotSpawnManager.SpawnedRobotsChanged += HandleSpawnedRobotsChanged;
		}

		if (operationsController != null) {
			operationsController.ModeChanged -= HandleModeChanged;
			operationsController.ModeChanged += HandleModeChanged;
		}
	}

	private void Unsubscribe() {
		if (robotCodeRegistry != null) {
			robotCodeRegistry.CodesChanged -= HandleCodesChanged;
		}

		if (robotSpawnManager != null) {
			robotSpawnManager.SpawnedRobotsChanged -= HandleSpawnedRobotsChanged;
		}

		if (operationsController != null) {
			operationsController.ModeChanged -= HandleModeChanged;
		}
	}

	private void RefreshMainActions(bool isService) {
		for (int i = 0; i < actions.Count; i++) {
			GameplayActionBinding binding = actions[i];
			if (binding == null || binding.Button == null) {
				continue;
			}

			bool shouldShow = isService ? binding.VisibleInService : binding.VisibleInPreparation;
			bool shouldEnable = isService ? binding.InteractableInService : binding.InteractableInPreparation;
			binding.Button.gameObject.SetActive(shouldShow);
			binding.Button.interactable = shouldEnable;

			if (binding.LabelText != null) {
				string label = ResolveActionLabel(binding, isService);
				binding.LabelText.text = string.IsNullOrWhiteSpace(label) ? binding.ActionType.ToString() : label;
			}
		}
	}

	private string ResolveActionLabel(GameplayActionBinding binding, bool isService) {
		if (binding == null) {
			return string.Empty;
		}

		string label = isService ? binding.ServiceLabel : binding.PreparationLabel;
		if (!string.IsNullOrWhiteSpace(label)) {
			return label;
		}

		switch (binding.ActionType) {
			case GameplayUIActionType.OpenShop:
				return "Mağazayı Aç";
			case GameplayUIActionType.ToggleBuildMode:
				return "Restoranı Düzenle";
			case GameplayUIActionType.OpenDocumentation:
				return "Dokümantasyonu Aç";
			case GameplayUIActionType.ToggleRestaurantMode:
				return isService ? "Servisi Sonlandır" : "Restoranı Aç";
			default:
				return binding.ActionType.ToString();
		}
	}

	private void RefreshCodingSection() {
		if (codingToggleButton != null) {
			codingToggleButton.gameObject.SetActive(!collapsed);
		}

		if (codingToggleButtonText != null) {
			codingToggleButtonText.text = "Robotlar";
		}

		IReadOnlyList<RobotSpawnPoint> spawnPoints = RobotSpawnPoint.AllSpawnPoints;
		bool hasRobots = spawnPoints != null && spawnPoints.Count > 0;
		if (hasRobots) {
			codingSectionExpanded = true;
			for (int i = 0; i < spawnPoints.Count; i++) {
				RobotSpawnPoint spawnPoint = spawnPoints[i];
				if (spawnPoint == null) {
					continue;
				}

				expandedRobotKeys.Add(spawnPoint.ProgramBindingKey);
			}
		}

		if (codingContentRoot != null) {
			codingContentRoot.SetActive(!collapsed && codingSectionExpanded && hasRobots);
		}

		RebuildRobotList();
	}

	private void RebuildRobotList() {
		ClearRobotItems();
		if (robotItemContainer == null || robotItemPrefab == null || codingContentRoot == null || !codingContentRoot.activeSelf) {
			return;
		}

		IReadOnlyList<RobotSpawnPoint> spawnPoints = RobotSpawnPoint.AllSpawnPoints;
		bool canRun = operationsController != null && operationsController.IsRestaurantOpen;
		for (int i = 0; i < spawnPoints.Count; i++) {
			RobotSpawnPoint spawnPoint = spawnPoints[i];
			if (spawnPoint == null) {
				continue;
			}

			CodingRobotActionItemUI item = Instantiate(robotItemPrefab, robotItemContainer);
			List<RobotCodeEntry> codes = robotCodeRegistry != null ? robotCodeRegistry.GetCodesForRobot(spawnPoint.ProgramBindingKey) : new List<RobotCodeEntry>();
			item.Bind(this, spawnPoint, codes, expandedRobotKeys.Contains(spawnPoint.ProgramBindingKey), canRun);
			spawnedRobotItems.Add(item);
		}
	}

	private void HandleCollapseClicked() {
		collapsed = !collapsed;
		ApplyCollapsedState(false);
	}

	private void HandleCodingToggleClicked() {
		if (codingContentRoot == null) {
			return;
		}

		codingSectionExpanded = !codingSectionExpanded;
		bool hasRobots = RobotSpawnPoint.AllSpawnPoints.Count > 0;
		codingContentRoot.SetActive(!collapsed && codingSectionExpanded && hasRobots);
		if (codingContentRoot.activeSelf) {
			RebuildRobotList();
		}
	}

	private void ApplyCollapsedState(bool forceEvent) {
		if (panelRect != null) {
			Vector2 size = panelRect.sizeDelta;
			size.x = CurrentWidth;
			panelRect.sizeDelta = size;
		}

		if (expandedContentRoot != null) {
			expandedContentRoot.SetActive(!collapsed);
		}

		if (collapseButtonText != null) {
			collapseButtonText.text = collapsed ? "<" : ">";
		}

		if (collapsed && codingContentRoot != null) {
			codingContentRoot.SetActive(false);
		} else if (!collapsed && codingContentRoot != null) {
			bool hasRobots = RobotSpawnPoint.AllSpawnPoints.Count > 0;
			codingContentRoot.SetActive(codingSectionExpanded && hasRobots);
		}

		if (forceEvent || CollapseChanged != null) {
			CollapseChanged?.Invoke(collapsed, CurrentWidth);
		}
	}

	private void RaiseAction(GameplayUIActionType actionType) {
		ActionInvoked?.Invoke(actionType);
	}

	private void HandleCodesChanged() {
		RebuildRobotList();
	}

	private void HandleSpawnedRobotsChanged() {
		RefreshCodingSection();
		RebuildRobotList();
	}

	private void HandleModeChanged(RestaurantOperationMode mode) {
		Refresh(mode);
	}

	private void ClearRobotItems() {
		for (int i = 0; i < spawnedRobotItems.Count; i++) {
			CodingRobotActionItemUI item = spawnedRobotItems[i];
			if (item != null) {
				Destroy(item.gameObject);
			}
		}

		spawnedRobotItems.Clear();
	}
}
