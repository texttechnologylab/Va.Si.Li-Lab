using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorTrigger : MonoBehaviour
{
    public door Door;

    private void OnTriggerEnter(Collider other){
        if(other.TryGetComponent<CapsuleCollider>(out CapsuleCollider collider)){
            if(!Door.IsOpen){
                Door.Open(other.transform.position);
            }
        }
    }

    private void OnTriggerExit(Collider other){
        if(other.TryGetComponent<CapsuleCollider>(out CapsuleCollider collider)){
            if(Door.IsOpen){
                Door.Close();
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
