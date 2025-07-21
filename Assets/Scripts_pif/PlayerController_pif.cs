using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerController_pif : MonoBehaviour
{
    [Header("Colliders")]
    public BoxCollider2D normalCollider;
    public BoxCollider2D glidingCollider;
    [SerializeField] private LayerMask diggableLayer = 1 << 8;
    
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float jumpBufferTime = 0.2f;
    public float maxJumpTime = 0.4f;
    public float jumpReleaseMultiplier = 0.5f;
    public float coyoteTime = 0.15f;
    
    [Header("Gliding")]
    public float glideAcceleration = 3f;
    public float maxGlideSpeed = 8f;
    public float glideVelocityMultiplier = 0.6f;
    [Tooltip("Minimum distance from ground required to start/continue gliding")]
    public float minimumGlideHeight = 2f;
    
    [Header("Physics")]
    public float gravityWhileJumping = 1f;
    public float gravityWhileFalling = 2f;
    public float gravityWhileGliding = 0.5f;
    public float terminalVelocity = -10f;
    [Tooltip("Terminal velocity used specifically while gliding")]
    public float glideTerminalVelocity = -3f;
    public float fastFallVelocity = 8f;
    
    [Header("Wall Mechanics")]
    public float wallKickOffDuration = 0.5f;
    
    [Header("Abilities")]
    [Header("Water Spout")]
    public GameObject waterSpoutPrefab;
    public float sprayDuration = 2f;
    public float sprayAcceleration = 8f;
    public float maxSprayVelocity = 15f;
    [Tooltip("Deceleration applied to horizontal movement when spraying horizontally without spout contact")]
    public float horizontalSprayDeceleration = 5f;
    [Tooltip("Instant velocity boost applied once when spout first contacts a surface")]
    public float sprayInstantBoost = 6f;
    [Tooltip("Multiplier applied to downward velocity when spray begins (similar to jump release multiplier)")]
    public float sprayStartVelocityMultiplier = 0.6f;
    [Tooltip("Distance from player when spawning horizontal water spouts")]
    public float horizontalSprayOffset = 0.5f;
    [Tooltip("Distance from player when spawning vertical water spouts")]
    public float verticalSprayOffset = 0.5f;
    [Header("Dig")]
    public float digForce = 12f;
    public float digDuration = 0.3f;
    public float digCooldown = 1f;
    public float digAcceleration = 25f;
    public float maxDigVelocity = 12f;
    [Tooltip("Cooldown time between spray uses")]
    public float sprayCooldown = 1f;
    
    [Header("Safety")]
    public float safeDistanceFromDamaging = 3f;
    
    [Header("Health & Respawn")]
    [Tooltip("Maximum health points")]
    public int maxHealth = 3;
    [Tooltip("Current health points")]
    public int currentHealth;
    [Tooltip("Starting position for respawn before any checkpoints")]
    private Vector3 startingPosition;
    [Tooltip("Current checkpoint position for respawn")]
    private Vector3 checkpointPosition;
    [Tooltip("Whether a checkpoint has been set")]
    private bool hasCheckpoint = false;
    [Tooltip("Duration of movement disable after respawning")]
    public float respawnMovementDisableDuration = 1f;
    [Tooltip("Duration of invulnerability frames after getting hit")]
    public float invulnerabilityFrames = 1.5f;
    
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isJumping = false;
    private bool isGliding = false;
    private bool isWallClinging = false;
    private bool isFastFalling = false;
    private bool isWallKickingOff = false;
    private bool isSpraying = false;
    private int sprayDirection = 0; //0 = Down, 1 = Up, 2 = Horizontal
    private bool isDigging = false;
    private bool isDigPhasing = false;
    private bool isInvulnerable = false;
    public int facingDirection = 1;
    
    // Timers and counters
    private float glideTimer = 0f;
    private float currentGlideSpeed = 0f;
    private float jumpTimeCounter = 0f;
    private float jumpBufferTimer = 0f;
    private float coyoteTimer = 0f;
    private float sprayTimer = 0f;
    private float sprayCooldownTimer = 0f;
    private float digCooldownTimer = 0f;
    private float digTimer = 0f;
    private float digExitTimer = 0f;
    private float wallKickOffTimer = 0f;
    
    // Water spout tracking
    private GameObject currentWaterSpout;
    private Vector2 spoutDirection;
    private Vector3 spoutOffset; // Relative offset from player to spout
    private bool sprayInstantBoostUsed = false; // Track if instant boost has been applied this spray
    
    // State tracking
    private bool leftGroundByJumping = false;
    private bool wasGroundedLastFrame = false;
    private bool sprayUsedThisJump = false;
    private bool digUsedThisJump = false;
    private bool isDigExiting = false;
    private bool footstepPlayed = false;
    
    // Other variables
    private Vector2 digExitVelocity;
    private Vector2 lastGroundedPosition;
    private int wallDirection = 0;
    
    // Component references
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction verticalAction;
    private InputAction jumpAction;
    private InputAction glideAction;    
    private InputAction fastFallAction;
    private InputAction sprayAction;
    private InputAction digAction;
    private InputAction interactAction;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Player_pip player;
    private ParticleSystem deathParticles;

    // Public property to access interact action from other scripts
    public InputAction InteractAction => interactAction;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetComponent<Player_pip>();
        deathParticles = GetComponentInChildren<ParticleSystem>();
        moveAction = playerInput.actions["Move"];
        verticalAction = playerInput.actions["Vertical"];
        jumpAction = playerInput.actions["Jump"];
        glideAction = playerInput.actions["Glide"];
        fastFallAction = playerInput.actions["FastFall"];
        sprayAction = playerInput.actions["Spray"];
        digAction = playerInput.actions["Dig"];
        interactAction = playerInput.actions["Interact"];
        lastGroundedPosition = transform.position; // Initialize last grounded position to current position
        
        // Initialize health and respawn system
        currentHealth = maxHealth;
        startingPosition = transform.position;
        checkpointPosition = startingPosition;
    }

    // Update is called once per frame
    void Update()
    {
        // Skip all movement processing if movement is disabled
        if (movementDisabled)
        {
            HandleVisuals(); // Still handle visuals/animations
            return;
        }
        
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        HandleInput();
        HandleMovementStates(moveInput);
        HandlePhysics();
        HandleVisuals();
        LogLastGroundedPosition();
    }
    
    private void HandleInput()
    {
        // Handle jump input buffering
        if (jumpAction.triggered)
        {
            jumpBufferTimer = jumpBufferTime;
        }

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;
    }
    
    private void HandleMovementStates(Vector2 moveInput)
    {
        UpdateGroundedState();
        UpdateCoyoteTime();
        ExecuteBufferedJump();
        
        HandleWallClinging(moveInput);
        HandleGliding(moveInput);
        HandleFastFalling();
        HandleSpray(moveInput);
        HandleDig(moveInput);
        HandleHorizontalMovement(moveInput);
        HandleVariableJump();
    }
    
    private void HandlePhysics()
    {
        UpdateGravity();
        ApplyTerminalVelocity();
        UpdateWallKickOff();
    }
    
    private void HandleVisuals()
    {
        UpdateSpriteFlipping();
        HandleAnimations();
    }
    
    private void ApplyTerminalVelocity()
    {
        // Use glide-specific terminal velocity when gliding, otherwise use normal terminal velocity
        // During spray, use normal terminal velocity (not glide terminal velocity) to allow proper spray acceleration
        float currentTerminalVelocity = (isGliding && !isSpraying) ? glideTerminalVelocity : terminalVelocity;
        
        // Limit downward velocity to terminal velocity
        if (rb.linearVelocity.y < currentTerminalVelocity)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentTerminalVelocity);
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
        else if (isSpraying)
        {
            rb.gravityScale = gravityWhileJumping; // Use jumping gravity while spraying
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
        // Check if fastfall button is held and we're in the air (but not jumping, gliding, digging, or spraying)
        // Fast fall can be used while wall clinging to cancel the wall cling
        if (fastFallAction.IsPressed() && !isGrounded && !isJumping && !isGliding && !isDigging && !isSpraying)
        {
            // If wall clinging, cancel wall cling when fast fall starts
            if (isWallClinging && !isFastFalling)
            {
                isWallClinging = false;
                wallDirection = 0;
            }
            
            // Apply fast fall effect while button is held
            if (!isFastFalling)
            {
                isFastFalling = true;
            }
            
            // Continuously apply downward velocity while fast falling
            if (rb.linearVelocity.y > -fastFallVelocity)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -fastFallVelocity);
            }
        }
        else
        {
            // Stop fast falling when button is released or conditions are no longer met (including when spraying starts)
            if (isFastFalling)
            {
                isFastFalling = false;
            }
        }
    }
    
    private void HandleSpray(Vector2 moveInput)
    {
        UpdateSprayCooldown();
        
        // Check if spray ability is unlocked
        if (!AbilityManager.IsAbilityUnlocked("Spray"))
        {
            return; // Exit early if spray is not unlocked
        }
        
        // Reset spray flag if grounded or wall clinging (allows immediate re-use)
        if (isGrounded || isWallClinging)
        {
            sprayUsedThisJump = false;
        }
        
        // Check if spray button is pressed and spray is available and not digging and not wall clinging
        // Spray is usable if: cooldown has expired AND not used this jump
        bool sprayAvailable = sprayCooldownTimer <= 0f && !sprayUsedThisJump;
        if (sprayAction.triggered && sprayAvailable && !isDigging && !isWallClinging && waterSpoutPrefab != null)
        {
            // Get separate horizontal and vertical inputs for spray direction
            float horizontal = moveAction.ReadValue<Vector2>().x;
            float vertical = verticalAction.ReadValue<float>();

            // Create combined input vector for spray
            Vector2 sprayInput = new Vector2(horizontal, vertical);

            // Determine spray direction based on input priority
            Vector2 sprayDirection = GetSprayDirection(sprayInput);

            // Spawn water spout
            SpawnWaterSpout(sprayDirection);

            // Forcefully remove horizontal velocity when spraying horizontally
            if (Mathf.Abs(sprayDirection.x) > 0.1f && Mathf.Abs(sprayDirection.y) < 0.1f)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            // Start spray duration and mark as used
            sprayTimer = sprayDuration;
            sprayCooldownTimer = sprayCooldown;
            sprayUsedThisJump = true;

            // Reset instant boost flag for new spray
            sprayInstantBoostUsed = false;

            // Set spray state
            isSpraying = true;

            // Apply spray start velocity adjustments
            float currentVerticalVelocity = rb.linearVelocity.y;
            
            // If fast falling, counter the fast fall velocity first
            if (isFastFalling && currentVerticalVelocity <= -fastFallVelocity)
            {
                currentVerticalVelocity += fastFallVelocity;
            }
            
            // Then apply spray start velocity multiplier to reduce any remaining downward velocity
            if (currentVerticalVelocity < 0)
            {
                currentVerticalVelocity *= sprayStartVelocityMultiplier;
            }
            
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentVerticalVelocity);

            // Disable conflicting movement states when starting spray
            DisableConflictingStatesForSpray();
            
            // Play spray sound effect AFTER disabling conflicting states to prevent it from being canceled
            player.PlayerSFX(4);
        }
        
        // Update spray timer and handle water spout effects
        if (isSpraying)
        {
            // Ensure conflicting states remain disabled while spraying
            EnsureSprayStateIntegrity();
            
            // Update water spout position to follow player
            if (currentWaterSpout != null)
            {
                UpdateWaterSpoutPosition();
                ApplyWaterSpoutAcceleration();
            }
            
            sprayTimer -= Time.deltaTime;
            
            // End spray if duration expires OR if spray button is released
            if (sprayTimer <= 0f || !sprayAction.IsPressed())
            {
                // Destroy water spout when spray ends
                if (currentWaterSpout != null)
                {
                    Destroy(currentWaterSpout);
                    currentWaterSpout = null;
                }
                isSpraying = false;
            }
        }
    }
    
    private void DisableConflictingStatesForSpray()
    {
        // End gliding if currently gliding
        if (isGliding)
        {
            DeactivateGlide();
        }
        
        // End fast falling if currently fast falling
        if (isFastFalling)
        {
            isFastFalling = false;
        }
        
        // End wall clinging if currently wall clinging
        if (isWallClinging)
        {
            isWallClinging = false;
            wallDirection = 0;
        }
    }
    
    private void EnsureSprayStateIntegrity()
    {
        // Continuously ensure conflicting states are disabled while spraying
        if (isGliding)
        {
            DeactivateGlide();
        }
        
        if (isFastFalling)
        {
            isFastFalling = false;
        }
    }
    
    private void HandleDig(Vector2 moveInput)
    {
        UpdateDigCooldown();
        
        // Check if dig ability is unlocked
        if (!AbilityManager.IsAbilityUnlocked("Dig"))
        {
            return; // Exit early if dig is not unlocked
        }
        
        if (CanStartDig())
        {
            StartDig(moveInput);
        }
        
        if (isDigging)
        {
            ProcessDigging();
        }
        else if (isDigPhasing)
        {
            ProcessDigPhasing();
        }
    }
    
    private void UpdateDigCooldown()
    {
        if (digCooldownTimer > 0f)
        {
            digCooldownTimer -= Time.deltaTime;
            
            // Reset digUsedThisJump when cooldown expires while grounded (allows re-use while grounded)
            if (digCooldownTimer <= 0f && isGrounded)
            {
                digUsedThisJump = false;
            }
        }
        
        // Note: Dig cooldown is reset to 0 in UpdateCoyoteTime() when grounded 
        // and in StartWallClinging() when wall clinging begins, making dig available again.
        // Cooldown only applies when dig is used while grounded.
    }
    
    private bool CanStartDig()
    {
        // Dig is usable if: not used this jump AND (not grounded OR cooldown has expired)
        bool canDig = digAction.triggered && !isSpraying && !digUsedThisJump && (!isGrounded || digCooldownTimer <= 0f);
        
        if (digAction.triggered)
        {
            Debug.Log($"Dig action triggered. Can dig: {canDig}");
            Debug.Log($"isSpraying: {isSpraying}, digUsedThisJump: {digUsedThisJump}, isGrounded: {isGrounded}, digCooldownTimer: {digCooldownTimer}");
        }
        
        return canDig;
    }
    
    private void StartDig(Vector2 moveInput)
    {
        Debug.Log("StartDig called - about to play jump SFX");
        SetDiggableCollisionEnabled(false);
        
        float horizontal = moveAction.ReadValue<Vector2>().x;
        float vertical = verticalAction.ReadValue<float>();
        Vector2 digInput = new Vector2(horizontal, vertical);
        Vector2 digDirection = GetDigDirection(digInput);
        
        rb.linearVelocity = digDirection * digForce;
        
        digTimer = digDuration;
        digUsedThisJump = true;
        
        // Only start cooldown if digging while grounded
        if (isGrounded)
        {
            digCooldownTimer = digCooldown;
        }
        
        isDigging = true;
        
        // Cancel other movement states BEFORE playing sound to prevent it from being canceled
        CancelOtherMovementStates();
        
        // Play jump sound effect as one-shot when dig begins (won't be interrupted)
        Debug.Log("Playing jump SFX for dig start using PlayOneShot");
        player.PlayerSFXOneShot(1);
        Debug.Log("Jump SFX one-shot call completed");
    }
    
    private void CancelOtherMovementStates()
    {
        if (isJumping) isJumping = false;
        if (isFastFalling) isFastFalling = false;
        if (isWallClinging)
        {
            isWallClinging = false;
            wallDirection = 0;
        }
        if (isGliding) DeactivateGlide();
    }
    
    private void ProcessDigging()
    {
        bool touchingClimbable = EnvironmentTracker.IsTouchingDiggableWall(normalCollider, glidingCollider, diggableLayer);
        
        if (touchingClimbable && !isDigPhasing)
        {
            isDigPhasing = true;
            // Start looping dig phase sound effect
            player.PlayerSFX(5);
        }
        else if (!touchingClimbable && isDigPhasing)
        {
            StartDigExit();
        }
        
        ProcessDigTimer();
    }
    
    private void StartDigExit()
    {
        isDigPhasing = false;
        isDigExiting = true;
        digUsedThisJump = false;
        digExitVelocity = rb.linearVelocity;
        digExitTimer = digTimer;
        
        // Stop dig phase sound effect when exiting dig phase
        player.StopSFX();
    }
    
    private void ProcessDigTimer()
    {
        if (isDigExiting && digExitTimer > 0f)
        {
            float progress = 1f - (digExitTimer / (digTimer > 0f ? digTimer : digDuration));
            Vector2 targetVelocity = digExitVelocity * 0.36f;
            Vector2 currentVelocity = Vector2.Lerp(digExitVelocity, targetVelocity, progress);
            
            rb.linearVelocity = currentVelocity;
            digExitTimer -= Time.deltaTime;
            
            if (digExitTimer <= 0f)
            {
                EndDig();
            }
        }
        else if (!isDigPhasing && !isDigExiting)
        {
            digTimer -= Time.deltaTime;
            if (digTimer <= 0f)
            {
                EndDig();
            }
        }
    }
    
    private void ProcessDigPhasing()
    {
        bool touchingClimbable = EnvironmentTracker.IsTouchingDiggableWall(normalCollider, glidingCollider, diggableLayer);
        
        if (!touchingClimbable)
        {
            StartDigExit();
        }
    }
    
    private void EndDig()
    {
        isDigging = false;
        isDigExiting = false;
        SetDiggableCollisionEnabled(true);
        
        // Stop dig phase sound effect when dig ends completely
        player.StopSFX();
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
        // Priority: Down > Up > Horizontal (left/right) > Facing direction (no input)
        // Use a threshold to account for input sensitivity
        float inputThreshold = 0.1f;

        // Check for down input first (highest priority)
        if (Mathf.Abs(sprayInput.y) > inputThreshold && sprayInput.y < 0)
        {
            animator.SetInteger("SprayDirection", 0);
            return new Vector2(0f, -1f); // Down
        }
        
        // Check for up input (second priority)
        if (Mathf.Abs(sprayInput.y) > inputThreshold && sprayInput.y > 0)
        {
            animator.SetInteger("SprayDirection", 1);
            return new Vector2(0f, 1f); // Up
        }
        
        // Check for horizontal input (third priority)
        if (Mathf.Abs(sprayInput.x) > inputThreshold)
        {
            animator.SetInteger("SprayDirection", 2);
            return new Vector2(sprayInput.x > 0 ? 1f : -1f, 0f);
        }

        // No valid input - use facing direction
        animator.SetInteger("SprayDirection", 2);
        return new Vector2(facingDirection, 0f);
    }
    
    private void HandleWallClinging(Vector2 moveInput)
    {
        if (ShouldPreventWallClinging()) return;
        
        if (CanStartWallClinging())
        {
            StartWallClinging();
        }
        
        if (isWallClinging)
        {
            ProcessWallClinging(moveInput);
        }
        
        // Check if we should stop wall clinging due to losing wall contact
        if (isWallClinging && !EnvironmentTracker.IsTouchingClimbableWall(rb))
        {
            isWallClinging = false;
            wallDirection = 0;
        }
    }
    
    private bool ShouldPreventWallClinging()
    {
        if (isWallKickingOff || isGrounded || isDigging)
        {
            if (isWallClinging)
            {
                isWallClinging = false;
                wallDirection = 0;
            }
            return true;
        }
        return false;
    }
    
    private bool CanStartWallClinging()
    {
        return EnvironmentTracker.IsTouchingClimbableWall(rb) && 
               !isWallClinging && 
               rb.linearVelocity.y < 0;
    }
    
    private void StartWallClinging()
    {
        if (isFastFalling)
        {
            isFastFalling = false;
        }
        
        isWallClinging = true;
        isJumping = false;
        DeactivateGlide();
        wallDirection = EnvironmentTracker.GetWallDirection(rb);
        sprayUsedThisJump = false;
        digUsedThisJump = false;
        
        // Reset spray and dig cooldowns when starting wall cling
        sprayCooldownTimer = 0f;
        digCooldownTimer = 0f;
        
        player.PlayerSFX(2);
    }
    
    private void ProcessWallClinging(Vector2 moveInput)
    {
        // Zero velocity to maintain wall cling
        rb.linearVelocity = new Vector2(0f, 0f);
        
        if (jumpAction.triggered)
        {
            ExecuteWallJump();
        }
        else if (moveInput.x != 0)
        {
            // Check if player is moving away from wall - if so, end wall clinging
            if ((wallDirection > 0 && moveInput.x < 0) || (wallDirection < 0 && moveInput.x > 0))
            {
                isWallClinging = false;
                wallDirection = 0;
            }
        }
    }
    
    private void ExecuteWallJump()
    {
        isWallClinging = false;
        isWallKickingOff = true;
        wallKickOffTimer = wallKickOffDuration;
        
        rb.linearVelocity = new Vector2(-wallDirection * moveSpeed, jumpForce);
        
        // Reverse facing direction to face the direction of the wall jump
        facingDirection = -wallDirection;
        
        isJumping = true;
        jumpTimeCounter = 0f;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        leftGroundByJumping = true;
        
        // Play jump sound effect when wall jump executes
        player.PlayerSFX(1);
    }
    
    
    private void HandleGliding(Vector2 moveInput)
    {
        bool shouldGlide = glideAction.IsPressed() && !isGrounded && !isWallClinging && !isDigging && !isSpraying && rb.linearVelocity.y < 0 && CanGlideAtCurrentHeight();
        
        if (shouldGlide)
        {
            if (!isGliding)
            {
                StartGliding(moveInput);
            }
            else
            {
                ContinueGliding(moveInput);
            }
        }
        else if (isGliding)
        {
            DeactivateGlide();
        }
    }
    
    private void StartGliding(Vector2 moveInput)
    {
        // Stop fast falling if currently fast falling
        if (isFastFalling)
        {
            isFastFalling = false;
        }
        //dd
        glidingCollider.enabled = true;
        normalCollider.enabled = false;
        isGliding = true;
        isJumping = false;
        glideTimer = 0f;
        
        // Start glide sound effect loop
        player.PlayerSFX(3);
        
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * glideVelocityMultiplier);
        }
        
        if (moveInput.x == 0)
        {
            rb.linearVelocity = new Vector2(facingDirection * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            currentGlideSpeed = rb.linearVelocity.x;
        }
    }
    
    private void ContinueGliding(Vector2 moveInput)
    {
        // Check if we should stop gliding due to height restriction
        if (!CanGlideAtCurrentHeight())
        {
            DeactivateGlide();
            return;
        }
        
        if (moveInput.x != 0)
        {
            float acceleration = glideAcceleration * moveInput.x * Time.deltaTime;
            float newVelocityX = rb.linearVelocity.x + acceleration;
            newVelocityX = Mathf.Clamp(newVelocityX, -maxGlideSpeed, maxGlideSpeed);
            
            rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
            
            if (newVelocityX > 0)
                facingDirection = 1;
            else if (newVelocityX < 0)
                facingDirection = -1;
        }
    }
    
    private bool CanGlideAtCurrentHeight()
    {            
        // Check minimum glide height - raycast downward to check for ground/climbable terrain
        Vector2 raycastStart = (Vector2)transform.position + Vector2.down * 0.1f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(raycastStart, Vector2.down, minimumGlideHeight);
        
        // Find the closest hit that has "Ground" or "Climbable" tag
        RaycastHit2D closestValidHit = new RaycastHit2D();
        float closestDistance = float.MaxValue;
        bool foundValidHit = false;
        
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && (hit.collider.CompareTag("Ground") || hit.collider.CompareTag("Climbable")))
            {
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestValidHit = hit;
                    foundValidHit = true;
                }
            }
        }
        
        // If we found a valid hit, check if we're far enough away
        if (foundValidHit)
        {
            // Add the 0.1f offset back to the distance since we started the raycast 0.1f below
            float actualDistance = closestValidHit.distance + 0.1f;
            return actualDistance >= minimumGlideHeight;
        }
        
        // No ground found within check distance, allow gliding
        return true;
    }
    
    private void HandleHorizontalMovement(Vector2 moveInput)
    {
        if (isWallClinging)
        {
            //rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else if (isWallKickingOff)
        {
            rb.linearVelocity = new Vector2(-wallDirection * moveSpeed, rb.linearVelocity.y);
        }
        else if (isGliding)
        {
            // Gliding movement handled in HandleGliding
        }
        else if (isDigging)
        {
            HandleDigMovement(moveInput);
        }
        else if (isSpraying)
        {
            UpdateFacingFromVelocity();
        }
        else
        {
            HandleNormalMovement(moveInput);
        }
    }
    
    private void HandleDigMovement(Vector2 moveInput)
    {
        if (isDigExiting)
        {
            UpdateFacingFromVelocity();
        }
        else if (isDigPhasing)
        {
            HandleDigPhasingMovement(moveInput);
        }
        else
        {
            UpdateFacingFromVelocity();
        }
    }
    
    private void HandleDigPhasingMovement(Vector2 moveInput)
    {
        float horizontal = moveInput.x;
        float vertical = verticalAction.ReadValue<float>();
        Vector2 inputDirection = new Vector2(horizontal, vertical);
        
        if (inputDirection.magnitude > 0.1f)
        {
            inputDirection = inputDirection.normalized;
            Vector2 acceleration = inputDirection * digAcceleration * Time.deltaTime;
            Vector2 newVelocity = rb.linearVelocity + acceleration;
            
            if (newVelocity.magnitude > maxDigVelocity)
            {
                newVelocity = newVelocity.normalized * maxDigVelocity;
            }
            
            rb.linearVelocity = newVelocity;
            
            if (Mathf.Abs(horizontal) > 0.1f)
            {
                facingDirection = horizontal > 0 ? 1 : -1;
            }
        }
    }
    
    private void HandleNormalMovement(Vector2 moveInput)
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        
        if (moveInput.x > 0)
            facingDirection = 1;
        else if (moveInput.x < 0)
            facingDirection = -1;

        if (!footstepPlayed && Mathf.Abs(rb.linearVelocity.x) > 0 && isGrounded)
        {
            StartCoroutine(FootstepSFX());
        }
    }
    
    private void UpdateFacingFromVelocity()
    {
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            facingDirection = rb.linearVelocity.x > 0 ? 1 : -1;
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
        if (collision.gameObject.CompareTag("Damaging") && !isInvulnerable)
        {
            StartCoroutine(DeathAnimation());
            return; // Exit early to prevent other collision logic
        }
        
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Climbable"))
        {
            isGrounded = EnvironmentTracker.IsStandingOnSurface(rb, isDigPhasing);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Only check for grounded state in Stay - damage is handled in Enter only to prevent double triggers
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
        // Check for damaging objects - only trigger if not invulnerable
        if (other.CompareTag("Damaging") && !isInvulnerable)
        {
            StartCoroutine(DeathAnimation());
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Don't check for damage in Stay to prevent double triggers
        // Damage is only handled in Enter when not invulnerable
    }
    private void DeactivateGlide()
    {
        // Stop glide sound effect immediately
        player.StopSFX();
        
        glideTimer = 0f;
        currentGlideSpeed = 0f;
        glidingCollider.enabled = false; // Disable gliding collider
        normalCollider.enabled = true; // Enable normal collider
        isGliding = false; // Set gliding state to false
    }


    private void TakeDamage()
    {   
        // Check if player should die (health is 0 or below)
        if (currentHealth <= 0)
        {
            Debug.Log("Player died - respawning at checkpoint");
            Die();
        }
        else
        {
            Debug.Log($"Player took damage but survived - resetting to last grounded position. Health: {currentHealth}/{maxHealth}");
            // Take damage but don't die - reset to last grounded position
            transform.position = lastGroundedPosition;
            rb.linearVelocity = Vector2.zero;
            SetDiggableCollisionEnabled(true);
            
            // Reset all states and timers
            ResetAllMovementStates();
            ResetAllTimers();
            
            // Reset defaults
            facingDirection = 1;
            wallDirection = 0;
            rb.gravityScale = gravityWhileFalling;
        }
    }
    
    private void Die()
    {
        // Reset health to maximum
        currentHealth = maxHealth;
        
        // Respawn at checkpoint or starting position
        Vector3 respawnPosition = hasCheckpoint ? checkpointPosition : startingPosition;
        transform.position = respawnPosition;
        rb.linearVelocity = Vector2.zero;
        SetDiggableCollisionEnabled(true);
        
        // Reset all states and timers
        ResetAllMovementStates();
        ResetAllTimers();
        
        // Reset defaults
        facingDirection = 1;
        wallDirection = 0;
        rb.gravityScale = gravityWhileFalling;
        
        // Disable movement temporarily after dying
        // StartCoroutine(DisableMovementTemporarily());
        
        Debug.Log($"Player died and respawned at: {respawnPosition}");
    }

    private IEnumerator DeathAnimation()
    {
        // Immediately set invulnerability to prevent double triggers
        isInvulnerable = true;
        
        // Stop all movement immediately
        rb.linearVelocity = Vector2.zero;
        
        // Disable movement to prevent glitchy behavior during death animation
        movementDisabled = true;
        
        // Play death particles and hide sprite
        deathParticles.Play();
        spriteRenderer.color = Color.clear;

        // Decrease health (this happens immediately, not after the animation)
        currentHealth--;
        
        Debug.Log($"Player took damage. Health: {currentHealth}/{maxHealth}");

        // Wait for the death animation to complete
        yield return new WaitForSeconds(invulnerabilityFrames);

        // Re-enable sprite visibility
        spriteRenderer.color = Color.white;
        
        // Process damage/death logic
        TakeDamage();
        
        // Re-enable movement after a brief delay to prevent immediate re-collision
        yield return new WaitForSeconds(0.1f);
        movementDisabled = false;
        
        // Remove invulnerability last
        isInvulnerable = false;
        
        Debug.Log("Death animation complete, player is now vulnerable again");
    } 
    
    private void ResetAllMovementStates()
    {
        isGrounded = false;
        isJumping = false;
        DeactivateGlide();
        isWallClinging = false;
        isFastFalling = false;
        isWallKickingOff = false;
        isSpraying = false;
        isDigging = false;
        isDigPhasing = false;
        isDigExiting = false;
        leftGroundByJumping = false;
        wasGroundedLastFrame = false;
        sprayUsedThisJump = false;
        digUsedThisJump = false;
        
        // Reset spray tracking
        sprayInstantBoostUsed = false;
        
        // Clean up water spout
        if (currentWaterSpout != null)
        {
            Destroy(currentWaterSpout);
            currentWaterSpout = null;
        }
    }
    
    private void ResetAllTimers()
    {
        jumpTimeCounter = 0f;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        sprayTimer = 0f;
        sprayCooldownTimer = 0f;
        wallKickOffTimer = 0f;
        digTimer = 0f;
        digCooldownTimer = 0f;
        digExitTimer = 0f;
    }

    private void HandleAnimations()
    {
        // Always update velocity for movement animations (but it will only be used if no higher priority state is active)
        animator.SetFloat("Velocity", Mathf.Abs(rb.linearVelocity.x));
        
        // Animation priority system (highest to lowest priority):
        // 1. Digging (highest priority - overrides everything)
        // 2. Spraying (high priority - overrides movement states)
        // 3. Wall Clinging (medium-high priority)
        // 4. Gliding (medium priority)
        // 5. Jumping (lower priority)
        // 6. Movement/Idle (lowest priority - handled by Velocity parameter and isGrounded)
        
        // Reset all state booleans first
        animator.SetBool("isDigging", false);
        animator.SetBool("isSpraying", false);
        animator.SetBool("isWallClinging", false);
        animator.SetBool("isGliding", false);
        animator.SetBool("isJumping", false);
        animator.SetBool("isGrounded", false);
        
        // Set the highest priority active state
        if (isDigging)
        {
            // Digging has highest priority - overrides everything
            animator.SetBool("isDigging", true);
        }
        else if (isSpraying)
        {
            // Spraying has high priority - overrides movement states
            // Do NOT set isGrounded when spraying to prevent idle/movement animations
            animator.SetBool("isSpraying", true);
        }
        else if (isWallClinging)
        {
            // Wall clinging has medium-high priority
            animator.SetBool("isWallClinging", true);
        }
        else if (isGliding)
        {
            // Gliding has medium priority
            animator.SetBool("isGliding", true);
        }
        else if (isJumping)
        {
            // Jumping has lower priority
            animator.SetBool("isJumping", true);
        }
        else
        {
            // Only set grounded state if no higher priority states are active
            // This ensures movement/idle animations only play when appropriate
            animator.SetBool("isGrounded", isGrounded);
        }
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
        // Update and log the last grounded position only when firmly grounded and stable
        if (IsFirmlyGrounded() && IsSafeFromDamagingObjects())
        {
            lastGroundedPosition = transform.position;
        }
    }
    
    private bool IsFirmlyGrounded()
    {
        // Check multiple conditions to ensure player is firmly grounded:
        // 1. Basic grounded check
        if (!isGrounded)
            return false;
            
        // 2. Player must not be in transitional movement states
        if (isJumping || isFastFalling || isWallClinging || isDigging || isDigPhasing || isDigExiting)
            return false;
            
        // 3. Vertical velocity should be near zero or downward (not moving upward)
        if (rb.linearVelocity.y > 0.1f)
            return false;
            
        // 4. Player should have been grounded for at least a brief moment (stability check)
        if (!wasGroundedLastFrame)
            return false;
            
        // 5. Check if the center of the player's collider is in contact with ground
        return HasStrongGroundContact();
    }
    
    private bool HasStrongGroundContact()
    {
        // Check if the center of the player's collider is in direct contact with ground
        Vector2 playerCenter = (Vector2)transform.position;
        
        // Get the bounds of the player's active collider
        Bounds colliderBounds = normalCollider.enabled ? normalCollider.bounds : glidingCollider.bounds;
        
        // Calculate the bottom center point of the player's collider
        Vector2 bottomCenter = new Vector2(playerCenter.x, colliderBounds.min.y);
        
        // Perform a small raycast downward from the bottom center to check for ground contact
        float rayDistance = 0.1f; // Small distance to check for immediate ground contact
        RaycastHit2D hit = Physics2D.Raycast(bottomCenter, Vector2.down, rayDistance);
        
        if (hit.collider != null)
        {
            // Check if this is a ground surface
            if (EnvironmentTracker.IsGroundSurface(hit.collider))
            {
                // Check if the normal is pointing upward (indicating standing on top)
                if (EnvironmentTracker.IsUpwardFacing(hit.normal))
                {
                    Debug.Log("Player center is firmly grounded");
                    return true;
                }
            }
        }
        
        // Additional check: use a small overlap circle at the bottom center
        // This catches cases where the raycast might miss due to collider edge alignment
        float overlapRadius = 0.05f;
        Vector2 overlapCenter = new Vector2(playerCenter.x, colliderBounds.min.y - overlapRadius);
        Collider2D[] overlappingColliders = Physics2D.OverlapCircleAll(overlapCenter, overlapRadius);
        
        foreach (Collider2D collider in overlappingColliders)
        {
            // Skip the player's own colliders
            if (collider == normalCollider || collider == glidingCollider)
                continue;
                
            if (EnvironmentTracker.IsGroundSurface(collider))
            {
                // For overlap detection, we assume the player is standing on top if the overlap is at the bottom
                Debug.Log("Player center overlap detected with ground");
                return true;
            }
        }
        
        Debug.Log("Player center is not firmly grounded");
        return false;
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

    private void UpdateGroundedState()
    {
        isGrounded = EnvironmentTracker.IsStandingOnSurface(rb, isDigPhasing);
    }
    
    private void UpdateCoyoteTime()
    {
        if (isGrounded)
        {
            if (!wasGroundedLastFrame)
            {
                if (isFastFalling)
                {
                    isFastFalling = false;
                }
                coyoteTimer = coyoteTime;
                leftGroundByJumping = false;
                sprayUsedThisJump = false;
                digUsedThisJump = false;
                
                // Reset spray and dig cooldowns when becoming grounded
                sprayCooldownTimer = 0f;
                digCooldownTimer = 0f;
            }
        }
        else
        {
            if (wasGroundedLastFrame && !leftGroundByJumping)
            {
                coyoteTimer = coyoteTime;
            }
            if (!leftGroundByJumping)
            {
                coyoteTimer -= Time.deltaTime;
            }
        }
        wasGroundedLastFrame = isGrounded;
    }
    
    private void ExecuteBufferedJump()
    {
        if (jumpBufferTimer > 0f && (isGrounded || (coyoteTimer > 0f && !leftGroundByJumping)))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            leftGroundByJumping = true;
            isJumping = true;
            jumpTimeCounter = 0f;
            
            // Play jump sound effect when jump actually executes
            player.PlayerSFX(1);
        }
    }
    
    private void HandleVariableJump()
    {
        if (isJumping && !isGliding && !isWallClinging && !isDigging && !isSpraying)
        {
            if (jumpAction.IsPressed() && jumpTimeCounter < maxJumpTime)
            {
                jumpTimeCounter += Time.deltaTime;
                float jumpMultiplier = 1f - (jumpTimeCounter / maxJumpTime);
                float newVerticalVelocity = rb.linearVelocity.y + (jumpForce * jumpMultiplier * Time.deltaTime);

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
                if (!jumpAction.IsPressed() && rb.linearVelocity.y > 0 && !isSpraying)
                {
                    float newVerticalVelocity = rb.linearVelocity.y * jumpReleaseMultiplier;

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

        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            isJumping = false;
        }
    }
    
    private void UpdateWallKickOff()
    {
        if (isWallKickingOff)
        {
            wallKickOffTimer -= Time.deltaTime;
            if (wallKickOffTimer <= 0f)
            {
                isWallKickingOff = false;
                wallDirection = 0;
            }
        }
    }
    
    private void UpdateSpriteFlipping()
    {
        // Use facingDirection field instead of velocity for more consistent facing
        if (facingDirection > 0)
        {
            // Facing right - normal rotation
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (facingDirection < 0)
        {
            // Facing left - rotate 180 degrees on Y axis
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }

    private void SpawnWaterSpout(Vector2 direction)
    {
        // Store direction for later use
        spoutDirection = direction;
        
        // Determine offset distance based on spray direction
        float offsetDistance;
        if (Mathf.Abs(direction.x) > 0.1f && Mathf.Abs(direction.y) < 0.1f)
        {
            // Horizontal spray (left or right)
            offsetDistance = horizontalSprayOffset;
        }
        else
        {
            // Vertical spray (up or down) or default
            offsetDistance = verticalSprayOffset;
        }
        
        // Calculate spawn position using direction-specific offset
        Vector3 spawnOffset = (Vector3)direction * offsetDistance;
        Vector3 spawnPosition = transform.position + spawnOffset;
        
        // Store the offset for position tracking
        spoutOffset = spawnOffset;
        
        // Calculate rotation to point in the spray direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        // Spawn the water spout
        currentWaterSpout = Instantiate(waterSpoutPrefab, spawnPosition, rotation);
        
        // Try to set up the water spout component if it exists (using reflection for now)
        var spoutComponent = currentWaterSpout.GetComponent("WaterSpout");
        if (spoutComponent != null)
        {
            // Use reflection to call Initialize method
            var method = spoutComponent.GetType().GetMethod("Initialize");
            method?.Invoke(spoutComponent, new object[] { this, direction });
        }
    }
    
    private void UpdateWaterSpoutPosition()
    {
        if (currentWaterSpout != null)
        {
            // Update the water spout position to maintain the same relative offset from the player
            Vector3 newSpoutPosition = transform.position + spoutOffset;
            currentWaterSpout.transform.position = newSpoutPosition;
        }
    }
    
    private void ApplyWaterSpoutAcceleration()
    {
        // Check if water spout is touching something other than the player
        if (currentWaterSpout != null)
        {
            // Try to get the WaterSpout component using string name first (for Unity compilation compatibility)
            var spoutComponent = currentWaterSpout.GetComponent("WaterSpout");
            bool isTouching = false;
            
            if (spoutComponent != null)
            {
                // Use reflection to call IsTouchingSomething method
                // This call is important even when grounded as it handles fire destruction
                var method = spoutComponent.GetType().GetMethod("IsTouchingSomething");
                if (method != null)
                {
                    isTouching = (bool)(method.Invoke(spoutComponent, null) ?? false);
                }
            }
            
            // Only apply movement effects if player is NOT grounded
            if (isTouching)
            {
                // Apply instant boost once when spout first makes contact
                if (!sprayInstantBoostUsed)
                {
                    ApplySprayInstantBoost();
                    sprayInstantBoostUsed = true;
                }
                
                // Apply acceleration in opposite direction of where the spout was placed
                Vector2 accelerationDirection = -spoutDirection;
                
                // Check current velocity in the acceleration direction to prevent exceeding max velocity
                float currentVelocityInDirection = Vector2.Dot(rb.linearVelocity, accelerationDirection);
                
                // Only apply force if we haven't reached the maximum velocity in that direction
                if (currentVelocityInDirection < maxSprayVelocity)
                {
                    // Apply continuous force while touching
                    Vector2 forceToApply = accelerationDirection * sprayAcceleration;
                    
                    // Use Force for continuous application over time
                    rb.AddForce(forceToApply, ForceMode2D.Force);
                    
                    // Clamp the velocity to prevent exceeding the maximum
                    Vector2 newVelocity = rb.linearVelocity;
                    float newVelocityInDirection = Vector2.Dot(newVelocity, accelerationDirection);
                    
                    if (newVelocityInDirection > maxSprayVelocity)
                    {
                        // Calculate how much to reduce the velocity in the acceleration direction
                        Vector2 excessVelocity = accelerationDirection * (newVelocityInDirection - maxSprayVelocity);
                        newVelocity -= excessVelocity;
                        rb.linearVelocity = newVelocity;
                    }
                }
            }
            else if (!isGrounded)
            {
                // Spout is not touching anything - apply horizontal deceleration if spraying horizontally (only when airborne)
                ApplyHorizontalSprayDeceleration();
            }
        }
    }
    
    private void ApplyHorizontalSprayDeceleration()
    {
        // Only apply deceleration when spraying in horizontal directions (left or right)
        if (Mathf.Abs(spoutDirection.x) > 0.1f && Mathf.Abs(spoutDirection.y) < 0.1f)
        {
            // Apply deceleration to horizontal velocity
            float currentHorizontalVelocity = rb.linearVelocity.x;
            
            if (Mathf.Abs(currentHorizontalVelocity) > 0.1f)
            {
                // Calculate deceleration direction (opposite to current horizontal movement)
                float decelerationDirection = -Mathf.Sign(currentHorizontalVelocity);
                
                // Apply deceleration force
                float decelerationForce = decelerationDirection * horizontalSprayDeceleration * Time.deltaTime;
                float newHorizontalVelocity = currentHorizontalVelocity + decelerationForce;
                
                // Prevent overshooting (don't reverse direction due to deceleration)
                if (Mathf.Sign(newHorizontalVelocity) != Mathf.Sign(currentHorizontalVelocity))
                {
                    newHorizontalVelocity = 0f;
                }
                
                // Apply the new horizontal velocity
                rb.linearVelocity = new Vector2(newHorizontalVelocity, rb.linearVelocity.y);
            }
        }
    }

    private void ApplySprayInstantBoost()
    {
        // Apply instant velocity boost in opposite direction of where the spout was placed
        Vector2 boostDirection = -spoutDirection;
        Vector2 instantBoost = boostDirection * sprayInstantBoost;
        
        // Add the instant boost to current velocity
        rb.linearVelocity += instantBoost;
        
        // Clamp the velocity to prevent exceeding the maximum in the boost direction
        Vector2 newVelocity = rb.linearVelocity;
        float velocityInBoostDirection = Vector2.Dot(newVelocity, boostDirection);
        
        if (velocityInBoostDirection > maxSprayVelocity)
        {
            // Calculate how much to reduce the velocity in the boost direction
            Vector2 excessVelocity = boostDirection * (velocityInBoostDirection - maxSprayVelocity);
            newVelocity -= excessVelocity;
            rb.linearVelocity = newVelocity;
        }
    }
    
    private void UpdateSprayCooldown()
    {
        if (sprayCooldownTimer > 0f)
        {
            sprayCooldownTimer -= Time.deltaTime;
        }
        
        // Note: Spray cooldown is reset in UpdateCoyoteTime() when grounded 
        // and in StartWallClinging() when wall clinging begins
    }
    
    // Movement control for dialogue system
    private bool movementDisabled = false;

    public void DisableMovement()
    {
        movementDisabled = true;
        
        // Stop all current movement
        rb.linearVelocity = Vector2.zero;
        
        // Reset all movement states
        isJumping = false;
        DeactivateGlide();
        isFastFalling = false;
        isWallClinging = false;
        
        // Properly end digging states and restore collision
        if (isDigging || isDigPhasing || isDigExiting)
        {
            isDigging = false;
            isDigPhasing = false;
            isDigExiting = false;
            SetDiggableCollisionEnabled(true);
        }
        
        // Stop spraying
        if (isSpraying)
        {
            isSpraying = false;
            if (currentWaterSpout != null)
            {
                Destroy(currentWaterSpout);
                currentWaterSpout = null;
            }
        }
        
        // Reset timers
        jumpTimeCounter = 0f;
        jumpBufferTimer = 0f;
        sprayTimer = 0f;
        digTimer = 0f;
        digExitTimer = 0f;
        wallDirection = 0;
        
        Debug.Log("Player movement disabled");
    }

    public void EnableMovement()
    {
        movementDisabled = false;
        
        // Ensure diggable collision is properly reset when movement is re-enabled
        SetDiggableCollisionEnabled(true);
        
        Debug.Log("Player movement enabled");
    }

    public bool IsMovementDisabled()
    {
        return movementDisabled;
    }
    

    private IEnumerator DisableMovementTemporarily()
    {
        DisableMovement();
        Debug.Log($"Movement disabled for {respawnMovementDisableDuration} seconds after respawn");
        yield return new WaitForSeconds(respawnMovementDisableDuration);
        EnableMovement();
        Debug.Log("Movement re-enabled after respawn");
    }
    
    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
        hasCheckpoint = true;
        Debug.Log($"Checkpoint set at position: {position}");
    }
    
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        Debug.Log($"Health restored to full: {currentHealth}/{maxHealth}");
    }
    
}
