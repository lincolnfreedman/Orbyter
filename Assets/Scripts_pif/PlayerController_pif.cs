using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController_pif : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float jumpBufferTime = 0.2f; // Time window to buffer jump input (in seconds)
    public float maxJumpTime = 0.4f; // Maximum time player can hold jump to increase height
    public float jumpReleaseMultiplier = 0.5f; // How much to reduce upward velocity when jump is released early
    public float coyoteTime = 0.15f; // Time window after leaving ground where player can still jump
    public float glideAcceleration = 3f; // How fast horizontal speed builds up while gliding
    public float maxGlideSpeed = 8f; // Maximum horizontal speed while gliding
    public float glideVelocityMultiplier = 0.6f; // How much downward velocity is reduced when starting to glide
    public float gravityWhileJumping = 1f; // Gravity scale while jumping and moving upward
    public float gravityWhileFalling = 2f; // Gravity scale while falling
    public float gravityWhileGliding = 0.5f; // Gravity scale while gliding
    public float terminalVelocity = -10f; // Maximum downward velocity (negative value)
    public float wallKickOffDuration = 0.5f; // How long the wall kick-off force is applied
    public float sprayForce = 15f; // Force applied during spray/dash
    public float sprayCooldown = 1f; // Cooldown time between sprays
    public float sprayDuration = 0.1f; // How long the spray state lasts
    public float fastFallVelocity = 8f; // Velocity added/removed for fast falling
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isJumping = false; // Track if player is currently in a jump
    private bool isGliding = false; // Track if player is currently gliding
    private bool isWallClinging = false; // Track if player is currently wall clinging
    private bool isFastFalling = false; // Track if player is currently fast falling
    private bool isWallKickingOff = false; // Track if player is currently kicking off a wall
    private bool isSpraying = false; // Track if player is currently spraying/dashing
    public int facingDirection = 1; // 1 for right, -1 for left
    private float glideTimer = 0f; // Track how long we've been gliding
    private float currentGlideSpeed = 0f; // Current horizontal glide speed
    private float jumpTimeCounter = 0f; // Track how long jump has been held
    private float jumpBufferTimer = 0f;
    private float coyoteTimer = 0f; // Track time since leaving ground
    private bool leftGroundByJumping = false; // Track if we left ground due to jumping
    private bool wasGroundedLastFrame = false; // Track previous grounded state
    private float sprayCooldownTimer = 0f; // Track spray cooldown
    private float sprayTimer = 0f; // Track how long we've been spraying
    private bool sprayUsedThisJump = false; // Track if spray has been used since last grounding/wall cling
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction verticalAction; // For up/down input detection
    private InputAction jumpAction;
    private InputAction glideAction;    
    private InputAction fastFallAction;
    private InputAction sprayAction;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private int wallDirection = 0; // Direction of the wall we're clinging to (-1 for left wall, 1 for right wall)
    private float wallKickOffTimer = 0f; // Timer for wall kick-off duration

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        moveAction = playerInput.actions["Move"];
        verticalAction = playerInput.actions["Vertical"]; // Assuming you have a "Vertical" action
        jumpAction = playerInput.actions["Jump"];
        glideAction = playerInput.actions["Glide"]; 
        fastFallAction = playerInput.actions["FastFall"]; 
        sprayAction = playerInput.actions["Spray"]; 
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("is wall clinging: " + isWallClinging);
        Debug.Log("is grounded: " + isGrounded);
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        
        // Handle jump input buffering FIRST (before wall clinging to give ground jumps priority)
        if (jumpAction.triggered)
        {
            jumpBufferTimer = jumpBufferTime; // Start the buffer timer
        }
        
        // Decrease buffer timer
        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }
        
        // Update coyote timer
        if (isGrounded)
        {
            // Only reset coyote timer if we just landed (transition from air to ground)
            if (!wasGroundedLastFrame)
            {
                // Cancel fast fall if active when landing
                if (isFastFalling)
                {
                    isFastFalling = false;
                    // Don't add upward velocity when landing as it would interfere with ground contact
                }
                
                coyoteTimer = coyoteTime; // Reset coyote timer when landing
                leftGroundByJumping = false; // Reset jump flag when landing
                sprayUsedThisJump = false; // Reset spray availability when landing
            }
        }
        else
        {
            // Only start coyote timer if we just left the ground and didn't jump
            if (wasGroundedLastFrame && !leftGroundByJumping)
            {
                coyoteTimer = coyoteTime; // Start coyote timer when leaving ground naturally
            }
            
            // Decrease coyote timer when in air
            if (!leftGroundByJumping)
            {
                coyoteTimer -= Time.deltaTime;
            }
        }
        
        // Store current grounded state for next frame
        wasGroundedLastFrame = isGrounded;
        
        // Execute jump if we have a buffered input and we're grounded OR within coyote time (and didn't leave by jumping)
        if (jumpBufferTimer > 0f && (isGrounded || (coyoteTimer > 0f && !leftGroundByJumping)))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer = 0f; // Clear the buffer after jumping
            coyoteTimer = 0f; // Clear coyote timer after jumping (prevent multiple coyote jumps)
            leftGroundByJumping = true; // Mark that we left ground by jumping
            isJumping = true; // Start tracking the jump
            jumpTimeCounter = 0f; // Reset jump time counter
        }
        
    
        HandleWallClinging(moveInput);
        
        HandleGliding(moveInput);

        HandleFastFalling();
        
        HandleSpray(moveInput);
        
        //(modified by gliding and wall cling state)
        HandleHorizontalMovement(moveInput);
          // Handle variable jump height
        if (isJumping && !isGliding && !isWallClinging) // Don't allow variable jump while gliding or wall clinging (but allow during wall kick-off)
        {
            // If jump button is still held and we haven't exceeded max jump time
            if (jumpAction.IsPressed() && jumpTimeCounter < maxJumpTime)
            {
                jumpTimeCounter += Time.deltaTime;
                // Continue applying upward force (diminishing over time)
                float jumpMultiplier = 1f - (jumpTimeCounter / maxJumpTime);
                float newVerticalVelocity = rb.linearVelocity.y + (jumpForce * jumpMultiplier * Time.deltaTime);
                
                // Preserve horizontal velocity if wall kicking off
                if (isWallKickingOff)
                {
                    rb.linearVelocity = new Vector2(-wallDirection * moveSpeed, newVerticalVelocity);
                }
                else
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, newVerticalVelocity);
                }
            }
            else
            {
                // Jump button released or max time reached - end variable jump
                if (!jumpAction.IsPressed() && rb.linearVelocity.y > 0)
                {
                    // Reduce upward velocity when jump is released early
                    float newVerticalVelocity = rb.linearVelocity.y * jumpReleaseMultiplier;
                    
                    // Preserve horizontal velocity if wall kicking off
                    if (isWallKickingOff)
                    {
                        rb.linearVelocity = new Vector2(-wallDirection * moveSpeed, newVerticalVelocity);
                    }
                    else
                    {
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, newVerticalVelocity);
                    }
                }
                isJumping = false;
            }
        }
        
        // Reset jumping state when grounded
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            isJumping = false;
        }
        
        // Update gravity based on current state
        UpdateGravity();
        
        // Apply terminal velocity limit
        ApplyTerminalVelocity();
        
        // Handle wall kick-off timer
        if (isWallKickingOff)
        {
            wallKickOffTimer -= Time.deltaTime;
            if (wallKickOffTimer <= 0f)
            {
                isWallKickingOff = false;
                wallDirection = 0; // Reset wall direction when kick-off ends
            }
        }

        // Handle Sprite Flipping based on velocity
        if (rb.linearVelocity.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (rb.linearVelocity.x < 0)
        {
            spriteRenderer.flipX = true;
        }
    }
    
    private void ApplyTerminalVelocity()
    {
        // Limit downward velocity to terminal velocity
        if (rb.linearVelocity.y < terminalVelocity)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, terminalVelocity);
        }
    }
    
    private void UpdateGravity()
    {
        if (isWallClinging)
        {
            rb.gravityScale = 0f; // No gravity while wall clinging
        }
        else if (isGliding)
        {
            rb.gravityScale = gravityWhileGliding;
        }
        else if (isJumping && rb.linearVelocity.y > 0)
        {
            rb.gravityScale = gravityWhileJumping;
        }
        else
        {
            rb.gravityScale = gravityWhileFalling;
        }
    }
    
    private void HandleFastFalling()
    {
        // Check if fastfall button is pressed and we're in the air (but not jumping, gliding, or wall clinging)
        if (fastFallAction.IsPressed() && !isGrounded && !isJumping && !isGliding && !isWallClinging && rb.linearVelocity.y < 0)
        {
            // Start fast falling if not already fast falling
            if (!isFastFalling)
            {
                isFastFalling = true;
                // Add downward velocity
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - fastFallVelocity);
            }
        }
        else
        {
            // Stop fast falling if currently fast falling
            if (isFastFalling)
            {
                isFastFalling = false;
                // Add upward velocity to counter the fast fall
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + fastFallVelocity);
            }
        }
    }
    
    private void HandleSpray(Vector2 moveInput)
    {
        // Check if spray button is pressed and spray hasn't been used this jump
        if (sprayAction.triggered && !sprayUsedThisJump)
        {
            // Get separate horizontal and vertical inputs for spray direction
            float horizontal = moveAction.ReadValue<Vector2>().x;
            float vertical = verticalAction.ReadValue<float>();
            
            // Create combined input vector for spray
            Vector2 sprayInput = new Vector2(horizontal, vertical);
            
            // Debug the input values
            Debug.Log($"Spray Input - Horizontal: {horizontal}, Vertical: {vertical}");
            
            // Determine spray direction based on input priority
            Vector2 sprayDirection = GetSprayDirection(sprayInput);
            
            // Add spray velocity instead of setting it
            rb.linearVelocity = sprayDirection * sprayForce;
            
            // Start spray duration and mark as used
            sprayTimer = sprayDuration;
            sprayUsedThisJump = true;
            
            // Set spray state
            isSpraying = true;
            
            // End gliding if currently gliding
            if (isGliding)
            {
                isGliding = false;
                glideTimer = 0f;
                currentGlideSpeed = 0f;
            }
            
            // Only end wall clinging if currently wall clinging
            if (isWallClinging)
            {
                isWallClinging = false;
                wallDirection = 0;
            }
        }
        
        // Update spray timer
        if (isSpraying)
        {
            sprayTimer -= Time.deltaTime;
            if (sprayTimer <= 0f)
            {
                isSpraying = false;
            }
        }
    }
    
    private Vector2 GetSprayDirection(Vector2 sprayInput)
    {
        // Priority: Horizontal (left/right) > Down > Up > Facing direction (no input)
        // Use a threshold to account for input sensitivity
        float inputThreshold = 0.1f;
        
        Debug.Log($"GetSprayDirection - X: {sprayInput.x}, Y: {sprayInput.y}");
        
        // Check for horizontal input first (highest priority)
        if (Mathf.Abs(sprayInput.x) > inputThreshold)
        {
            Vector2 direction = new Vector2(sprayInput.x > 0 ? 1f : -1f, 0f);
            Debug.Log($"Spray Direction: Horizontal {direction}");
            return direction;
        }
        
        // Check for vertical input
        if (Mathf.Abs(sprayInput.y) > inputThreshold)
        {
            // Down has higher priority than up
            if (sprayInput.y < 0)
            {
                Debug.Log("Spray Direction: Down");
                return new Vector2(0f, -1f); // Down
            }
            else
            {
                Debug.Log("Spray Direction: Up");
                return new Vector2(0f, 1f); // Up
            }
        }
        
        // No input - use facing direction
        Vector2 facingDir = new Vector2(facingDirection, 0f);
        Debug.Log($"Spray Direction: Facing {facingDir}");
        return facingDir;
    }
    
    private void HandleWallClinging(Vector2 moveInput)
    {
        // Update isGrounded with the more reliable check
        isGrounded = IsStandingOnSurface();
        
        // Don't allow wall clinging if currently wall kicking off or if grounded
        if (isWallKickingOff || isGrounded)
        {
            // If we were wall clinging and now we're grounded, stop clinging
            if (isWallClinging)
            {
                isWallClinging = false;
                wallDirection = 0;
            }
            return;
        }
        
        // Check if we're in the air and touching a climbable wall
        if (IsTouchingClimbableWall())
        {
            // Only start wall clinging if we have negative velocity (falling), but continue if already clinging
            if (!isWallClinging && rb.linearVelocity.y < 0)
            {
                // Cancel fast fall if active before wall clinging
                if (isFastFalling)
                {
                    isFastFalling = false;
                    // Add upward velocity to counter the fast fall
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + fastFallVelocity);
                }
                
                // Start wall clinging (only when falling)
                isWallClinging = true;
                isJumping = false; // End any current jump
                isGliding = false; // End any current glide
                wallDirection = GetWallDirection(); // Store which side the wall is on
                sprayUsedThisJump = false; // Reset spray availability when wall clinging
            }
            
            // If we're wall clinging (either just started or continuing), maintain it
            if (isWallClinging)
            {
                // Always set vertical velocity to 0 while wall clinging
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                
                // Handle wall jump/kick-off
                if (jumpAction.triggered)
                {
                    // Perform wall jump - use normal jump mechanics
                    isWallClinging = false;
                    isWallKickingOff = true;
                    wallKickOffTimer = wallKickOffDuration;
                    
                    // Apply normal jump force vertically and set horizontal velocity away from wall
                    rb.linearVelocity = new Vector2(-wallDirection * moveSpeed, jumpForce);
                    
                    isJumping = true;
                    jumpTimeCounter = 0f;
                    jumpBufferTimer = 0f; // Clear jump buffer
                    coyoteTimer = 0f; // Clear coyote timer after wall jumping
                    leftGroundByJumping = true; // Mark that we left ground by jumping (prevents coyote time abuse)
                }
                else
                {
                    // Allow movement away from wall to end wall cling
                    if (moveInput.x != 0)
                    {
                        // If moving away from the wall, end wall clinging
                        if ((wallDirection > 0 && moveInput.x < 0) || (wallDirection < 0 && moveInput.x > 0))
                        {
                            isWallClinging = false;
                            wallDirection = 0;
                        }
                        else
                        {
                            // Moving towards wall - maintain clinging (keep vertical velocity at 0)
                            rb.linearVelocity = new Vector2(0f, 0f);
                        }
                    }
                    else
                    {
                        // No input - maintain wall cling (keep vertical velocity at 0)
                        rb.linearVelocity = new Vector2(0f, 0f);
                    }
                }
            }
        }
        else
        {
            // Not touching a climbable wall or not falling, stop clinging
            if (isWallClinging)
            {
                isWallClinging = false;
                wallDirection = 0;
            }
        }
    }
    
    private bool IsTouchingClimbableWall()
    {
        // Get all current collisions
        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int contactCount = rb.GetContacts(contacts);
        
        for (int i = 0; i < contactCount; i++)
        {
            // Check if touching a climbable wall (vertical surface)
            // Wall normals point horizontally (left or right)
            if (contacts[i].collider.CompareTag("Climbable") && Mathf.Abs(contacts[i].normal.x) > 0.7f)
            {
                return true;
            }
        }
        
        return false;
    }
    
    private int GetWallDirection()
    {
        // Get all current collisions
        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int contactCount = rb.GetContacts(contacts);
        
        for (int i = 0; i < contactCount; i++)
        {
            // Check if touching a climbable wall (vertical surface)
            if (contacts[i].collider.CompareTag("Climbable") && Mathf.Abs(contacts[i].normal.x) > 0.7f)
            {
                // Return the direction of the wall normal
                // If normal points right (positive x), wall is on the left (-1)
                // If normal points left (negative x), wall is on the right (1)
                return contacts[i].normal.x > 0 ? -1 : 1;
            }
        }
        
        return 0; // No wall found
    }
        private void HandleGliding(Vector2 moveInput)
    {
        // Check if glide button is pressed and we're in the air (but not wall clinging)
        if (glideAction.IsPressed() && !isGrounded && !isWallClinging && rb.linearVelocity.y < 0) // Only glide when falling and not wall clinging
        {
            if (!isGliding)
            {
                // Cancel fast fall if active before gliding
                if (isFastFalling)
                {
                    isFastFalling = false;
                    // Add upward velocity to counter the fast fall
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + fastFallVelocity);
                }
                
                // Start gliding
                isGliding = true;
                isJumping = false; // End any current jump
                glideTimer = 0f; // Reset glide timer
                
                // Reduce downward velocity when starting to glide
                if (rb.linearVelocity.y < 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * glideVelocityMultiplier);
                }
                
                // If no direction is held, set base speed in facing direction
                if (moveInput.x == 0)
                {
                    rb.linearVelocity = new Vector2(facingDirection * moveSpeed, rb.linearVelocity.y);
                }
                else
                {
                    // Start with current horizontal velocity
                    currentGlideSpeed = rb.linearVelocity.x;
                }
            }
            else
            {
                // Continue gliding - apply directional acceleration
                if (moveInput.x != 0)
                {
                    // Apply acceleration in the input direction
                    float acceleration = glideAcceleration * moveInput.x * Time.deltaTime;
                    float newVelocityX = rb.linearVelocity.x + acceleration;
                    
                    // Clamp to max glide speed
                    newVelocityX = Mathf.Clamp(newVelocityX, -maxGlideSpeed, maxGlideSpeed);
                    
                    rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
                    
                    // Update facing direction based on velocity direction
                    if (newVelocityX > 0)
                    {
                        facingDirection = 1;
                    }
                    else if (newVelocityX < 0)
                    {
                        facingDirection = -1;
                    }
                }
                // If no input, maintain current horizontal velocity (no acceleration)
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
        else if (isWallKickingOff)
        {
            // During wall kick-off, gradually decelerate horizontal velocity from initial kickoff speed to 0
            // Calculate how much time has passed since the wall kick-off started
            float timeElapsed = wallKickOffDuration - wallKickOffTimer;
            float progress = timeElapsed / wallKickOffDuration; // 0 at start, 1 at end
            
            // Interpolate from initial kickoff velocity to 0
            float currentHorizontalVelocity = Mathf.Lerp(moveSpeed, 0f, progress);
            
            // Apply the decelerated velocity in the wall kick-off direction
            rb.linearVelocity = new Vector2(-wallDirection * currentHorizontalVelocity, rb.linearVelocity.y);
        }
        else if (isGliding)
        {
            // Gliding movement is handled in HandleGliding method
            // No additional horizontal movement processing needed here
        }
        else if (isSpraying)
        {
            // Don't override velocity during spray - let the spray momentum carry
            // Still update facing direction based on current velocity if significant
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                facingDirection = rb.linearVelocity.x > 0 ? 1 : -1;
            }
        }
        else
        {
            // Normal horizontal movement
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
            
            // Update facing direction based on movement input
            if (moveInput.x > 0)
            {
                facingDirection = 1; // Facing right
            }
            else if (moveInput.x < 0)
            {
                facingDirection = -1; // Facing left
            }
            // If moveInput.x == 0, maintain current facing direction
        }
    }    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check for damaging objects first
        if (collision.gameObject.CompareTag("Damaging"))
        {
            Die();
            return; // Exit early to prevent other collision logic
        }
        
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Climbable"))
        {
            CheckGroundContact(collision);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Check for damaging objects first
        if (collision.gameObject.CompareTag("Damaging"))
        {
            Die();
            return; // Exit early to prevent other collision logic
        }
        
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Climbable"))
        {
            CheckGroundContact(collision);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Climbable"))
        {
            // Check if we're still touching any ground after this collision ends
            isGrounded = IsStandingOnSurface();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check for damaging objects
        if (other.CompareTag("Damaging"))
        {
            Die();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Check for damaging objects
        if (other.CompareTag("Damaging"))
        {
            Die();
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

    private bool IsStandingOnSurface()
    {
        return HasContactWithNormal(contact => IsGroundSurface(contact.collider) && IsUpwardFacing(contact.normal));
    }

    private bool HasContactWithNormal(System.Func<ContactPoint2D, bool> condition)
    {
        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int contactCount = rb.GetContacts(contacts);
        
        for (int i = 0; i < contactCount; i++)
        {
            if (condition(contacts[i]))
            {
                return true;
            }
        }
        
        return false;
    }


    private bool IsGroundSurface(Collider2D collider)
    {
        return collider.CompareTag("Ground") || collider.CompareTag("Climbable");
    }


    private bool IsUpwardFacing(Vector2 normal)
    {
        return normal.y > 0.7f; // 0.7 allows for slightly sloped surfaces
    }

    private void Die()
    {
        // Reset position to origin
        transform.position = Vector3.zero;
        
        // Reset velocity
        rb.linearVelocity = Vector2.zero;
        
        // Reset all movement states
        isGrounded = false;
        isJumping = false;
        isGliding = false;
        isWallClinging = false;
        isFastFalling = false;
        isWallKickingOff = false;
        isSpraying = false;
        
        // Reset movement timers and counters
        glideTimer = 0f;
        currentGlideSpeed = 0f;
        jumpTimeCounter = 0f;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        sprayTimer = 0f;
        wallKickOffTimer = 0f;
        
        // Reset movement flags
        leftGroundByJumping = false;
        wasGroundedLastFrame = false;
        sprayUsedThisJump = false;
        
        // Reset direction and wall states
        facingDirection = 1; // Reset to facing right
        wallDirection = 0;
        
        // Reset gravity scale to default
        rb.gravityScale = gravityWhileFalling;
    }
}
