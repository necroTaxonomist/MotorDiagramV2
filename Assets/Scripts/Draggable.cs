using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent( typeof(BoxCollider) )]
public class Draggable : MonoBehaviour
{
    //------------
    // Properties
    //------------
    public BoxCollider Box => GetComponent<BoxCollider>();

    private bool mDragging = false;
    public bool Dragging
    {
        get => mDragging;
        set
        {
            mDragging = value;

            foreach ( var beh in GetComponents<PauseWhileDragging>() )
            {
                beh.enabled = !mDragging;
            }
        }
    }

    public Vector3 GrabPoint { get; private set; } = Vector3.zero;

    //---------
    // Methods
    //---------
    public void MouseDown( Vector3 screenPos, Vector3 worldPos )
    {
        Ray ray = Camera.main.ScreenPointToRay( screenPos );
        RaycastHit hit;

        if ( Box.Raycast( ray, out hit, 2000 ) )
        {
            Dragging = true;
            GrabPoint = worldPos;
        }
    }

    public void MouseUp()
    {
        if ( Dragging )
        {
            Dragging = false;
        }
    }

    private Vector3 GetRealMousePosition()
    {
        return Camera.main.ScreenToWorldPoint( Input.mousePosition );
    }

    private Vector3 GetCameraDirection()
    {
        return Camera.main.transform.forward;
    }

    //------------------------------
    // Inherited from MonoBehaviour
    //------------------------------

    // Update is called once per frame
    protected virtual void Update()
    {
        if ( Input.GetMouseButtonDown( 0 ) )
        {
            MouseDown( Input.mousePosition, GetRealMousePosition() );
        }
        else if ( Input.GetMouseButtonUp( 0 ) )
        {
            MouseUp();
        }

        if ( Dragging )
        {
            Vector3 planeNormal = GetCameraDirection();

            Vector3 myPos = Vector3.ProjectOnPlane( this.transform.position, planeNormal );
            Vector3 grabPos = Vector3.ProjectOnPlane( GrabPoint, planeNormal );
            Vector3 mousePos = Vector3.ProjectOnPlane( GetRealMousePosition(), planeNormal );

            Vector3 grabDir = grabPos - myPos;
            Vector3 mouseDir = mousePos - myPos;

            var rot = Quaternion.FromToRotation( grabDir, mouseDir );

            transform.rotation = rot * transform.rotation;
            GrabPoint = mousePos;
        }
    }
}
