using ECS.Components;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace MonoBehaviours.UI
{
    /// <summary>
    /// Manages the skill bar UI with 4 slots showing radial cooldown overlays,
    /// countdown text, keybind badges, and click-to-activate buttons.
    /// Created programmatically by UISetup.
    /// </summary>
    public class SkillBarController : MonoBehaviour
    {
        private Image[] cooldownOverlays;
        private TextMeshProUGUI[] cooldownTexts;
        private Button[] skillButtons;
        private GameObject skillBarRoot;

        // ECS access (lazy init)
        private EntityManager em;
        private Entity skillInputEntity;
        private Entity skillCooldownEntity;
        private Entity gameStateEntity;
        private bool ecsInitialized;

        // Ready flash tracking
        private bool[] wasCoolingDown = new bool[4];
        private float[] flashTimers = new float[4];
        private readonly Color flashColor = new Color(1f, 1f, 1f, 0.5f);
        private readonly Color clearColor = new Color(0f, 0f, 0f, 0f);
        private const float FlashDuration = 0.3f;

        /// <summary>
        /// Called by UISetup after creating the skill bar UI hierarchy.
        /// </summary>
        public void Initialize(Image[] overlays, TextMeshProUGUI[] texts,
                               Button[] buttons, GameObject root)
        {
            cooldownOverlays = overlays;
            cooldownTexts = texts;
            skillButtons = buttons;
            skillBarRoot = root;

            // Wire button click handlers
            for (int i = 0; i < 4; i++)
            {
                int skillIndex = i;
                buttons[i].onClick.AddListener(() => OnSkillButtonClicked(skillIndex));
            }
        }

        private void OnSkillButtonClicked(int skillIndex)
        {
            if (!TryInitECS()) return;

            var inputData = em.GetComponentData<SkillInputData>(skillInputEntity);
            switch (skillIndex)
            {
                case 0: inputData.Skill1Pressed = true; break;
                case 1: inputData.Skill2Pressed = true; break;
                case 2: inputData.Skill3Pressed = true; break;
                case 3: inputData.Skill4Pressed = true; break;
            }
            em.SetComponentData(skillInputEntity, inputData);
        }

        private bool TryInitECS()
        {
            if (ecsInitialized) return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;

            em = world.EntityManager;

            var skillInputQuery = em.CreateEntityQuery(typeof(SkillInputData));
            if (skillInputQuery.CalculateEntityCount() == 0) return false;
            skillInputEntity = skillInputQuery.GetSingletonEntity();

            var cooldownQuery = em.CreateEntityQuery(typeof(SkillCooldownData));
            if (cooldownQuery.CalculateEntityCount() == 0) return false;
            skillCooldownEntity = cooldownQuery.GetSingletonEntity();

            var gameStateQuery = em.CreateEntityQuery(typeof(GameStateData));
            if (gameStateQuery.CalculateEntityCount() == 0) return false;
            gameStateEntity = gameStateQuery.GetSingletonEntity();

            ecsInitialized = true;
            return true;
        }

        private void LateUpdate()
        {
            if (cooldownOverlays == null || !TryInitECS()) return;

            // Show/hide based on game phase (visible only during Playing)
            var gameState = em.GetComponentData<GameStateData>(gameStateEntity);
            bool visible = gameState.CurrentPhase == GamePhase.Playing;
            if (skillBarRoot.activeSelf != visible)
                skillBarRoot.SetActive(visible);

            if (!visible) return;

            var cooldowns = em.GetComponentData<SkillCooldownData>(skillCooldownEntity);

            // Update each slot
            UpdateSlot(0, cooldowns.Skill1Remaining, cooldowns.Skill1MaxCooldown);
            UpdateSlot(1, cooldowns.Skill2Remaining, cooldowns.Skill2MaxCooldown);
            UpdateSlot(2, cooldowns.Skill3Remaining, cooldowns.Skill3MaxCooldown);
            UpdateSlot(3, cooldowns.Skill4Remaining, cooldowns.Skill4MaxCooldown);
        }

        private void UpdateSlot(int index, float remaining, float maxCooldown)
        {
            // Radial fill
            cooldownOverlays[index].fillAmount = maxCooldown > 0 ? remaining / maxCooldown : 0f;

            // Countdown text
            cooldownTexts[index].text = remaining > 0 ? Mathf.CeilToInt(remaining).ToString() : "";

            // Ready flash: when cooldown transitions from active to ready
            bool isCoolingDown = remaining > 0;
            if (wasCoolingDown[index] && !isCoolingDown)
            {
                flashTimers[index] = FlashDuration;
            }
            wasCoolingDown[index] = isCoolingDown;

            // Animate flash
            if (flashTimers[index] > 0)
            {
                flashTimers[index] -= Time.deltaTime;
                float t = flashTimers[index] / FlashDuration;
                cooldownOverlays[index].color = Color.Lerp(clearColor, flashColor, t);
            }
            else if (!isCoolingDown)
            {
                cooldownOverlays[index].color = clearColor;
            }
            else
            {
                cooldownOverlays[index].color = new Color(0f, 0f, 0f, 0.7f);
            }
        }
    }
}
