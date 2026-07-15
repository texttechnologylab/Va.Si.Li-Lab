using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlaceObject : MonoBehaviour
{
    public GameObject ghost;
    public GameObject placed;
    public GameObject ghost1;
    public GameObject placed1;
    public GameObject ghost2;
    public GameObject placed2;
    public Text money;
    public Text obj;
    public int price1 = 10;
    public int price2 = 20;
    public int price3 = 5;
    public int currPrice = 10;

    private int totalBlock = 500;
    // Start is called before the first frame update
    void Start()
    {
        obj.text = "Item: Bed";//set base text
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.J)){//update price and UI
            currPrice = price1;
            obj.text = "Item: Bed";
        }

        if(Input.GetKey(KeyCode.K)){
            currPrice = price2;
            obj.text = "Item: Wardrobe";
        }

        if(Input.GetKey(KeyCode.L)){
            currPrice = price3;
            obj.text = "Item: Rug";
        }

        money.text = "Current Mons: " + totalBlock.ToString();

        if(totalBlock > 0){
            RaycastHit hit;
            
            if(currPrice == price1){//switch object
                ghost1.SetActive(false);
                ghost2.SetActive(false);
                ghost.SetActive(true);
                if(Physics.Raycast(transform.position,transform.TransformDirection(Vector3.forward),out hit, Mathf.Infinity)){
                    ghost.transform.position = hit.point;
                    if(Input.GetMouseButtonDown(0)){
                        totalBlock -= currPrice;
                        Instantiate(placed,ghost.transform.position,ghost.transform.rotation);
                    }
                }
            }

            if(currPrice == price2){
                ghost.SetActive(false);
                ghost2.SetActive(false);
                ghost1.SetActive(true);
                if(Physics.Raycast(transform.position,transform.TransformDirection(Vector3.forward),out hit, Mathf.Infinity)){
                    ghost1.transform.position = hit.point;
                    if(Input.GetMouseButtonDown(0)){
                        totalBlock -= currPrice;
                        Instantiate(placed1,ghost1.transform.position,ghost1.transform.rotation);
                    }
                }
            }

            if(currPrice == price3){
                ghost1.SetActive(false);
                ghost.SetActive(false);
                ghost2.SetActive(true);
                if(Physics.Raycast(transform.position,transform.TransformDirection(Vector3.forward),out hit, Mathf.Infinity)){
                    ghost2.transform.position = hit.point;
                    if(Input.GetMouseButtonDown(0)){
                        totalBlock -= currPrice;
                        Instantiate(placed2,ghost2.transform.position,ghost2.transform.rotation);
                    }
                }
            }
        }else{
            ghost.SetActive(false);
            ghost1.SetActive(false);
            ghost2.SetActive(false);
        }
    }
}
