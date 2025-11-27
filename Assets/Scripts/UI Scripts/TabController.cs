using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public Image[] tabArray;
    public GameObject[] pageArray;
    [SerializeField] private UIController menuController;

    // Start is called before the first frame update
    void Start()
    {
        SelectTab(0);
    }
    
    public void SelectTab(int tabNo) {
        if (tabNo == 1) {
            menuController.DisplayMenuInventoryPanel();
        }
        for (int i = 0; i < pageArray.Length; i++) {
            pageArray[i].SetActive(false);
            tabArray[i].color = Color.grey;
        }
        pageArray[tabNo].SetActive(true);
        tabArray[tabNo].color = Color.white;
    }
}
