using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimation : MonoBehaviour
{
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // 获取动画和图片渲染组件
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 1. 获取玩家的键盘输入
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // 2. 判断玩家是否在移动
        bool isMoving = (Mathf.Abs(moveX) > 0f || Mathf.Abs(moveY) > 0f);
        
        // 3. 把移动状态传递给 Animator 里的 "isWalking" 布尔值
        anim.SetBool("isWalking", isMoving);

        // 4. 控制角色左右翻转
        if (moveX > 0)
        {
            spriteRenderer.flipX = false; // 向右走，不翻转
        }
        else if (moveX < 0)
        {
            spriteRenderer.flipX = true;  // 向左走，翻转
        }
    }
}