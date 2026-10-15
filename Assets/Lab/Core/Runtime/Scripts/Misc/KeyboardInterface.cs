using TLab.VKeyborad;
using UnityEngine;

public class KeyboardInterface : BaseVKeyborad
{
    private TouchScreenKeyboard _keyboard;
    private bool initialSwitch = true;
    [SerializeField] private bool m_hideOnStart = false;
    protected override void Start()
    {
        base.Start();
        
        if (m_hideOnStart)
            SetVisibility(false);
    }

    void OnEnable()
    {
        initialSwitch = true;
    }

    public override void SwitchInputField(BaseInputField inputFieldBase)
    {
        if (initialSwitch)
        {
            initialSwitch = false;
            return;
        }
        base.SwitchInputField(inputFieldBase);
        Debug.Log("Switched input field to: " + inputFieldBase.name);
    }

    void Update()
    {
        if (m_inputField && m_inputField.inputFieldIsActive && _keyboard == null)
        {
            Debug.Log("Opening keyboard for input field: " + m_inputField.name);
            if (inputFieldBase != null)
            {
                _keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
            }
        }
        try
        {
            if (_keyboard != null && _keyboard.status == TouchScreenKeyboard.Status.Done)
            {
                _keyboard = null;
                if (m_inputField is InputField inputField)
                {
                    inputField.text = _keyboard.text;
                }

            }
        }
        catch (System.Exception _e)
        {

        }
    }
}
