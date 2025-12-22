using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScreen : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Method to load the village map scene when play button is clicked
    public void PlayGame()
    {
        SceneManager.LoadScene("VilageMapScene");
    }
}
