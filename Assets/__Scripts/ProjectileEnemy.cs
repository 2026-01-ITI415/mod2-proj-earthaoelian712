using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileEnemy : Enemy
{
    private Rigidbody rb;

    void Start()
    {
        // 获取自身的刚体组件，用来检测速度
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // 1. 保留原有逻辑：如果飞出屏幕底端，自我销毁
        if (bndCheck != null && bndCheck.LocIs(BoundsCheck.eScreenLocs.offDown))
        {
            Destroy(gameObject);
        }

        // 2. 【新增逻辑】：如果子弹停下来了（速度极小），自动清除
        // magnitude 代表速度的绝对大小。当它减速到 0.5 以下时，认为已经停滞
        if (rb != null && rb.linearVelocity.magnitude < 0.5f)
        {
            // 可以在这里加个粒子特效或者声音，目前是直接销毁
            Destroy(gameObject);
        }
    }
}