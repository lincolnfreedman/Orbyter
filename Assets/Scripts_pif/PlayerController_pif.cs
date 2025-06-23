using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController_pif : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float jumpBufferTime = 0.2f; // Time window to buffer jump input (in seconds)
    public float maxJumpTime = 0.4f; // Maximum time player can hold jump to increase height
    public float jumpReleaseMultiplier = 0.5f; // How much to reduce upward velocity when jump is released early
    public float glideGravityReduction = 0.7f; // How much to reduce gravity while gliding (0.7 = 30% of normal gravity)
    public float glideAcceleration = 3f; // How fast horizontal speed builds up while gliding
    public float maxGlideSpeed = 8f; // Maximum horizontal speed while gliding
    public float wallClingGravityReduction = 0.1f; // How much gravity to apply while wall clinging (0.1 = 10% of normal gravity)
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isJumping = false; // Track if player is currently in a jump
    private bool isGliding = false; // Track if player is currently gliding
    private bool isWallClinging = false; // Track if player is currently wall clinging
    private float glideDirection = 0f; // Direction locked when gliding starts
    private float glideTimer = 0f; // Track how long we've been gliding
    private float currentGlideSpeed = 0f; // Current horizontal glide speed
    private float jumpTimeCounter = 0f; // Track how long jump has been held
    private float jumpBufferTimer = 0f;
    private float originalGravityScale; // Store original gravity scale
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction glideAction;    private InputAction wallClingAction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale; // Store the original gravity scale
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        glideAction = playerInput.actions["Glide"]; // Assuming you have a "Glide" action in your Input Actions
        wallClingAction = playerInput.actions["WallCling"]; // Assuming you have a "WallCling" action in your Input Actions
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        
        // Handle wall clinging
        HandleWallClinging(moveInput);
        
        // Handle gliding
        HandleGliding(moveInput);
        
        // Handle horizontal movement (modified by gliding and wall cling state)
        HandleHorizontalMovement(moveInput);
        
        // Handle jump input buffering
        if (jumpAction.triggered)
        {
            jumpBufferTimer = jumpBufferTime; // Start the buffer timer
        }
        
        // Decrease buffer timer
        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }
        
        // Execute jump if we have a buffered input and we're grounded
        if (jumpBufferTimer > 0f && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer = 0f; // Clear the buffer after jumping
            isJumping = true; // Start tracking the jump
            jumpTimeCounter = 0f; // Reset jump time counter
        }
          // Handle variable jump height
        if (isJumping && !isGliding && !isWallClinging) // Don't allow variable jump while gliding or wall clinging
        {
            // If jump button is still held and we haven't exceeded max jump time
            if (jumpAction.IsPressed() && jumpTimeCounter < maxJumpTime)
            {
                jumpTimeCounter += Time.deltaTime;
                // Continue applying upward force (diminishing over time)
                float jumpMultiplier = 1f - (jumpTimeCounter / maxJumpTime);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + (jumpForce * jumpMultiplier * Time.deltaTime));
            }
            else
            {
                // Jump button released or max time reached - end variable jump
                if (!jumpAction.IsPressed() && rb.linearVelocity.y > 0)
                {
                    // Reduce upward velocity when jump is released early
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpReleaseMultiplier);
                }
                isJumping = false;
            }
        }
        
        // Reset jumping state when grounded
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            isJumping = false;
        }    }
    
    private void HandleWallClinging(Vector2 moveInput)
    {
        // Check if wall cling button is pressed and we're in the air
        if (wallClingAction.IsPressed() && !isGrounded)
        {
            // Check if we're touching a wall
            if (IsTouchingWall(moveInput.x))
            {
                if (!isWallClinging)
                {
                    // Start wall clinging
                    isWallClinging = true;
                    isJumping = false; // End any current jump
                    isGliding = false; // End any current glide
                    rb.gravityScale = originalGravityScale * wallClingGravityReduction; // Reduce gravity significantly
                    rb.linearVelocity = new Vector2(0f, 0f); // Stop all movement
                }
                else
                {
                    // Continue wall clinging - keep velocity at zero
                    rb.linearVelocity = new Vector2(0f, 0f);
                }
            }
            else
            {
                // Not touching a wall, stop clinging
                if (isWallClinging)
                {
                    isWallClinging = false;
                    rb.gravityScale = originalGravityScale; // Restore normal gravity
                }
            }
        }
        else
        {
            // Wall cling button not pressed, stop clinging
            if (isWallClinging)
            {
                isWallClinging = false;
                rb.gravityScale = originalGravityScale; // Restore normal gravity
            }
        }
    }
    
    private bool IsTouchingWall(float horizontalInput)
    {
        // Get all current collisions
        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int contactCount = rb.GetContacts(contacts);
        
        for (int i = 0; i < contactCount; i++)
        {
            // Check if touching a wall (vertical surface)
            // Wall normals point horizontally (left or right)
            if (contacts[i].collider.CompareTag("Ground") && Mathf.Abs(contacts[i].normal.x) > 0.7f)
            {
                // Check if the wall is in the direction the player is trying to move
                // This prevents clinging to walls behind the player
                if ((horizontalInput > 0 && contacts[i].normal.x < 0) || // Moving right, wall on right
                    (horizontalInput < 0 && contacts[i].normal.x > 0) || // Moving left, wall on left
                    (horizontalInput == 0)) // No input, allow clinging to any wall
                {
                    return true;
                }
            }
        }
        
        return false;
    }
        private void HandleGliding(Vector2 moveInput)
    {
        // Check if glide button is pressed and we're in the air (but not wall clinging)
        if (glideAction.IsPressed() && !isGrounded && !isWallClinging && rb.linearVelocity.y < 0) // Only glide when falling and not wall clinging
        {
            if (!isGliding)
            {
                // Start gliding
                isGliding = true;
                isJumping = false; // End any current jump
                glideDirection = moveInput.x; // Lock in the direction when gliding starts
                glideTimer = 0f; // Reset glide timer
                currentGlideSpeed = moveSpeed; // Start with base move speed
                rb.gravityScale = originalGravityScale * glideGravityReduction; // Reduce gravity
            }
            else
            {
                // Continue gliding - build up speed over time
                glideTimer += Time.deltaTime;
                currentGlideSpeed = Mathf.Min(moveSpeed + (glideAcceleration * glideTimer), maxGlideSpeed);
            }
        }
        else
        {
            if (isGliding)
            {
                // Stop gliding
                isGliding = false;
                glideTimer = 0f;
                currentGlideSpeed = 0f;
                rb.gravityScale = originalGravityScale; // Restore normal gravity
            }
        }
    }
      private void HandleHorizontalMovement(Vector2 moveInput)
    {
        if (isWallClinging)
        {
            // While wall clinging, no horizontal movement allowed
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else if (isGliding)
        {
            // While gliding, use locked direction with progressive acceleration
            rb.linearVelocity = new Vector2(glideDirection * currentGlideSpeed, rb.linearVelocity.y);
        }
        else
        {
            // Normal horizontal movement
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
    }private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            CheckGroundContact(collision);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            CheckGroundContact(collision);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            // Check if we're still touching any ground after this collision ends
            isGrounded = IsStillTouchingGround();
        }
    }

    private void CheckGroundContact(Collision2D collision)
    {
        // Check if any contact point has a normal pointing upward (player is on top)
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // Normal pointing up means the surface is below the player
            // We use a threshold (0.7) to allow for slightly sloped surfaces
            if (contact.normal.y > 0.7f)
            {
                isGrounded = true;
                return;
            }
        }
        
        // If no upward-facing normals found, player is not grounded
        isGrounded = false;
    }

    private bool IsStillTouchingGround()
    {
        // Get all current collisions
        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int contactCount = rb.GetContacts(contacts);
        
        for (int i = 0; i < contactCount; i++)
        {
            if (contacts[i].collider.CompareTag("Ground") && contacts[i].normal.y > 0.7f)
            {
                return true;
            }
        }
        
        return false;
    }
}
