using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CountedBehavior : MonoBehaviour
{
    //------------
    // Properties
    //------------
    protected abstract ISet<MonoBehaviour> LocalInstances { get; }

    //------------------------------
    // Inherited from MonoBehaviour
    //------------------------------

    // OnEnable is called when the object becomes enabled and active.
    protected virtual void OnEnable()
    {
        try
        {
            LocalInstances.Add( this );
        }
        catch ( System.NotImplementedException )
        {
        }
    }

    // OnDisable is called when the behaviour becomes disabled
    protected virtual void OnDisable()
    {
        try
        {
            LocalInstances.Remove( this );
        }
        catch ( System.NotImplementedException )
        {
        }
    }
}
