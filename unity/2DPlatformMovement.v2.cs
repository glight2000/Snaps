using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UIElements;

namespace CIGJ
{
    public class PlayerMovement : MonoBehaviour
    {
        private float _jumpSpeed = 7;

        public float JumpSpeed
        {
            get => _jumpSpeed;
            set => _jumpSpeed = value;
        }

        private float _moveSpeed = 0;

        public float MoveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = value;
        }

        private bool _isConstMove;

        public bool IsConstMove
        {
            get => _isConstMove;
            set
            {
                _isConstMove = value;
                _animator.SetBool(IsIdle, !_isConstMove);
            }
        }

        private bool _isChasing;

        public bool IsChasing
        {
            get => _isChasing;
            set => _isChasing = value;
        }

        private float _chasingRate = 1;

        public float ChasingRate
        {
            get => _chasingRate;
            set => _chasingRate = value;
        }

        private SpriteRenderer _playerRenderer;

        private const float _FallMultiplier = 2.5f;
        private const float _LowJumpMultiplier = 2f;

        private Animator _animator;
        private Rigidbody2D _rigidbody;
        private Collider2D _collider;

        private bool _isJumpPressed;
        private bool _isJumpHandled;
        private int _jumpCount;
        private bool _isLeftBand;
        private bool _isTopBand;
        private bool _isRightBand;
        private bool _isBottomBand;

        private void OnEnable()
        {
            _playerRenderer = GetComponentInChildren<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            _animator.SetFloat(Speed, Mathf.Abs(_rigidbody.velocity.x));

            // 探针
            transform.Find("p_b").GetComponent<Collider2D>().OnTriggerEnter2DAsObservable().Subscribe(_ =>
            {
                // 下方探针进入
                if (_.gameObject.CompareTag("Ground"))
                {
                    _isBottomBand = true;
                    _jumpCount = 2;
                }
            }).AddTo(this);
            transform.Find("p_b").GetComponent<Collider2D>().OnTriggerExit2DAsObservable().Subscribe(_ =>
            {
                // 下方探针离开
                if (_.gameObject.CompareTag("Ground"))
                {
                    if (_animator.GetBool(IsIdle) || _animator.GetBool(IsWalking))
                    {
                        _jumpCount = 1;
                    }

                    _isBottomBand = false;
                }
            }).AddTo(this);

            transform.Find("p_r").GetComponent<Collider2D>().OnTriggerEnter2DAsObservable().Subscribe(_ =>
            {
                // 右侧探针进入
                if (_.gameObject.CompareTag("Ground"))
                {
                    _isRightBand = true;
                }
            }).AddTo(this);
            transform.Find("p_r").GetComponent<Collider2D>().OnTriggerExit2DAsObservable().Subscribe(_ =>
            {
                // 右侧探针离开
                if (_.gameObject.CompareTag("Ground"))
                {
                    _isRightBand = false;
                }
            }).AddTo(this);

            transform.Find("p_l").GetComponent<Collider2D>().OnTriggerEnter2DAsObservable().Subscribe(_ =>
            {
                // 左侧探针进入
                if (_.gameObject.CompareTag("Ground"))
                {
                    _isLeftBand = true;
                }
            }).AddTo(this);
            transform.Find("p_l").GetComponent<Collider2D>().OnTriggerExit2DAsObservable().Subscribe(_ =>
            {
                // 左侧探针离开
                if (_.gameObject.CompareTag("Ground"))
                {
                    _isLeftBand = false;
                }
            }).AddTo(this);
        }

        private float _hAxisRaw;
        private float _vAxis;
        private static readonly int Speed = Animator.StringToHash("speed");
        private static readonly int IsFalling = Animator.StringToHash("isFalling");
        private static readonly int IsIdle = Animator.StringToHash("isIdle");
        private static readonly int IsRising = Animator.StringToHash("isRising");
        private static readonly int Jump = Animator.StringToHash("jump");
        private static readonly int IsWalking = Animator.StringToHash("isWalking");

        private void Update()
        {
            if (Input.GetKeyDown("up") || (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began) || Input.GetMouseButtonDown(0))
            {
                _isJumpPressed = true;
                if (_jumpCount > 0)
                {
                    _animator.SetTrigger(Jump);
                    _isJumpHandled = false;
                    _jumpCount--;
                }
            }
            else if (Input.GetKeyUp("up") || (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended) || Input.GetMouseButtonUp(0))
            {
                _isJumpPressed = false;
            }

            if (Input.GetKeyDown("left"))
            {
                _playerRenderer.flipX = true;
            }

            if (Input.GetKeyDown("right"))
            {
                _playerRenderer.flipX = false;
            }
        }

        private void FixedUpdate()
        {
            // 修正animator状态
            if (_rigidbody.velocity.y < 0.001 && !_animator.GetBool(IsFalling))
            {
                _animator.SetBool(IsRising, false);
                _animator.SetBool(IsFalling, true);
            }
            else if (_rigidbody.velocity.y > 0.001 && !_animator.GetBool(IsRising))
            {
                _animator.SetBool(IsRising, true);
                _animator.SetBool(IsFalling, false);
            }
            else if (Mathf.Abs(_rigidbody.velocity.y) < 0.001 && (!_animator.GetBool(IsRising) || !_animator.GetBool(IsFalling)))
            {
                _animator.SetBool(IsRising, false);
                _animator.SetBool(IsFalling, false);
            }
            if (Mathf.Abs(_rigidbody.velocity.x) > 0.001 && !_animator.GetBool(IsWalking))
            {
                _animator.SetBool(IsWalking, true);
            }
            else if(_animator.GetBool(IsWalking))
            {
                _animator.SetBool(IsWalking, false);
            }

            // 处理移动
            _hAxisRaw = _isConstMove ? 1 : Input.GetAxisRaw("Horizontal");
            if (_isConstMove && _isChasing && _isBottomBand)
            {
                _hAxisRaw = _chasingRate;
            }

            _rigidbody.velocity = new Vector2(_hAxisRaw * _moveSpeed, _rigidbody.velocity.y);

            // 处理跳跃
            if (!_isJumpHandled)
            {
                _rigidbody.velocity = Vector2.up * _jumpSpeed;
                _isJumpHandled = true;
            }

            // 优化上升和下落
            if (_rigidbody.velocity.y < 0)
            {
                _rigidbody.velocity += Vector2.up * Physics2D.gravity.y * (_FallMultiplier - 1) * Time.deltaTime;
            }
            else if (_rigidbody.velocity.y > 0 && !_isJumpPressed)
            {
                _rigidbody.velocity += Vector2.up * Physics2D.gravity.y * (_LowJumpMultiplier - 1) * Time.deltaTime;
            }

            // 两侧受阻
            if (_rigidbody.velocity.x > 0 && _isRightBand)
            {
                _rigidbody.velocity = new Vector2(0, _rigidbody.velocity.y);
            }

            if (_rigidbody.velocity.x < 0 && _isLeftBand)
            {
                _rigidbody.velocity = new Vector2(0, _rigidbody.velocity.y);
            }
        }
    }
}
