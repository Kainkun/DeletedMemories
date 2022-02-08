using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class PlatformerController : MonoBehaviour
{
    #region ----------------Variables----------------

    #region ----------------Movement----------------
    [Header("Movement")]
    public float maxRunSpeed = 15;
    public float maxSpeed = 25;
    public float timeToMaxSpeed = 0.2f;
    public float timeToStop = 0.1f;
    public float inAirAccelerationMultiplier = 0.5f;
    public float inAirDecelerationMultiplier = 0.5f;
    private Vector2 _moveInputDirection;
    public AnimationCurve accelerationCurve = new AnimationCurve(new Keyframe(0, 0,0,1, 0,0.25f), new Keyframe(1, 1, 1, 0, 0.25f, 0));
    public AnimationCurve decelerationCurve = new AnimationCurve(new Keyframe(-1, 1,0,-1, 0,0.25f), new Keyframe(0, 0, -1, 0, 0.25f, 0));
    public AnimationCurve inverseAccelerationCurve;
    public AnimationCurve inverseDecelerationCurve;
    private Vector2 _velocity;
    private Vector2 _platformDelta;
    private Vector2 _lastPlatformVelocity;
    private Vector2 _position;
    #endregion

    #region ----------------Jumping----------------
    [Header("Jumping")]
    public float maxJumpHeight = 5;
    public float timeToJumpApex = 0.4f;
    public int maxJumps = 2;
    private int _availableJumps;
    private bool _fastFall;
    public float gravityMultiplier = 2;
    public float maxFallSpeed = -50;

    public float coyoteTime = 0.1f;
    private bool _isCoyoteTime;
    private float _timeSinceLeftGround;

    public float jumpBufferTime = 0.1f;
    private bool _waitingForJump;
    private float _timeSinceJumpPress;

    public float jumpCooldownTime = 0.1f; //JUMP MUST ESCAPE GROUNDED BOXCAST BEFORE COOLDOWN FINISHES
    private bool _jumpInCooldown;
    private float _timeSinceLastJump;

    private bool _inAirFromJumping;
    private bool _inAirFromFalling;
    
    private bool _jumpButtonHolding;
    #endregion

    #region ----------------Physics----------------
    [Header("Physics")]
    public float groundCheckThickness = 0.1f;
    private LayerMask _traversableMask;
    private LayerMask _cornerCorrectionMask;
    private Rigidbody2D _rb;
    private BoxCollider2D _boxCollider;
    private Vector2 _size;
    private float _halfWidth;
    private float _halfHeight;
    private Vector2 _groundCheckSize;
    private float _timeOnGround;
    [Range(0,0.99f)]
    public float verticalCornerCorrectionWidthPercent = 0.5f;
    private float _verticalCornerCorrectionWidth;
    [Range(0,0.99f)]
    public float horizontalCornerCorrectionHeightPercent = 0.2f;
    private float _horizontalCornerCorrectionHeight;
    private bool _isGrounded;
    private bool _wasGrounded;
    private float _groundedDistance;
    #endregion

    #region ----------------Effects----------------
    [Header("Effects")]
    public GameObject airJumpParticles;
    public GameObject groundJumpParticles;
    public GameObject landParticles;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    #endregion

    #region ----------------Audio----------------
    [Header("Audio")]
    public AudioClip[] groundJumpSounds;
    public AudioClip[] airJumpSounds;
    public AudioClip[] landSounds;
    private AudioSource _audioSource;
    #endregion

    #region ----------------Input----------------
    [Header("Input")]
    public UnityEvent onJump;
    public UnityEvent onGroundJump;
    public UnityEvent onAirJump;
    public UnityEvent onLeaveGround;
    public UnityEvent onLand;
    #endregion

    #region ----------------Other----------------
    [Header("Other")]
    public Transform xpostrail;
    public Transform xvelrail;
    #endregion
    #endregion

    private void OnValidate()
    {
        GetComponents();
        SetStartingVariables();
    }

    private void Awake()
    {
        GetComponents();
        SetStartingVariables();
        AddListeners();
    }
    
    #region ----------------Awake Functions----------------
    void GetComponents()
    {
        _rb = GetComponent<Rigidbody2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _audioSource = GetComponent<AudioSource>();
        _animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void SetStartingVariables()
    {
        _size = _boxCollider.size;
        _halfWidth = _size.x / 2;
        _halfHeight = _size.y / 2;
        _groundCheckSize = new Vector2(_size.x, 0.001f);
        _verticalCornerCorrectionWidth = _halfWidth * verticalCornerCorrectionWidthPercent;
        _horizontalCornerCorrectionHeight = _size.y * horizontalCornerCorrectionHeightPercent;
        
        inverseAccelerationCurve = AnimCurveUtils.InverseIncreasingCurve(accelerationCurve);
        inverseDecelerationCurve = AnimCurveUtils.InverseDecreasingCurve(decelerationCurve);
        
        _traversableMask = LayerMask.GetMask("Default", "Platform");
        _cornerCorrectionMask = LayerMask.GetMask("Default");

        _availableJumps = maxJumps;
        _timeSinceJumpPress = Mathf.Infinity;
        _timeSinceLastJump = Mathf.Infinity;
        _fastFall = true;

        if (CheckIfGrounded())
        {
            _wasGrounded = true;
            _timeOnGround = Mathf.Infinity;
        }
        else
        {
            _timeSinceLeftGround = Mathf.Infinity;
        }
    }

    void AddListeners()
    {
        onLeaveGround.AddListener(() => _animator.Play("Stretch"));
        onGroundJump.AddListener(() => _audioSource.PlayOneShot(groundJumpSounds[Random.Range(0,groundJumpSounds.Length)], 0.4f));
        onGroundJump.AddListener(() => Instantiate(groundJumpParticles, transform));
        onAirJump.AddListener(() => _audioSource.PlayOneShot(airJumpSounds[Random.Range(0,airJumpSounds.Length)], 0.6f));
        onAirJump.AddListener(() => Instantiate(airJumpParticles, transform));
        onLand.AddListener(() => _audioSource.PlayOneShot(landSounds[Random.Range(0,landSounds.Length)]));
        onLand.AddListener(() => Instantiate(landParticles, transform.position - new Vector3(0, _halfHeight, 0), quaternion.identity));
        onLand.AddListener(() => _animator.Play("Squash"));
    }
    #endregion


    private void Start()
    {
        InputManager.Get().Jump += HandleJump;
        InputManager.Get().Move += HandleMove;
        InputManager.Get().Look += HandleLook;
    }

    private void OnDestroy()
    {
        if (GameManager.applicationIsQuitting)
            return;
        InputManager.Get().Jump -= HandleJump;
        InputManager.Get().Move -= HandleMove;
        InputManager.Get().Look -= HandleLook;
    }
    
    #region ----------------Handle Functions----------------
    private void HandleMove(Vector2 value)
    {
        _moveInputDirection = value;
    }

    private void HandleLook(Vector2 value)
    {
        //print(value);
    }


    private void HandleJump(float value)
    {
        if (value > 0)
        {
            _jumpButtonHolding = true;
            _timeSinceJumpPress = 0;
        }
        else
        {
            _jumpButtonHolding = false;
            _fastFall = true;
        }
    }
    #endregion

    
    private void FixedUpdate()
    {
        _velocity = _rb.velocity;
        _position = (Vector2)transform.position;

        RaycastHit2D hit;
        bool standingOnRigidBody = CheckIfGrounded(out hit) && hit.rigidbody;
        bool standingOnMovingKinematic = false;
        MovingKinematic mk = null;
        
        if(standingOnRigidBody)
        {
            mk = hit.rigidbody.GetComponent<MovingKinematic>();
            if (mk)
                standingOnMovingKinematic = true;
        }
        
        if(standingOnMovingKinematic)
        {
            _platformDelta = mk.delta;
            _lastPlatformVelocity = mk.velocity;
        }
        else
        {
            _platformDelta = Vector2.zero;
        }
        _position += _platformDelta;
        
        JumpingUpdate();
        GravityUpdate();
        MovementUpdate();
        
        CornerCorrectionUpdate();
        SnapToGroundUpdate();
        
        _rb.velocity = _velocity;
        transform.position = _position;
        
        DebugTrailsUpdate();
    }
    
    #region ----------------FixedUpdate Functions----------------

    private bool CheckIfGrounded()
    {
        RaycastHit2D hit;
        return CheckIfGrounded(out hit);
    }
    private bool CheckIfGrounded(out RaycastHit2D hit)
    {
        _wasGrounded = _isGrounded;

        hit = Physics2D.BoxCast((Vector2)_position + (Vector2.down * _halfHeight), _groundCheckSize, 0, Vector2.down, groundCheckThickness, _traversableMask);

        if (hit && (_wasGrounded || _velocity.y <= 0))
        {
            _isGrounded = true;
            _groundedDistance = hit.distance;
            _timeOnGround += Time.fixedDeltaTime;
            return true;
        }
        else
        {
            _isGrounded = false;
            _groundedDistance = Mathf.Infinity;
            return false;
        }
    }
    
    private void JumpingUpdate()
    {
        //first frame landing on ground
        if (!_wasGrounded && _isGrounded)
        {
            _fastFall = true;
            _timeSinceLeftGround = 0;
            _inAirFromJumping = false;
            _inAirFromFalling = false;
            _availableJumps = maxJumps;
            onLand?.Invoke();
        }

        //first frame leaving ground
        if (_wasGrounded && !_isGrounded)
        {
            _timeOnGround = 0;
            _availableJumps--;

            if (_jumpInCooldown)
                _inAirFromJumping = true;
            else
                _inAirFromFalling = true;

            print(_lastPlatformVelocity);
            _velocity += _lastPlatformVelocity;
            _lastPlatformVelocity = Vector2.zero;

            onLeaveGround?.Invoke();
        }

        if (_waitingForJump && !_jumpInCooldown)
        {
            if (_isGrounded || _isCoyoteTime)
                GroundJump();
            else if (_availableJumps > 0)
                AirJump();
        }

        if (_inAirFromFalling)
        {
            _timeSinceLeftGround += Time.fixedDeltaTime;
            _isCoyoteTime = _timeSinceLeftGround < coyoteTime;
        }
        else
        {
            _isCoyoteTime = false;
        }

        _timeSinceJumpPress += Time.fixedDeltaTime;
        _waitingForJump = _timeSinceJumpPress < jumpBufferTime;

        _timeSinceLastJump += Time.fixedDeltaTime;
        _jumpInCooldown = _timeSinceLastJump < jumpCooldownTime;
    }

    private void GravityUpdate()
    {
        //Gravity
        if (_velocity.y < 0)
            _fastFall = true;
        float gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2) * Time.fixedDeltaTime;
        if (_fastFall)
            gravity *= gravityMultiplier;
        if (!_isGrounded)
            _velocity.y += gravity;
        else if(!_jumpInCooldown)
            _velocity.y = 0;
        _velocity.y = Mathf.Max(_velocity.y, maxFallSpeed);
    }

    private void MovementUpdate()
    {
        // if (Mathf.Abs(_velocity.x) > maxRunSpeed)
        // {
        //     float percentMaxSpeed = Mathf.Abs(_velocity.x) / maxSpeed;
        //     
        //     if (_isGrounded)
        //         percentMaxSpeed = Mathf.Clamp01(inverseDecelerationCurve.Evaluate(-percentMaxSpeed) - ((1 / timeToStop) * Time.fixedDeltaTime));
        //     else
        //         percentMaxSpeed = Mathf.Clamp01(inverseDecelerationCurve.Evaluate(-percentMaxSpeed) - ((1 / timeToStop) * Time.fixedDeltaTime) * 0.1f);
        //     
        //     _velocity.x = maxSpeed * decelerationCurve.Evaluate(-percentMaxSpeed) * Mathf.Sign(_velocity.x);
        //     return;
        // }
        
        //Movement
        float percentSpeed = Mathf.Abs(_velocity.x) / maxRunSpeed;
        bool a = _moveInputDirection.x < -0.01f && _velocity.x <= 0.1f;
        bool b = _moveInputDirection.x > 0.01f && _velocity.x >= -0.1f;
        if (a || b) //accelerate
        {
            if (_isGrounded)
                percentSpeed = Mathf.Clamp01(inverseAccelerationCurve.Evaluate(percentSpeed) + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime));
            else
                percentSpeed = Mathf.Clamp01(inverseAccelerationCurve.Evaluate(percentSpeed) + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime) * inAirAccelerationMultiplier);
            
            _velocity.x = maxRunSpeed * accelerationCurve.Evaluate(percentSpeed) * Mathf.Sign(_moveInputDirection.x);
        }
        else //decelerate
        {
            if (_isGrounded)
                percentSpeed = Mathf.Clamp01(inverseDecelerationCurve.Evaluate(-percentSpeed) - ((1 / timeToStop) * Time.fixedDeltaTime));
            else
                percentSpeed = Mathf.Clamp01(inverseDecelerationCurve.Evaluate(-percentSpeed) - ((1 / timeToStop) * Time.fixedDeltaTime) * inAirDecelerationMultiplier);
            
            _velocity.x = maxRunSpeed * decelerationCurve.Evaluate(-percentSpeed) * Mathf.Sign(_velocity.x);
        }
    }

    private void CornerCorrectionUpdate()
    {
        //Vertical Corner Correction
        if(_velocity.y > 0)
        {
            Vector2 rightOrigin = (Vector2) _position + new Vector2(_halfWidth, _halfHeight);
            Vector2 leftOrigin = (Vector2) _position + new Vector2(-_halfWidth, _halfHeight);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.up, _velocity.y * Time.fixedDeltaTime * 2, _cornerCorrectionMask);
            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.up, _velocity.y * Time.fixedDeltaTime * 2, _cornerCorrectionMask);

            if (leftHit && !rightHit)
            {
                RaycastHit2D leftHitDist = Physics2D.Raycast(new Vector2(_position.x, leftHit.point.y + 0.01f), Vector2.left, _halfWidth, _cornerCorrectionMask);
                if (leftHitDist && (_halfWidth - leftHitDist.distance) <= _verticalCornerCorrectionWidth)
                    _position += Vector2.right * ((_halfWidth - leftHitDist.distance) + 0.05f);
            }
            else if (rightHit && !leftHit)
            {
                RaycastHit2D rightHitDist = Physics2D.Raycast(new Vector2(_position.x, rightHit.point.y + 0.01f), Vector2.right, _halfWidth, _cornerCorrectionMask);
                if (rightHitDist && (_halfWidth - rightHitDist.distance) <= _verticalCornerCorrectionWidth)
                    _position += Vector2.left * ((_halfWidth - rightHitDist.distance) + 0.05f);
            }
        }

        //Horizontal Corner Correction
        if (_velocity.y > 0)
            return;
        if (_velocity.x > 0)
        {
            Vector2 rightOrigin = (Vector2) _position + new Vector2(_halfWidth, -_halfHeight);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.right, _velocity.x * Time.fixedDeltaTime * 2, _traversableMask);

            if (rightHit)
            {
                RaycastHit2D rightHitDist = Physics2D.Raycast(new Vector2(rightHit.point.x + 0.01f, _position.y + _halfHeight), Vector2.down, _size.y, _traversableMask);
                if (rightHitDist && (_size.y - rightHitDist.distance) <= _horizontalCornerCorrectionHeight)
                    _position += new Vector2(0.05f, ((_size.y - rightHitDist.distance) + 0.05f));
            }
        }
        else if (_velocity.x < 0)
        {
            Vector2 leftOrigin = (Vector2) _position + new Vector2(-_halfWidth, -_halfHeight);
            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.left, -_velocity.x * Time.fixedDeltaTime * 2, _traversableMask);

            if (leftHit)
            {
                RaycastHit2D leftHitDist = Physics2D.Raycast(new Vector2(leftHit.point.x - 0.05f, _position.y + _halfHeight), Vector2.down, _size.y, _traversableMask);
                if (leftHitDist && (_size.y - leftHitDist.distance) <= _horizontalCornerCorrectionHeight)
                    _position += new Vector2(-0.05f, ((_size.y - leftHitDist.distance) + 0.05f));
            }
        }
    }

    private void SnapToGroundUpdate()
    {
        //Snap To Ground
        if (!_jumpInCooldown && CheckIfGrounded())
            if(_groundedDistance < Mathf.Infinity)
                _position += new Vector2(0, -_groundedDistance + 0.001f);
    }

    private void DebugTrailsUpdate()
    {
        //Debug trails
        Vector2 v = xpostrail.position;
        v.x += Time.fixedDeltaTime;
        v.y = transform.position.x;
        xpostrail.position = v;
        v = xvelrail.position;
        v.x += Time.fixedDeltaTime;
        v.y = _rb.velocity.x;
        xvelrail.position = v;
    }
    #endregion

    #region ----------------Jump Functions--------
    private void Jump()
    {
        _velocity.y = (2 * maxJumpHeight) / timeToJumpApex;

        _fastFall = !_jumpButtonHolding;
        _inAirFromJumping = true;
        _inAirFromFalling = false;
        _timeSinceLastJump = 0;
        onJump?.Invoke();
    }

    private void GroundJump()
    {
        Jump();
        onGroundJump?.Invoke();
    }

    private void AirJump()
    {
        _availableJumps--;
        Jump();
        onAirJump?.Invoke();
    }
    #endregion
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            _rb.velocity = new Vector2(10, 20f);
        
        Gizmos.color = Color.red;
        
        var position = transform.position;
        var velocity = _rb.velocity;

        if (_rb.velocity.y > 0)
        {
            Gizmos.DrawWireCube(position + new Vector3(_halfWidth - _verticalCornerCorrectionWidth / 2, _halfHeight + velocity.y * Time.fixedDeltaTime, 0), new Vector3(_verticalCornerCorrectionWidth, velocity.y * Time.fixedDeltaTime * 2, 0));
            Gizmos.DrawWireCube(position + new Vector3(-_halfWidth + _verticalCornerCorrectionWidth / 2, _halfHeight + velocity.y * Time.fixedDeltaTime, 0), new Vector3(_verticalCornerCorrectionWidth, velocity.y * Time.fixedDeltaTime * 2, 0));
        }

        if(velocity.x > 0)
            Gizmos.DrawWireCube(position + new Vector3(_halfWidth + velocity.x * Time.fixedDeltaTime, -_size.y / 2 + _horizontalCornerCorrectionHeight / 2, 0), new Vector3(velocity.x * Time.fixedDeltaTime * 2, _horizontalCornerCorrectionHeight, 0));
        else if(velocity.x < 0)
            Gizmos.DrawWireCube(position + new Vector3(-_halfWidth + velocity.x * Time.fixedDeltaTime, -_size.y / 2 + _horizontalCornerCorrectionHeight / 2, 0), new Vector3(velocity.x * Time.fixedDeltaTime * 2, _horizontalCornerCorrectionHeight, 0));

        Gizmos.DrawWireCube(position + new Vector3(0, -_halfHeight - (groundCheckThickness / 2), 0), new Vector3(_groundCheckSize.x, groundCheckThickness, 0));
    }
}