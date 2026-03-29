using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Eflatun.SceneReference;


namespace SceneManagement
{
    public class SceneGroupManager
    {
        public SceneLoadingStatus Status { get; private set; } = SceneLoadingStatus.Neutral; 
        public event Action<string> SceneLoaded = delegate { };
        public event Action<string> SceneUnloaded = delegate { };
        public event Action SceneGroupLoaded = delegate { };

        SceneGroup ActiveSceneGroup;
        private readonly Dictionary<string, SceneInstance> _addressableInstances = new();

        private List<string> _protectedScenes = new List<string>()
        {
            "BootStrapper"
        };

        public async UniTask LoadScenes(SceneGroup sceneGroup, IProgress<float> progress, bool reloadDupScenes = false)
        {
            Status = SceneLoadingStatus.Loading;
            ActiveSceneGroup = sceneGroup;

            await UnloadScenes();

            var scenesToLoad = sceneGroup.Scenes;
            var tasks = new List<UniTask>();

            // Получаем список уже загруженных сцен, чтобы избежать повторной загрузки, если это не требуется
            var loadedScenes = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);

            // Общий прогресс для Addressables чуть сложнее, 
            // поэтому используем простой счетчик завершенных задач
            int completedTasks = 0;
            int totalTasks = scenesToLoad.Count;

            foreach (var sceneData in scenesToLoad)
            {
                if (!reloadDupScenes && loadedScenes.Contains(sceneData.Name)) continue;

                // Проверяем через Eflatun, является ли сцена адресной
                if (sceneData.Reference.State == SceneReferenceState.Addressable)
                {
                    tasks.Add(LoadAddressableScene(sceneData, UpdateProgress));
                }
                else
                {
                    tasks.Add(LoadBuildScene(sceneData, UpdateProgress));
                }
            }

            await UniTask.WhenAll(tasks);

            void UpdateProgress()
            {
                completedTasks++;
                progress?.Report((float)completedTasks / totalTasks);
            }

            // Установка активной сцены
            var activeSceneName = ActiveSceneGroup.FindSceneNameByType(SceneType.ActiveScene);
            if (!string.IsNullOrEmpty(activeSceneName))
            {
                Scene activeScene = SceneManager.GetSceneByName(activeSceneName);
                if (activeScene.IsValid()) SceneManager.SetActiveScene(activeScene);
            }

            SceneGroupLoaded.Invoke();
        }

        private async UniTask LoadBuildScene(SceneData data, Action onComplete)
        {
            await SceneManager.LoadSceneAsync(data.Reference.Path, LoadSceneMode.Additive).ToUniTask();
            onComplete?.Invoke();
        }

        private async UniTask LoadAddressableScene(SceneData data, Action onComplete)
        {
            // Addressables.LoadSceneAsync возвращает SceneInstance
            var handle = Addressables.LoadSceneAsync(data.Reference.Address, LoadSceneMode.Additive);
            var instance = await handle.ToUniTask();

            // Сохраняем инстанс, иначе Addressables не поймет, что выгружать
            _addressableInstances[data.Name] = instance;
            onComplete?.Invoke();
        }



        public async UniTask UnloadScenes()
        {
            Status = SceneLoadingStatus.Unloading;

            var activeSceneName = SceneManager.GetActiveScene().name;
            var tasks = new List<UniTask>();

            for (int i = SceneManager.sceneCount - 1; i > 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || _protectedScenes.Contains(scene.name) || scene.name == activeSceneName) continue;

                if (_addressableInstances.TryGetValue(scene.name, out var instance))
                {
                    tasks.Add(Addressables.UnloadSceneAsync(instance).ToUniTask());
                    _addressableInstances.Remove(scene.name);
                }
                else
                {
                    tasks.Add(SceneManager.UnloadSceneAsync(scene).ToUniTask());
                }
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }

            await Resources.UnloadUnusedAssets().ToUniTask();
        }
        public void SetProtectionForScene(string sceneName, bool wouldProtect)
        {
            if(wouldProtect)
                if(!_protectedScenes.Contains(sceneName))
                    _protectedScenes.Add(sceneName);

            if(!wouldProtect)
                if(_protectedScenes.Contains(sceneName))
                    _protectedScenes.Remove(sceneName);
        }
    }
    public enum SceneLoadingStatus
    {
        Neutral,
        Unloading,
        Loading,
    }
}