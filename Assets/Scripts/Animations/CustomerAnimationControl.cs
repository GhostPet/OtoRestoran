using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Customer))]
[RequireComponent(typeof(Animator))]
public class CustomerAnimationControl : MonoBehaviour {
	[Header("References")]
	[SerializeField] private Customer customer;
	[SerializeField] private Animator animator;

	[Header("Animator Parameters")]
	[SerializeField] private string seatingParameter = "IsSeating";
	[SerializeField] private string thinkingParameter = "IsThinking";
	[SerializeField] private string orderingParameter = "IsOrdering";
	[SerializeField] private string waitingParameter = "IsWaiting";
	[SerializeField] private string eatingParameter = "IsEating";
	[SerializeField] private string leavingParameter = "IsLeaving";
	[SerializeField] private string movingParameter = "IsMoving";
	[SerializeField] private string approachSeatParameter = "IsApproachingSeat";

	private readonly HashSet<string> availableParameters = new();

	private void Reset() {
		CacheComponents();
	}

	private void Awake() {
		CacheComponents();
		CacheAnimatorParameters();
	}

	private void OnValidate() {
		CacheComponents();
	}

	private void Update() {
		if (customer == null || animator == null) {
			return;
		}

		SetAnimatorBool(seatingParameter, customer.IsSeating);
		SetAnimatorBool(thinkingParameter, customer.IsThinking);
		SetAnimatorBool(orderingParameter, customer.IsOrdering);
		SetAnimatorBool(waitingParameter, customer.IsWaiting);
		SetAnimatorBool(eatingParameter, customer.IsEating);
		SetAnimatorBool(leavingParameter, customer.IsLeaving);
		SetAnimatorBool(movingParameter, customer.IsMoving);
		SetAnimatorBool(approachSeatParameter, customer.IsApproachingSeat);
	}

	private void CacheComponents() {
		if (customer == null) {
			customer = GetComponent<Customer>();
		}

		if (animator == null) {
			animator = GetComponent<Animator>();
		}
	}

	private void CacheAnimatorParameters() {
		availableParameters.Clear();
		if (animator == null) {
			return;
		}

		AnimatorControllerParameter[] parameters = animator.parameters;
		for (int i = 0; i < parameters.Length; i++) {
			availableParameters.Add(parameters[i].name);
		}
	}

	private void SetAnimatorBool(string parameterName, bool value) {
		if (string.IsNullOrWhiteSpace(parameterName)) {
			return;
		}

		if (!availableParameters.Contains(parameterName)) {
			return;
		}

		animator.SetBool(parameterName, value);
	}
}
