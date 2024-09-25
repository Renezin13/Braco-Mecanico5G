using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;

public class VRButtonDetector : MonoBehaviour
{
    private bool isButtonPressed = false;
    private bool previousButtonState = false;
    private HttpListener listener;
    private string url = "http://localhost:8000/";
    private Thread serverThread;

    private void Start()
    {
        StartServer();
    }

    private void Update()
    {
        DetectSecondaryButtonPress();
    }

    private void OnDestroy()
    {
        StopServer();
    }

    private void DetectSecondaryButtonPress()
    {
        // Verifica o estado atual do botão secundário (index trigger)
        bool currentButtonState = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);

        // Verifica se houve uma transição de pressionado para solto (ou vice-versa)
        if (currentButtonState != previousButtonState)
        {
            previousButtonState = currentButtonState;

            if (currentButtonState)
            {
                // O botão foi pressionado
                Debug.Log("Botão secundário pressionado!");
                isButtonPressed = true;
            }
            else
            {
                // O botão foi solto
                Debug.Log("Botão secundário solto!");
                isButtonPressed = false;
            }
        }
    }

    private void StartServer()
    {
        listener = new HttpListener();
        listener.Prefixes.Add(url);
        serverThread = new Thread(HandleIncomingConnections);
        serverThread.Start();
    }

    private void StopServer()
    {
        if (listener != null && listener.IsListening)
        {
            listener.Stop();
            serverThread.Abort();
        }
    }

    private void HandleIncomingConnections()
    {
        listener.Start();
        Debug.Log("Listening for connections on " + url);

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                string responseString = $"{{ \"isButtonPressed\": {isButtonPressed.ToString().ToLower()} }}";
                byte[] data = Encoding.UTF8.GetBytes(responseString);
                response.ContentType = "application/json";
                response.ContentLength64 = data.Length;
                response.OutputStream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling request: {ex.Message}");
                string errorResponse = "{ \"error\": \"Internal Server Error\" }";
                byte[] errorData = Encoding.UTF8.GetBytes(errorResponse);
                response.ContentType = "application/json";
                response.ContentLength64 = errorData.Length;
                response.OutputStream.Write(errorData, 0, errorData.Length);
            }
            finally
            {
                response.Close();
            }
        }
    }
