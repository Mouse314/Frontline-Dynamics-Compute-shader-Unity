using UnityEngine;

public class Main : MonoBehaviour
{
    public FractionsManager fractionsManager;
    public UIController uiController;
    public ComputeWorker computeWorker;
    public DrawController drawController;
    public ComputeBufferController computeBufferController;

    private void Awake()
    {
        uiController.fractionsManager = fractionsManager;
        uiController.computeWorker = computeWorker;
        uiController.computeBufferController = computeBufferController;

        drawController.computeWorker = computeWorker;
        drawController.fractionsManager = fractionsManager;
    }
}
