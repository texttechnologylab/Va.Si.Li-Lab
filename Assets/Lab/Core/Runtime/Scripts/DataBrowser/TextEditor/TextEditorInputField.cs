using System;
using System.Collections;
using TMPro;
using Ubiq.Samples;
using Ubiq.XR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using WebSocketSharp;

public class TextEditorInputField : InputField
{

    //TODOO:
    // - Closing VAObjects -> Network wide
    // - Spawning in room context
    // - Connecting highlighted substring


    //This one hides itself in the Inspector. God knows why. You need to activate debug modus to see it.
    public Keyboard keyboard;

    private bool hasSelection { get { return caretPositionInternal != caretSelectPositionInternal; } }


    protected override void Start()
    {
        base.Start();
        keyboard.OnInput.AddListener(Keyboard_OnInput);

    }

    public override void OnDeselect(BaseEventData eventData)
    {
        // We don't want that here. Deselect only, when we give that command!

        //DeactivateInputField();

        //base.OnDeselect(eventData); //I am a little bit sad about this (Selectable), but I don't want to copy the whole code from the base class
    }



    public override void OnPointerDown(PointerEventData eventData)
    {
        //TODO: check why original function doe not work. nWhtih that also add highlight functionality!!
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_TextComponent.rectTransform, eventData.position, eventData.pressEventCamera, out localMousePos);

        int charIdx = GetCharacterIndexFromPosition(localMousePos);
        caretSelectPositionInternal = caretPositionInternal = charIdx + m_DrawStart;

        UpdateLabel();
        eventData.Use();
    }

    void Keyboard_OnInput(KeyCode keyCode)
    {
        // Our own input handling. Add other key in the future.
        var intKeyCode = (int)keyCode;
        string text = m_TextComponent.text;
        if (keyCode == KeyCode.Backspace)
        {
            Backspace();
        }
        else if (intKeyCode >= 97 && intKeyCode <= 122)
        {
            // ASCII codes for a -> z
            if (keyboard.currentKeyCase == Keyboard.KeyCase.Upper)
            {
                // To uppercase ascii
                intKeyCode -= 32;
            }
            Append((char)intKeyCode);
        }
        else if (intKeyCode >= 48 && intKeyCode <= 57)
        {
            Append((char)intKeyCode);
        }
        else if (intKeyCode == 32)
        {
            Append((char)intKeyCode);
        }

        SendOnValueChangedAndUpdateLabel();
    }



    private void Backspace()
    {
        if (readOnly)
            return;

        if (hasSelection)
        {
            Delete();
            //UpdateTouchKeyboardFromEditChanges();
            SendOnValueChangedAndUpdateLabel();
        }
        else
        {
            if (caretPositionInternal > 0 && caretPositionInternal - 1 < text.Length)
            {
                m_Text = text.Remove(caretPositionInternal - 1, 1);
                caretSelectPositionInternal = caretPositionInternal = caretPositionInternal - 1;

                //UpdateTouchKeyboardFromEditChanges();
                SendOnValueChangedAndUpdateLabel();
            }
        }
    }

    private void Delete()
    {
        if (readOnly)
            return;

        if (caretPositionInternal == caretSelectPositionInternal)
            return;

        if (caretPositionInternal < caretSelectPositionInternal)
        {
            m_Text = text.Substring(0, caretPositionInternal) + text.Substring(caretSelectPositionInternal, text.Length - caretSelectPositionInternal);
            caretSelectPositionInternal = caretPositionInternal;
        }
        else
        {
            m_Text = text.Substring(0, caretSelectPositionInternal) + text.Substring(caretPositionInternal, text.Length - caretPositionInternal);
            caretPositionInternal = caretSelectPositionInternal;
        }
    }

    private void SendOnValueChangedAndUpdateLabel()
    {
        SendOnValueChanged();
        UpdateLabel();
    }

    private void SendOnValueChanged()
    {
        UISystemProfilerApi.AddMarker("InputField.value", this);
        if (onValueChanged != null)
            onValueChanged.Invoke(text);
    }
}
