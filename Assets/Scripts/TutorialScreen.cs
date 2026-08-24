using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialScreen : MonoBehaviour
{

    [SerializeField] private GameObject[] firstObjs;
    [SerializeField] private GameObject secObj,thirdObj,fourthObj, fifthObj,nextBtn,tutorialScreen;
   
    
    


    private int index = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        index = 0;
        foreach (var obj in firstObjs)
        {
            obj.SetActive(true);
        }
      }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ControlTutorail()
    {

        switch(index)
        {
            case 0:
                foreach (var obj in firstObjs)
                {
                    obj.SetActive(false);
                }
                secObj.SetActive(true);
                index++;
                break;
            case 1:
                secObj.SetActive(false);
                thirdObj.SetActive(true);
                index++;
                break;
            case 2:
                thirdObj.SetActive(false);
                fourthObj.SetActive(true);
                index++;
                break;
            case 3:
                fourthObj.SetActive(false);
                fifthObj.SetActive(true);
                index++;
                nextBtn.SetActive(false);
               GameManager.Instance.isGameStarted = true;
                break;
           
        }
    }
}
