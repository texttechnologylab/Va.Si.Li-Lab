using UnityEngine;
using UnityEngine.UI;

public class VotingUIController: MonoBehaviour{
    public Toggle yesNoToggle;
    public Toggle mcqToggle;
    public GameObject yesNoContainer;
    public GameObject mcqContainer;
    public GameObject questionText;
    void Start(){

        yesNoToggle.onValueChanged.AddListener(delegate { OnYesNoToggleValueChanged(); });
        mcqToggle.onValueChanged.AddListener(delegate { OnMCQToggleValueChanged(); });

    }

    void OnYesNoToggleValueChanged(){
        if(yesNoToggle.isOn){
            yesNoContainer.gameObject.SetActive(true);
        }
        else{
            yesNoContainer.gameObject.SetActive(false);
        }
    }
    void OnMCQToggleValueChanged(){
        if(mcqToggle.isOn){
            mcqContainer.gameObject.SetActive(true);
        }
        else{
            mcqContainer.gameObject.SetActive(false);
        }
    }
}