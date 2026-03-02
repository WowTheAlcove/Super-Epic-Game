using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShipRTToSprite : MonoBehaviour
{
    [SerializeField] private Texture2D shipRenderTexture;
    public Sprite shipRtSprite { get; private set; }
    private SpriteRenderer mySpriteRenderer;

    private void Awake()
    {
        SceneManager.LoadScene("3DShip",  LoadSceneMode.Additive);
        mySpriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        shipRtSprite = Sprite.Create(shipRenderTexture, new Rect(0, 0, shipRenderTexture.width, shipRenderTexture.height), new Vector2(0.5f,0.5f), 16);
        mySpriteRenderer.sprite = shipRtSprite;
    }
}