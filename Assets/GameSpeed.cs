using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSpeed : MonoBehaviour
{
    public void PauseGame()
    {
        Time.timeScale = 0f;
    }
    public void X1Speed()
    {
        Time.timeScale = 1f;
    }
    public void X2SPeed()
    {
        Time.timeScale = 2f;
    }
    public void X4Speed()
    {
        Time.timeScale = 4f;
    }
}
