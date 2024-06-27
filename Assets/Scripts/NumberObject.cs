using DG.Tweening;
using UnityEngine;

public class NumberObject : MonoBehaviour
{
    [Header("Config")] public int levelValue;

    [Header("References")] [SerializeField]
    private NumberObjectMeshSO meshDataSo;

    [Header("Debug")] [SerializeField] private GridCell occupiedCell;
    [SerializeField] private PlacementPoint currentPoint;
    private GameObject _currentModel;
    public bool isMovingToPoint;

    private void Awake()
    {
        levelValue = Random.Range(1, 7);
        occupiedCell = transform.GetComponentInParent<GridCell>();
        occupiedCell.SetNumberObject(this);
        SetMesh();
    }

    private void SetMesh()
    {
        if (_currentModel) Destroy(_currentModel);

        _currentModel = Instantiate(meshDataSo.meshes[levelValue - 1], transform.position + (Vector3.up / 4),
            Quaternion.identity,
            transform);
    }

    public void OnCellPicked()
    {
        if (PointManager.instance.GetOccupiedPointCount() == 7)
        {
            Debug.LogError("NO EMPTY POINT LEFT");
            return;
        }

        MoveToPoint(PointManager.instance.GetAvailablePoint(), true);
    }

    private void MoveToPoint(PlacementPoint targetPoint, bool informCell = false)
    {
        isMovingToPoint = true;
        if (informCell)
        {
            occupiedCell.OnNumberObjectLeft();
            occupiedCell.SetNumberObject(null);
        }

        currentPoint?.SetFree();
        currentPoint = targetPoint;
        currentPoint.SetOccupied(this);

        transform.DOMove(targetPoint.transform.position, 0.5f).OnComplete(() =>
        {
            isMovingToPoint = false;
            PointManager.instance.OnNewNumberArrived();
            transform.SetParent(currentPoint.transform);
        });
    }

    public void InnerSortMovement(PlacementPoint targetPoint)
    {
        currentPoint = targetPoint;
        currentPoint.SetOccupied(this);
        transform.DOMove(targetPoint.transform.position, 0.5f).OnComplete(() =>
        {
            transform.SetParent(currentPoint.transform);
        });
    }

    public void Merge(Vector3 targetPos)
    {
        currentPoint?.SetFree();
        transform.DOMoveX(targetPos.x, .25f).OnComplete((() => gameObject.SetActive(false)));
    }

    public void UpgradeSelf()
    {
        levelValue++;
        SetMesh();
        PointManager.instance.OnNewNumberArrived();
    }
}