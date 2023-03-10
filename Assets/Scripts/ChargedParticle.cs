using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ChargedParticle : CountedBehavior
{
    //-----------
    // Constants
    //-----------
    public const double PERMITTIVITY = 8.8541878128e-12;
    public const double PERMEABILITY = 1.25663706212e-6;
    public const double COULOMB_CONST = 8.9875517923e9;

    //------------
    // Properties
    //------------
    [SerializeField]
    private double charge = 1;
    public double Charge
    {
        get => charge;
        set => charge = value;
    }

    [SerializeField]
    private bool magnetic;
    public bool Magnetic
    {
        get => magnetic;
        set => magnetic = value;
    }

    public Rigidbody Rb => GetComponent<Rigidbody>();

    //---------
    // Methods
    //---------
    public Vector3 ForceUpon( ChargedParticle other )
    {
        Vector3 disp = other.Rb.position - this.Rb.position;
        float r = disp.magnitude;
        float r2 = r * r;

        if ( this.Magnetic == other.Magnetic )
        {
            double forceMag;
            if ( Magnetic )
            {
                forceMag = (PERMEABILITY * this.Charge * other.Charge) / (4 * Mathf.PI * r2);
            }
            else
            {
                forceMag = COULOMB_CONST * this.Charge * other.Charge / r2;
            }

            return (float)forceMag * disp.normalized;
        }
        else
        {
            // TODO
            return Vector3.zero;
        }
    }

    //--------------------------------
    // Inherited from CountedBehavior
    //--------------------------------
    protected static HashSet<MonoBehaviour> localInstances = new HashSet<MonoBehaviour>();
    protected override ISet<MonoBehaviour> LocalInstances => localInstances;

    public static IEnumerable<ChargedParticle> Instances => localInstances.Cast<ChargedParticle>();

    //------------------------------
    // Inherited from MonoBehaviour
    //------------------------------
    
    // Start is called before the first frame update
    protected virtual void Start()
    {
        
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        
    }

    protected virtual void FixedUpdate()
    {
        Vector3 totalForce = Vector3.zero;

        foreach ( ChargedParticle other in Instances )
        {
            if ( other == this )
            {
                continue;
            }

            totalForce += other.ForceUpon( this );
        }

        Rb.AddForce( totalForce );
    }
}
