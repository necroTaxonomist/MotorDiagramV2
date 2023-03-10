using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldSensor : MonoBehaviour
{
    //-----------
    // Constants
    //-----------
    private const float GRAY_VALUE = .75f;

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
    private float maximum = 1;
    public float Maximum
    {
        get => maximum;
        set => maximum = value;
    }

    //------------------------------
    // Inherited from MonoBehaviour
    //------------------------------
    
    protected virtual void Awake()
    {
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        Vector3 disp = this.transform.position - Magnet.transform.position;
        Vector3 dir = disp.normalized;
        float strength = Vector3.Dot( dir, Magnet.Field ) / Maximum;

        float absStr = Mathf.Abs( strength );

        float primary = Mathf.Lerp( GRAY_VALUE, 1, absStr );
        float gray = Mathf.Lerp( GRAY_VALUE, 0, absStr );

        Color posColor, negColor;
        if ( strength >= 0 )
        {
            posColor = new Color( primary, gray, gray );
            negColor = new Color( gray, gray, primary );
        }
        else
        {
            posColor = new Color( gray, gray, primary );
            negColor = new Color( primary, gray, gray );
        }

        var propBlock = new MaterialPropertyBlock();
        propBlock.SetColor( "_PosColor", posColor );
        propBlock.SetColor( "_NegColor", negColor );

        foreach ( var rend in GetComponentsInChildren<Renderer>() )
        {
            rend.SetPropertyBlock( propBlock );
        }
    }
}
