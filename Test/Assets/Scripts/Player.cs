using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Scripts;
namespace Scripts
{
    public class Player : MonoBehaviour
    {
        public static Player Instance;
        [SerializeField] private PlayerInput MoveAction;
        [SerializeField] private float MoveSpeed;
        public Vector2 InputMove = Vector2.zero;
        [SerializeField] private float jumpPower;
        [SerializeField] private GameObject Bullets;
        [SerializeField] private GameObject ShotPosition;
        [SerializeField] private GameObject AttackCollision;
        [SerializeField] private GameObject QuickAttackCollision;
        [SerializeField] private float MaxBulletTime;
        [SerializeField] private Image BulletUI;
        public GameObject Arrow;
        public bool isMove = true;
        [SerializeField] private int MaxJumpCount;
        [SerializeField] private Animator animator;
        [SerializeField] private AudioClip ReflectionSound;
        [SerializeField] private AudioClip ShotSound;
        [SerializeField] private AudioClip DamageSound;

        [SerializeField] [JapaneseLabel("2回目のジャンプまでのクールタイム")]
        private float jumpCooldown = 0.2f;

        private AudioSource audioSource;
        [NonSerialized] public float BulletTime;
        [NonSerialized] public int direction = 1;
        private bool isfirst = true;
        private bool isGround;
        private bool isJump;
        private int jumpCount;
        private float lastJumpTime; // 最後にジャンプした時間
        private Rigidbody2D rb;
        private float startY;
        [SerializeField] CameraAreaManager cameraAreaManager;
        [SerializeField] MapManager mapManager;

        [FormerlySerializedAs("limitSpeed")] [SerializeField]
        private float maxFallSpeed = 20f;

        [SerializeField,JapaneseLabel("最大スタミナ")] private float maxStamina = 100f;
        [SerializeField,JapaneseLabel("スタミナ回復量")] private float staminaRecoveryPerSecond = 10f;
        [NonSerialized,JapaneseLabel("現スタミナ")] public float currentStamina;
        [JapaneseLabel("スタミナ消費量")]public float staminaDrainPerSecond = 20f;
        private bool IsAttacking = false;
        public Slider staminaSlider;
        
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            MoveAction.actions["Move"].performed += OnMove;
            MoveAction.actions["Move"].canceled += OnMove;
            MoveAction.actions["Jump"].started += OnJump;
            MoveAction.actions["Shot"].started += OnShot;
            MoveAction.actions["Attack"].performed += OnAttack;
            MoveAction.actions["Attack"].canceled += OffAttack;
            MoveAction.actions["Jump"].canceled += OffJump;
            MoveAction.actions["QuickAttack"].performed += OnQuickAttack;

            rb = GetComponent<Rigidbody2D>();
            Arrow.SetActive(false);
            jumpCount = MaxJumpCount;
            audioSource = GetComponent<AudioSource>();

            cameraAreaManager = GameObject.FindObjectOfType<CameraAreaManager>();
            mapManager = GameObject.FindObjectOfType<MapManager>();
            currentStamina = maxStamina;
            staminaSlider.maxValue = currentStamina;
        }

        private void Update()
        {
            BulletUI.fillAmount = (MaxBulletTime - BulletTime) / MaxBulletTime;

            if (!GetComponent<Renderer>().isVisible)
            {
                if (isfirst)
                {
                    isfirst = false;
                }
                else
                {
                    Vector3 pos = transform.position;

                    if (pos.x < cameraAreaManager.LeftMax)
                        pos.x = cameraAreaManager.RightMax;
                    else if (pos.x > cameraAreaManager.RightMax)
                        pos.x = cameraAreaManager.LeftMax;

                    if (pos.y < cameraAreaManager.DownMax)
                    {
                        if (mapManager.CanLoop(pos, MapManager.Side.down) == true)
                        {
                            pos.y = cameraAreaManager.UpMax;
                        }
                        else
                        {
                            pos.y = cameraAreaManager.DownMax;
                        }

                        // Debug.Log(rb.linearVelocity);
                        if (rb.linearVelocity.y < maxFallSpeed * -1)
                        {
                            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed * -1);
                        }
                    }
                    else if (pos.y > cameraAreaManager.UpMax)
                    {
                        if (mapManager.CanLoop(pos, MapManager.Side.up) == true)
                        {
                            pos.y = cameraAreaManager.DownMax;
                        }
                        else
                        {
                            pos.y = cameraAreaManager.UpMax;
                        }
                    }

                    transform.position = pos;
                }
            }

            if (BulletTime > 0)
                BulletTime -= Time.deltaTime;
            if (!isMove)
                return;
            if (InputMove.x < 0)
            {
                transform.position += new Vector3(MoveSpeed * InputMove.x, 0, 0) * Time.deltaTime;
                transform.localScale = new Vector3(1f, 1f, -1f);
                direction = -1;
            }
            else if (InputMove.x > 0)
            {
                transform.position += new Vector3(MoveSpeed * InputMove.x, 0, 0) * Time.deltaTime;
                transform.localScale = new Vector3(1f, 1f, 1f);
                direction = 1;
            }

            animator.SetFloat("Jump", rb.linearVelocityY);
            
            if (!IsAttacking && currentStamina < maxStamina)
            {
                currentStamina += staminaRecoveryPerSecond * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
            staminaSlider.value = currentStamina;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.tag == "Ground")
                jumpCount = MaxJumpCount;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            InputMove = context.ReadValue<Vector2>();

            if (InputMove != Vector2.zero)
            {
                animator.SetBool("isMove", true);
                var Angle = Mathf.Atan2(InputMove.y, InputMove.x) * Mathf.Rad2Deg;
                Arrow.transform.rotation = Quaternion.Euler(0f, 0f, Angle);
            }
            else
            {
                animator.SetBool("isMove", false);
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (jumpCount > 0 && Time.time - lastJumpTime >= jumpCooldown)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
                jumpCount--;
                lastJumpTime = Time.time;
                animator.SetTrigger("isJump");
            }
        }

        public void OffJump(InputAction.CallbackContext context)
        {
            isJump = false;
            animator.SetBool("isJump", false);
        }

        public void OnShot(InputAction.CallbackContext context)
        {
            if (BulletTime <= 0)
            {
                // audioSource.PlayOneShot(ShotSound);
                // var bullets = Instantiate(Bullets, ShotPosition.transform.position, Quaternion.identity);
                // var bullet = bullets.GetComponent<Bullet>();
                // bullet.PowerDirection = direction;
                BulletTime = MaxBulletTime;
                animator.SetTrigger("isShot");
            }
        }

        public void Shot()
        {
            audioSource.PlayOneShot(ShotSound);
            var bullets = Instantiate(Bullets, ShotPosition.transform.position, Quaternion.identity);
            var bullet = bullets.GetComponent<Bullet>();
            bullet.PowerDirection = direction;
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (currentStamina <= 0) return;

            IsAttacking = true;
            AttackCollision.gameObject.SetActive(true);
            //Invoke("AttackFinish", 0.3f);

        }

        public void OnQuickAttack(InputAction.CallbackContext context)
        {
            if (currentStamina <= 0) return;

            IsAttacking = true;
            AttackCollision.gameObject.SetActive(true);
            Invoke("AttackFinish", 0.3f);
            animator.SetTrigger("isAttack");
        }
        private void OffAttack(InputAction.CallbackContext context)
        {
            AttackFinish();
            animator.SetTrigger("isAttack");
        }
        public void AttackFinish()
        {
            IsAttacking = false;
            AttackCollision.gameObject.SetActive(false);
            QuickAttackCollision.gameObject.SetActive(false);
        }

        public void PlayReflectionSound()
        {
            audioSource.PlayOneShot(ReflectionSound);
        }

        public void PlayDamageSound()
        {
            audioSource.PlayOneShot(DamageSound);
        }

        public void PlayerReset()
        {
            MoveAction.actions["Move"].performed -= OnMove;
            MoveAction.actions["Move"].canceled -= OnMove;
            MoveAction.actions["Jump"].started -= OnJump;
            MoveAction.actions["Shot"].started -= OnShot;
            MoveAction.actions["Attack"].performed -= OnAttack;
            MoveAction.actions["Attack"].canceled -= OffAttack;
            MoveAction.actions["Jump"].canceled -= OffJump;
            MoveAction.actions["QuickAttack"].performed -= OnQuickAttack;
        }
    }
}