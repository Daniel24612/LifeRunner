using System.Collections.Generic;
using System.Linq;
using ResourceManagement;
using UnityEngine;
namespace SceneManagement
{
    [CreateAssetMenu(fileName = "New Scene Group", menuName = "Scene Management/Scene Group")]
    public class SceneGroup : ScriptableObject
    {
        public string GroupName = "New Scene Group";
        public Sprite PreviewImage;
        public List<SceneData> Scenes;
        public ResourcesPreset ResourcesPreset;
        public string FindSceneNameByType(SceneType sceneType)
        {
            return Scenes.FirstOrDefault(scene => scene.SceneType == sceneType)?.Name;
        }
    }

}
