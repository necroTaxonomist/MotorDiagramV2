using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetSim : PauseWhileDragging
{
    //------------
    // Properties
    //------------
    [SerializeField]
    private Vector3 moment = Vector3.right;
    public Vector3 Moment
    {
        get => moment;
        set => moment = value;
    }

    [SerializeField]
    private Vector3 field = Vector3.zero;
    private Vector3 mSavedDirection = Vector3.up;
    public Vector3 Field
    {
        get => field;
        set
        {
            if ( field.magnitude != 0 && value.magnitude == 0 )
            {
                mAngularVelocity += mFieldVelocity;
            }

            field = value;
            if ( field.magnitude != 0 )
            {
                mSavedDirection = field.normalized;
            }
        }
    }

    public float FieldMagnitude
    {
        get => Field.magnitude;
        set
        {
            if ( Field.magnitude == 0 )
            {
                Field = value * mSavedDirection;
            }
            else
            {
                Field = value * Field.normalized;
            }
        }
    }

    public Vector3 FieldDirection
    {
        get
        {
            if ( Field.magnitude != 0 )
            {
                return Field.normalized;
            }
            else
            {
                return mSavedDirection;
            }
        }
        set
        {
            mSavedDirection = value.normalized;
            Field = Field.magnitude * mSavedDirection;
        }
    }

    [SerializeField]
    private float friction = 0;
    public float Friction
    {
        get
        {
            if ( Field.magnitude == 0 )
            {
                return friction;
            }
            else
            {
                return Field.magnitude / 4;
            }
        }
        set => friction = value;
    }

    [SerializeField]
    private Vector3 frequency = Vector3.zero;
    private Vector3 mSavedFrequencyDirection = Vector3.back;
    private Vector3 mFieldVelocity = Vector3.zero;
    private Quaternion mPerFrameRot = Quaternion.identity;
    public Vector3 Frequency
    {
        get => frequency;
        set
        {
            Vector3 prevFieldVelocity = mFieldVelocity;

            frequency = value;
            mFieldVelocity = 360 * frequency;
            mPerFrameRot = Quaternion.AngleAxis( mFieldVelocity.magnitude * Time.fixedDeltaTime, mFieldVelocity.normalized );

            if ( frequency.magnitude != 0 )
            {
                mSavedFrequencyDirection = frequency.normalized;
            }

            if ( Field.magnitude != 0 )
            {
                Vector3 delta = mFieldVelocity - prevFieldVelocity;
                mAngularVelocity -= delta;
            }
        }
    }

    public float FrequencyMagnitude
    {
        get => Frequency.magnitude;
        set
        {
            if ( Frequency.magnitude == 0 )
            {
                Frequency = value * mSavedFrequencyDirection;
            }
            else
            {
                Frequency = value * Frequency.normalized;
            }
        }
    }

    public float StableKineticEnergy
    {
        get
        {
            float w = Mathf.Deg2Rad * mFieldVelocity.magnitude;
            float w2 = w * w;
            return w2 / 2;
        }
    }

    public float StableDisplacement
    {
        get
        {
            float k = StableKineticEnergy;
            float mb = Moment.magnitude * Field.magnitude;

            if ( mb == 0 )
            {
                // No energy
                return 0;
            }

            float cosx = (k / mb) - 1;
            float xRads = Mathf.Acos( cosx );
            return Mathf.Rad2Deg * xRads;
        }
    }

    public Vector3 ApparentField
    {
        get
        {
            if ( Frequency.magnitude == 0 )
            {
                return Field;
            }

            float disp = StableDisplacement;
            Quaternion rot = Quaternion.AngleAxis( -disp, Frequency );
            return rot * Field;
        }
    }

    public Vector3 Rotation
    {
        get => this.transform.rotation.eulerAngles;
    }

    private Vector3 mAngularVelocity = Vector3.zero;

    //---------
    // Methods
    //---------
    private void Simulate()
    {
        if ( Field.magnitude != 0 && Frequency.magnitude != 0 )
        {
            Field = mPerFrameRot * Field;
            this.transform.rotation = mPerFrameRot * this.transform.rotation;
        }

        float dt = Time.fixedDeltaTime;
        float dt2 = dt * dt;

        Vector3 torque = Vector3.Cross( this.transform.rotation * Moment, Field );
        Vector3 friction = -mAngularVelocity.normalized * Friction;

        Vector3 acceleration = torque + friction;   // Assume MOI = 1

        Vector3 dxFromA = acceleration * dt2 / 2;
        Vector3 dxFromV = mAngularVelocity * dt;

        Quaternion dx = Quaternion.AngleAxis( dxFromA.magnitude, dxFromA.normalized ) *
            Quaternion.AngleAxis( dxFromV.magnitude, dxFromV.normalized );

        Vector3 dv = acceleration * dt;

        this.transform.rotation = dx * this.transform.rotation;
        mAngularVelocity += dv;
    }

    //------------------------------
    // Inherited from MonoBehaviour
    //------------------------------
    
    protected virtual void Awake()
    {
        Field = Field;
        Frequency = Frequency;
    }

    protected virtual void FixedUpdate()
    {
        Simulate();
    }
}
