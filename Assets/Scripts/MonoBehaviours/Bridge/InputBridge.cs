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
        private Entity skillInputEntity;
        private Plane gameplayPlane;

        void Start()
        {
            mainCamera = Camera.main;
            em = World.DefaultGameObjectInjectionWorld.EntityManager;
            inputEntity = em.CreateEntityQuery(typeof(InputData))
                .GetSingletonEntity();
            skillInputEntity = em.CreateEntityQuery(typeof(SkillInputData))
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

            // Skill activation input (keyboard keys 1-4, New Input System only)
            // IMPORTANT: Read CURRENT data first, then OR keyboard presses on top.
            // SkillBarController may have already set flags via UI button clicks this frame.
            // We must NOT overwrite with a fresh struct or UI-triggered presses will be silently dropped.
            var skillInput = em.GetComponentData<SkillInputData>(skillInputEntity);
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                skillInput.Skill1Pressed |= keyboard.digit1Key.wasPressedThisFrame;
                skillInput.Skill2Pressed |= keyboard.digit2Key.wasPressedThisFrame;
                skillInput.Skill3Pressed |= keyboard.digit3Key.wasPressedThisFrame;
                skillInput.Skill4Pressed |= keyboard.digit4Key.wasPressedThisFrame;
            }
            em.SetComponentData(skillInputEntity, skillInput);
        }
    }
}
