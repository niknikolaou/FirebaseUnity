using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;



public class FirebaseAuthManager : MonoBehaviour
{
    // Firebase variable
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseUser user;

    // Login Variables
    [Space]
    [Header("Login")]
    public InputField emailLoginField;
    public InputField passwordLoginField;

    // Registration Variables
    [Space]
    [Header("Registration")]
    public InputField nameRegisterField;
    public InputField emailRegisterField;
    public InputField passwordRegisterField;
    public InputField confirmPasswordRegisterField;

    // Firestore Variables
    [Space]
    [Header("Database")]
    public InputField scoreField;

    private void Start()
    {
        StartCoroutine(CheckAndFixDependenciesAsync());
    }

    private IEnumerator CheckAndFixDependenciesAsync()
    {
        var depentancyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => depentancyTask.IsCompleted);

        dependencyStatus = depentancyTask.Result;

        if (dependencyStatus == DependencyStatus.Available)
        {
            InitializeFirebase();
            yield return new WaitForEndOfFrame();
            StartCoroutine(CheckForAutoLogin());
        }
        else
        {
            Debug.LogError("Could not resolve all firebase dependencies: " + dependencyStatus);
        }
    }

    void InitializeFirebase()
    {
        //Set the default instance object
        auth = FirebaseAuth.DefaultInstance;

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    private IEnumerator CheckForAutoLogin()
    {
        if (user != null)
        {
            var reloadUserTask = user.ReloadAsync();
            yield return new WaitUntil(() => reloadUserTask.IsCompleted);
            AutoLogin();
        }
        else
        {
            UIManager.Instance.OpenLoginPanel();
        }
    }

    private void AutoLogin()
    {
        if (user != null)
        {
            if (user.IsEmailVerified)
            {
                References.userName = user.DisplayName;
                UIManager.Instance.OpenGamePanel();
            }
            else
            {
                SendEmailForVerification();
            }
        }
        else
        {
            UIManager.Instance.OpenLoginPanel();
        }
    }

    // Track state changes of the auth object.
    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
                UIManager.Instance.OpenLoginPanel();
                ClearLoginInputFieldText();
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
            }
        }
    }

    private void ClearLoginInputFieldText()
    {
        emailLoginField.text = "";
        passwordLoginField.text = "";
    }

    public void LogOut()
    {
        if (auth != null && user != null)
        {
            auth.SignOut();
        }
    }

    public void Login()
    {
        StartCoroutine(LoginAsync(emailLoginField.text, passwordLoginField.text));
    }

    private IEnumerator LoginAsync(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError(loginTask.Exception);

            FirebaseException firebaseException = loginTask.Exception.GetBaseException() as FirebaseException;
            AuthError authError = (AuthError)firebaseException.ErrorCode;


            string failedMessage = "Login Failed! Because ";

            switch (authError)
            {
                case AuthError.InvalidEmail:
                    failedMessage += "Email is invalid";
                    break;
                case AuthError.WrongPassword:
                    failedMessage += "Wrong Password";
                    break;
                case AuthError.MissingEmail:
                    failedMessage += "Email is missing";
                    break;
                case AuthError.MissingPassword:
                    failedMessage += "Password is missing";
                    break;
                default:
                    failedMessage = "Login Failed";
                    break;
            }

            Debug.Log(failedMessage);
        }
        else
        {
            user = loginTask.Result;

            Debug.LogFormat("{0} You Are Successfully Logged In", user.DisplayName);
            if (user.IsEmailVerified)
            {
                References.userName = user.DisplayName;
                UIManager.Instance.OpenGamePanel();
            }
            else
            {
                SendEmailForVerification();
            }
        }
    }

    public void Register()
    {
        StartCoroutine(RegisterAsync(nameRegisterField.text, emailRegisterField.text, passwordRegisterField.text, confirmPasswordRegisterField.text));
    }

    private IEnumerator RegisterAsync(string name, string email, string password, string confirmPassword)
    {
        if (name == "")
        {
            Debug.LogError("User Name is empty");
        }
        else if (email == "")
        {
            Debug.LogError("email field is empty");
        }
        else if (passwordRegisterField.text != confirmPasswordRegisterField.text)
        {
            Debug.LogError("Password does not match");
        }
        else
        {
            var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

            yield return new WaitUntil(() => registerTask.IsCompleted);

            if (registerTask.Exception != null)
            {
                Debug.LogError(registerTask.Exception);

                FirebaseException firebaseException = registerTask.Exception.GetBaseException() as FirebaseException;
                AuthError authError = (AuthError)firebaseException.ErrorCode;

                string failedMessage = "Registration Failed! Becuase ";
                switch (authError)
                {
                    case AuthError.InvalidEmail:
                        failedMessage += "Email is invalid";
                        break;
                    case AuthError.WrongPassword:
                        failedMessage += "Wrong Password";
                        break;
                    case AuthError.MissingEmail:
                        failedMessage += "Email is missing";
                        break;
                    case AuthError.MissingPassword:
                        failedMessage += "Password is missing";
                        break;
                    default:
                        failedMessage = "Registration Failed";
                        break;
                }

                Debug.Log(failedMessage);
            }
            else
            {
                // Get The User After Registration Success
                user = registerTask.Result;

                UserProfile userProfile = new UserProfile { DisplayName = name };

                var updateProfileTask = user.UpdateUserProfileAsync(userProfile);

                yield return new WaitUntil(() => updateProfileTask.IsCompleted);

                if (updateProfileTask.Exception != null)
                {
                    // Delete the user if user update failed
                    user.DeleteAsync();

                    Debug.LogError(updateProfileTask.Exception);

                    FirebaseException firebaseException = updateProfileTask.Exception.GetBaseException() as FirebaseException;
                    AuthError authError = (AuthError)firebaseException.ErrorCode;


                    string failedMessage = "Profile update Failed! Becuase ";
                    switch (authError)
                    {
                        case AuthError.InvalidEmail:
                            failedMessage += "Email is invalid";
                            break;
                        case AuthError.WrongPassword:
                            failedMessage += "Wrong Password";
                            break;
                        case AuthError.MissingEmail:
                            failedMessage += "Email is missing";
                            break;
                        case AuthError.MissingPassword:
                            failedMessage += "Password is missing";
                            break;
                        default:
                            failedMessage = "Profile update Failed";
                            break;
                    }

                    Debug.Log(failedMessage);
                }
                else
                {
                    Debug.Log("Registration Sucessful Welcome " + user.DisplayName);
                    if (user.IsEmailVerified)
                    {
                        UIManager.Instance.OpenLoginPanel();
                    }
                    else
                    {
                        SendEmailForVerification();
                    }
                }
            }
        }
    }

    public void SendEmailForVerification()
    {
        StartCoroutine(SendEmailForVerificationAsync());
    }

    private IEnumerator SendEmailForVerificationAsync()
    {
        if (user != null)
        {
            var  sendEmailTask = user.SendEmailVerificationAsync();
            yield return new WaitUntil(()=> sendEmailTask.IsCompleted);
            if (sendEmailTask.Exception != null)
            {
                FirebaseException firebaseException = sendEmailTask.Exception.GetBaseException() as FirebaseException;
                AuthError error = (AuthError)firebaseException.ErrorCode;

                string errorMessage = "Unknow Error : Please try again later";

                switch (error)
                {
                    case AuthError.Cancelled:
                        errorMessage = "Email Verification Was Cancel";
                        break;
                    case AuthError.TooManyRequests:
                        errorMessage = "Too many Requests";
                        break;
                    case AuthError.InvalidRecipientEmail:
                        errorMessage = "The Email you enter is invalid";
                        break;
                }
                UIManager.Instance.ShowVerificationResponce(false,user.Email,errorMessage);
            }
            else
            {
                Debug.Log("Email send successfully send");
                UIManager.Instance.ShowVerificationResponce(true, user.Email, null);
                
            }
        }
    }

    public void OpenGameScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    // Firestore Database functions

    public void AddScore()
    {
        StartCoroutine(AddScoreToFirestoreAsync(int.Parse(scoreField.text)));
    }

    public void GetScore()
    {
        StartCoroutine(GetScoreFromFirestoreAsync());
    }

    private IEnumerator AddScoreToFirestoreAsync(int score)
    {
        if (user == null)
        {
            Debug.LogError("User is not logged in");
            yield break;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("diamondHands").Document(user.UserId);

        Dictionary<string, object> data = new Dictionary<string, object>
    {
        { "score", score }
    };

        var task = docRef.SetAsync(data, SetOptions.MergeAll);

        // Wait until the task is completed
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsCanceled || task.IsFaulted)
        {
            Debug.LogError("Failed to add score to Firestore");
            yield break;
        }

        Debug.Log("Score added to Firestore");
    }

    private IEnumerator GetScoreFromFirestoreAsync()
    {
        if (user == null)
        {
            Debug.LogError("User is not logged in");
            yield break;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("diamondHands").Document(user.UserId);

        var task = docRef.GetSnapshotAsync();
        // Wait until the task is completed
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsCanceled || task.IsFaulted)
        {
            Debug.LogError("Failed to get score from Firestore");
            yield break;
        }

        DocumentSnapshot snapshot = task.Result;

        if (snapshot.Exists)
        {

            Dictionary<string, object> data = snapshot.ToDictionary();
            object score = data["score"];
            Debug.Log($"Score retrieved from Firestore: {score}");
        }
        else
        {
            Debug.Log("No score data found in Firestore");
        }
    }



}
