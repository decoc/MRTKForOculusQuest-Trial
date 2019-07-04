using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class ControllerDebug : MonoBehaviour
{
    public Text textUI;
    StringBuilder builder = new StringBuilder();

    private void Start()
    {
        Application.logMessageReceived += (log, trace, obj) =>
        {
            textUI.text += $"{log}\r\n";
        };

        //var joysticks = Input.GetJoystickNames();
        //foreach (var joystick in joysticks)
        //    builder.AppendLine(joystick);

    }

    // Update is called once per frame
    void Update()
    {
        //var rPos = $"L_POS:{InputTracking.GetLocalPosition(XRNode.LeftHand)}";
        //var lPos = $"R_POS:{InputTracking.GetLocalPosition(XRNode.RightHand)}";

        //builder.AppendLine(rPos);
        //builder.AppendLine(lPos);


    }
}
