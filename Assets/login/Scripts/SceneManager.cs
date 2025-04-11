using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class SceneManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private NetworkManager networkManager;

    private VisualElement loginUI;
    private VisualElement registerUI;

    private TextField userNameInput;
    private TextField emailInput;
    private TextField passwordInput;
    private TextField reenterPasswordInput;
    private DropdownField dayDropdown;
    private DropdownField monthDropdown;
    private DropdownField yearDropdown;
    private DropdownField genderDropdown;
    private DropdownField continentDropdown;
    private DropdownField countryDropdown;

    // Campos invisibles
    private TextField deviceModelField;
    private TextField osField;

    private Label messageLabel;

    private int currentUserId = 0;
    private string sessionStartTime = "";

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        loginUI = root.Q<VisualElement>("Login");
        registerUI = root.Q<VisualElement>("Register");

        userNameInput = registerUI.Q<TextField>("UserNameInput");
        emailInput = registerUI.Q<TextField>("EmailInput");
        var passwords = registerUI.Query<TextField>("Password").ToList();
        passwordInput = passwords[0];
        reenterPasswordInput = passwords[1];

        dayDropdown = registerUI.Q<DropdownField>("DayDropdown");
        monthDropdown = registerUI.Q<DropdownField>("MonthDropdown");
        yearDropdown = registerUI.Q<DropdownField>("YearDropdown");
        genderDropdown = registerUI.Q<DropdownField>("GenderDropdown");
        continentDropdown = registerUI.Q<DropdownField>("ContinentDropdown");
        countryDropdown = registerUI.Q<DropdownField>("CountryDropdown");

        deviceModelField = registerUI.Q<TextField>("DeviceModelField");
        osField = registerUI.Q<TextField>("OSField");

        messageLabel = registerUI.Q<Label>("Text");

        PopulateDateDropdowns();
        PopulateGenderDropdown();
        PopulateContinentDropdown();

        continentDropdown.RegisterValueChangedCallback(evt => UpdateCountryDropdown(evt.newValue));

        // Guardar info invisible
        deviceModelField.value = SystemInfo.deviceModel;
        osField.value = SystemInfo.operatingSystem;

        var registerButton = registerUI.Q<Button>("RegisterButton");
        if (registerButton != null)
            registerButton.clicked += SubmitRegister;

        var loginUsernameField = loginUI.Q<TextField>("LoginUser");
        var loginPasswordField = loginUI.Q<TextField>("LoginPass");
        var loginMessageLabel = loginUI.Q<Label>("Text");
        var loginButton = loginUI.Q<Button>("LoginButton");

        if (loginButton != null)
        {
            loginButton.clicked += () =>
            {
                string username = loginUsernameField.value.Trim();
                string password = loginPasswordField.value.Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    loginMessageLabel.text = "Please complete all fields.";
                    return;
                }

                loginMessageLabel.text = "Processing login...";

                networkManager.LoginUserExtended(
                    username,
                    password,
                    SystemInfo.deviceModel,
                    SystemInfo.operatingSystem,
                    Application.platform.ToString(),
                    Application.systemLanguage.ToString(),
                    (response) =>
                    {
                        loginMessageLabel.text = response.message;
                        Debug.Log($"🧠 Username: {username}, Password: {password}");
                        Debug.Log($"🪪 Response userId: {response.userId}");

                        if (response.done)
                        {
                            currentUserId = response.userId;
                            sessionStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            Debug.Log($"✅ Usuario autenticado. ID: {currentUserId}, Inicio: {sessionStartTime}");
                        }
                    }
                );
            };
        }

        ShowLogin();
    }

    private void PopulateDateDropdowns()
    {
        List<string> days = new List<string>();
        for (int i = 1; i <= 31; i++) days.Add(i.ToString("D2"));
        dayDropdown.choices = days;
        dayDropdown.index = 0;

        monthDropdown.choices = DateTimeFormatInfo.InvariantInfo.MonthNames[..12].ToList();
        monthDropdown.index = 0;

        List<string> years = new List<string>();
        int currentYear = DateTime.Now.Year;
        for (int i = currentYear; i >= 1900; i--) years.Add(i.ToString());
        yearDropdown.choices = years;
        yearDropdown.index = 0;
    }

    private void PopulateGenderDropdown()
    {
        genderDropdown.choices = new List<string> { "Male", "Female", "Non-binary", "Prefer not to say" };
        genderDropdown.index = 0;
    }

    private void PopulateContinentDropdown()
    {
        continentDropdown.choices = new List<string>(CountryData.CountriesByContinent.Keys);
        continentDropdown.index = 0;
        UpdateCountryDropdown(continentDropdown.value);
    }

    private void UpdateCountryDropdown(string continent)
    {
        if (CountryData.CountriesByContinent.TryGetValue(continent, out var countries))
        {
            countryDropdown.choices = countries;
            countryDropdown.index = 0;
        }
    }

    public void SubmitRegister()
    {
        if (string.IsNullOrEmpty(userNameInput.value) ||
            string.IsNullOrEmpty(emailInput.value) ||
            string.IsNullOrEmpty(passwordInput.value) ||
            string.IsNullOrEmpty(reenterPasswordInput.value))
        {
            messageLabel.text = "Please complete all fields.";
            return;
        }

        if (passwordInput.value != reenterPasswordInput.value)
        {
            messageLabel.text = "Passwords do not match.";
            return;
        }

        string day = dayDropdown.value;
        string month = (monthDropdown.index + 1).ToString("D2");
        string year = yearDropdown.value;
        string birthDateString = $"{year}-{month}-{day}";

        if (!DateTime.TryParseExact(birthDateString, "yyyy-MM-dd", null, DateTimeStyles.None, out DateTime birthDate))
        {
            messageLabel.text = "Invalid date of birth.";
            return;
        }

        string gender = genderDropdown.value;
        string country = countryDropdown.value;
        string deviceModel = SystemInfo.deviceModel;
        string operatingSystem = SystemInfo.operatingSystem;
        string platform = Application.platform.ToString();
        string systemLanguage = Application.systemLanguage.ToString();

        messageLabel.text = "Processing...";

        networkManager.CreateUserExtended(
            userNameInput.value,
            emailInput.value,
            passwordInput.value,
            birthDate.ToString("yyyy-MM-dd"),
            gender,
            country,
            deviceModel,
            operatingSystem,
            platform,
            systemLanguage,
            (response) =>
            {
                messageLabel.text = response.message;
            });
    }

    public void ShowLogin()
    {
        loginUI.style.display = DisplayStyle.Flex;
        registerUI.style.display = DisplayStyle.None;
    }

    public void ShowRegister()
    {
        loginUI.style.display = DisplayStyle.None;
        registerUI.style.display = DisplayStyle.Flex;
    }

    private void OnApplicationQuit()
    {
        if (currentUserId != 0 && !string.IsNullOrEmpty(sessionStartTime))
        {
            string endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Debug.Log($"🚪 Cerrando aplicación - Guardando sesión con ID {currentUserId}");
            StartCoroutine(SaveSessionBeforeExit(currentUserId, sessionStartTime, endTime));
        }
    }

    private IEnumerator SaveSessionBeforeExit(int userId, string start, string end)
    {
        bool done = false;

        networkManager.SaveSession(userId, start, end, (res) =>
        {
            Debug.Log("📤 Sesión guardada: " + res.message);
            done = true;
        });

        float timeout = 3f;
        while (!done && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!done)
        {
            Debug.LogWarning("⚠️ La sesión no se guardó antes de cerrar.");
        }
    }
}
