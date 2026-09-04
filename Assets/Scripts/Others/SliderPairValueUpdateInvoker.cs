using System;
using UnityEngine;
using UnityEngine.Serialization;

public class SliderPairValueUpdateInvoker : MonoBehaviour
{
    [SerializeField] private SliderValueUpdateInvoker pairX;
    [SerializeField] private SliderValueUpdateInvoker pairY;
    [SerializeField] private SliderValueUpdateInvoker pairZ;

    public event Action<float[]> OnSliderPairSettled;
    private Action<object> _onX, _onY, _onZ;

    private Func<float[]> _getter;
    private Action<float[]> _setter;
    
    private bool _shouldInvoke = true;

    private void Start()
    {
        _onX = v => UpdateComponent(0, v);
        _onY = v => UpdateComponent(1, v);
        _onZ = v => UpdateComponent(2, v);
        
        pairX.OnSliderSettled += _onX;
        pairY.OnSliderSettled += _onY;
        pairZ.OnSliderSettled += _onZ;
    }

    private void OnDestroy()
    {
        pairX.OnSliderSettled -= _onX;
        pairY.OnSliderSettled -= _onY;
        pairZ.OnSliderSettled -= _onZ;
    }

    public void Bind(Func<float[]> getter, Action<float[]> setter)
    {
        _getter = getter;
        _setter = setter;
        
        RefreshFromSource();
    }

    private void UpdateComponent(int index, object value)
    {
        if (_getter == null) return;
        
        var currentValue = _getter();
        currentValue[index] = Convert.ToSingle(value);
        
        _setter?.Invoke(currentValue);
        if (_shouldInvoke) OnSliderPairSettled?.Invoke(currentValue);
    }

    private void RefreshFromSource()
    {
        if (_getter == null) return;
        
        ChangeSliderPairValues(_getter());
    }

    public void ChangeSliderPairValues(float[] vector)
    {
        if (vector == null) return;
        
        _shouldInvoke = false;
        
        pairX.ChangeSliderValue(vector[0]);
        pairY.ChangeSliderValue(vector[1]);
        pairZ.ChangeSliderValue(vector[2]);
        
        _shouldInvoke = true; 
    }
}
