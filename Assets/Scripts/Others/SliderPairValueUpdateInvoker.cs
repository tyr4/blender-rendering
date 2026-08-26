using System;
using UnityEngine;

public class SliderPairValueUpdateInvoker : MonoBehaviour
{
    [SerializeField] private SliderValueUpdateInvoker pairX;
    [SerializeField] private SliderValueUpdateInvoker pairY;
    [SerializeField] private SliderValueUpdateInvoker pairZ;

    public event Action<float[]> OnSliderPairSettled;
    private float[] _vector;

    private void Start()
    {
        _vector = new float[3];
        
        pairX.OnSliderSettled += UpdateXValue;
        pairY.OnSliderSettled += UpdateYValue;
        pairZ.OnSliderSettled += UpdateZValue;
    }

    private void OnDestroy()
    {
        pairX.OnSliderSettled -= UpdateXValue;
        pairY.OnSliderSettled -= UpdateYValue;
        pairZ.OnSliderSettled -= UpdateZValue;
    }
    

    private void UpdateXValue(object value)
    {
        _vector[0] = Convert.ToSingle(value);

        OnSliderPairSettled?.Invoke(_vector);
    }

    private void UpdateYValue(object value)
    {
        _vector[1] = Convert.ToSingle(value);

        OnSliderPairSettled?.Invoke(_vector);
    }

    private void UpdateZValue(object value)
    {
        _vector[2] = Convert.ToSingle(value);

        OnSliderPairSettled?.Invoke(_vector);
    }

    public void ChangeSliderPairValues(float[] vector)
    {
        vector.CopyTo(_vector, 0);
    }
    
}
