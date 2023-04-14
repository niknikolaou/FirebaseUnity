using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField]
    private GameObject loginPanel;

    [SerializeField]
    private GameObject registrationPanel;

    [SerializeField]
    private GameObject gamePanel;

    [SerializeField]
    private GameObject emailVerificationPanel;

    [SerializeField]
    private Text emailVerificationText;


    private void Awake()
    {
        CreateInstance();
    }

    private void CreateInstance()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    private void ClearUI()
    {
        loginPanel.SetActive(false);
        registrationPanel.SetActive(false);
        emailVerificationPanel.SetActive(false);
        gamePanel.SetActive(false);
    }

    public void OpenLoginPanel()
    {
        ClearUI();
        loginPanel.SetActive(true);
        
    }

    public void OpenRegistrationPanel()
    {
        ClearUI();
        registrationPanel.SetActive(true);
    }

    public void OpenGamePanel()
    {
        ClearUI();
        gamePanel.SetActive(true);
    }

    public void ShowVerificationResponce(bool isEmailSend, string emailId, string errorMessage)
    {
        ClearUI();
        emailVerificationPanel.SetActive(true);
        if(isEmailSend )
        {
            emailVerificationText.text = $"Verification email has been sent to {emailId}. \n Please verify your email address to login.";
        }
        else
        {
            emailVerificationText.text = $"Could't send email: {errorMessage}";
        }
    }
}
