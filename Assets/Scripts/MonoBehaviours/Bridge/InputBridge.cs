using ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonoBehaviours.Bridge
{
    public class InputBridge : MonoBehaviour
    {
        private Camera mainCamera;
        private EntityManager em;
        private Entity inputEntity;
        private Plane gameplayPlane;

        void Start()
        {
            mainCamera = Camera.main;
            em = World.DefaultGameObjectInjectionWorld.EntityManager;
            inputEntity = em.CreateEntityQuery(typeof(InputData))
                .GetSingletonEntity();

            // Gameplay plane at Y=0, facing up
            // For perspective top-down camera, the plane determines
            // where mouse rays intersect the game world
            gameplayPlane = new Plane(Vector3.up, Vector3.zero);
        }

        void Update()
        {
            var mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            var ray = mainCamera.ScreenPointToRay(mousePos);

            var inputData = new InputData();
            if (gameplayPlane.Raycast(ray, out float distance))
            {
                var worldPoint = ray.GetPoint(distance);
                inputData.MouseWorldPos = new float2(worldPoint.x, worldPoint.z);
                inputData.MouseValid = true;
            }
            else
            {
                inputData.MouseValid = false;
            }

            em.SetComponentData(inputEntity, inputData);
        }
    }
}
