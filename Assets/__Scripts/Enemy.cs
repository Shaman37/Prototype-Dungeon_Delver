using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    static protected Vector3[] directions = new Vector3[] { Vector3.right, Vector3.up, Vector3.left, Vector3.down};

    [Header("Set in Inspector: Enemy")]
    public float maxHealth = 1;
    public float knockbackSpeed = 10;
    public float knockbackDuration = 0.25f;
    public float invincibleDuration = 0.5f;
    

    [Header("Set Dynamically: Enemy")]
    public float health;
    public bool  invincible = false;
    public bool  knockback = false;
    
    private float   _invincibleDone = 0;
    private float   _knockbackDone = 0;
    private Vector3 _knockbackVel;

    protected Animator anime;
    protected Rigidbody rigid;
    protected SpriteRenderer sRend;
    
    virtual protected void Awake()
    {
        health = maxHealth;
        anime = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody>();
        sRend = GetComponent<SpriteRenderer>();
    }

    virtual protected void Update()
    {
        if (invincible && Time.time > _invincibleDone) invincible = false;

        sRend.color = invincible ? Color.red : Color.white;

        if (knockback)
        {
            rigid.velocity = _knockbackVel;
            if (Time.time < _knockbackDone) return;
        }

        anime.speed = 1;
        knockback = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (invincible) return;

        DamageEffect dEff = other.gameObject.GetComponent<DamageEffect>();
        if (dEff == null) return;

        health -= dEff.damage;
        if (health <= 0) Die();

        invincible = true;
        _invincibleDone = Time.time + invincibleDuration;

        if (dEff.knockback)
        {
            Vector3 delta = transform.position - other.transform.root.position;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                delta.x = (delta.x > 0) ? 1 : -1;
                delta.y = 0;
            }
            else
            {
                delta.x = 0;
                delta.y = (delta.y > 0) ? 1 : -1;
            }

            _knockbackVel = delta * knockbackSpeed;
            rigid.velocity = _knockbackVel;

            knockback = true;
            _knockbackDone = Time.time + knockbackDuration;
            anime.speed = 0;
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
