using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccelerationSeperationTest : MonoBehaviour
{
    Vector3 velo = Vector3.zero;
    Vector3 acc = Vector3.zero;
    Vector2 veloLimits = new Vector2(0f, 6f);
    public float linearAccLimit = 6;
    public float rotAccLimit = 3;
    public float angleOfBehind = 30;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetMouseButton(0)) {
            RaycastHit hitinfo;
            bool didhit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitinfo);
            Vector3 direction = Vector3.zero;
            if (didhit)
            {
                direction = hitinfo.point - transform.position;
                direction.y = 0f;
                direction = direction.normalized;
            }
            acc = (direction * 6f - velo) / Time.deltaTime;//direction * 5f;
            //acc = new Vector3(Input.mousePosition.x - Screen.width / 2f, 0f, Input.mousePosition.y - Screen.height / 2f).normalized
            //* 5f;
        }else acc = Vector3.zero;
        //Breakup acc into forward and tangential
        Vector3 tanAcc;
        Vector3 normAcc;
        float dt = Time.deltaTime;
        SplitAcc(in acc, out tanAcc, out normAcc);

        
        tanAcc = Mathf.Clamp(tanAcc.magnitude, 0f, linearAccLimit)
            * tanAcc.normalized;
        normAcc = Mathf.Clamp(normAcc.magnitude, 0f, rotAccLimit)
            * normAcc.normalized;
        acc = tanAcc + normAcc;

        velo += acc * Time.deltaTime;
        float speed = velo.magnitude;
        speed = Mathf.Clamp(speed, veloLimits.x, veloLimits.y);
        velo = speed * velo.normalized;

        if(velo.normalized.magnitude > 0f)transform.rotation = Quaternion.LookRotation(velo.normalized, Vector3.up);
        transform.position += velo * Time.deltaTime;
    }
    void SplitAcc(in Vector3 acc, out Vector3 tanAcc, out Vector3 normAcc)
    {
        Vector3 forward = velo.normalized;
        if(velo.normalized.magnitude <= 0f)
        {
            //forward = transform.forward;
            tanAcc = acc;
            normAcc = Vector3.zero;
            return;
        }
        tanAcc = Vector3.Project(acc, forward.normalized);
        normAcc = acc - tanAcc;
    }
}


