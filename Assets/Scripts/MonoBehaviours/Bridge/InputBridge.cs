using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class InputBridge : MonoBehaviour
{
    private Camera _mainCamera;
    private EntityManager _em;
    private Entity _inputEntity;
    private Plane _gameplayPlane;

    void Start()
    {
        _mainCamera = Camera.main;
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        _inputEntity = _em.CreateEntityQuery(typeof(InputData))
            .GetSingletonEntity();

        // Gameplay plane at Y=0, facing up
        // For perspective top-down camera, the plane determines
        // where mouse rays intersect the game world
        _gameplayPlane = new Plane(Vector3.up, Vector3.zero);
    }

    void Update()
    {
        var mousePos = Input.mousePosition;
        var ray = _mainCamera.ScreenPointToRay(mousePos);

        var inputData = new InputData();
        if (_gameplayPlane.Raycast(ray, out float distance))
        {
            var worldPoint = ray.GetPoint(distance);
            inputData.MouseWorldPos = new float2(worldPoint.x, worldPoint.z);
            inputData.MouseValid = true;
        }
        else
        {
            inputData.MouseValid = false;
        }

        _em.SetComponentData(_inputEntity, inputData);
    }
}
