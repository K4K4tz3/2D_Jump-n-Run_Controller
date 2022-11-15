using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy_Zombie : MonoBehaviour
{
    protected string className = "Zombie";
    [Header("Debug Options")]
    [SerializeField] protected bool debugOn_Off;
    [SerializeField] protected bool debugRaycast;
    [SerializeField] protected bool debugMove;
    [SerializeField] protected bool debugPlayer;

    [Header("Values")]
    [SerializeField] protected float moveSpeed;

    //Character stat's:
    protected float health = 150.0f;
    protected float damage = 5.0f;
    protected float cooldownAttack = 0;

    // Component Variable's
    protected Rigidbody2D rb;
    protected Animator animator;
    protected GameObject player;

    //Detect variable's
    protected float playerDistance;
    protected float detectRange = 10f;
    protected float attackRange = 2f;
    protected bool isInRange = false;

    // Move variable's
    protected bool isSummoned = false;
    protected bool infrontOfWall = false;
    protected bool canMove;

    protected int moveValue = 0;

    protected void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player");
        animator = GetComponent<Animator>();

        moveSpeed = 60;

        Physics2D.IgnoreCollision(player.GetComponent<Collider2D>(), GetComponent<Collider2D>());
    }
    protected void Update()
    {
        if (health > 0)
        {
            DetectEnemy();
            if (debugOn_Off) UpdateDebug();
            if (isSummoned) Move();
            UpdateVisuals();
            CheckRaycast();
        }
    }
    //Movement Method's
    protected void Move()
    {
        if(!infrontOfWall) rb.velocity = new Vector2(moveValue * moveSpeed * Time.fixedDeltaTime, rb.velocity.y);
    }
    // Attack Method's
    protected void DetectEnemy()
    {
        //gets the location of the player, if the player is in range, the enemy will be summoned
        playerDistance = Vector3.Distance(player.transform.position, transform.position);
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(className + "_Idle")) isSummoned = true;

        //determines where the player is, left or right and sets the moveValue to the corresponding direction (left = -1, right = 1)
        if (playerDistance < detectRange && isSummoned && playerDistance > attackRange)
        {
            if (transform.position.x > player.transform.position.x) moveValue = -1;
            else if (transform.position.x < player.transform.position.x) moveValue = 1;
            isInRange = true;
        }
        else // if the player is in the attack range, start the attack
        {
            if (playerDistance < attackRange && cooldownAttack < Time.time) animator.SetTrigger("Attack");
            isInRange = false;
        }
    }
    protected void Attack()
    {
        if (playerDistance < attackRange)
        {
            player.SendMessage("RecieveDamage", damage);
            cooldownAttack = Time.time + 1.0f;
        }
    }
    protected void RecieveDamage(float damage)
    {
        health -= damage;
        if (health <= 0f) animator.SetTrigger("isDead");
        else animator.SetTrigger("gotHit");
        if(debugOn_Off) Debug.Log(health);
    }
    //Update Method's
    protected void UpdateDebug()
    {
        if (debugRaycast) Debug.Log($"State: {infrontOfWall}, Distance: {Physics2D.Raycast(this.gameObject.transform.position, transform.TransformDirection(Vector2.left)).distance}");
        if (debugMove) Debug.Log($"Move Value: {moveValue}");
        if (debugPlayer) Debug.Log($"Player Position: {playerDistance}");
    }
    protected void UpdateVisuals()
    {
        //activate's the trigger for the summoning animation
        if (playerDistance < detectRange) animator.SetTrigger("detectedPlayer");

        //changes the sprite direction for the corresponding direction the character is facing/should face + sets the Move value for the animator on/off
        if (!infrontOfWall)
        {
            if (moveValue != 0) gameObject.transform.localScale = new Vector3(-moveValue, 1, 1);
            animator.SetBool("isMoving", true);
        }
        else animator.SetBool("isMoving", false);
    }
    protected void CheckRaycast()
    {
        if (Physics2D.Raycast(this.gameObject.transform.position, transform.TransformDirection(new Vector2(moveValue, 0)), 1.2f)) infrontOfWall = true;
        else infrontOfWall = false;
    }
    IEnumerator SetBoolAfterSeconds(int seconds, List<string> list, bool state)
    {
        yield return new WaitForSeconds(seconds);
        foreach (string i in list) animator.SetBool(i, state);
    }
}
