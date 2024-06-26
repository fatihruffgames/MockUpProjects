using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class BaseBoltClass : MonoBehaviour
{
    #region Events For Subclasses

    public event Action RealMoveStartedEvent;
    public event Action AnyMoveSequenceEndedEvent;
    public event Action AnyMoveSequenceStartedEvent;
    public event Action CollidedAndRoleIsPassiveEvent;
    public event Action PickedEvent;
    public event Action ReleasedEvent;

    #endregion

    [Header("Base Config")] 
    public ColourEnum colourEnum;
    private const float FrontOffset = 1f;

    [Header("Base References")] [SerializeField]
    protected ParticleSystem sparkParticle;

    [SerializeField] protected Transform slotObject;
    [SerializeField] protected BoltHeadCollision headCollision;
    [SerializeField] protected List<BaseBoltClass> obstacleBolts;

    [Header("Base Debug")] protected bool IsActive;
    [HideInInspector] public bool isPicked;
    [HideInInspector] public bool performFakeMove;
    private bool _shouldRotate;
    protected bool BlockPickingAnotherSequenceIsOn;
    private const float RotationSpeed = 700f;
    private Tween _fakeMoveTween;
    private Vector3 _startPos;
    private Quaternion _initRot;
    private PlacementPoint _currentPoint;

    protected virtual void Awake()
    {
        IsActive = true;

        headCollision.SetParent(this);
        headCollision.CollidedWithBoltEvent += OnCollidedWithBolt;
    }

    protected abstract void OnCollidedWithBolt(BaseBoltClass collidedBolt);

    protected void OnMouseUp()
    {
        if (Rotater.instance.rotaterIsPerfomed) return;
        Debug.LogWarning("On Picked called");
        OnPicked();
    }

    public void OnPicked()
    {
        if (!GameManager.instance.isLevelActive) return;
        if (!IsPickable()) return;

        PickedEvent?.Invoke();
        isPicked = true;
        _shouldRotate = true;

        if (!CanPerformMoving())
        {
            FakeMove();
        }

        else
        {
            RealMove();
        }
    }

    private void RealMove()
    {
        transform.SetParent(null);
        int iterator = 0;
        while (iterator < 3)
        {
            Taptic.Light();
            iterator++;
        }

        AnyMoveSequenceStartedEvent?.Invoke();
        RealMoveStartedEvent?.Invoke();
        IsActive = false;
        Vector3 movementDirection = transform.up * FrontOffset;
        Vector3 targetPosition = transform.position + movementDirection;

        transform.DOMove(targetPosition, .5f).SetDelay(0.15f).OnComplete(() =>
        {
            _shouldRotate = false;
            AnyMoveSequenceEndedEvent?.Invoke();
            OnReleased();
        });
    }

    protected void OnReleased()
    {
        UnsubscribeFromEvents();
        Destroy(transform.GetComponent<Collider>());
        Destroy(transform.GetComponent<Rigidbody>());

        ReleasedEvent?.Invoke();
        ColoredHole coloredHole = HoleManager.instance.GetCurrentHole();

        if (ColourUtility.CheckIfColorsMatch(colourEnum, coloredHole.GetColorEnum())
            && !coloredHole.willBeDisappeared)
        {
            if (coloredHole.GetAvailablePoint() != null)
            {
                GoToPoint(coloredHole.GetAvailablePoint(), coloredHole);
            }
        }
        else
        {
            if (NeutralHole.instance.GetAvailablePoint())
            {
                GoToPoint(NeutralHole.instance.GetAvailablePoint(), NeutralHole.instance);
            }
            else
            {
                Debug.LogWarning("No Placeable point left on NEUTRAL HOLE, FAIL");
                GameManager.instance.EndGame(false);
            }
        }
    }

    public void GoToPoint(PlacementPoint targetPoint, BaseHoleClass newHole = null)
    {
        _currentPoint?.SetFree();
        _currentPoint = targetPoint;
        targetPoint.SetOccupied(this);

        if (newHole && newHole != NeutralHole.instance)
        {
            ColoredHole hole = newHole as ColoredHole;
            hole?.CheckDisappearingSequence();
        }

        transform.SetParent(targetPoint.transform);
        transform.forward = targetPoint.transform.forward;
        Sequence sq = DOTween.Sequence();
        sq.Append(
            transform.DOLocalMove(Vector3.up, .35f).OnComplete(() => { _shouldRotate = true; }));
        sq.Append(transform.DOLocalMove(Vector3.zero
            , .25f).OnStart(() => { _shouldRotate = false; }));
        sq.OnComplete(() =>
        {
            if (newHole == null) return;

            if (newHole == NeutralHole.instance)
                newHole.OnBoltArrived();
            else
            {
                if (newHole.GetOccupiedPointCount() == newHole.GetPointCount())
                    newHole.OnBoltArrived();
            }
        });
    }

    private void FakeMove()
    {
        Debug.Log("Fake move performed: " + gameObject.name);
        AnyMoveSequenceStartedEvent?.Invoke();
        performFakeMove = true;

        Vector3 movementDirection = transform.up * FrontOffset;
        Vector3 targetPosition = transform.position + movementDirection;
        _startPos = transform.position;
        _initRot = transform.rotation;

        #region Delay for rotation

        float delay = .15f;
        if (transform.GetComponent<ParentBolts>())
        {
            delay = transform.GetComponent<ParentBolts>().GetChildrenBoltCount() > 0 ? .35f : delay;
        }

        #endregion

        _fakeMoveTween = transform.DOMove(targetPosition, .5f).SetDelay(delay);
        _fakeMoveTween.Play();
    }

    public void StopFakeMove(BaseBoltClass collidedBolt)
    {
        if (!performFakeMove) return;

        Taptic.Medium();
        _shouldRotate = false;
        performFakeMove = false;
        _fakeMoveTween.Kill();
        collidedBolt?.OnCollidedAndRoleIsPassive();
        transform.DOMove(_startPos, .5f);

        DOTween.To(() => 0f, x =>
            {
                transform.rotation =
                    Quaternion.Lerp(transform.rotation, _initRot, x);
            }, 0.99f, .5f)
            .OnComplete(() =>
            {
                AnyMoveSequenceEndedEvent?.Invoke();
                isPicked = false;
            });
    }

    private void OnCollidedAndRoleIsPassive()
    {
        CollidedAndRoleIsPassiveEvent?.Invoke();
        BlockPickingAnotherSequenceIsOn = true;

        Sequence sq = DOTween.Sequence();
        Vector3 startPos = transform.localPosition;
        sq.Append(transform.DOLocalMove(startPos + transform.up * 0.125f, .125f));
        sq.Append(transform.DOLocalMove(startPos, .125f));
        sq.OnComplete((() =>
            BlockPickingAnotherSequenceIsOn = false));

        Debug.Log("Collided role is passive: " + gameObject.name);

        // transform.DOLocalMove(transform.localPosition + transform.up * 0.125f, .125f)
        //     .SetLoops(2, LoopType.Yoyo)
        //     .OnComplete(() => { BlockPickingAnotherSequenceIsOn = false; });
    }

    protected virtual void Update()
    {
        if (_shouldRotate)
        {
            transform.Rotate(Vector3.up, 1 * RotationSpeed * Time.deltaTime);
        }
    }

    protected abstract bool CanPerformMoving();
    protected abstract bool IsPickable();

    public bool IsBoltActive()
    {
        return IsActive;
    }

    public ColourEnum GetColor()
    {
        return colourEnum;
    }

    protected abstract void UnsubscribeFromEvents();

    protected void SetSlotParent(Transform newParent)
    {
        slotObject.SetParent(newParent);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}