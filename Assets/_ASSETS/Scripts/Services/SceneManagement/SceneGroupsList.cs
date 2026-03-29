using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SceneManagement
{
    [CreateAssetMenu(fileName = "SceneGroupsList", menuName = "SceneManagement/SceneGroupsList")]
    public class SceneGroupsList : ScriptableObject
    {
        [Tooltip("!Please don`t create groups with same name!")]
        [SerializeField] private List<SceneGroup> groups;

        public bool TryGetGroupByName(string groupName, out SceneGroup sceneGroup)
        {
            sceneGroup = groups.Where(a => a.GroupName == groupName).FirstOrDefault();
            if (sceneGroup == null)
            {
                Debug.LogWarning("There is no Scene-Group with name: " + groupName);
                return false;
            }
            return true;
        }
        public SceneGroup GetGroupByIndex(int index)
        {
            if(groups.Count > index)
                return groups[index];

            Debug.LogError("Index is invalid: " + index);
            return null;
        }
    }
}