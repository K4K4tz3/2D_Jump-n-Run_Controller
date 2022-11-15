using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Character_Controller_2D : MonoBehaviour
{
    [Header("Advanced Mechanics")]
    [SerializeField] private bool canHighJump;
    [SerializeField] private bool canAttack;
    [SerializeField] private bool canDelayedJump;
    [SerializeField] private bool canWallJumpSlide;
    [SerializeField] private bool canDoubleJump;
    [SerializeField] private bool canCoyoteTime;


    [Header("Movement Values")]
    [SerializeField] private int moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float animationFalling;
    [SerializeField] private float animationJumping;

    [Header("Debug Options")]
    [SerializeField] private bool debugMove;
    [SerializeField] private bool debugAnimation;
    [SerializeField] private bool debugRaycast;
    [SerializeField] private bool debugVelocity;

    //Character stats
    private float health = 500f;
    private float strength = 5.0f;
    private float weaponDamage = 10.0f;

    // Jump variable's
    private float fallMultiplier = .5f;
    private float jumpMultiplier;
    private float jumpActionStarted = 0;
    private (float time, bool allowed) saveJump;
    private bool doubleJump = false;
    private (float time, bool state) coyotetime = (0f, false);

    // Raycast
    private bool isOnGround;
    private (bool state, float timestamp, bool perform, bool wallForce) isAtWall = (false, 0f, true, true);


    // Enemy Objects
    private GameObject[] enemys;
    private (GameObject _object, string _name) closestEnemy = (null, "");
    private bool facingEnemy;

    // Component Variable's
    private Rigidbody2D rb;
    private Animator animator;
    private GameObject cooldownAttack;
    private CapsuleCollider2D capsuleCollider;
    private BoxCollider2D boxCollider;

    private int facingDirection = 0;
    private Vector2 moveValue;
    public void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        moveSpeed = 400;
        jumpForce = 6f;
        animationFalling = -2f;
        animationJumping = 2f;

        enemys = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemys)
        {
            Physics2D.IgnoreCollision(enemy.GetComponent<Collider2D>(), GetComponent<Collider2D>());
        }
        cooldownAttack = GameObject.Find("Cooldown_Attack");
    }
    private void Start()
    {
        cooldownAttack.SendMessage("SetCooldownTime", 2.5f);
    }
    public void Update()
    {
        // Update's
        UpdateVisual();
        UpdateMove();
        UpdateDebug();
        CheckRaycast();
        smoothFall();
        // Extra features
        ExtraJump();
        DetectClosestEnemy();
        WallHang();
        UpdateCollider();
    }
    // Input Action Event's
    public void OnMove(InputAction.CallbackContext context)
    {
        moveValue = context.ReadValue<Vector2>();
        //Set Character direction
        if (moveValue.x > 0)
        {
            gameObject.transform.localScale = new Vector3(2, 2, 1);
            facingDirection = 1;
        }
        else if (moveValue.x < 0)
        {
            gameObject.transform.localScale = new Vector3(-2, 2, 1);
            facingDirection = -1;
        }
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started) jumpActionStarted = Time.time; // Gets the time when the jump action is started to calculate the jump multiplier for high jumps
        if (animator.GetBool("isOnGround") && context.performed) // Normal Jump
        {
            jumpMultiplier = 1;
            if (context.duration > .2f) jumpMultiplier += (2f / 100) * (100 / 2.5f * (Time.time - jumpActionStarted)); // High Jump
            rb.velocity += Vector2.up * jumpForce * jumpMultiplier;
            coyotetime.state = false;
        }
        else if(isAtWall.state && Time.time <= isAtWall.timestamp && context.performed && canWallJumpSlide) // Wall Jump
        {
            Debug.Log("Wall Jump");
            isAtWall.perform = false;
            moveValue.x = -facingDirection * 0.4f;
            rb.velocity += Vector2.up * 8;
            gameObject.transform.localScale = new Vector3(-facingDirection * 2, 2, 1);
            animator.SetTrigger("isJumpingFromWall");
        }
        else if(!animator.GetBool("isOnGround") && context.performed && doubleJump) // Double Jump
        {
            doubleJump = false;
            rb.velocity += Vector2.up * jumpForce;
        }
        else if(Time.time <= coyotetime.time && coyotetime.state && context.performed && canCoyoteTime) // Coyote Jump
        {
            rb.velocity += Vector2.up * 8;
            coyotetime = (0f, false);;
        }
        else 
        {
            saveJump = (Time.time + .5f, true);
        }
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (animator.GetBool("canAttack") && !animator.GetBool("isJumping") && !animator.GetBool("isFalling") && context.performed && canAttack)
        {
            animator.SetBool("isAttacking1", true);
            StartCoroutine(SetBoolAfterSeconds(1, new List<string>() { "canAttack", "isAttacking1" },false)); // Resets both animation values after one second
            cooldownAttack.SendMessage("ResetCooldown");
        }
    }
    // Jump Method's
    private void smoothFall()
    {
        if (rb.velocity.y < 0.025) // when the character is falling, it's falling velocity will be reduced for a slower fall
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        if (Mathf.Abs(moveValue.x) < 1 && Mathf.Abs(moveValue.x) > 0.001 && animator.GetBool("isFalling")) moveValue.x -= 0.1f * Time.deltaTime;
        if (Mathf.Abs(moveValue.x) < 0.3f) moveValue.x = 0f;
    }
    private void ExtraJump()
    {
        //this is used for a delayed jump(when the player presses the jump key a bit before he lands) -> as soon as he lands he will automatically jump
        if (saveJump.allowed && saveJump.time <= Time.time && animator.GetBool("isOnGround") && canDelayedJump)
        {
            saveJump.allowed = false;
            rb.velocity += Vector2.up * jumpForce;
        }
        // resets the double jump bool, as soon as the character is on the ground
        if (animator.GetBool("isOnGround") && canDoubleJump) doubleJump = true;
        // sets the coyote timer, if the player just fell of a platform
        if (!animator.GetBool("isOnGround") && rb.velocity.y < -1.5f) coyotetime.time = Time.time + 1f;
        if (animator.GetBool("isOnGround")) coyotetime.state = true;
    }
    private void WallHang()
    {
        if (isAtWall.state && isAtWall.perform && canWallJumpSlide)
        {
            if (Time.time <= isAtWall.timestamp) // if time is lower than is remaining hang time, he wont slide down
            {
                rb.gravityScale = 0;
                if (isAtWall.wallForce)
                {
                    rb.velocity = new Vector2(facingDirection * 10, 0);
                    isAtWall.wallForce = false;
                }
            }
            else if(isAtWall.state && !isOnGround) // Wall Slide
            {
                rb.gravityScale = 1;
            }
            else
            {
                animator.SetBool("isAtWall", false);
                isAtWall.wallForce = true;
            }
        }
        else rb.gravityScale = 1;
    }
    // Attack Method's
    private void DealDamage()
    {
        if (Vector3.Distance(closestEnemy._object.transform.position, transform.position) < 5f && facingEnemy) closestEnemy._object.SendMessage("RecieveDamage", strength * weaponDamage);
    }
    private void RecieveDamage(float damage)
    {
        health -= damage;
        if (health > 0)
        {
            animator.SetTrigger("RecievedDamage");
            animator.SetBool("isAlive", true);
        }
        else
        {
            animator.SetBool("isAlive", false);
            animator.SetTrigger("Died");
        }
    }
    private void DetectClosestEnemy()
    {
        // detects the closest enemy of all enemy's
        foreach (GameObject enemy in enemys)
        {
            if (closestEnemy._name == "")
            {
                closestEnemy._object = enemy;
                closestEnemy._name = enemy.name;
            }
            else if (Vector3.Distance(enemy.transform.position, transform.position) < Vector3.Distance(closestEnemy._object.transform.position, transform.position))
            {
                closestEnemy._object = enemy;
                closestEnemy._name = enemy.name;
            }
        }
        // Checks if the character is facing his enemy -> he will only deal damage if he faces his enemy
        if ((transform.position.x < closestEnemy._object.transform.position.x && facingDirection == 1) || (transform.position.x > closestEnemy._object.transform.position.x && facingDirection == -1))
        {
            facingEnemy = true;
        }
        else facingEnemy = false;
    }
    // Update Method's
    private void UpdateMove()
    {
        rb.velocity = new Vector2(moveValue.x * moveSpeed * Time.fixedDeltaTime, rb.velocity.y);
    }
    private void UpdateVisual()
    {
        // Falling animation
        if (rb.velocity.y < animationFalling) animator.SetBool("isFalling", true);
        else if (rb.velocity.y > animationFalling) animator.SetBool("isFalling", false);
        // Jumping animation
        if (rb.velocity.y > animationJumping) animator.SetBool("isJumping", true);
        else if (rb.velocity.y < animationJumping) animator.SetBool("isJumping", false);
        // walking animation
        if (moveValue.x != 0) animator.SetBool("isMoving", true);
        else if (Mathf.Abs(moveValue.x) == 0) animator.SetBool("isMoving", false);
    }
    private void CheckRaycast()
    {
        // Checks the ground if the character is touching it
        isOnGround = Physics2D.Raycast(this.gameObject.transform.position, transform.TransformDirection(Vector2.down), 1.2f);
        animator.SetBool("isOnGround", isOnGround);
        if (Physics2D.Raycast(this.gameObject.transform.position, transform.TransformDirection(Vector2.down), 2)) animator.SetBool("isAtWall", false);
        // Checks if the player is at any wall
        isAtWall.state = Physics2D.Raycast(this.gameObject.transform.position, transform.TransformDirection(new Vector2(facingDirection, 0)), .7f);
        if (isAtWall.state) // if the character is at a wall, the wall hang timer will be set to 5 seconds from now
        {
            if (isAtWall.timestamp < 1f)
            {
                animator.SetBool("isAtWall", true); 
                isAtWall.timestamp = Time.time + 5;
                isAtWall.perform = true;
            }
        }
        else if(!isAtWall.state || isOnGround)
        {
            animator.SetBool("isAtWall", false);
            isAtWall.timestamp = 0f;
        }
    }
    private void UpdateDebug()
    {
        if (debugMove) Debug.Log($"Move Value: {moveValue.x}");
        if (debugVelocity) Debug.Log($"Velocity X: {moveValue.x}; Velocity Y: {rb.velocity.y}");
        if (debugAnimation) Debug.Log($"isMoving: {animator.GetBool("isMoving")}; isJumping: {animator.GetBool("isJumping")}; isFalling: {animator.GetBool("isFalling")}");
    }
    private void UpdateCollider()
    {
        // Changes between the collider size for normal and being at a wall
        if (!isAtWall.state || animator.GetBool("isOnGround")) capsuleCollider.size = new Vector2(0.61f, 1.15f);
        else capsuleCollider.size = new Vector2(0.28f, 1.15f);
    }
    private void SetCooldown((string name, bool state) cooldown)
    {
        if (cooldown.name == "attackCooldown")
        {
            animator.SetBool("canAttack", cooldown.state);
        }
    }
    IEnumerator SetBoolAfterSeconds(int seconds, List<string> list, bool state)
    {
        yield return new WaitForSeconds(seconds);
        foreach(string i in list) animator.SetBool(i, state);
    }
}
