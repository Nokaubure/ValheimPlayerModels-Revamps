#if UNITY_WEBGL || (UNITY_2022_1_OR_NEWER && ENABLE_IL2CPP)
// No multithread
#else
#define ENABLE_MULTITHREAD
#endif

using UnityEngine;
using System.Collections.Generic;
using System.Threading;

[AddComponentMenu("Dynamic Bone/Dynamic Bone")]
public class DynamicBone : MonoBehaviour
{
#if UNITY_5_3_OR_NEWER
    [Tooltip("The roots of the transform hierarchy to apply physics.")]
#endif
    public Transform m_Root = null;
    public List<Transform> m_Roots = null;

#if UNITY_5_3_OR_NEWER
    [Tooltip("Internal physics simulation rate.")]
#endif
    public float m_UpdateRate = 60.0f;

    public enum UpdateMode
    {
        Normal,
        AnimatePhysics,
        UnscaledTime,
        Default
    }
    public UpdateMode m_UpdateMode = UpdateMode.Default;

#if UNITY_5_3_OR_NEWER
    [Tooltip("How much the bones slowed down.")]
#endif
    [Range(0, 1)]
    public float m_Damping = 0.1f;
    public AnimationCurve m_DampingDistrib = null;

#if UNITY_5_3_OR_NEWER
    [Tooltip("How much the force applied to return each bone to original orientation.")]
#endif
    [Range(0, 1)]
    public float m_Elasticity = 0.1f;
    public AnimationCurve m_ElasticityDistrib = null;

#if UNITY_5_3_OR_NEWER
    [Tooltip("How much bone's original orientation are preserved.")]
#endif
    [Range(0, 1)]
    public float m_Stiffness = 0.1f;
    public AnimationCurve m_StiffnessDistrib = null;

    // Separate stiffness and damping for yaw and pitch axes
#if UNITY_5_3_OR_NEWER
    [Tooltip("How stiff the spring is in the local yaw rotational axis.")]
#endif
    [Range(0, 1)]
    public float m_YawStiffness = 0.1f;
#if UNITY_5_3_OR_NEWER
    [Tooltip("Damping along the local yaw rotational axis. Small values result in dramatic jiggling. Larger values result in more subtle motions.")]
#endif
    [Range(0, 1)]
    public float m_YawDamping = 0.1f;

#if UNITY_5_3_OR_NEWER
    [Tooltip("How stiff the spring is in the local pitch rotational axis.")]
#endif
    [Range(0, 1)]
    public float m_PitchStiffness = 0.1f;
#if UNITY_5_3_OR_NEWER
    [Tooltip("Damping along the local pitch rotational axis. Small values result in dramatic jiggling. Larger values result in more subtle motions.")]
#endif
    [Range(0, 1)]
    public float m_PitchDamping = 0.1f;

#if UNITY_5_3_OR_NEWER
    [Tooltip("Specify the maximum angular deflection allowed (degrees). If 0, ignored.")]
#endif
    [Range(0, 360)]
    public float m_AngleConstraint = 0f;

#if UNITY_5_3_OR_NEWER
    [Tooltip("Lock translation updates; only apply rotations to bones when enabled.")]
#endif
    public bool m_LockTranslation = false;

#if UNITY_5_3_OR_NEWER
    [Tooltip("How much character's position change is ignored in physics simulation.")]
#endif
    [Range(0, 1)]
    public float m_Inert = 0;
    public AnimationCurve m_InertDistrib = null;

#if UNITY_5_3_OR_NEWER
    [Tooltip("How much the bones slowed down when collide.")]
#endif
    public float m_Friction = 0;
    public AnimationCurve m_FrictionDistrib = null;

#if UNITY_5_3_OR_NEWER
    [Tooltip("Each bone can be a sphere to collide with colliders. Radius describe sphere's size.")]
#endif
    public float m_Radius = 0;
    public AnimationCurve m_RadiusDistrib = null;

#if UNITY_5_3_OR_NEWER
    [Tooltip("If End Length is not zero, an extra bone is generated at the end of transform hierarchy.")]
#endif
    public float m_EndLength = 0;

#if UNITY_5_3_OR_NEWER
    [Tooltip("If End Offset is not zero, an extra bone is generated at the end of transform hierarchy.")]
#endif
    public Vector3 m_EndOffset = Vector3.zero;

#if UNITY_5_3_OR_NEWER
    [Tooltip("The force apply to bones. Partial force apply to character's initial pose is cancelled out.")]
#endif
    public Vector3 m_Gravity = Vector3.zero;

    // new: allow adjusting angle of generated end bone when using End Length
#if UNITY_5_3_OR_NEWER
    [Tooltip("Euler rotation (degrees) applied to the end bone direction when using End Length.")]
#endif
    public Vector3 m_EndRotation = Vector3.zero;

#if UNITY_5_3_OR_NEWER
    [Tooltip("The force apply to bones.")]
#endif
    public Vector3 m_Force = Vector3.zero;

    // If true, interpret m_Gravity and m_Force in world space (global axes). If false, interpret in root local space.
#if UNITY_5_3_OR_NEWER
    [Tooltip("If enabled, gravity and force are treated in world (global) space so negative Y always points down.")]
#endif
    public bool m_UseGlobalGravity = true;

#if UNITY_5_3_OR_NEWER
    [Tooltip("Control how physics blends with existing animation.")]
#endif
    [Range(0, 1)
    ]
    public float m_BlendWeight = 1.0f;

#if UNITY_5_3_OR_NEWER
    [Tooltip("Collider objects interact with the bones.")]
#endif
    public List<DynamicBoneColliderBase> m_Colliders = null;

#if UNITY_5_3_OR_NEWER
    [Tooltip("Bones exclude from physics simulation.")]
#endif
    public List<Transform> m_Exclusions = null;

#if UNITY_5_3_OR_NEWER
    [Tooltip("If not empty, only bones in this list (or their descendants) will be affected by the Dynamic Bone; other bones under the root will be ignored.")]
#endif
    public List<Transform> m_Inclusions = null;

    public enum FreezeAxis
    {
        None, X, Y, Z
    }
#if UNITY_5_3_OR_NEWER
    [Tooltip("Constrain bones to move on specified plane.")]
#endif	
    public FreezeAxis m_FreezeAxis = FreezeAxis.None;

#if UNITY_5_3_OR_NEWER
    [Tooltip("Disable physics simulation automatically if character is far from camera or player.")]
#endif
    public bool m_DistantDisable = false;
    public Transform m_ReferenceObject = null;
    public float m_DistanceToObject = 20;

    [HideInInspector]
    public bool m_Multithread = true;

    Vector3 m_ObjectMove;
    Vector3 m_ObjectPrevPosition;
    float m_ObjectScale;

    float m_Time = 0;
    float m_Weight = 1.0f;
    bool m_DistantDisabled = false;
    int m_PreUpdateCount = 0;

    // previous values used to detect changes in editor/runtime so we can rebuild particle list
    float m_PrevEndLength = 0f;
    Vector3 m_PrevEndOffset = Vector3.zero;
    Vector3 m_PrevEndRotation = Vector3.zero;

    class Particle
    {
        public Transform m_Transform;
        public int m_ParentIndex;
        public int m_ChildCount;
        public float m_Damping;
        public float m_Elasticity;
        public float m_Stiffness;
        public float m_Inert;
        public float m_Friction;
        public float m_Radius;
        public float m_BoneLength;
        public bool m_isCollide;
        public bool m_TransformNotNull;

        public Vector3 m_Position;
        public Vector3 m_PrevPosition;
        public Vector3 m_EndOffset;
        public Vector3 m_InitLocalPosition;
        public Quaternion m_InitLocalRotation;

        // prepare data
        public Vector3 m_TransformPosition;
        public Vector3 m_TransformLocalPosition;
        public Matrix4x4 m_TransformLocalToWorldMatrix;
    }

    class ParticleTree
    {
        public Transform m_Root;
        public Vector3 m_LocalGravity;
        public Matrix4x4 m_RootWorldToLocalMatrix;
        public float m_BoneTotalLength;
        public List<Particle> m_Particles = new List<Particle>();

        // prepare data
        public Vector3 m_RestGravity;
        public Vector3 m_RestForce;
    }

    List<ParticleTree> m_ParticleTrees = new List<ParticleTree>();

    // prepare data
    float m_DeltaTime;
    List<DynamicBoneColliderBase> m_EffectiveColliders;

#if ENABLE_MULTITHREAD
    // multithread
    bool m_WorkAdded = false;
    static List<DynamicBone> s_PendingWorks = new List<DynamicBone>();
    static List<DynamicBone> s_EffectiveWorks = new List<DynamicBone>();
    static AutoResetEvent s_AllWorksDoneEvent;
    static int s_RemainWorkCount;
    static Semaphore s_WorkQueueSemaphore;
    static int s_WorkQueueIndex;
#endif

    static int s_UpdateCount;
    static int s_PrepareFrame;

    void Start()
    {
        SetupParticles();
    }

    void FixedUpdate()
    {
        if (m_UpdateMode == UpdateMode.AnimatePhysics)
        {
            PreUpdate();
        }
    }

    void Update()
    {
        if (m_UpdateMode != UpdateMode.AnimatePhysics)
        {
            PreUpdate();
        }

#if ENABLE_MULTITHREAD
        if (m_PreUpdateCount > 0 && m_Multithread)
        {
            AddPendingWork(this);
            m_WorkAdded = true;
        }
#endif
        ++s_UpdateCount;
    }

    void LateUpdate()
    {
        if (m_PreUpdateCount == 0)
            return;

        if (s_UpdateCount > 0)
        {
            s_UpdateCount = 0;
            ++s_PrepareFrame;
        }

        SetWeight(m_BlendWeight);

#if ENABLE_MULTITHREAD
        if (m_WorkAdded)
        {
            m_WorkAdded = false;
            ExecuteWorks();
        }
        else
#endif
        {
            CheckDistance();
            if (IsNeedUpdate())
            {
                Prepare();
                UpdateParticles();
                ApplyParticlesToTransforms();
            }
        }

        m_PreUpdateCount = 0;
    }

    void Prepare()
    {
        m_DeltaTime = Time.deltaTime;
#if UNITY_5_3_OR_NEWER
        if (m_UpdateMode == UpdateMode.UnscaledTime)
        {
            m_DeltaTime = Time.unscaledDeltaTime;
        }
        else if (m_UpdateMode == UpdateMode.AnimatePhysics)
        {
            m_DeltaTime = Time.fixedDeltaTime * m_PreUpdateCount;
        }
#endif

        m_ObjectScale = Mathf.Abs(transform.lossyScale.x);
        m_ObjectMove = transform.position - m_ObjectPrevPosition;
        m_ObjectPrevPosition = transform.position;

        for (int i = 0; i < m_ParticleTrees.Count; ++i)
        {
            ParticleTree pt = m_ParticleTrees[i];
            // interpret gravity and force either in world space (global axes) or in the root's local space
            if (m_UseGlobalGravity)
            {
                pt.m_RestGravity = m_Gravity;
                pt.m_RestForce = m_Force;
            }
            else
            {
                pt.m_RestGravity = pt.m_Root.TransformDirection(m_Gravity);
                pt.m_RestForce = pt.m_Root.TransformDirection(m_Force);
            }

            for (int j = 0; j < pt.m_Particles.Count; ++j)
            {
                Particle p = pt.m_Particles[j];
                if (p.m_TransformNotNull)
                {
                    p.m_TransformPosition = p.m_Transform.position;
                    p.m_TransformLocalPosition = p.m_Transform.localPosition;
                    p.m_TransformLocalToWorldMatrix = p.m_Transform.localToWorldMatrix;
                }
            }
        }

        if (m_EffectiveColliders != null)
        {
            m_EffectiveColliders.Clear();
        }

        if (m_Colliders != null)
        {
            for (int i = 0; i < m_Colliders.Count; ++i)
            {
                DynamicBoneColliderBase c = m_Colliders[i];
                if (c != null && c.enabled)
                {
                    if (m_EffectiveColliders == null)
                    {
                        m_EffectiveColliders = new List<DynamicBoneColliderBase>();
                    }
                    m_EffectiveColliders.Add(c);

                    if (c.PrepareFrame != s_PrepareFrame)       // colliders used by many dynamic bones only prepares once
                    {
                        c.Prepare();
                        c.PrepareFrame = s_PrepareFrame;
                    }
                }
            }
        }
    }

    bool IsNeedUpdate()
    {
        return m_Weight > 0 && !(m_DistantDisable && m_DistantDisabled);
    }

    void PreUpdate()
    {
        if (IsNeedUpdate())
        {
            InitTransforms();
        }
        ++m_PreUpdateCount;
    }

    void CheckDistance()
    {
        if (!m_DistantDisable)
            return;

        Transform rt = m_ReferenceObject;
        if (rt == null && Camera.main != null)
        {
            rt = Camera.main.transform;
        }

        if (rt != null)
        {
            float d2 = (rt.position - transform.position).sqrMagnitude;
            bool disable = d2 > m_DistanceToObject * m_DistanceToObject;
            if (disable != m_DistantDisabled)
            {
                if (!disable)
                {
                    ResetParticlesPosition();
                }
                m_DistantDisabled = disable;
            }
        }
    }

    void OnEnable()
    {
        ResetParticlesPosition();
    }

    void OnDisable()
    {
        InitTransforms();
    }

    void OnValidate()
    {
        m_UpdateRate = Mathf.Max(m_UpdateRate, 0);
        m_Damping = Mathf.Clamp01(m_Damping);
        m_Elasticity = Mathf.Clamp01(m_Elasticity);
        m_Stiffness = Mathf.Clamp01(m_Stiffness);
        m_YawStiffness = Mathf.Clamp01(m_YawStiffness);
        m_YawDamping = Mathf.Clamp01(m_YawDamping);
        m_PitchStiffness = Mathf.Clamp01(m_PitchStiffness);
        m_PitchDamping = Mathf.Clamp01(m_PitchDamping);
        m_AngleConstraint = Mathf.Clamp(m_AngleConstraint, 0f, 360f);
        m_Inert = Mathf.Clamp01(m_Inert);
        m_Friction = Mathf.Clamp01(m_Friction);
        m_Radius = Mathf.Max(m_Radius, 0);

        if (Application.isEditor && Application.isPlaying)
        {
            // if roots or end parameters changed at runtime in editor, rebuild particles
            if (IsRootChanged() || m_EndLength != m_PrevEndLength || m_EndOffset != m_PrevEndOffset || m_EndRotation != m_PrevEndRotation)
            {
                InitTransforms();
                SetupParticles();
            }
            else
            {
                UpdateParameters();
            }
        }
    }

    bool IsRootChanged()
    {
        var roots = new List<Transform>();
        if (m_Root != null)
        {
            roots.Add(m_Root);
        }

        if (m_Roots != null)
        {
            foreach (var root in m_Roots)
            {
                if (root != null && !roots.Contains(root))
                {
                    roots.Add(root);
                }
            }
        }

        if (roots.Count != m_ParticleTrees.Count)
            return true;

        for (int i = 0; i < roots.Count; ++i)
        {
            if (roots[i] != m_ParticleTrees[i].m_Root)
                return true;
        }

        return false;
    }

    void OnDidApplyAnimationProperties()
    {
        UpdateParameters();
    }

    void OnDrawGizmosSelected()
    {
        if (!enabled)
            return;

        if (Application.isEditor && !Application.isPlaying && transform.hasChanged)
        {
            //InitTransforms();
            SetupParticles();
        }

        Gizmos.color = Color.white;
        for (int i = 0; i < m_ParticleTrees.Count; ++i)
        {
            DrawGizmos(m_ParticleTrees[i]);
        }
    }

    void DrawGizmos(ParticleTree pt)
    {
        for (int i = 0; i < pt.m_Particles.Count; ++i)
        {
            Particle p = pt.m_Particles[i];
            if (p.m_ParentIndex >= 0)
            {
                Particle p0 = pt.m_Particles[p.m_ParentIndex];
                Gizmos.DrawLine(p.m_Position, p0.m_Position);
            }

            if (p.m_Radius > 0)
            {
                Gizmos.DrawWireSphere(p.m_Position, p.m_Radius * m_ObjectScale);
            }

            // draw angle constraint cone gizmo (from parent)
            if (p.m_ParentIndex >= 0 && m_AngleConstraint > 0f)
            {
                Particle p0 = pt.m_Particles[p.m_ParentIndex];
                // compute rest direction
                Matrix4x4 m0 = p0.m_TransformLocalToWorldMatrix;
                m0.SetColumn(3, p0.m_Position);
                Vector3 restPos = p.m_TransformNotNull ? m0.MultiplyPoint3x4(p.m_TransformLocalPosition) : m0.MultiplyPoint3x4(p.m_EndOffset);
                Vector3 restDir = (restPos - p0.m_Position).normalized;
                float len = ((p.m_TransformNotNull) ? (p0.m_TransformPosition - p.m_TransformPosition).magnitude : p0.m_TransformLocalToWorldMatrix.MultiplyVector(p.m_EndOffset).magnitude);

                int seg = 16;
                Vector3 up = Mathf.Abs(restDir.y) < 0.99f ? Vector3.up : Vector3.right;
                Vector3 u = Vector3.Cross(restDir, up).normalized;
                if (u.sqrMagnitude < 1e-6f) u = Vector3.Cross(restDir, Vector3.forward).normalized;
                Vector3 tilt = Quaternion.AngleAxis(m_AngleConstraint, u) * restDir;
                Vector3 prev = p0.m_Position + Quaternion.AngleAxis(0, restDir) * tilt * len;
                for (int s = 1; s <= seg; ++s)
                {
                    float ang = 360f * s / seg;
                    Vector3 cur = p0.m_Position + Quaternion.AngleAxis(ang, restDir) * tilt * len;
                    Gizmos.DrawLine(prev, cur);
                    Gizmos.DrawLine(p0.m_Position, cur);
                    prev = cur;
                }
            }
        }
    }

    public void SetWeight(float w)
    {
        if (m_Weight != w)
        {
            if (w == 0)
            {
                InitTransforms();
            }
            else if (m_Weight == 0)
            {
                ResetParticlesPosition();
            }
            m_Weight = m_BlendWeight = w;
        }
    }

    public float GetWeight()
    {
        return m_Weight;
    }

    void UpdateParticles()
    {
        if (m_ParticleTrees.Count <= 0)
            return;

        int loop = 1;
        float timeVar = 1;
        float dt = m_DeltaTime;

        if (m_UpdateMode == UpdateMode.Default)
        {
            if (m_UpdateRate > 0)
            {
                timeVar = dt * m_UpdateRate;
            }
        }
        else
        {
            if (m_UpdateRate > 0)
            {
                float frameTime = 1.0f / m_UpdateRate;
                m_Time += dt;
                loop = 0;

                while (m_Time >= frameTime)
                {
                    m_Time -= frameTime;
                    if (++loop >= 3)
                    {
                        m_Time = 0;
                        break;
                    }
                }
            }
        }

        if (loop > 0)
        {
            for (int i = 0; i < loop; ++i)
            {
                UpdateParticles1(timeVar, i);
                UpdateParticles2(timeVar);
            }
        }
        else
        {
            SkipUpdateParticles();
        }
    }

    public void SetupParticles()
    {
        m_ParticleTrees.Clear();

        if (m_Root != null)
        {
            AppendParticleTree(m_Root);
        }

        if (m_Roots != null)
        {
            for (int i = 0; i < m_Roots.Count; ++i)
            {
                Transform root = m_Roots[i];
                if (root == null)
                    continue;

                if (m_ParticleTrees.Exists(x => x.m_Root == root))
                    continue;

                AppendParticleTree(root);
            }
        }

        m_ObjectScale = Mathf.Abs(transform.lossyScale.x);
        m_ObjectPrevPosition = transform.position;
        m_ObjectMove = Vector3.zero;

        for (int i = 0; i < m_ParticleTrees.Count; ++i)
        {
            ParticleTree pt = m_ParticleTrees[i];
            AppendParticles(pt, pt.m_Root, -1, 0);
        }

        UpdateParameters();

        // store previous end values so changes in the inspector during play trigger a rebuild
        m_PrevEndLength = m_EndLength;
        m_PrevEndOffset = m_EndOffset;
        m_PrevEndRotation = m_EndRotation;
    }

    void AppendParticleTree(Transform root)
    {
        if (root == null)
            return;

        var pt = new ParticleTree();
        pt.m_Root = root;
        pt.m_RootWorldToLocalMatrix = root.worldToLocalMatrix;
        m_ParticleTrees.Add(pt);
    }

    void AppendParticles(ParticleTree pt, Transform b, int parentIndex, float boneLength)
    {
        var p = new Particle();
        p.m_Transform = b;
        p.m_TransformNotNull = b != null;
        p.m_ParentIndex = parentIndex;

        if (b != null)
        {
            p.m_Position = p.m_PrevPosition = b.position;
            p.m_InitLocalPosition = b.localPosition;
            p.m_InitLocalRotation = b.localRotation;
        }
        else 	// end bone
        {
            Transform pb = pt.m_Particles[parentIndex].m_Transform;
            if (m_EndLength > 0)
            {
                Transform ppb = pb.parent;
                // compute default direction (from grandparent to parent) in world space
                Vector3 defaultDir;
                if (ppb != null)
                {
                    defaultDir = (pb.position - ppb.position).normalized;
                }
                else
                {
                    defaultDir = pb.TransformDirection(Vector3.right).normalized;
                }

                // apply end rotation (Euler degrees) relative to parent orientation
                Quaternion rot = Quaternion.Euler(m_EndRotation);
                Vector3 rotatedDirWorld = pb.rotation * rot * defaultDir;

                // convert to local offset direction and scale by length
                p.m_EndOffset = pb.InverseTransformDirection(rotatedDirWorld) * m_EndLength;
            }
            else
            {
                p.m_EndOffset = pb.InverseTransformPoint(transform.TransformDirection(m_EndOffset) + pb.position);
            }
            p.m_Position = p.m_PrevPosition = pb.TransformPoint(p.m_EndOffset);
            p.m_InitLocalPosition = Vector3.zero;
            p.m_InitLocalRotation = Quaternion.identity;
        }

        if (parentIndex >= 0)
        {
            boneLength += (pt.m_Particles[parentIndex].m_Transform.position - p.m_Position).magnitude;
            p.m_BoneLength = boneLength;
            pt.m_BoneTotalLength = Mathf.Max(pt.m_BoneTotalLength, boneLength);
            ++pt.m_Particles[parentIndex].m_ChildCount;
        }

        int index = pt.m_Particles.Count;
        pt.m_Particles.Add(p);

        if (b != null)
        {
            for (int i = 0; i < b.childCount; ++i)
            {
                Transform child = b.GetChild(i);

                bool exclude;

                // If an inclusion list is provided, only include children that are in the inclusion list
                if (m_Inclusions != null && m_Inclusions.Count > 0)
                {
                    exclude = !IsInInclusions(child);
                }
                else
                {
                    exclude = false;
                    if (m_Exclusions != null)
                    {
                        exclude = m_Exclusions.Contains(child);
                    }
                }

                if (!exclude)
                {
                    AppendParticles(pt, child, index, boneLength);
                }
                else if (m_EndLength > 0 || m_EndOffset != Vector3.zero)
                {
                    AppendParticles(pt, null, index, boneLength);
                }
            }

            if (b.childCount == 0 && (m_EndLength > 0 || m_EndOffset != Vector3.zero))
            {
                AppendParticles(pt, null, index, boneLength);
            }
        }
    }

    // Returns true if the transform itself is in the inclusion list or any of its descendants are.
    bool IsInInclusions(Transform t)
    {
        if (m_Inclusions == null || m_Inclusions.Count == 0 || t == null)
            return false;

        if (m_Inclusions.Contains(t))
            return true;

        for (int i = 0; i < t.childCount; ++i)
        {
            if (IsInInclusions(t.GetChild(i)))
                return true;
        }

        return false;
    }

    public void UpdateParameters()
    {
        SetWeight(m_BlendWeight);

        for (int i = 0; i < m_ParticleTrees.Count; ++i)
        {
            UpdateParameters(m_ParticleTrees[i]);
        }
    }

    void UpdateParameters(ParticleTree pt)
    {
        // m_LocalGravity = m_Root.InverseTransformDirection(m_Gravity);
        pt.m_LocalGravity = pt.m_RootWorldToLocalMatrix.MultiplyVector(m_Gravity).normalized * m_Gravity.magnitude;

        for (int i = 0; i < pt.m_Particles.Count; ++i)
        {
            Particle p = pt.m_Particles[i];
            p.m_Damping = m_Damping;
            p.m_Elasticity = m_Elasticity;
            p.m_Stiffness = m_Stiffness;
            p.m_Inert = m_Inert;
            p.m_Friction = m_Friction;
            p.m_Radius = m_Radius;

            if (pt.m_BoneTotalLength > 0)
            {
                float a = p.m_BoneLength / pt.m_BoneTotalLength;
                if (m_DampingDistrib != null && m_DampingDistrib.keys.Length > 0)
                    p.m_Damping *= m_DampingDistrib.Evaluate(a);
                if (m_ElasticityDistrib != null && m_ElasticityDistrib.keys.Length > 0)
                    p.m_Elasticity *= m_ElasticityDistrib.Evaluate(a);
                if (m_StiffnessDistrib != null && m_StiffnessDistrib.keys.Length > 0)
                    p.m_Stiffness *= m_StiffnessDistrib.Evaluate(a);
                if (m_InertDistrib != null && m_InertDistrib.keys.Length > 0)
                    p.m_Inert *= m_InertDistrib.Evaluate(a);
                if (m_FrictionDistrib != null && m_FrictionDistrib.keys.Length > 0)
                    p.m_Friction *= m_FrictionDistrib.Evaluate(a);
                if (m_RadiusDistrib != null && m_RadiusDistrib.keys.Length > 0)
                    p.m_Radius *= m_RadiusDistrib.Evaluate(a);
            }

            p.m_Damping = Mathf.Clamp01(p.m_Damping);
            p.m_Elasticity = Mathf.Clamp01(p.m_Elasticity);
            p.m_Stiffness = Mathf.Clamp01(p.m_Stiffness);
            p.m_Inert = Mathf.Clamp01(p.m_Inert);
            p.m_Friction = Mathf.Clamp01(p.m_Friction);
            p.m_Radius = Mathf.Max(p.m_Radius, 0);
        }
    }

    void InitTransforms()
    {
        for (int i = 0; i < m_ParticleTrees.Count; ++i)
        {
            InitTransforms(m_ParticleTrees[i]);
        }
    }

    void InitTransforms(ParticleTree pt)
    {
        for (int i = 0; i < pt.m_Particles.Count; ++i)
        {
            Particle p = pt.m_Particles[i];
            if (p.m_TransformNotNull)
            {
                p.m_Transform.localPosition = p.m_InitLocalPosition;
                p.m_Transform.localRotation = p.m_InitLocalRotation;
            }
        }
    }

    public void ResetParticlesPosition()
    {
        for (int i = 0; i < m_ParticleTrees.Count; ++i)
        {
            ResetParticlesPosition(m_ParticleTrees[i]);
        }

        m_ObjectPrevPosition = transform.position;
    }

    void ResetParticlesPosition(ParticleTree pt)
    {
        for (int i = 0; i < pt.m_Particles.Count; ++i)
        {
            Particle p = pt.m_Particles[i];
            if (p.m_TransformNotNull)
            {
                p.m_Position = p.m_PrevPosition = p.m_Transform.position;
            }
            else	// end bone
            {
                Transform pb = pt.m_Particles[p.m_ParentIndex].m_Transform;
                p.m_Position = p.m_PrevPosition = pb.TransformPoint(p.m_EndOffset);
            }
            p.m_isCollide = false;
        }
    }

    void UpdateParticles1(float timeVar, int loopIndex)
    {
        for (int i = 0; i < m_ParticleTrees.Count; ++i)
        {
            UpdateParticles1(m_ParticleTrees[i], timeVar, loopIndex);
        }
    }

    void UpdateParticles1(ParticleTree pt, float timeVar, int loopIndex)
    {
        Vector3 objectMove = loopIndex == 0 ? m_ObjectMove : Vector3.zero;      // only first loop consider object move

        for (int i = 0; i < pt.m_Particles.Count; ++i)
        {
            Particle p = pt.m_Particles[i];
            if (p.m_ParentIndex >= 0)
            {
                // verlet integration
                Vector3 v = p.m_Position - p.m_PrevPosition;
                Vector3 rmove = objectMove * p.m_Inert;
                p.m_PrevPosition = p.m_Position + rmove;

                // compute damping: support separate yaw/pitch damping when provided
                float damping = p.m_Damping;
                if (m_YawDamping != 0f || m_PitchDamping != 0f)
                {
                    Particle parent = pt.m_Particles[p.m_ParentIndex];
                    Matrix4x4 m0 = parent.m_TransformLocalToWorldMatrix;
                    m0.SetColumn(3, parent.m_Position);
                    Vector3 restPos = parent.m_TransformNotNull ? m0.MultiplyPoint3x4(p.m_TransformLocalPosition) : m0.MultiplyPoint3x4(p.m_EndOffset);
                    Vector3 restDir = (restPos - parent.m_Position).normalized;
                    Vector3 right = parent.m_TransformLocalToWorldMatrix.GetColumn(0).normalized;
                    Vector3 up = parent.m_TransformLocalToWorldMatrix.GetColumn(1).normalized;
                    float wy = Mathf.Abs(Vector3.Dot(restDir, right));
                    float wp = Mathf.Abs(Vector3.Dot(restDir, up));
                    float sum = wy + wp;
                    if (sum > 0f)
                    {
                        wy /= sum;
                        wp /= sum;
                    }
                    else
                    {
                        wy = wp = 0.5f;
                    }

                    float baseYaw = m_YawDamping;
                    float basePitch = m_PitchDamping;
                    if (pt.m_BoneTotalLength > 0)
                    {
                        float a = p.m_BoneLength / pt.m_BoneTotalLength;
                        if (m_DampingDistrib != null && m_DampingDistrib.keys.Length > 0)
                        {
                            float factor = m_DampingDistrib.Evaluate(a);
                            baseYaw *= factor;
                            basePitch *= factor;
                        }
                    }
                    damping = wy * baseYaw + wp * basePitch;
                }

                if (p.m_isCollide)
                {
                    damping += p.m_Friction;
                    if (damping > 1)
                    {
                        damping = 1;
                    }
                    p.m_isCollide = false;
                }
                p.m_Position += v * (1 - damping) + ((pt.m_RestGravity + pt.m_RestForce) * (m_ObjectScale * timeVar)) + rmove;
            }
            else
            {
                p.m_PrevPosition = p.m_Position;
                p.m_Position = p.m_TransformPosition;
            }
        }
    }

    void UpdateParticles2(float timeVar)
    {
        for (int i = 0; i < m_ParticleTrees.Count; ++i)
        {
            UpdateParticles2(m_ParticleTrees[i], timeVar);
        }
    }

    void UpdateParticles2(ParticleTree pt, float timeVar)
    {
        var movePlane = new Plane();

        for (int i = 1; i < pt.m_Particles.Count; ++i)
        {
            Particle p = pt.m_Particles[i];
            Particle p0 = pt.m_Particles[p.m_ParentIndex];

            float restLen;
            if (p.m_TransformNotNull)
            {
                restLen = (p0.m_TransformPosition - p.m_TransformPosition).magnitude;
            }
            else
            {
                restLen = p0.m_TransformLocalToWorldMatrix.MultiplyVector(p.m_EndOffset).magnitude;
            }

            // keep shape
            // compute stiffness: support separate yaw/pitch stiffness when provided
            float stiffness = Mathf.Lerp(1.0f, p.m_Stiffness, m_Weight);
            if (m_YawStiffness != 0f || m_PitchStiffness != 0f)
            {
                Matrix4x4 m0 = p0.m_TransformLocalToWorldMatrix;
                m0.SetColumn(3, p0.m_Position);
                Vector3 restPos = p.m_TransformNotNull ? m0.MultiplyPoint3x4(p.m_TransformLocalPosition) : m0.MultiplyPoint3x4(p.m_EndOffset);
                Vector3 restDir = (restPos - p0.m_Position).normalized;
                Vector3 right = p0.m_TransformLocalToWorldMatrix.GetColumn(0).normalized;
                Vector3 up = p0.m_TransformLocalToWorldMatrix.GetColumn(1).normalized;
                float wy = Mathf.Abs(Vector3.Dot(restDir, right));
                float wp = Mathf.Abs(Vector3.Dot(restDir, up));
                float sum = wy + wp;
                if (sum > 0f)
                {
                    wy /= sum;
                    wp /= sum;
                }
                else
                {
                    wy = wp = 0.5f;
                }

                float baseYaw = m_YawStiffness;
                float basePitch = m_PitchStiffness;
                if (pt.m_BoneTotalLength > 0)
                {
                    float a = p.m_BoneLength / pt.m_BoneTotalLength;
                    if (m_StiffnessDistrib != null && m_StiffnessDistrib.keys.Length > 0)
                    {
                        float factor = m_StiffnessDistrib.Evaluate(a);
                        baseYaw *= factor;
                        basePitch *= factor;
                    }
                }

                float eff = wy * baseYaw + wp * basePitch;
                stiffness = Mathf.Lerp(1.0f, eff, m_Weight);
            }

            if (stiffness > 0 || p.m_Elasticity > 0)
            {
                Matrix4x4 m0 = p0.m_TransformLocalToWorldMatrix;
                m0.SetColumn(3, p0.m_Position);
                Vector3 restPos;
                if (p.m_TransformNotNull)
                {
                    restPos = m0.MultiplyPoint3x4(p.m_TransformLocalPosition);
                }
                else
                {
                    restPos = m0.MultiplyPoint3x4(p.m_EndOffset);
                }

                Vector3 d = restPos - p.m_Position;
                p.m_Position += d * (p.m_Elasticity * timeVar);

                if (stiffness > 0)
                {
                    d = restPos - p.m_Position;
                    float len = d.magnitude;
                    float maxlen = restLen * (1 - stiffness) * 2;
                    if (len > maxlen)
                    {
                        p.m_Position += d * ((len - maxlen) / len);
                    }
                }
            }

            // collide
            if (m_EffectiveColliders != null)
            {
                float particleRadius = p.m_Radius * m_ObjectScale;
                for (int j = 0; j < m_EffectiveColliders.Count; ++j)
                {
                    DynamicBoneColliderBase c = m_EffectiveColliders[j];
                    p.m_isCollide |= c.Collide(ref p.m_Position, particleRadius);
                }
            }

            // freeze axis, project to plane 
            if (m_FreezeAxis != FreezeAxis.None)
            {
                Vector3 planeNormal = p0.m_TransformLocalToWorldMatrix.GetColumn((int)m_FreezeAxis - 1).normalized;
                movePlane.SetNormalAndPosition(planeNormal, p0.m_Position);
                p.m_Position -= movePlane.normal * movePlane.GetDistanceToPoint(p.m_Position);
            }

            // angle constraint: limit angular deflection from rest direction
            if (m_AngleConstraint > 0f)
            {
                Matrix4x4 m0_rest = p0.m_TransformLocalToWorldMatrix;
                m0_rest.SetColumn(3, p0.m_Position);
                Vector3 restPos = p.m_TransformNotNull ? m0_rest.MultiplyPoint3x4(p.m_TransformLocalPosition) : m0_rest.MultiplyPoint3x4(p.m_EndOffset);
                Vector3 restDir = (restPos - p0.m_Position).normalized;
                Vector3 curDir = (p.m_Position - p0.m_Position);
                float curDist = curDir.magnitude;
                if (curDist > 1e-6f)
                {
                    curDir /= curDist;
                    float ang = Vector3.Angle(restDir, curDir);
                    if (ang > m_AngleConstraint)
                    {
                        Vector3 axis = Vector3.Cross(restDir, curDir);
                        if (axis.sqrMagnitude > 1e-6f)
                        {
                            axis.Normalize();
                            Vector3 newDir = Quaternion.AngleAxis(m_AngleConstraint, axis) * restDir;
                            p.m_Position = p0.m_Position + newDir * curDist;
                        }
                    }
                }
            }

            // keep length
            Vector3 dd = p0.m_Position - p.m_Position;
            float leng = dd.magnitude;
            if (leng > 0)
            {
                p.m_Position += dd * ((leng - restLen) / leng);
            }
        }
    }

    void SkipUpdateParticles()
    {
        for (int i = 0; i < m_ParticleTrees.Count; ++i)
        {
            SkipUpdateParticles(m_ParticleTrees[i]);
        }
    }

    // only update stiffness and keep bone length
    void SkipUpdateParticles(ParticleTree pt)
    {
        for (int i = 0; i < pt.m_Particles.Count; ++i)
        {
            Particle p = pt.m_Particles[i];
            if (p.m_ParentIndex >= 0)
            {
                p.m_PrevPosition += m_ObjectMove;
                p.m_Position += m_ObjectMove;

                Particle p0 = pt.m_Particles[p.m_ParentIndex];

                float restLen;
                if (p.m_TransformNotNull)
                {
                    restLen = (p0.m_TransformPosition - p.m_TransformPosition).magnitude;
                }
                else
                {
                    restLen = p0.m_TransformLocalToWorldMatrix.MultiplyVector(p.m_EndOffset).magnitude;
                }

                // keep shape
                float stiffness = Mathf.Lerp(1.0f, p.m_Stiffness, m_Weight);
                if (m_YawStiffness != 0f || m_PitchStiffness != 0f)
                {
                    Matrix4x4 m0 = p0.m_TransformLocalToWorldMatrix;
                    m0.SetColumn(3, p0.m_Position);
                    Vector3 restPos = p.m_TransformNotNull ? m0.MultiplyPoint3x4(p.m_TransformLocalPosition) : m0.MultiplyPoint3x4(p.m_EndOffset);
                    Vector3 restDir = (restPos - p0.m_Position).normalized;
                    Vector3 right = p0.m_TransformLocalToWorldMatrix.GetColumn(0).normalized;
                    Vector3 up = p0.m_TransformLocalToWorldMatrix.GetColumn(1).normalized;
                    float wy = Mathf.Abs(Vector3.Dot(restDir, right));
                    float wp = Mathf.Abs(Vector3.Dot(restDir, up));
                    float sum = wy + wp;
                    if (sum > 0f)
                    {
                        wy /= sum;
                        wp /= sum;
                    }
                    else
                    {
                        wy = wp = 0.5f;
                    }

                    float baseYaw = m_YawStiffness;
                    float basePitch = m_PitchStiffness;
                    if (pt.m_BoneTotalLength > 0)
                    {
                        float a = p.m_BoneLength / pt.m_BoneTotalLength;
                        if (m_StiffnessDistrib != null && m_StiffnessDistrib.keys.Length > 0)
                        {
                            float factor = m_StiffnessDistrib.Evaluate(a);
                            baseYaw *= factor;
                            basePitch *= factor;
                        }
                    }

                    float eff = wy * baseYaw + wp * basePitch;
                    stiffness = Mathf.Lerp(1.0f, eff, m_Weight);
                }

                if (stiffness > 0)
                {
                    Matrix4x4 m0 = p0.m_TransformLocalToWorldMatrix;
                    m0.SetColumn(3, p0.m_Position);
                    Vector3 restPos;
                    if (p.m_TransformNotNull)
                    {
                        restPos = m0.MultiplyPoint3x4(p.m_TransformLocalPosition);
                    }
                    else
                    {
                        restPos = m0.MultiplyPoint3x4(p.m_EndOffset);
                    }

                    Vector3 d = restPos - p.m_Position;
                    float len = d.magnitude;
                    float maxlen = restLen * (1 - stiffness) * 2;
                    if (len > maxlen)
                    {
                        p.m_Position += d * ((len - maxlen) / len);
                    }
                }

                // angle constraint in skipped updates
                if (m_AngleConstraint > 0f)
                {
                    Matrix4x4 m0_rest = p0.m_TransformLocalToWorldMatrix;
                    m0_rest.SetColumn(3, p0.m_Position);
                    Vector3 restPos = p.m_TransformNotNull ? m0_rest.MultiplyPoint3x4(p.m_TransformLocalPosition) : m0_rest.MultiplyPoint3x4(p.m_EndOffset);
                    Vector3 restDir = (restPos - p0.m_Position).normalized;
                    Vector3 curDir = (p.m_Position - p0.m_Position);
                    float curDist = curDir.magnitude;
                    if (curDist > 1e-6f)
                    {
                        curDir /= curDist;
                        float ang = Vector3.Angle(restDir, curDir);
                        if (ang > m_AngleConstraint)
                        {
                            Vector3 axis = Vector3.Cross(restDir, curDir);
                            if (axis.sqrMagnitude > 1e-6f)
                            {
                                axis.Normalize();
                                Vector3 newDir = Quaternion.AngleAxis(m_AngleConstraint, axis) * restDir;
                                p.m_Position = p0.m_Position + newDir * curDist;
                            }
                        }
                    }
                }

                // keep length
                Vector3 dd = p0.m_Position - p.m_Position;
                float leng = dd.magnitude;
                if (leng > 0)
                {
                    p.m_Position += dd * ((leng - restLen) / leng);
                }
            }
            else
            {
                p.m_PrevPosition = p.m_Position;
                p.m_Position = p.m_TransformPosition;
            }
        }
    }

    static Vector3 MirrorVector(Vector3 v, Vector3 axis)
    {
        return v - axis * (Vector3.Dot(v, axis) * 2);
    }

    void ApplyParticlesToTransforms()
    {
        Vector3 ax = Vector3.right;
        Vector3 ay = Vector3.up;
        Vector3 az = Vector3.forward;
        bool nx = false, ny = false, nz = false;

#if !UNITY_5_4_OR_NEWER
        // detect negative scale
        Vector3 lossyScale = transform.lossyScale;
        if (lossyScale.x < 0 || lossyScale.y < 0 || lossyScale.z < 0)
        {
            Transform mirrorObject = transform;
            do
            {
                Vector3 ls = mirrorObject.localScale;
                nx = ls.x < 0;
                if (nx)
                    ax = mirrorObject.right;
                ny = ls.y < 0;
                if (ny)
                    ay = mirrorObject.up;
                nz = ls.z < 0;
                if (nz)
                    az = mirrorObject.forward;
                if (nx || ny || nz)
                    break;

                mirrorObject = mirrorObject.parent;
            }
            while (mirrorObject != null);
        }
#endif

        for (int i = 0; i < m_ParticleTrees.Count; ++i)
        {
            ApplyParticlesToTransforms(m_ParticleTrees[i], ax, ay, az, nx, ny, nz);
        }
    }

    void ApplyParticlesToTransforms(ParticleTree pt, Vector3 ax, Vector3 ay, Vector3 az, bool nx, bool ny, bool nz)
    {
        for (int i = 1; i < pt.m_Particles.Count; ++i)
        {
            Particle p = pt.m_Particles[i];
            Particle p0 = pt.m_Particles[p.m_ParentIndex];

            if (p0.m_ChildCount <= 1)        // do not modify bone orientation if has more then one child
            {
                Vector3 localPos;
                if (p.m_TransformNotNull)
                {
                    localPos = p.m_Transform.localPosition;
                }
                else
                {
                    localPos = p.m_EndOffset;
                }
                Vector3 v0 = p0.m_Transform.TransformDirection(localPos);
                Vector3 v1 = p.m_Position - p0.m_Position;
#if !UNITY_5_4_OR_NEWER
                if (nx)
                    v1 = MirrorVector(v1, ax);
                if (ny)
                    v1 = MirrorVector(v1, ay);
                if (nz)
                    v1 = MirrorVector(v1, az);
#endif
                Quaternion rot = Quaternion.FromToRotation(v0, v1);
                p0.m_Transform.rotation = rot * p0.m_Transform.rotation;
            }

            if (p.m_TransformNotNull)
            {
                    if (!m_LockTranslation)
                    {
                        p.m_Transform.position = p.m_Position;
                    }
            }
        }
    }

#if ENABLE_MULTITHREAD
    static void AddPendingWork(DynamicBone db)
    {
        s_PendingWorks.Add(db);
    }

    static void AddWorkToQueue(DynamicBone db)
    {
        s_WorkQueueSemaphore.Release();
    }

    static DynamicBone GetWorkFromQueue()
    {
        int idx = Interlocked.Increment(ref s_WorkQueueIndex);
        return s_EffectiveWorks[idx];
    }

    static void ThreadProc()
    {
        while (true)
        {
            s_WorkQueueSemaphore.WaitOne();

            DynamicBone db = GetWorkFromQueue();
            db.UpdateParticles();

            if (Interlocked.Decrement(ref s_RemainWorkCount) <= 0)
            {
                s_AllWorksDoneEvent.Set();
            }
        }
    }

    static void InitThreadPool()
    {
        s_AllWorksDoneEvent = new AutoResetEvent(false);
        s_WorkQueueSemaphore = new Semaphore(0, int.MaxValue);

        int threadCount = System.Environment.ProcessorCount;

        for (int i = 0; i < threadCount; ++i)
        {
            var t = new Thread(ThreadProc);
            t.IsBackground = true;
            t.Start();
        }
    }

    static void ExecuteWorks()
    {
        if (s_PendingWorks.Count <= 0)
            return;

        s_EffectiveWorks.Clear();

        for (int i = 0; i < s_PendingWorks.Count; ++i)
        {
            DynamicBone db = s_PendingWorks[i];
            if (db != null && db.enabled)
            {
                db.CheckDistance();
                if (db.IsNeedUpdate())
                {
                    s_EffectiveWorks.Add(db);
                }
            }
        }

        s_PendingWorks.Clear();
        if (s_EffectiveWorks.Count <= 0)
            return;

        if (s_AllWorksDoneEvent == null)
        {
            InitThreadPool();
        }

        int workCount = s_RemainWorkCount = s_EffectiveWorks.Count;
        s_WorkQueueIndex = -1;

        for (int i = 0; i < workCount; ++i)
        {
            DynamicBone db = s_EffectiveWorks[i];
            db.Prepare();
            AddWorkToQueue(db);
        }

        s_AllWorksDoneEvent.WaitOne();

        for (int i = 0; i < workCount; ++i)
        {
            DynamicBone db = s_EffectiveWorks[i];
            db.ApplyParticlesToTransforms();
        }
    }
#endif
}
