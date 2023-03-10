using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatorSim : MonoBehaviour
{
    //------------
    // Properties
    //------------
    [SerializeField]
    private GameObject magnet = null;
    public MagnetSim Magnet
    {
        get => magnet.GetComponent<MagnetSim>();
        set => magnet = value.gameObject;
    }

    [SerializeField]
    private int poles = 1;
    public int Poles
    {
        get => poles;
        set
        {
            poles = value;

            DestroyCoils();
            CreateCoils();

            Voltage = Voltage;
            Frequency = Frequency;
            Phase = Phase;
        }
    }
    public float PolesF
    {
        get => Poles;
        set => Poles = Mathf.RoundToInt( value );
    }

    [SerializeField]
    private GameObject coilPrefab = null;
    public GameObject CoilPrefab
    {
        get => coilPrefab;
        set => coilPrefab = value;
    }

    [SerializeField]
    private float coilRadius = 1;
    public float CoilRadius
    {
        get => coilRadius;
        set => coilRadius = value;
    }

    public float BetweenPoles => 360 / (2 * Poles);

    private float mVoltage = 0;
    public float Voltage
    {
        get => mVoltage;
        set
        {
            mVoltage = value;
            if ( Poles == 1 )
            {
                Magnet.FieldMagnitude = Mathf.Cos( Mathf.Deg2Rad * mPhase) * value;
            }
            else
            {
                Magnet.FieldMagnitude = value;
            }
        }
    }

    private float mFrequency = 0;
    public float Frequency
    {
        get => mFrequency;
        set
        {
            mFrequency = value;
            if ( Poles == 1 )
            {
                Magnet.FrequencyMagnitude = 0;
            }
            else
            {
                Magnet.FrequencyMagnitude = value;
            }
        }
    }

    private float mPhase = 0;
    public float Phase
    {
        get
        {
            if ( Poles == 1 )
            {
                return mPhase;
            }
            else
            {
                return Vector3.SignedAngle(
                    this.transform.up,
                    Magnet.FieldDirection,
                    -this.transform.forward
                );
            }
        }
        set
        {
            value = Mathf.Repeat( value, 360 );

            if ( Poles == 1 )
            {
                mPhase = value;
                Magnet.Field = this.transform.up * Mathf.Cos( Mathf.Deg2Rad * mPhase) * Voltage;
            }
            else
            {
                Quaternion rot = Quaternion.AngleAxis( value, -this.transform.forward );
                Magnet.FieldDirection = rot * this.transform.up;
            }
        }
    }

    public int ActivePole
    {
        get
        {
            float angle = Phase;

            angle += BetweenPoles / 2;

            if ( angle < 0 )
            {
                angle += 360;
            }

            return Mathf.FloorToInt( angle / BetweenPoles );
        }
        set
        {
            Phase = value * BetweenPoles;
        }
    }

    //-----------
    // Functions
    //-----------
    public void Flip()
    {
        int pole = ActivePole;
        ActivePole = (pole + 1) % (2 * Poles);
    }

    private void DestroyCoils()
    {
        foreach ( Transform child in this.transform )
        {
            GameObject.Destroy( child.gameObject );
        }
    }

    private void CreateCoils()
    {
        for ( int i = 0; i < 2 * Poles; ++i )
        {
            var obj = GameObject.Instantiate( CoilPrefab, Vector3.zero, Quaternion.identity, this.transform );
            obj.transform.localRotation = Quaternion.AngleAxis( i * BetweenPoles, this.transform.forward );
            obj.transform.localPosition = obj.transform.localRotation * this.transform.up * CoilRadius;

            var sensor = obj.GetComponentInChildren<FieldSensor>();
            sensor.Magnet = Magnet;
        }
    }

    //------------------------------
    // Inherited from MonoBehaviour
    //------------------------------
    
    protected virtual void Awake()
    {
        Poles = Poles;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if ( Poles == 1 )
        {
            Phase += 360 * Frequency * Time.deltaTime;
        }
    }
}
