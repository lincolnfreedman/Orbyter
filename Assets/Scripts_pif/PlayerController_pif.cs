using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerController_pif : MonoBehaviour
{
    public BoxCollider2D normalCollider;
    public BoxCollider2D glidingCollider;
    [SerializeField] private LayerMask diggableLayer = 1 << 8; // Layer mask for climbable objects (default to layer 8 - "Climbable")
    // Note: Make sure all climbable objects (tiles, walls, etc.) are on the same layer for dig phasing to work correctly
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
    public float digForce = 12f; // Force applied during dig dash
    public float digDuration = 0.3f; // How long the dig dash lasts
    public float digCooldown = 1f; // Cooldown time between digs
    public float digAcceleration = 25f; // Acceleration for directional movement during dig phasing
    public float maxDigVelocity = 12f; // Maximum velocity during dig phasing
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isJumping = false; // Track if player is currently in a jump
    private bool isGliding = false; // Track if player is currently gliding
    private bool isWallClinging = false; // Track if player is currently wall clinging
    private bool isFastFalling = false; // Track if player is currently fast falling
    private bool isWallKickingOff = false; // Track if player is currently kicking off a wall
    private bool isSpraying = false; // Track if player is currently spraying/dashing
    private bool isDigging = false; // Track if player is currently digging/dashing
    private bool isDigPhasing = false; // Track if player is phasing through climbable objects during dig
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
    private float digCooldownTimer = 0f; // Track dig cooldown
    private float digTimer = 0f; // Track how long we've been digging
    private bool digUsedThisJump = false; // Track if dig has been used since last grounding/wall cling
    private Vector2 digExitVelocity; // Velocity when exiting dig phase
    private float digExitTimer = 0f; // Timer for velocity interpolation after exiting dig phase
    private bool isDigExiting = false; // Track if we're in the dig exit velocity interpolation phase
    private Vector2 lastGroundedPosition; // Track the character's last grounded position
    public float safeDistanceFromDamaging = 3f; // Minimum distance from damaging objects to update last grounded position
    private bool footstepPlayed = false;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction verticalAction; // For up/down input detection
    private InputAction jumpAction;
    private InputAction glideAction;    
    private InputAction fastFallAction;
    private InputAction sprayAction;
    private InputAction digAction;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Player_pip player;
    private int wallDirection = 0; // Direction of the wall we're clinging to (-1 for left wall, 1 for right wall)
    private float wallKickOffTimer = 0f; // Timer for wall kick-off duration

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetComponent<Player_pip>();
        moveAction = playerInput.actions["Move"];
        verticalAction = playerInput.actions["Vertical"];
        jumpAction = playerInput.actions["Jump"];
        glideAction = playerInput.actions["Glide"];
        fastFallAction = playerInput.actions["FastFall"];
        sprayAction = playerInput.actions["Spray"];
        digAction = playerInput.actions["Dig"];
        lastGroundedPosition = transform.position; // Initialize last grounded position to current position
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        // Handle jump input buffering FIRST (before wall clinging to give ground jumps priority)
        if (jumpAction.triggered)
        {
            jumpBufferTimer = jumpBufferTime; // Start the buffer timer
            player.PlayerSFX(1);
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
                digUsedThisJump = false; // Reset dig availability when landing
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

        HandleDig(moveInput);

        //(modified by gliding and wall cling state)
        HandleHorizontalMovement(moveInput);
        // Handle variable jump height
        if (isJumping && !isGliding && !isWallClinging && !isDigging) // Don't allow variable jump while gliding, wall clinging, or digging (but allow during wall kick-off)
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
                if (!jumpAction.IsPressed() && rb.linearVelocity.y > 0 && !isSpraying)
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

        // Handle Animations
        HandleAnimations();
        
        // Log the character's last grounded position at the end of each frame
        LogLastGroundedPosition();
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
        if (isDigging)
        {
            rb.gravityScale = 0f; // No gravity while digging
        }
        else if (isDigPhasing)
        {
            rb.gravityScale = 0f; // No gravity while phasing through walls (even after dig ends)
        }
        else if (isWallClinging)
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
        // Check if fastfall button is pressed and we're in the air (but not jumping, gliding, digging, or wall clinging)
        if (fastFallAction.IsPressed() && !isGrounded && !isJumping && !isGliding && !isDigging)
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
        // Check if spray button is pressed and spray hasn't been used this jump and not digging
        if (sprayAction.triggered && !sprayUsedThisJump && !isDigging)
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

            rb.AddForce(sprayDirection * sprayForce, ForceMode2D.Impulse);

            // Start spray duration and mark as used
            sprayTimer = sprayDuration;
            sprayUsedThisJump = true;
            
            // Set spray state
            isSpraying = true;
            
            // End gliding if currently gliding
            if (isGliding)
            {
                DeactivateGlide();
                
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
                Debug.Log("spray ended");
            }
        }
    }
    
    private void HandleDig(Vector2 moveInput)
    {
        // Update dig cooldown timer
        if (digCooldownTimer > 0f)
        {
            digCooldownTimer -= Time.deltaTime;
        }
        
        // Check if dig button is pressed and not currently spraying and dig hasn't been used this jump and cooldown is ready
        if (digAction.triggered && !isSpraying && !digUsedThisJump && digCooldownTimer <= 0f)
        {
            SetDiggableCollisionEnabled(false);
            // Get separate horizontal and vertical inputs for dig direction
            float horizontal = moveAction.ReadValue<Vector2>().x;
            float vertical = verticalAction.ReadValue<float>();
            
            // Create combined input vector for dig
            Vector2 digInput = new Vector2(horizontal, vertical);
            
            // Determine dig direction based on input
            Vector2 digDirection = GetDigDirection(digInput);
            
            // Set dig velocity
            rb.linearVelocity = digDirection * digForce;
            
            // Start dig duration and cooldown
            digTimer = digDuration;
            digCooldownTimer = digCooldown;
            digUsedThisJump = true; // Mark dig as used this jump
            
            // Set dig state
            isDigging = true;
            
            // Cancel all other movement states
            if (isJumping)
            {
                isJumping = false;
            }
            
            if (isFastFalling)
            {
                isFastFalling = false;
            }
            
            if (isWallClinging)
            {
                isWallClinging = false;
                wallDirection = 0;
            }
            
            if (isGliding)
            {
                DeactivateGlide();
               
            }
        }
        
        // Update dig timer and phasing
        if (isDigging)
        {
            // Check if we're touching a climbable object during dig
            bool touchingClimbable = EnvironmentTracker.IsTouchingDiggableWall(normalCollider, glidingCollider, diggableLayer);
            
            if (touchingClimbable && !isDigPhasing)
            {
                // Start phasing - ignore collision with climbable objects
                isDigPhasing = true;
            }
            else if (!touchingClimbable && isDigPhasing)
            {
                // No longer touching climbable objects, end phasing and start velocity interpolation
                isDigPhasing = false;
                isDigExiting = true;
                digExitVelocity = rb.linearVelocity; // Store current velocity for interpolation
                digExitTimer = digTimer; // Store remaining dig time for interpolation duration
            }
            
            // Handle velocity interpolation when exiting dig phase
            if (isDigExiting && digExitTimer > 0f)
            {
                // Calculate interpolation progress (0 to 1, where 0 is start of exit, 1 is end)
                float progress = 1f - (digExitTimer / (digTimer > 0f ? digTimer : digDuration));

                // Interpolate from full velocity to 36% of exit velocity
                Vector2 targetVelocity = digExitVelocity * 0.36f;
                Vector2 currentVelocity = Vector2.Lerp(digExitVelocity, targetVelocity, progress);
                
                rb.linearVelocity = currentVelocity;
                
                // Decrease the exit timer
                digExitTimer -= Time.deltaTime;
                
                // End dig exit when timer reaches zero
                if (digExitTimer <= 0f)
                {
                    isDigExiting = false;
                    isDigging = false;
                    SetDiggableCollisionEnabled(true);
                }
            }
            else if (!isDigPhasing && !isDigExiting)
            {
                // Normal dig timer countdown when not phasing and not in exit phase
                digTimer -= Time.deltaTime;
                if (digTimer <= 0f)
                {
                    isDigging = false;
                    SetDiggableCollisionEnabled(true);
                }
            }
            // If phasing, don't decrease timer - keep digging state active
        }
        else if (isDigPhasing)
        {
            // Handle phasing state even when not digging (e.g., when dig timer expired while inside wall)
            bool touchingClimbable = EnvironmentTracker.IsTouchingDiggableWall(normalCollider, glidingCollider, diggableLayer);
            
            if (!touchingClimbable)
            {
                // End phasing and start velocity interpolation
                isDigPhasing = false;
                isDigExiting = true;
                digExitVelocity = rb.linearVelocity; // Store current velocity for interpolation
                digExitTimer = digTimer; // Store remaining dig time for interpolation duration
            }
        }
    }
    
    private Vector2 GetDigDirection(Vector2 digInput)
    {
        // Use a threshold to account for input sensitivity
        float inputThreshold = 0.1f;
        
        // Get normalized input for 8-directional movement
        bool hasHorizontal = Mathf.Abs(digInput.x) > inputThreshold;
        bool hasVertical = Mathf.Abs(digInput.y) > inputThreshold;
        
        // If grounded, prevent downward digging (down, down-left, down-right)
        if (isGrounded && hasVertical && digInput.y < 0)
        {
            // Default to facing direction instead
            return new Vector2(facingDirection, 0f);
        }
        
        // Handle 8-directional input
        if (hasHorizontal && hasVertical)
        {
            // Diagonal directions (normalize to ensure consistent speed)
            float x = digInput.x > 0 ? 1f : -1f;
            float y = digInput.y > 0 ? 1f : -1f;
            return new Vector2(x, y).normalized;
        }
        else if (hasHorizontal)
        {
            // Horizontal only
            return new Vector2(digInput.x > 0 ? 1f : -1f, 0f);
        }
        else if (hasVertical)
        {
            // Vertical only
            return new Vector2(0f, digInput.y > 0 ? 1f : -1f);
        }
        
        // No input - use facing direction
        return new Vector2(facingDirection, 0f);
    }
    
    private Vector2 GetSprayDirection(Vector2 sprayInput)
    {
        // Priority: Horizontal (left/right) > Down > Up > Facing direction (no input)
        // Use a threshold to account for input sensitivity
        float inputThreshold = 0.1f;

        if (Mathf.Abs(sprayInput.y) > inputThreshold && sprayInput.y > 0)
        {
            return new Vector2(0f, 1f); // Up
        }
            
        
        // Check for horizontal input first (highest priority)
        if (Mathf.Abs(sprayInput.x) > inputThreshold)
        {
            Vector2 direction = new Vector2(sprayInput.x > 0 ? 1f : -1f, 0f);
            Debug.Log($"Spray Direction: Horizontal {direction}");
            return direction;
        }
        
        // Check for vertical input
        if (Mathf.Abs(sprayInput.y) > inputThreshold && sprayInput.y < 0)
        {
            return new Vector2(0f, -1f); // Down
        }
        
        // No input - use facing direction
        Vector2 facingDir = new Vector2(facingDirection, 0f);
        Debug.Log($"Spray Direction: Facing {facingDir}");
        return facingDir;
    }
    
    private void HandleWallClinging(Vector2 moveInput)
    {
        isGrounded = EnvironmentTracker.IsStandingOnSurface(rb, isDigPhasing);
        
        // Don't allow wall clinging if currently wall kicking off, if grounded, if fast falling, or if digging
        if (isWallKickingOff || isGrounded || isFastFalling || isDigging)
        {
            // If we were wall clinging and now we're grounded, fast falling, or digging, stop clinging
            if (isWallClinging)
            {
                isWallClinging = false;
                wallDirection = 0;
            }
            return;
        }
        
        // Check if we're in the air and touching a climbable wall
        if (EnvironmentTracker.IsTouchingClimbableWall(rb) && !isFastFalling)
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
                DeactivateGlide();
                wallDirection = EnvironmentTracker.GetWallDirection(rb); // Store which side the wall is on
                sprayUsedThisJump = false; // Reset spray availability when wall clinging
                digUsedThisJump = false; // Reset dig availability when wall clinging

                player.PlayerSFX(2);
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
    
    
        private void HandleGliding(Vector2 moveInput)
    {
        // Check if glide button is pressed and we're in the air (but not wall clinging or digging)
        if (glideAction.IsPressed() && !isGrounded && !isWallClinging && !isDigging && rb.linearVelocity.y < 0) // Only glide when falling and not wall clinging or digging
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
                glidingCollider.enabled = true; // Enable gliding collider
                normalCollider.enabled = false; // Disable normal collider
                isGliding = true;
                isJumping = false;
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
                DeactivateGlide();
             
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
            //float currentHorizontalVelocity = Mathf.Lerp(moveSpeed, 0f, progress);
            
            // Apply the decelerated velocity in the wall kick-off direction
            rb.linearVelocity = new Vector2(-wallDirection * moveSpeed, rb.linearVelocity.y);
        }
        else if (isGliding)
        {
            // Gliding movement is handled in HandleGliding method
            // No additional horizontal movement processing needed here
        }
        else if (isDigging)
        {
            // Check if we're in dig exit velocity interpolation phase
            if (isDigExiting)
            {
                // Don't override velocity during dig exit interpolation - let HandleDig manage it
                // Still update facing direction based on current velocity if significant
                if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                {
                    facingDirection = rb.linearVelocity.x > 0 ? 1 : -1;
                }
            }
            // Check if we're in dig phasing mode for 8-directional movement
            else if (isDigPhasing)
            {
                // 8-directional acceleration-based movement during dig phasing
                float horizontal = moveInput.x;
                float vertical = verticalAction.ReadValue<float>();
                
                // Create input direction vector
                Vector2 inputDirection = new Vector2(horizontal, vertical);
                
                // Get current velocity
                Vector2 currentVelocity = rb.linearVelocity;
                
                // If there's any input, apply acceleration in that direction
                if (inputDirection.magnitude > 0.1f)
                {
                    // Normalize input for consistent acceleration
                    inputDirection = inputDirection.normalized;
                    
                    // Apply acceleration in the input direction
                    Vector2 acceleration = inputDirection * digAcceleration * Time.deltaTime;
                    Vector2 newVelocity = currentVelocity + acceleration;
                    
                    // Clamp to maximum dig velocity
                    if (newVelocity.magnitude > maxDigVelocity)
                    {
                        newVelocity = newVelocity.normalized * maxDigVelocity;
                    }
                    
                    rb.linearVelocity = newVelocity;
                    
                    // Update facing direction based on horizontal input
                    if (Mathf.Abs(horizontal) > 0.1f)
                    {
                        facingDirection = horizontal > 0 ? 1 : -1;
                    }
                }
                // No deceleration when no input - preserve momentum
            }
            else
            {
                // Normal dig behavior - don't override velocity during dig - let the dig momentum carry
                // Still update facing direction based on current velocity if significant
                if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                {
                    facingDirection = rb.linearVelocity.x > 0 ? 1 : -1;
                }
            }
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

            if (!footstepPlayed && Mathf.Abs(rb.linearVelocity.x) > 0 && isGrounded)
            {
                StartCoroutine(FootstepSFX());
            }
        }
    }

    private IEnumerator FootstepSFX()
    {
        footstepPlayed = true;
        player.PlayerSFX(0);
        yield return new WaitForSeconds(0.5f);
        footstepPlayed = false;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check for damaging objects first
        if (collision.gameObject.CompareTag("Damaging"))
        {
            TakeDamage();
            return; // Exit early to prevent other collision logic
        }
        
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Climbable"))
        {
            isGrounded = EnvironmentTracker.IsStandingOnSurface(rb, isDigPhasing);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Check for damaging objects first
        if (collision.gameObject.CompareTag("Damaging"))
        {
            TakeDamage();
            return; // Exit early to prevent other collision logic
        }
        
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Climbable"))
        {
            isGrounded = EnvironmentTracker.IsStandingOnSurface(rb, isDigPhasing);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Climbable"))
        {
            // Check if we're still touching any ground after this collision ends
            isGrounded = EnvironmentTracker.IsStandingOnSurface(rb, isDigPhasing);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check for damaging objects
        if (other.CompareTag("Damaging"))
        {
            TakeDamage();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Check for damaging objects
        if (other.CompareTag("Damaging"))
        {
            TakeDamage();
        }
    }
    private void DeactivateGlide()
    {
        glideTimer = 0f;
        currentGlideSpeed = 0f;
        glidingCollider.enabled = false; // Disable gliding collider
        normalCollider.enabled = true; // Enable normal collider
        isGliding = false; // Set gliding state to false
    }


    private void TakeDamage()
    {
        // Reset position to origin
        transform.position = lastGroundedPosition;
        
        // Reset velocity
        rb.linearVelocity = Vector2.zero;

        // Re-enable collision with climbable objects if phasing was active
        if (isDigPhasing)
        {
            SetDiggableCollisionEnabled(true);
        }
        
        // Reset all movement states
        isGrounded = false;
        isJumping = false;
        DeactivateGlide(); // Ensure glide is deactivated
        isWallClinging = false;
        isFastFalling = false;
        isWallKickingOff = false;
        isSpraying = false;
        isDigging = false;
        isDigPhasing = false;
        isDigExiting = false;
        
        // Reset movement timers and counters
        jumpTimeCounter = 0f;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        sprayTimer = 0f;
        wallKickOffTimer = 0f;
        digTimer = 0f;
        digCooldownTimer = 0f;
        digExitTimer = 0f;
        
        // Reset movement flags
        leftGroundByJumping = false;
        wasGroundedLastFrame = false;
        sprayUsedThisJump = false;
        digUsedThisJump = false;
        
        // Reset direction and wall states
        facingDirection = 1; // Reset to facing right
        wallDirection = 0;
        
        // Reset gravity scale to default
        rb.gravityScale = gravityWhileFalling;
    }

    private void HandleAnimations()
    {
        animator.SetFloat("Velocity", Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isGliding", isGliding);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isWallClinging", isWallClinging);
        animator.SetBool("isDigging", isDigging);
        animator.SetBool("isDigPhasing", isDigPhasing);
    }

    private void SetDiggableCollisionEnabled(bool enabled)
    {
        // Get the current layer of the player's colliders
        int playerLayer = gameObject.layer;

        // Get the first layer number from the diggable layer mask
        Physics2D.IgnoreLayerCollision(playerLayer, GetLayerFromMask(diggableLayer), !enabled);
    }
    
    private int GetLayerFromMask(LayerMask layerMask)
    {
        // Convert layer mask to layer number
        int mask = layerMask.value;
        if (mask == 0) return -1;
        
        int layer = 0;
        while (mask > 1)
        {
            mask >>= 1;
            layer++;
        }
        return layer;
    }
    
    private void LogLastGroundedPosition()
    {
        // Update and log the last grounded position if currently grounded and safe from damaging objects
        if (isGrounded && IsSafeFromDamagingObjects())
        {
            lastGroundedPosition = transform.position;
            // Uncomment the line below to enable console logging (for debugging)
            // Debug.Log($"Last grounded position updated: {lastGroundedPosition}");
        }
    }
    
    private bool IsSafeFromDamagingObjects()
    {
        // Find all colliders with "Damaging" tag within the safe distance
        Collider2D[] damagingObjects = Physics2D.OverlapCircleAll(transform.position, safeDistanceFromDamaging);
        
        foreach (Collider2D collider in damagingObjects)
        {
            if (collider.CompareTag("Damaging"))
            {
                // Found a damaging object within safe distance - not safe to update position
                return false;
            }
        }
        
        // No damaging objects found within safe distance
        return true;
    }
}
