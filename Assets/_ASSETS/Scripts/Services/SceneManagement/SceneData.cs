using UnityEngine;
using System;
using Eflatun.SceneReference;
using System.Collections.Generic;

namespace SceneManagement
{
    [Serializable]
    public class SceneData
    {
        public SceneReference Reference;
        public string Name => Reference.Name;
        public SceneType SceneType;

    }

    public enum SceneType
    {
        ActiveScene,
        MainMenu,
        UI,
        HUD,
        Cinematic,
        Enviroment,
        Tooling
    }
}
