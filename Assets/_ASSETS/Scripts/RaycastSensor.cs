using System.Collections.Generic;
using UnityEngine;
public class RaycastSensor
{
    public RaycastHit HitInfo => hitInfo;
    public bool HasDetectedHit => hitInfo.collider != null;

    private Transform tr;
    private bool includeRotation = true;
    private float castLength = 1f;
    private Vector3 origin = Vector3.zero;
    private Vector3 castDirection = Vector3.forward;
    public LayerMask layermask = 255;

    private RaycastHit hitInfo;

    public RaycastSensor(Transform playerTransform)
    {
        tr = playerTransform;
    }

    public void Cast()
    {
        Vector3 worldOrigin = tr.TransformPoint(origin);
        Vector3 worldDirection = GetCastDirection();
        Physics.Raycast(worldOrigin, worldDirection, out hitInfo, castLength, layermask, QueryTriggerInteraction.Ignore);
    }

    public RaycastSensor SetIncludeRotation(bool include)
    {
        includeRotation = include;
        return this;
    }
    public RaycastSensor SetCastDirection(Vector3 direction)
    {
        if (direction != Vector3.zero)
            castDirection = direction.normalized;
        return this;
    }
    public RaycastSensor SetCastLength(float length)
    {
        if (length > 0)
            castLength = length;
        return this;
    }
    public RaycastSensor SetOrigin(Vector3 localOrigin)
    {
        origin = localOrigin;
        return this;
    }
    public RaycastSensor SetLayerMask(LayerMask mask)
    {
        layermask = mask;
        return this;
    }
    public RaycastSensor SetSettings(RaycastSensorSettings settings)
    {
        SetOrigin(settings.Origin);
        SetCastDirection(settings.Direction);
        SetCastLength(settings.CastLength);
        SetLayerMask(settings.LayerMask);
        return this;
    }
    Vector3 GetCastDirection()
    {
        if (!includeRotation) return castDirection;

        return tr.rotation * castDirection;
    }
    public RaycastSensorSettings GetSettings()
    {
        return new RaycastSensorSettings
        {
            Origin = origin,
            Direction = castDirection,
            CastLength = castLength,
            LayerMask = layermask
        };
    }
    public void DrawDebug()
    {
        if (!HasDetectedHit) return;

        Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.red, Time.deltaTime);
        float markerSize = 0.2f;
        Debug.DrawLine(hitInfo.point + Vector3.up * markerSize, hitInfo.point - Vector3.up * markerSize, Color.green, Time.deltaTime);
        Debug.DrawLine(hitInfo.point + Vector3.right * markerSize, hitInfo.point - Vector3.right * markerSize, Color.green, Time.deltaTime);
        Debug.DrawLine(hitInfo.point + Vector3.forward * markerSize, hitInfo.point - Vector3.forward * markerSize, Color.green, Time.deltaTime);
    }
}
public struct RaycastSensorSettings
{
    public Vector3 Origin;
    public Vector3 Direction;
    public float CastLength;
    public LayerMask LayerMask;
}

/// <summary>
/// Uses one sensor as several by changing settings.
/// Only for one transform.
/// </summary>
public class MultiRS<T>
{
    public bool IsAllHited
    {
        get
        {
            foreach (var info in sensorInfos.Values)
            {
                if (!info.Detected) return false;
            }
            return true;
        }
    }
    private RaycastSensor sensor;
    private Dictionary<T, RSInfo> sensorInfos = new Dictionary<T, RSInfo>();
    public MultiRS(Transform playerTransform)
    {
        sensor = new RaycastSensor(playerTransform).SetIncludeRotation(true);
    }
    public void UpdateAllSensors()
    {
        var keys = new List<T>(sensorInfos.Keys);
        foreach (var key in keys)
        {
            UpdateConcreteSensor(key);
        }
    }
    public void UpdateConcreteSensor(T sensorName)
    {
        if (!sensorInfos.ContainsKey(sensorName)) return;

        sensor.SetSettings(sensorInfos[sensorName].settings).Cast();
        sensorInfos[sensorName] = new RSInfo
        {
            settings = sensorInfos[sensorName].settings,
            hitInfo = sensor.HitInfo
        };
    }
    public MultiRS<T> AddSensor(T sensorName, RaycastSensorSettings settings)
    {
        if (!sensorInfos.ContainsKey(sensorName))
            sensorInfos.Add(sensorName, new RSInfo { settings = settings });
        else
            sensorInfos[sensorName] = new RSInfo { settings = settings };
        return this;
    }
    public RSInfo GetSensorInfo(T sensorName)
    {
        if (sensorInfos.ContainsKey(sensorName))
            return sensorInfos[sensorName];
        else
           return default;
    }
    public struct RSInfo
    {
        public RaycastHit hitInfo;
        public bool Detected => hitInfo.collider != null;
        public RaycastSensorSettings settings;
    }
}