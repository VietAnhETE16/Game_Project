using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReportUIController : MonoBehaviour
{
    [Header("References")]
    public AnomalyManager anomalyManager;

    [Header("UI")]
    public GameObject reportPanel;
    public TMP_Dropdown anomalyTypeDropdown;
    public TMP_Text resultText;

    [Header("Buttons")]
    public Button reportButton;
    public Button submitButton;
    public Button closeButton;

    private void Start()
    {
        if (reportPanel != null)
            reportPanel.SetActive(false);

        SetupDropdown();

        if (reportButton != null)
            reportButton.onClick.AddListener(OpenReportPanel);

        if (submitButton != null)
            submitButton.onClick.AddListener(SubmitReport);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseReportPanel);

        if (resultText != null)
            resultText.text = "";
    }

    private void SetupDropdown()
    {
        if (anomalyTypeDropdown == null)
            return;

        anomalyTypeDropdown.ClearOptions();

        anomalyTypeDropdown.options.Add(new TMP_Dropdown.OptionData("Object Movement"));
        anomalyTypeDropdown.options.Add(new TMP_Dropdown.OptionData("Object Disappearance"));
        anomalyTypeDropdown.options.Add(new TMP_Dropdown.OptionData("Extra Object"));
        anomalyTypeDropdown.options.Add(new TMP_Dropdown.OptionData("Intruder"));
        anomalyTypeDropdown.options.Add(new TMP_Dropdown.OptionData("Painting Change"));

        anomalyTypeDropdown.value = 0;
        anomalyTypeDropdown.RefreshShownValue();
    }

    private void OpenReportPanel()
    {
        if (reportPanel != null)
            reportPanel.SetActive(true);

        if (resultText != null)
            resultText.text = "";
    }

    private void CloseReportPanel()
    {
        if (reportPanel != null)
            reportPanel.SetActive(false);
    }

    private void SubmitReport()
    {
        if (anomalyManager == null || anomalyTypeDropdown == null)
            return;

        AnomalyType selectedType = ConvertDropdownValueToAnomalyType(anomalyTypeDropdown.value);

        bool success = anomalyManager.ReportAnomaly(selectedType);

        if (resultText != null)
        {
            if (success)
                resultText.text = "Report Successful";
            else
                resultText.text = "Report Failed";
        }

        if (success && reportPanel != null)
        {
            reportPanel.SetActive(false);
        }
    }

    private AnomalyType ConvertDropdownValueToAnomalyType(int value)
    {
        switch (value)
        {
            case 0:
                return AnomalyType.ObjectMovement;

            case 1:
                return AnomalyType.ObjectDisappearance;

            case 2:
                return AnomalyType.ExtraObject;

            case 3:
                return AnomalyType.Intruder;

            case 4:
                return AnomalyType.PaintingChange;

            default:
                return AnomalyType.ObjectMovement;
        }
    }
}