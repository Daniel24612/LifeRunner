using System.Collections.Generic;
using UnityEngine;
public class RaycastSensor
{
    public virtual RaycastHit HitInfo => _hitInfo;
    public virtual bool HasDetectedHit => _hitInfo.collider != null;

    protected Transform _tr;

    protected Vector3 _currentPos;
    protected Quaternion _currentRot;

    protected bool _includeRotation = true;
    protected float _castLength = 1f;
    protected Vector3 _origin = Vector3.zero;
    protected Vector3 _castDirection = Vector3.forward;
    protected LayerMask _layermask = 255;

    private RaycastHit _hitInfo;

    protected List<RaycastHit> _hits;
    public RaycastSensor(Transform playerTransform)
    {
        _tr = playerTransform;
    }

    public virtual void Cast()
    {
        Vector3 worldOrigin = Vector3.zero;
        Vector3 worldDirection = _castDirection;
        
        worldOrigin = _tr.TransformPoint(_origin);
        worldDirection = GetCastDirection(_tr.rotation);
       
        Physics.Raycast(worldOrigin, worldDirection, out _hitInfo, _castLength, _layermask, QueryTriggerInteraction.Ignore);
       
    }
    public virtual RaycastSensor SetTransform(Transform transform)
    {
        _tr = transform;
        return this;
    }
    public virtual RaycastSensor SetIncludeRotation(bool include)
    {
        _includeRotation = include;
        return this;
    }
    public virtual RaycastSensor SetCastDirection(Vector3 direction)
    {
        if (direction != Vector3.zero)
            _castDirection = direction.normalized;
        return this;
    }
    public virtual RaycastSensor SetCastLength(float length)
    {
        if (length > 0)
            _castLength = length;
        return this;
    }
    public virtual RaycastSensor SetOrigin(Vector3 localOrigin)
    {
        _origin = localOrigin;
        return this;
    }
    public virtual RaycastSensor SetLayerMask(LayerMask mask)
    {
        _layermask = mask;
        return this;
    }
    public virtual RaycastSensor SetPos(Vector3 pos)
    {
        _currentPos = pos;
        return this;
    }
    public virtual RaycastSensor SetRot(Quaternion rot)
    {
        _currentRot = rot;
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
    protected Vector3 GetCastDirection(Quaternion rot)
    {
        if (!_includeRotation) return _castDirection;

        return rot * _castDirection;
    }
    public RaycastSensorSettings GetSettings()
    {
        return new RaycastSensorSettings
        {
            Origin = _origin,
            Direction = _castDirection,
            CastLength = _castLength,
            LayerMask = _layermask
        };
    }
}
public struct RaycastSensorSettings
{
    public Vector3 Origin;
    public Vector3 Direction;
    public float CastLength;
    public LayerMask LayerMask;
}

public class CapsuleCastSensor : RaycastSensor
{
    public override bool HasDetectedHit => _hits.Count > 0;
    public override RaycastHit HitInfo => _hits[0];

    private float _radius;
    private float _height;

    public CapsuleCastSensor(Transform transform) : base(transform)
    {

    }
    public override void Cast()
    {
        Vector3 worldOrigin = Vector3.zero;
        Quaternion worldRotation = Quaternion.identity;

        if (_tr != null)
        {
            worldOrigin = _tr.TransformPoint(_origin);
            worldRotation = _tr.rotation;
        }

        if (_currentPos != Vector3.zero || _currentRot != Quaternion.identity)
        {
            // Учитываем кастомную позицию, если она была задана через SetPos
            worldOrigin = _currentPos + (_currentRot * _origin);
            worldRotation = _currentRot;
        }

        Vector3 worldDirection = GetCastDirection(worldRotation);

        // Вычисляем центры сфер капсулы в мировых координатах
        Vector3 p1 = worldOrigin + worldRotation * (Vector3.up * _radius);
        Vector3 p2 = worldOrigin + worldRotation * (Vector3.up * (_height - _radius));

        var hits = Physics.CapsuleCastAll(p1, p2, _radius, worldDirection, _castLength, _layermask);

        _hits = new List<RaycastHit>(hits);

        _currentPos = Vector3.zero;
        _currentRot = Quaternion.identity;
    }

    public CapsuleCastSensor SetRadius(float radius)
    {
        _radius = radius;
        return this;
    }
    public CapsuleCastSensor SetHeight(float height)
    {
        _height = height;
        return this;
    }


    public override RaycastSensor SetCastDirection(Vector3 direction)
    {
        return base.SetCastDirection(direction) as CapsuleCastSensor;
    }
    public override RaycastSensor SetIncludeRotation(bool include)
    {
        return base.SetIncludeRotation(include) as CapsuleCastSensor;
    }
    public override RaycastSensor SetOrigin(Vector3 localOrigin)
    {
        return base.SetOrigin(localOrigin) as CapsuleCastSensor;
    }
    public override RaycastSensor SetPos(Vector3 pos)
    {
        return base.SetPos(pos) as CapsuleCastSensor;
    }
    public override RaycastSensor SetRot(Quaternion rot)
    {
        return base.SetRot(rot) as CapsuleCastSensor;
    }
    public override RaycastSensor SetCastLength(float length)
    {
        return base.SetCastLength(length) as CapsuleCastSensor;
    }
    public override RaycastSensor SetLayerMask(LayerMask mask)
    {
        return base.SetLayerMask(mask) as CapsuleCastSensor;
    }
    public override RaycastSensor SetTransform(Transform transform)
    {
        return base.SetTransform(transform) as CapsuleCastSensor;
    }
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
    public MultiRS<T> AddOrSetSensor(T sensorName, RaycastSensorSettings settings)
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
    public void SetNewSensor(RaycastSensor sensor)
    {

    }
    public struct RSInfo
    {
        public RaycastHit hitInfo;
        public bool Detected => hitInfo.collider != null;
        public RaycastSensorSettings settings;
    }
}