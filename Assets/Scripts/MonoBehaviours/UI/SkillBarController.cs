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
        private GameObject[] slotRoots;  // Root GO of each skill slot for show/hide

        // ECS access (lazy init)
        private EntityManager em;
        private Entity skillInputEntity;
        private Entity skillCooldownEntity;
        private Entity gameStateEntity;
        private Entity skillUnlockEntity;
        private Entity skillStatsEntity;
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
                               Button[] buttons, GameObject root, GameObject[] slots)
        {
            cooldownOverlays = overlays;
            cooldownTexts = texts;
            skillButtons = buttons;
            skillBarRoot = root;
            slotRoots = slots;

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

            // Guard: prevent activation of locked skills
            var unlockData = em.GetComponentData<SkillUnlockData>(skillUnlockEntity);
            bool isUnlocked = skillIndex switch
            {
                0 => unlockData.Skill1Unlocked,
                1 => unlockData.Skill2Unlocked,
                2 => unlockData.Skill3Unlocked,
                3 => unlockData.Skill4Unlocked,
                _ => false
            };
            if (!isUnlocked) return;

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

            var unlockQuery = em.CreateEntityQuery(typeof(SkillUnlockData));
            if (unlockQuery.CalculateEntityCount() == 0) return false;
            skillUnlockEntity = unlockQuery.GetSingletonEntity();

            var statsQuery = em.CreateEntityQuery(typeof(SkillStatsData));
            if (statsQuery.CalculateEntityCount() == 0) return false;
            skillStatsEntity = statsQuery.GetSingletonEntity();

            ecsInitialized = true;
            return true;
        }

        private void LateUpdate()
        {
            if (cooldownOverlays == null || !TryInitECS()) return;

            // Show/hide based on game phase (visible only during Playing)
            var gameState = em.GetComponentData<GameStateData>(gameStateEntity);
            bool visible = gameState.Phase == GamePhase.Playing;
            if (skillBarRoot.activeSelf != visible)
                skillBarRoot.SetActive(visible);

            if (!visible) return;

            var cooldowns = em.GetComponentData<SkillCooldownData>(skillCooldownEntity);
            var unlocks = em.GetComponentData<SkillUnlockData>(skillUnlockEntity);
            var stats = em.GetComponentData<SkillStatsData>(skillStatsEntity);

            bool[] unlocked = { unlocks.Skill1Unlocked, unlocks.Skill2Unlocked,
                                unlocks.Skill3Unlocked, unlocks.Skill4Unlocked };

            // Toggle slot visibility based on unlock state
            for (int i = 0; i < 4; i++)
            {
                if (slotRoots != null && slotRoots[i] != null)
                {
                    if (slotRoots[i].activeSelf != unlocked[i])
                        slotRoots[i].SetActive(unlocked[i]);
                }
            }

            // Update each slot (skip locked slots)
            if (unlocked[0]) UpdateSlot(0, cooldowns.Skill1Remaining, stats.LaserCooldown);
            if (unlocked[1]) UpdateSlot(1, cooldowns.Skill2Remaining, stats.ChainCooldown);
            if (unlocked[2]) UpdateSlot(2, cooldowns.Skill3Remaining, stats.EmpCooldown);
            if (unlocked[3]) UpdateSlot(3, cooldowns.Skill4Remaining, stats.OverchargeCooldown);
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
