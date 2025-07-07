using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace View
{
    public class NumericStepper : MonoBehaviour
    {
        [SerializeField] private int minValue = 0;
        [SerializeField] private int maxValue = 5; //todo config

        [SerializeField] private Text label;

        [SerializeField] private UnityEvent OnChanged;

        public int Value => _value;
        private int _value = 0;

        private void Start()
        {
            _value = Mathf.Clamp(_value, minValue, maxValue);
            label.text = _value.ToString();
            OnChanged?.Invoke();
        }

        public void Increment()
        {
            if (_value >= maxValue) return;
            
            label.text = $"{++_value}";
            OnChanged?.Invoke();
        }

        public void Decrement()
        {
            if (_value <= minValue) return;
            
            label.text = $"{--_value}";
            OnChanged?.Invoke();
        }
    }
}