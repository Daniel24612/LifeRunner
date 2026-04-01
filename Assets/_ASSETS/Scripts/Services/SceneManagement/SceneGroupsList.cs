using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SceneManagement
{
    [CreateAssetMenu(fileName = "SceneGroupsList", menuName = "Scene Management/SceneGroupsList")]
    public class SceneGroupsList : ScriptableObject
    {
        [Tooltip("!Please don`t create groups with same name!")]
        [SerializeField] private List<SceneGroup> groups;

        public SceneGroup GetGroupByName(string groupName)
        {
            var sceneGroup = groups.Where(a => a.GroupName == groupName).FirstOrDefault();
            if (sceneGroup == null)
            {
                Debug.LogWarning("There is no Scene-Group with name: " + groupName);
                return null;
            }
            return sceneGroup;
        }
        public SceneGroup GetGroupByIndex(int index)
        {
            if(groups.Count > index)
                return groups[index];

            Debug.LogError("Index is invalid: " + index);
            return null;
        }
        public IReadOnlyCollection<SceneGroup> GetAllGroups()
        {
            return groups.AsReadOnly();
        }
    }
}