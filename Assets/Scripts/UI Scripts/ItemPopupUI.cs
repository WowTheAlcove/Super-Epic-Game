using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemPopupUI : MonoBehaviour
{
    public static ItemPopupUI Instance { get; private set; }
    public GameObject itemPopupPrefab;
    [SerializeField] private int maxPopups = 5;
    [SerializeField] private float popupDuration = 2f;
    private readonly Queue<GameObject> activePopups = new Queue<GameObject>();

    private void Awake() {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of ItemPopupUIController detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    public void ShowItemPopup(string itemName, Sprite itemIcon)
    {
        GameObject newPopup = Instantiate(itemPopupPrefab, transform);
        //newPopup.GetComponentInChildren<TMP_Text>().text = itemName;
        //no name for now, just the icon
        newPopup.GetComponentInChildren<TMP_Text>().text = "";

        if(newPopup.transform.Find("ItemIcon").TryGetComponent<Image>(out Image image)){
            // as long as the prefab was correctly set up with an image component
            image.sprite = itemIcon;
        }

        activePopups.Enqueue(newPopup);

        if(activePopups.Count > maxPopups)// if we have more popups than the max allowed
        {
            GameObject oldPopup = activePopups.Dequeue();
            Destroy(oldPopup);
        }

        //Fade out and destroy
        StartCoroutine(FadeOutAndDestroy(newPopup));
    }

    private IEnumerator FadeOutAndDestroy(GameObject popup)
    {
        yield return new WaitForSecondsRealtime(popupDuration);
        if (popup == null) {
            //if the popup was destroyed before it's wait duration ended
            yield break;
        }

        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null) {
            //if the popup doesn't have a canvas group, add one
            Debug.LogError("Popup does not have a CanvasGroup component.");
        }

        for (float timePassed = 0f; timePassed < 1f; timePassed += Time.deltaTime)
        {
            if (canvasGroup == null) {
                //if the popup is destroyed mid fade
                yield break;
            }
            {
                canvasGroup.alpha = 1f - timePassed;
            }
            yield return null;
        }

        Destroy(popup);
    }
}