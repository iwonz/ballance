using UnityEngine;

namespace BallancePhysics.Wapper
{
  
  [AddComponentMenu("BallancePhysics/PhysicsConstraintForce")]
  [RequireComponent(typeof(PhysicsObject))]
  
  public class PhysicsConstraintForce : MonoBehaviour
  {
    [Tooltip("Constant force magnitude.")]
    [SerializeField]
    private Vector3 m_ForcePosition;
    [Tooltip("Constant force direction.")]
    [SerializeField]
    private Vector3 m_ForceDirection;
    [Tooltip("Reference frame for the constant force application point.")]
    [SerializeField]
    private Transform m_ForcePositionRef;
    [Tooltip("Reference frame for the constant force direction.")]
    [SerializeField]
    private Transform m_ForceDirectionRef;
    [Tooltip("Force magnitude.")]
    [SerializeField]
    private float m_Force = 1.0f;

    private PhysicsConstantForceData _ForceData = null;
    private PhysicsObject _PhysicsObject = null;
    private PhysicsObject PhysicsObject
    {
      get
      {
        if (_PhysicsObject == null)
          _PhysicsObject = GetComponent<PhysicsObject>();
        return _PhysicsObject;
      }
    }

    /// <summary>
    /// Set the constant force magnitude.
    /// </summary>
    /// <value></value>
    
    public float Force
    {
      get => m_Force; 
      set
      {
        m_Force = value; 
        if(_ForceData != null)
          _ForceData.Force = value;
      }
    }
    /// <summary>
    /// Set the constant force magnitude.
    /// </summary>
    /// <value></value>
    
    public Vector3 ForcePosition
    {
      get => m_ForcePosition; 
      set
      {
        m_ForcePosition = value; 
        if(_ForceData != null)
          _ForceData.Pos = value;
      }
    }
    /// <summary>
    /// Set the constant force direction.
    /// </summary>
    /// <value></value>
    
    public Vector3 ForceDirection
    {
      get => m_ForceDirection; 
      set
      {
        m_ForceDirection = value;
        if(_ForceData != null)
          _ForceData.Direction = value;
      }
    }
    /// <summary>
    /// Position reference for the constant force applied to this object.
    /// </summary>
    /// <value></value>
    
    public Transform ForcePositionRef
    {
      get => m_ForcePositionRef; 
      set
      {
        m_ForcePositionRef = value;
        if(_ForceData != null)
          _ForceData.PositionRef = value;
      }
    }
    /// <summary>
    /// Direction reference for the constant force applied to this object.
    /// </summary>
    /// <value></value>
    
    public Transform ForceDirectionRef
    {
      get => m_ForceDirectionRef; 
      set
      {
        m_ForceDirectionRef = value;
        if(_ForceData != null)
          _ForceData.DirectionRef = value;
      }
    }

    private void DeleteForceData()
    {
      if (_ForceData != null)
      {
        _ForceData.Delete();
        _ForceData = null;
      }
    }
    private void AddForceData()
    {
      if (_ForceData == null)
        _ForceData = PhysicsObject.AddConstantForceWithPositionAndRef(m_Force, m_ForceDirection, m_ForcePosition, m_ForceDirectionRef, m_ForcePositionRef);
    }

    private void OnEnable()
    {
      AddForceData();
    }
    private void OnDisable()
    {
      DeleteForceData();
    }
  }
}