using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class door : MonoBehaviour
{
    public bool IsOpen = false;
    private bool IsRotatingDoor = true;
    private float Speed = 1f;
    private float RotationAmount = 90f;
    private float ForwardDirection = 0;

    private Vector3 StartRotation;
    private Vector3 Forward;

    private Coroutine AnimationCoroutine;
    // Start is called before the first frame update
    private void Awake(){
        StartRotation = transform.rotation.eulerAngles;
        Forward = transform.right;
    }

    public void Open(Vector3 UserPosition){
        if(!IsOpen){
            if(AnimationCoroutine != null){
                StopCoroutine(AnimationCoroutine);
            }
        }

        if(IsRotatingDoor){
            float dot = Vector3.Dot(Forward,(UserPosition - transform.position).normalized);
            AnimationCoroutine = StartCoroutine(DoRotationOpen(dot));
        }
    }

    private IEnumerator DoRotationOpen(float ForwardAmount){
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation;

        if(ForwardAmount >= ForwardDirection){
            endRotation = Quaternion.Euler(new Vector3(0,StartRotation.y - RotationAmount,0));
        }else{
            endRotation = Quaternion.Euler(new Vector3(0,StartRotation.y + RotationAmount,0));
        }

        IsOpen = true;
        float time = 0;
        while(time <1){
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, time);
            yield return null;
            time += Time.deltaTime * Speed;
        }
    }

    public void Close(){
        if(IsOpen){
            if(AnimationCoroutine != null){
                StopCoroutine(AnimationCoroutine);
            }
        }

        if(IsRotatingDoor){
            AnimationCoroutine = StartCoroutine(DoRotationClose());    
        }
    }

    private IEnumerator DoRotationClose(){
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = Quaternion.Euler(StartRotation);
        
        IsOpen = false;
        float time = 0;
        while(time <1){
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, time);
            yield return null;
            time += Time.deltaTime * Speed;
        }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
