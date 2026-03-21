using UnityEngine;
namespace MyScriptUtils
{
    public static class LayerMaskUtils
    {
        public static bool IsInLayerMask(LayerMask myLayers, LayerMask objLayer)
        {
            return ((myLayers & (1 << objLayer)) != 0);
        }
    }
}