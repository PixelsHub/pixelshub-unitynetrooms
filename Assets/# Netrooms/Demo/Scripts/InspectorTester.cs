using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelsHub.Netrooms;

public class InspectorTester : MonoBehaviour
{
    [SerializeField]
    private NetworkStringVars stringVars;

    [SerializeField]
    private NetworkChat chat;

    [Space(8)]
    [SerializeField]
    private string variableTestValue;

    [SerializeField]
    private bool executeVariableTest;

    [Space(8)]
    [SerializeField]
    private string messageValue;

    [SerializeField]
    private bool executeChatTest;


    private void OnValidate()
    {
        if(Application.isPlaying)
        {
            if(executeVariableTest)
            {
                executeVariableTest = false;

                const string testKeyPrefix = "TEST";
                string testKey = $"{testKeyPrefix}{stringVars.Count}";

                stringVars.SetVariable(testKey, variableTestValue);
                variableTestValue = string.Empty;
            }

            if(executeChatTest)
            {
                executeChatTest = false;

                chat.AddMessage(new()
                {
                    author = chat.OwnerClientId.ToString(),
                    text = messageValue
                });

                messageValue = string.Empty;
            }
        }
    }
}
