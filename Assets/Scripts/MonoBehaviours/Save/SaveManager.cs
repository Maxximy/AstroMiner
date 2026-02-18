using System;
using System.IO;
using ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace MonoBehaviours.Save
{
    /// <summary>
    /// Singleton MonoBehaviour managing JSON save/load with WebGL IndexedDB flush.
    /// Self-instantiates via [RuntimeInitializeOnLoadMethod] -- no manual scene setup required.
    /// Persists across scenes via DontDestroyOnLoad.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SaveFileName = "astrominer_save.json";
        private const string PlayerPrefsKey = "astrominer_save";

        private SaveData _currentSave;

        /// <summary>
        /// The currently loaded save data. Always non-null after Awake.
        /// </summary>
        public SaveData CurrentSave => _currentSave;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("SaveManager");
                go.AddComponent<SaveManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _currentSave = Load();
            Debug.Log($"SaveManager: Initialized. Loaded credits: {_currentSave.TotalCredits}, version: {_currentSave.SaveVersion}");
        }

        /// <summary>
        /// Save data to JSON at Application.persistentDataPath.
        /// Falls back to PlayerPrefs if file I/O fails.
        /// Flushes IndexedDB on WebGL via WebGLHelper.
        /// </summary>
        public void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);

            try
            {
                string path = Path.Combine(Application.persistentDataPath, SaveFileName);
                File.WriteAllText(path, json);
                WebGLHelper.FlushSaveData();
                Debug.Log($"SaveManager: Saved to {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: File.WriteAllText failed ({e.Message}). Falling back to PlayerPrefs.");
                PlayerPrefs.SetString(PlayerPrefsKey, json);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Load save data from JSON file, with PlayerPrefs fallback.
        /// Returns a fresh SaveData if no save exists or on error.
        /// </summary>
        public SaveData Load()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, SaveFileName);

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var data = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log($"SaveManager: Loaded from file ({path})");
                    return data;
                }

                if (PlayerPrefs.HasKey(PlayerPrefsKey))
                {
                    string json = PlayerPrefs.GetString(PlayerPrefsKey);
                    var data = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log("SaveManager: Loaded from PlayerPrefs fallback");
                    return data;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: Load failed ({e.Message}). Returning fresh save.");
            }

            Debug.Log("SaveManager: No existing save found. Starting fresh.");
            return new SaveData();
        }

        /// <summary>
        /// Auto-save: reads current credits from ECS GameStateData singleton,
        /// updates the in-memory save, and persists to disk.
        /// Called on run end (GameOverState.Enter).
        /// </summary>
        public void AutoSave()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogWarning("SaveManager: AutoSave skipped -- ECS world not available.");
                return;
            }

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(GameStateData));
            if (query.CalculateEntityCount() == 0)
            {
                Debug.LogWarning("SaveManager: AutoSave skipped -- GameStateData singleton not found.");
                return;
            }

            var gameState = query.GetSingleton<GameStateData>();
            _currentSave.TotalCredits = gameState.Credits;
            Save(_currentSave);
            Debug.Log($"SaveManager: Auto-saved. Credits: {gameState.Credits}");
        }

        /// <summary>
        /// Load saved credits into the ECS GameStateData singleton.
        /// Called on first run start to restore progress from previous session.
        /// </summary>
        public void LoadIntoECS()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogWarning("SaveManager: LoadIntoECS skipped -- ECS world not available.");
                return;
            }

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(GameStateData));
            if (query.CalculateEntityCount() == 0)
            {
                Debug.LogWarning("SaveManager: LoadIntoECS skipped -- GameStateData singleton not found.");
                return;
            }

            var entity = query.GetSingletonEntity();
            var data = em.GetComponentData<GameStateData>(entity);
            data.Credits = _currentSave.TotalCredits;
            em.SetComponentData(entity, data);

            Debug.Log($"SaveManager: Loaded into ECS. Credits: {_currentSave.TotalCredits}, Level: {_currentSave.CurrentLevel}");
        }
    }
}
