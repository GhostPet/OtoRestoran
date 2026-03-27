using UnityEngine;

[RequireComponent(typeof(Customer))]
[RequireComponent(typeof(Animator))]
public class CustomerAnimationSimple : MonoBehaviour
{
    private Animator animator;
    private Customer customer;

    void Awake()
    {
        animator = GetComponent<Animator>();
        customer = GetComponent<Customer>();
    }

    void Update()
    {
        // Yürüyor mu?
        animator.SetBool("IsMoving", customer.IsMoving);

        // Oturmalı mı?
        bool isSeating = customer.IsSeating && !customer.IsMoving;
        animator.SetBool("IsSeating", isSeating);
    }
}