using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RandomText : MonoBehaviour
{
    public List<string> list = new List<string> { "+", "0", "-"};
    Dictionary<string, List<int>> buttons = new Dictionary<string, List<int>>();
    public List<TextMeshPro> textFields;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setrandomString(Button button_click)
    {
        Debug.Log("Set Fields");
        string name_button = button_click.gameObject.name;
        if (!buttons.ContainsKey(name_button))
        {
            Debug.Log("init");
            List<int> randoms_numbers = new List<int>();
            foreach (var i in textFields)
            {
                Debug.Log(i);
                randoms_numbers.Add(Random.Range(0, list.Count));
            }
            buttons.Add(name_button, randoms_numbers);
        }
        for (int i=0; i<buttons[name_button].Count; i++)
        {
            textFields[i].SetText(list[buttons[name_button][i]]);
        }
    }
}
