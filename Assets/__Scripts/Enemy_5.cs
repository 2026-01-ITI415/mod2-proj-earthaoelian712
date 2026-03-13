using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_5 : Enemy
{
    [Header("Enemy_5 专属干扰弹设置")]
    [Tooltip("请在这里拖入干扰弹 Prefab")]
    public GameObject projectilePrefab;

    // 【修改点1】将射击间隔调大，例如 4 秒射一次
    [Tooltip("发射间隔（秒）")]
    public float fireRate = 4f;

    // 【修改点2】稍微降低初始速度，让它像天女散花
    [Tooltip("干扰弹飞行速度")]
    public float projectileSpeed = 10f;

    void Start()
    {
        InvokeRepeating("Fire", 1f, fireRate);
    }

    void Fire()
    {
        if (projectilePrefab == null) return;
        if (bndCheck != null && !bndCheck.isOnScreen) return;

        // 散射角度：一次 5 发
        float[] angles = { -30f, -15f, 0f, 15f, 30f };

        foreach (float angle in angles)
        {
            GameObject proj = Instantiate<GameObject>(projectilePrefab);
            proj.transform.position = transform.position + Vector3.down * 1.5f;

            Rigidbody rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 spreadDirection = Quaternion.Euler(0, 0, angle) * Vector3.down;
                rb.linearVelocity = spreadDirection * projectileSpeed;
            }
        }
    }
}