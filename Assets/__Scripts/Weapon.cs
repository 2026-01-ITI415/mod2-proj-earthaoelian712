using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary
/// This is an enum of the various possible weapon types.
/// It also includes a "shield" type to allow a shield PowerUp.
/// Items marked [NI] below are Not Implemented in this book.
/// </summary
public enum eWeaponType
{
    none,       // The default / no weapon
    blaster,    // A simple blaster
    spread,     // Multiple shots simultaneously
    phaser,     // [NI] Shots that move in waves
    missile,    // [NI] Homing missiles

    laser,      // [NI] Damage over time
    shield      // Raise shieldLevel
}


/// <summary
/// The WeaponDefinition class allows you to set the properties
///   of a specific weapon in the Inspector. The Main class has
///   an array of WeaponDefinitions that makes this possible.
/// </summary
[System.Serializable]                                                         // a
public class WeaponDefinition
{                                               // b
    public eWeaponType type = eWeaponType.none;
    [Tooltip("Letter to show on the PowerUp Cube")]                           // c
    public string letter;
    [Tooltip("Color of PowerUp Cube")]
    public Color powerUpColor = Color.white;                           // d
    [Tooltip("Prefab of Weapon model that is attached to the Player Ship")]
    public GameObject weaponModelPrefab;
    [Tooltip("Prefab of projectile that is fired")]
    public GameObject projectilePrefab;
    [Tooltip("Color of the Projectile that is fired")]
    public Color projectileColor = Color.white;                        // d
    [Tooltip("Damage caused when a single Projectile hits an Enemy")]
    public float damageOnHit = 0;
    [Tooltip("Damage caused per second by the Laser [Not Implemented]")]
    public float damagePerSec = 0;
    [Tooltip("Seconds to delay between shots")]
    public float delayBetweenShots = 0;
    [Tooltip("Velocity of individual Projectiles")]
    public float velocity = 50;
}

public class Weapon : MonoBehaviour
{
    static public Transform PROJECTILE_ANCHOR;

    [Header("Dynamic")]                                                        // a
    [SerializeField]                                                           // a
    [Tooltip("Setting this manually while playing does not work properly.")]   // a
    private eWeaponType _type = eWeaponType.none;
    public WeaponDefinition def;
    public float nextShotTime; // Time the Weapon will fire next

    private GameObject weaponModel;
    private Transform shotPointTrans;

    void Start()
    {
        // Set up PROJECTILE_ANCHOR if it has not already been done
        if (PROJECTILE_ANCHOR == null)
        {                                       // b
            GameObject go = new GameObject("_ProjectileAnchor");
            PROJECTILE_ANCHOR = go.transform;
        }

        shotPointTrans = transform.GetChild(0);                              // c

        // Call SetType() for the default _type set in the Inspector
        SetType(_type);                                                      // d

        // Find the fireEvent of a Hero Component in the parent hierarchy
        Hero hero = GetComponentInParent<Hero>();                              // e
        if (hero != null) hero.fireEvent += Fire;
    }

    public eWeaponType type
    {
        get { return (_type); }
        set { SetType(value); }
    }

    public void SetType(eWeaponType wt)
    {
        _type = wt;
        if (type == eWeaponType.none)
        {                                       // f
            this.gameObject.SetActive(false);
            return;
        }
        else
        {
            this.gameObject.SetActive(true);
        }
        // Get the WeaponDefinition for this type from Main
        def = Main.GET_WEAPON_DEFINITION(_type);
        // Destroy any old model and then attach a model for this weapon     // g
        if (weaponModel != null) Destroy(weaponModel);
        weaponModel = Instantiate<GameObject>(def.weaponModelPrefab, transform);
        weaponModel.transform.localPosition = Vector3.zero;
        weaponModel.transform.localScale = Vector3.one;

        nextShotTime = 0; // You can fire immediately after _type is set.    // h
    }

    private void Fire()
    {
        // 1. 安全检查
        if (!gameObject.activeInHierarchy) return;

        // --- 冷却判断 ---
        // 注意：激光因为是每帧生成的，建议在 Main 面板把 Laser 的 delayBetweenShots 设为 0 或 0.05
        if (Time.time < nextShotTime) return;

        ProjectileHero p;
        Vector3 vel = Vector3.up * def.velocity;

        // --- 武器类型判断 ---
        switch (type)
        {
            case eWeaponType.blaster:
                p = MakeProjectile();
                p.vel = vel;
                break;

            case eWeaponType.spread:
                p = MakeProjectile(); p.vel = vel;
                p = MakeProjectile();
                p.transform.rotation = Quaternion.AngleAxis(10, Vector3.back);
                p.vel = p.transform.rotation * vel;
                p = MakeProjectile();
                p.transform.rotation = Quaternion.AngleAxis(-10, Vector3.back);
                p.vel = p.transform.rotation * vel;
                p = MakeProjectile();
                p.transform.rotation = Quaternion.AngleAxis(20, Vector3.back);
                p.vel = p.transform.rotation * vel;
                p = MakeProjectile();
                p.transform.rotation = Quaternion.AngleAxis(-20, Vector3.back);
                p.vel = p.transform.rotation * vel;
                break;

            case eWeaponType.laser:
                p = MakeProjectile();
                if (p == null) break;

                Destroy(p.gameObject, 0.05f);
                LineRenderer lr = p.GetComponent<LineRenderer>();

                if (lr != null)
                {
                    lr.useWorldSpace = true;
                    lr.positionCount = 2;

                    // ==========================================
                    // 🔫 新增：初始生成点控制核心代码
                    // ==========================================
                    // 你可以在这里自由调节激光的真正发射位置！
                    // X轴(左右): 正数向右，负数向左
                    // Y轴(上下): 正数向上(机头前方)，负数向下(机尾)
                    // Z轴(深度): 保持为0
                    Vector3 laserOffset = new Vector3(0f, 0.8f, 0f);

                    // 最终真正的发射点 = 原本的枪口位置 + 你的自定义偏移量
                    Vector3 startPos = shotPointTrans.position + laserOffset;
                    // ==========================================

                    Vector3 endPos = startPos + Vector3.up * 50f;

                    // 射线现在从你精准控制的 startPos 发射
                    RaycastHit[] hits = Physics.RaycastAll(startPos, Vector3.up, 50f);

                    RaycastHit validHit = new RaycastHit();
                    bool foundTarget = false;
                    float closestDist = 999f;

                    foreach (RaycastHit h in hits)
                    {
                        // 过滤掉自己和自己的普通子弹
                        if (h.collider.transform.root == this.transform.root) continue;
                        if (h.collider.GetComponent<ProjectileHero>() != null) continue;

                        if (h.distance < closestDist)
                        {
                            closestDist = h.distance;
                            validHit = h;
                            foundTarget = true;
                        }
                    }

                    if (foundTarget)
                    {
                        endPos = validHit.point;

                        GameObject virtualProj = Instantiate<GameObject>(def.projectilePrefab);

                        // 隐形子弹稍微刺入敌人身体一点点，确保触发
                        virtualProj.transform.position = validHit.point + Vector3.down * 0.2f;
                        virtualProj.transform.localScale = new Vector3(3f, 3f, 3f);

                        MeshRenderer[] renderers = virtualProj.GetComponentsInChildren<MeshRenderer>();
                        foreach (MeshRenderer mr in renderers)
                        {
                            mr.enabled = false;
                        }

                        ProjectileHero pHero = virtualProj.GetComponent<ProjectileHero>();
                        if (pHero != null)
                        {
                            pHero.type = eWeaponType.laser;
                            Rigidbody rb = virtualProj.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                rb.velocity = Vector3.up * 200f;
                            }
                        }

                        Destroy(virtualProj, 0.1f);
                    }

                    // LineRenderer 的起点和终点
                    lr.SetPosition(0, startPos);
                    lr.SetPosition(1, endPos);
                }
                p.vel = Vector3.zero;
                break;
        }
    }

    private ProjectileHero MakeProjectile()
    {                                 // m
        GameObject go;
        go = Instantiate<GameObject>(def.projectilePrefab, PROJECTILE_ANCHOR); // n
        ProjectileHero p = go.GetComponent<ProjectileHero>();

        Vector3 pos = shotPointTrans.position;
        pos.z = 0;                                                            // o
        p.transform.position = pos;

        p.type = type;
        nextShotTime = Time.time + def.delayBetweenShots;                    // p
        return (p);
    }
}


