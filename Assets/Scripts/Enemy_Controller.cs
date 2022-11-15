using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Controller : MonoBehaviour
{
    private string className;
    [Header("Debug Options")]
    [SerializeField] private bool debugOn_Off;
    // Component Variable's
    private Rigidbody2D rb;
    private Animator animator;
    private GameObject player;
    //Detect variable's
    private float playerDistance;
    private float detectRange = 10f;
    private bool isSummoned = false;

    private float moveSpeed;

    public void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player");
        animator = GetComponent<Animator>();

        moveSpeed = 400;
    }
    private void Update()
    {
        DetectEnemy();
        if (debugOn_Off) UpdateDebug();
        if (isSummoned) Move();
        UpdateVisuals();
    }
    //Movement Method's
    protected void Attack()
    {

    }
    protected void Move()
    {
        Debug.Log("Ready to move");
    }
    //Update Method's
    protected void DetectEnemy()
    {
        playerDistance = Vector3.Distance(player.transform.position, transform.position);
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(className + "_Spawn")) isSummoned = true;
    }
    protected void UpdateDebug()
    {

    }
    protected void UpdateVisuals()
    {
        if (playerDistance < detectRange) animator.SetTrigger("detectedPlayer");
    }
    IEnumerator SetBoolAfterSeconds(int seconds, List<string> list, bool state)
    {
        yield return new WaitForSeconds(seconds);
        foreach (string i in list) animator.SetBool(i, state);
    }
}
