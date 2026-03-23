using UnityEngine;
namespace UnityUtils
{
    public static class LayerMaskUtils
    {
        public static bool Contains(LayerMask myLayers, LayerMask objLayer)
        {
            return ((myLayers & (1 << objLayer)) != 0);
        }
    }
}