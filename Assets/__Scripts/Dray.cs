using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Dray : MonoBehaviour, IFacingMover, IKeyMaster
{
    public enum eMode
    {
        idle,
        move,
        attack,
        transition,
        knockback
    }
    
    [Header("Set in Inspector")]
    public float speed = 5;
    public float attackDuration = 0.25f;
    public float attackDelay = 0.5f;
    public float transitionDelay = 0.5f;
    public int   maxHealth = 10;
    public float knockbackSpeed = 10;
    public float knockbackDuration = 0.25f;
    public float invincibleDuration = 0.5f;
    

    [Header("Set Dynamically")]
    public int                   dirHeld = -1;
    public int                   facing = 1;
    public eMode                 mode = eMode.idle;
    public int                   numKeys = 0;
    public bool                  invincible = true;    
    [SerializeField] private int _health;    
    
    private Rigidbody      _rigid;
    private Animator       _anim;
    private InRoom         _inRoom;
    private SpriteRenderer _sRend;

    // Mode related fields
    private float     _timeAtkDone = 0;
    private float     _timeAtkNext = 0;
    private float     _transitionDone = 0;
    private Vector2   _transitionPos;
    private float     _knockbackDone = 0;
    private float     _invincibleDone = 0;
    private Vector3   _knockbackVel;
    
    private KeyCode[] _keys = new KeyCode[]{ KeyCode.RightArrow,KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow};
    private Vector3[] _directions = new Vector3[]{ Vector3.right,Vector3.up, Vector3.left, Vector3.down};
    

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();
        _inRoom = GetComponent<InRoom>();
        _sRend = GetComponent<SpriteRenderer>();
        _health = maxHealth;
    }   

    private void Update()
    {   
        if (invincible && Time.time > _invincibleDone) invincible = false;

        _sRend.color = invincible ? Color.red : Color.white;

        if (mode == eMode.knockback)
        {
            _rigid.velocity = _knockbackVel;
            if (Time.time < _knockbackDone) return;
        }

        if (mode == eMode.transition)
        {
            _rigid.velocity = Vector3.zero;
            _anim.speed = 0;
            roomPos = _transitionPos;

            if (Time.time < _transitionDone) mode = eMode.idle;
        }

        dirHeld = -1;
        
        // Handle movement input
        for (int i = 0; i < 4; i++)
        {
             if (Input.GetKey(_keys[i])) dirHeld = i;
        }

        // Handle attack input
        if (Input.GetKeyDown(KeyCode.Z) && Time.time >= _timeAtkNext)
        {
            mode = eMode.attack;
            _timeAtkDone = Time.time + attackDuration;
            _timeAtkNext = Time.time + attackDelay;
        }

        // Finish attack
        if (Time.time >= _timeAtkDone) mode = eMode.idle;

        // Handle mode management when not attacking
        if (mode != eMode.attack)
        {
            if (dirHeld == -1)
            {
                mode = eMode.idle;
            }
            else
            {
                facing = dirHeld;
                mode = eMode.move;
            }
        }
        
        // Current mode actions
        Vector3 vel = Vector3.zero;

        switch (mode)
        {
            case eMode.attack:
                _anim.CrossFade("Dray_Attack_" + facing, 0);
                _anim.speed = 0;
                break;
            
            case eMode.idle:
                _anim.CrossFade("Dray_Walk_" + facing, 0);
                _anim.speed = 0;
                break;
            
            case eMode.move:
                vel = _directions[dirHeld];
                _anim.CrossFade("Dray_Walk_" + facing, 0);
                _anim.speed = 1;
                break;
        }

        _rigid.velocity = vel * speed;
    }

    private void LateUpdate()
    {
        Vector2 rPos = GetRoomPosOnGrid(0.5f);

        int doorNum;
        for (doorNum = 0; doorNum < 4; doorNum++)
        {
            if (rPos == InRoom.DOORS[doorNum]) break;
        }

        if (doorNum > 3 || doorNum != facing) return;

        Vector2 rm = roomNum;
        switch (doorNum)
        {
            case 0:
                rm.x += 1;
                break;

            case 1:
                rm.y += 1;
                break;

            case 2:
                rm.x -= 1;
                break;

            case 3:
                rm.y -= 1;
                break;
        }

        if (rm.x >= 0 && rm.x <= InRoom.MAX_RM_X)
        {
            if (rm.y >= 0 && rm.y <= InRoom.MAX_RM_Y)
            {
                roomNum = rm;
                _transitionPos = InRoom.DOORS[(doorNum + 2) % 4];
                roomPos = _transitionPos;
                mode = eMode.transition;
                _transitionDone = Time.time + transitionDelay;
            }
        }

    }

    private void OnCollisionEnter(Collision other)
    {
        if (invincible) return;

        DamageEffect dEff = other.gameObject.GetComponent<DamageEffect>();
        if (dEff == null) return;

        health -= dEff.damage;
        invincible = true;
        _invincibleDone = Time.time + invincibleDuration;

        if (dEff.knockback)
        {
            Vector3 delta = transform.position - other.transform.position;
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
            _rigid.velocity = _knockbackVel;

            mode = eMode.knockback;
            _knockbackDone = Time.time + knockbackDuration;
        }
    }


    public int GetFacing()
    {
        return facing;
    }

    public float GetSpeed()
    {
        return speed;
    }

    public Vector2 GetRoomPosOnGrid(float mult = -1)
    {
        return _inRoom.GetRoomPosOnGrid(mult);
    }
    

    public bool moving
    {
        get
        {
            return (mode == eMode.move);
        }
    }

    public float gridMult
    {
        get
        {
            return _inRoom.gridMult;
        }
    }

    public Vector2 roomPos
    {
        get
        {
            return _inRoom.roomPos;
        }
        set
        {
            _inRoom.roomPos = value;
        }
    }
    
    public Vector2 roomNum
    {
        get
        {
            return _inRoom.roomNum;
        }
        set
        {
            _inRoom.roomNum = value;
        }
    }

    public int keyCount
    {
        get
        {
            return numKeys;
        }
        set
        {
            numKeys = value;
        }
    }

    public int health
    {
        get
        {
            return _health;
        }
        set
        {
            _health = value;
        }
    }    
}
