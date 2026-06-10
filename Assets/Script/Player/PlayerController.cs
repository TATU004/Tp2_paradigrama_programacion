using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))] 
public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 mousePos;
    private Camera viewCamera;

    [Header("玩家UI Rect")]
    public RectTransform healthFillRect; 
    public RectTransform dashFillRect;
    public RectTransform skillFillRect; 
    public float healthBarFullWidth = 200f; 
    public float dashBarFullWidth = 150f;
    public float skillBarFullWidth = 150f; 
    
    [Header("UI文本")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI dashText;
    public TextMeshProUGUI skillText; 

    [Header("冲刺设置")]
    public float dashSpeed = 15f;    
    public float dashDuration = 0.2f; 
    public float dashCooldown = 1f;   
    private float dashCooldownTimer;
    private bool isDashing;
    private bool canDash = true;

    [Header("普通攻击")]
    public GameObject slashEffectPrefab; 
    public Transform attackPoint;         
    public float attackRange = 1.5f;      
    public float attackCooldown = 0.3f;   
    public int attackDamage = 10;         
    public LayerMask enemyLayers;        
    private bool isAttacking;

    [Header("右键大招设置")]
    public GameObject skillPrefab; 
    public float skillRange = 4f; 
    public int skillDamage = 20; 
    public float skillCooldown = 5f; 
    private float skillCooldownTimer;
    private bool canSkill = true;
    private bool isSkilling;

    [Header("画面反馈")]
    private Transform mainCameraTransform; 
    public float shakeDuration = 0.2f;     
    public float shakeMagnitude = 0.15f;   
    private bool isShaking = false;

    [Header("音效设置")]
    private AudioSource audioSource;
    public AudioClip swingSFX; 
    public AudioClip hitSFX;   
    public AudioClip dashSFX;  
    public AudioClip skillSFX; 
    public AudioClip hurtSFX; 

    [Header("PowerUp 强化状态")]
    public GameObject shieldVisual; // 拖入玩家子物体中的半透明护盾图片
    private bool isInvincible = false;
    private bool isSuperBuffed = false;

    private int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        viewCamera = Camera.main; 
        if (viewCamera != null) mainCameraTransform = viewCamera.transform;

        currentHealth = maxHealth;
        UpdateHealthUI();
        UpdateDashUI(1f);
        UpdateSkillUI(1f);
        if (shieldVisual != null) shieldVisual.SetActive(false);

        if (enemyLayers == 0) enemyLayers = LayerMask.GetMask("Enemy");
    }

    void Update()
    {
        HandleTimers();

        if (isDashing || isSkilling)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;
        mousePos = viewCamera.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetKeyDown(KeyCode.Space) && canDash) StartCoroutine(Dash());
        if (Input.GetMouseButtonDown(0) && !isAttacking) StartCoroutine(Attack());
        if (Input.GetMouseButtonDown(1) && canSkill) StartCoroutine(ReleaseSkill()); 
    }

    void FixedUpdate()
    {
        if (isDashing || isSkilling) return;
        // 如果处于SuperBuff状态，速度提升50%
        float currentSpeed = isSuperBuffed ? moveSpeed * 1.5f : moveSpeed;
        rb.MovePosition(rb.position + moveInput * currentSpeed * Time.fixedDeltaTime);
    }

    void HandleTimers()
    {
        float currentDashCD = isSuperBuffed ? dashCooldown * 0.5f : dashCooldown;
        float currentSkillCD = isSuperBuffed ? skillCooldown * 0.5f : skillCooldown;

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
            UpdateDashUI(1f - (dashCooldownTimer / currentDashCD));
            if (dashText != null) dashText.text = Mathf.Max(0f, dashCooldownTimer).ToString("F1") + "s";
        }
        else if (!isDashing)
        {
            canDash = true;
            UpdateDashUI(1f);
            if (dashText != null) dashText.text = "READY";
        }

        if (skillCooldownTimer > 0)
        {
            skillCooldownTimer -= Time.deltaTime;
            UpdateSkillUI(1f - (skillCooldownTimer / currentSkillCD));
            if (skillText != null) skillText.text = Mathf.Max(0f, skillCooldownTimer).ToString("F1") + "s";
        }
        else if (!isSkilling)
        {
            canSkill = true;
            UpdateSkillUI(1f);
            if (skillText != null) skillText.text = "SKILL";
        }
    }

    void UpdateHealthUI()
    {
        if (healthFillRect != null)
        {
            float ratio = (float)currentHealth / maxHealth;
            healthFillRect.sizeDelta = new Vector2(healthBarFullWidth * ratio, healthFillRect.sizeDelta.y);
        }
        if (healthText != null) healthText.text = currentHealth + " / " + maxHealth;
    }

    void UpdateDashUI(float ratio) { if (dashFillRect != null) dashFillRect.sizeDelta = new Vector2(dashBarFullWidth * ratio, dashFillRect.sizeDelta.y); }
    void UpdateSkillUI(float ratio) { if (skillFillRect != null) skillFillRect.sizeDelta = new Vector2(skillBarFullWidth * ratio, skillFillRect.sizeDelta.y); }

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        if (audioSource != null && dashSFX != null) audioSource.PlayOneShot(dashSFX);
        Vector2 dashDirection = moveInput == Vector2.zero ? ((Vector2)mousePos - rb.position).normalized : moveInput;
        rb.linearVelocity = dashDirection * dashSpeed;
        yield return new WaitForSeconds(dashDuration);
        rb.linearVelocity = Vector2.zero;
        isDashing = false;
        dashCooldownTimer = isSuperBuffed ? dashCooldown * 0.5f : dashCooldown;
    }
    
    IEnumerator Attack()
    {
        isAttacking = true;
        Vector2 attackDirection = (mousePos - (Vector2)attackPoint.position).normalized;
        Vector2 finalAttackPoint = (Vector2)attackPoint.position + attackDirection * attackRange;

        if (slashEffectPrefab != null)
        {
            float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
            GameObject slash = Instantiate(slashEffectPrefab, finalAttackPoint, Quaternion.Euler(0, 0, angle));
            Destroy(slash, 1f);
        }
        
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(finalAttackPoint, attackRange * 0.5f, enemyLayers);
        if (hitEnemies.Length > 0)
        {
            if (audioSource != null && hitSFX != null) audioSource.PlayOneShot(hitSFX);
            foreach (Collider2D enemy in hitEnemies)
            {
                EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null) enemyHealth.TakeDamage(attackDamage);
            }
        }
        else
        {
            if (audioSource != null && swingSFX != null) audioSource.PlayOneShot(swingSFX);
        }

        // 如果SuperBuff，攻击间隔减半
        yield return new WaitForSeconds(isSuperBuffed ? attackCooldown * 0.5f : attackCooldown);
        isAttacking = false;
    }

    IEnumerator ReleaseSkill()
    {
        canSkill = false;
        isSkilling = true;
        rb.linearVelocity = Vector2.zero; 
        if (audioSource != null && skillSFX != null) audioSource.PlayOneShot(skillSFX);

        if (skillPrefab != null)
        {
            GameObject vfx = Instantiate(skillPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 1.5f);
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, skillRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null) enemyHealth.TakeDamage(skillDamage);
        }

        yield return new WaitForSeconds(0.3f); 
        isSkilling = false;
        skillCooldownTimer = isSuperBuffed ? skillCooldown * 0.5f : skillCooldown;
    }

    IEnumerator ShakeCamera()
    {
        if (isShaking || mainCameraTransform == null) yield break;
        isShaking = true;
        Vector3 originalLocalPos = mainCameraTransform.localPosition;
        float elapsed = 0.0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            mainCameraTransform.localPosition = originalLocalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCameraTransform.localPosition = originalLocalPos;
        isShaking = false;
    }

    // --- 外部道具激活接口 ---
    public void ActivateShield(float duration) { StartCoroutine(ShieldRoutine(duration)); }
    public void Heal(int amount) { currentHealth = Mathf.Min(maxHealth, currentHealth + amount); UpdateHealthUI(); }
    public void ActivateSuperBuff(float duration) { StartCoroutine(SuperBuffRoutine(duration)); }

    IEnumerator ShieldRoutine(float duration)
    {
        isInvincible = true;
        if (shieldVisual != null) shieldVisual.SetActive(true);
        yield return new WaitForSeconds(duration);
        if (shieldVisual != null) shieldVisual.SetActive(false);
        isInvincible = false;
    }

    IEnumerator SuperBuffRoutine(float duration)
    {
        isSuperBuffed = true;
        yield return new WaitForSeconds(duration);
        isSuperBuffed = false;
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return; // 无敌时不扣血

        currentHealth -= damage;
        UpdateHealthUI();
        if (audioSource != null && hurtSFX != null) audioSource.PlayOneShot(hurtSFX);
        if (currentHealth > 0) StartCoroutine(ShakeCamera());
    }

    // 当敌人撞击带盾玩家时触发弹开
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isInvincible && collision.gameObject.CompareTag("Enemy"))
        {
            Rigidbody2D enemyRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 bounceDirection = (collision.transform.position - transform.position).normalized;
                enemyRb.linearVelocity = bounceDirection * 12f; // 弹开力度
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (attackPoint != null) Gizmos.DrawWireSphere(attackPoint.position, attackRange * 0.5f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, skillRange);
    }
}