using System.Collections.Generic;
using TMPro;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject uiPanel;

    public FractionsManager fractionsManager;
    public ComputeWorker computeWorker;
    public ComputeBufferController computeBufferController;

    public TMP_Dropdown fractionDropdown;
    public TMP_Text fractionNameText;
    public TMP_Dropdown computeUnitDropdown;
    public TMP_Text computeUnitNameText;
    public TMP_Dropdown computeBufferDropdown;
    public TMP_Text computeBufferNameText;

    private bool _isHidden = false;

    void Start()
    {
        // Fraction UI Setup
        fractionDropdown.ClearOptions();

        fractionsManager.fractions.ForEach(fraction =>
        {
            fractionDropdown.options.Add(new TMP_Dropdown.OptionData(fraction.fractionName, null, fraction.fractionColor));
        });
        UpdateFractionNameText();
        fractionDropdown.onValueChanged.AddListener(index =>
        {
            fractionsManager.SetActiveFraction(index);
            UpdateFractionNameText();
        });

        // Compute Unit UI Setup
        computeUnitDropdown.ClearOptions();

        computeWorker.computeUnits.ForEach(computeUnit =>
        {
            computeUnitDropdown.options.Add(new TMP_Dropdown.OptionData(computeUnit.Name));
        });
        UpdateComputeUnitNameText();
        computeUnitDropdown.onValueChanged.AddListener(index =>
        {
            computeWorker.activeShader = computeWorker.computeUnits[index].computeShader;
            UpdateComputeUnitNameText();
        });

        // Compute Buffer UI Setup
        computeBufferDropdown.ClearOptions();

        computeBufferController.computeBufferWraps.ForEach(computeBufferWrap =>
        {
            computeBufferDropdown.options.Add(new TMP_Dropdown.OptionData(computeBufferWrap.Name));
        });
        UpdateComputeBufferNameText();
        computeBufferDropdown.onValueChanged.AddListener(index =>
        {
            computeBufferController.activeBufferWrap = computeBufferController.computeBufferWraps[index];
            UpdateComputeBufferNameText();
        });
    }

    void Update()
    {
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            _isHidden = !_isHidden;
            uiPanel.SetActive(_isHidden);
        }
    }

    public void UpdateFractionNameText()
    {
        fractionNameText.text = "current fraction: " + fractionsManager.activeFraction.fractionName;
    }

    public void UpdateComputeUnitNameText()
    {
        int index = computeUnitDropdown.value;
        computeUnitNameText.text = "current operation: " + computeWorker.computeUnits[index].Name;
    }

    public void UpdateComputeBufferNameText()
    {
        int index = computeBufferDropdown.value;
        computeBufferNameText.text = "current visualization: " + computeBufferController.computeBufferWraps[index].Name;
    }
}
