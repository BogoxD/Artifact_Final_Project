using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSpeed : MonoBehaviour
{
    public void PauseGame()
    {
        Time.timeScale = 0;
    }
    public void X1Speed()
    {
        Time.timeScale = 1;
    }
    public void X2SPeed()
    {
        Time.timeScale = 2;
    }
    public void X4Speed()
    {
        Time.timeScale = 4;
    }
}
