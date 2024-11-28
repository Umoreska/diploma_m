using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextChecker : MonoBehaviour
{
    [SerializeField] private TMP_InputField seed_input;

    public void IsNumeric(string input) {
        string res = string.Empty;

        foreach (char c in input)
            if (char.IsDigit(c))
                res += c;
        seed_input.text = res;
    }
}
