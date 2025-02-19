using UnityEngine;
using UnityEngine.UI;

public class FootOverlayManagerTwoFeet : MonoBehaviour
{
    [Header("Left Foot Overlays")]
    public Image leftFootLeftOverlay;
    public Image leftFootRightOverlay;
    public Image leftFootTopOverlay;
    public Image leftFootBottomOverlay;

    [Header("Right Foot Overlays")]
    public Image rightFootLeftOverlay;
    public Image rightFootRightOverlay;
    public Image rightFootTopOverlay;
    public Image rightFootBottomOverlay;

    [Header("Foot UI Groups")]
    // These are the parent GameObjects that hold each foot's overlays.
    public GameObject leftFootGroup;
    public GameObject rightFootGroup;

    public static FootOverlayManagerTwoFeet Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Updates the overlay colors for the specified foot based on the zone.
    /// </summary>
    public void UpdateOverlayForZone(int zone, string foot)
    {
        Color green = new Color(0, 1, 0, 0.5f);
        Color red = new Color(1, 0, 0, 0.5f);

        if (foot.ToLower() == "left")
        {
            switch (zone)
            {
                case 1:
                    leftFootLeftOverlay.color = green;
                    leftFootRightOverlay.color = green;
                    leftFootTopOverlay.color = green;
                    leftFootBottomOverlay.color = green;
                    break;
                case 2:
                    leftFootLeftOverlay.color = red;
                    leftFootRightOverlay.color = red;
                    leftFootTopOverlay.color = red;
                    leftFootBottomOverlay.color = red;
                    break;
                case 3:
                    leftFootLeftOverlay.color = green;
                    leftFootRightOverlay.color = green;
                    leftFootTopOverlay.color = green;
                    leftFootBottomOverlay.color = red;
                    break;
                case 4:
                    leftFootLeftOverlay.color = green;
                    leftFootRightOverlay.color = green;
                    leftFootTopOverlay.color = red;
                    leftFootBottomOverlay.color = green;
                    break;
                case 5:
                    leftFootLeftOverlay.color = green;
                    leftFootRightOverlay.color = red;
                    leftFootTopOverlay.color = green;
                    leftFootBottomOverlay.color = green;
                    break;
                case 6:
                    leftFootLeftOverlay.color = red;
                    leftFootRightOverlay.color = green;
                    leftFootTopOverlay.color = green;
                    leftFootBottomOverlay.color = green;
                    break;
                case 7:
                    leftFootLeftOverlay.color = red;
                    leftFootRightOverlay.color = red;
                    leftFootTopOverlay.color = red;
                    leftFootBottomOverlay.color = red;
                    break;
                default:
                    leftFootLeftOverlay.color = green;
                    leftFootRightOverlay.color = green;
                    leftFootTopOverlay.color = green;
                    leftFootBottomOverlay.color = green;
                    break;
            }
        }
        else if (foot.ToLower() == "right")
        {
            switch (zone)
            {
                case 1:
                    rightFootLeftOverlay.color = green;
                    rightFootRightOverlay.color = green;
                    rightFootTopOverlay.color = green;
                    rightFootBottomOverlay.color = green;
                    break;
                case 2:
                    rightFootLeftOverlay.color = red;
                    rightFootRightOverlay.color = red;
                    rightFootTopOverlay.color = red;
                    rightFootBottomOverlay.color = red;
                    break;
                case 3:
                    rightFootLeftOverlay.color = green;
                    rightFootRightOverlay.color = green;
                    rightFootTopOverlay.color = green;
                    rightFootBottomOverlay.color = red;
                    break;
                case 4:
                    rightFootLeftOverlay.color = green;
                    rightFootRightOverlay.color = green;
                    rightFootTopOverlay.color = red;
                    rightFootBottomOverlay.color = green;
                    break;
                case 5:
                    rightFootLeftOverlay.color = green;
                    rightFootRightOverlay.color = red;
                    rightFootTopOverlay.color = green;
                    rightFootBottomOverlay.color = green;
                    break;
                case 6:
                    rightFootLeftOverlay.color = red;
                    rightFootRightOverlay.color = green;
                    rightFootTopOverlay.color = green;
                    rightFootBottomOverlay.color = green;
                    break;
                case 7:
                    rightFootLeftOverlay.color = red;
                    rightFootRightOverlay.color = red;
                    rightFootTopOverlay.color = red;
                    rightFootBottomOverlay.color = red;
                    break;
                default:
                    rightFootLeftOverlay.color = green;
                    rightFootRightOverlay.color = green;
                    rightFootTopOverlay.color = green;
                    rightFootBottomOverlay.color = green;
                    break;
            }
        }
        else if (foot.ToLower() == "both")
        {
            // Update both feet.
            UpdateOverlayForZone(zone, "left");
            UpdateOverlayForZone(zone, "right");
        }
    }

    /// <summary>
    /// Sets which foot's UI group is active.
    /// </summary>
    public void SetActiveFoot(string activeFoot)
    {
        activeFoot = activeFoot.ToLower();
        if (activeFoot == "left")
        {
            if (leftFootGroup != null)
                leftFootGroup.SetActive(true);
            if (rightFootGroup != null)
                rightFootGroup.SetActive(false);
            Debug.Log("Active foot set to LEFT");
        }
        else if (activeFoot == "right")
        {
            if (rightFootGroup != null)
                rightFootGroup.SetActive(true);
            if (leftFootGroup != null)
                leftFootGroup.SetActive(false);
            Debug.Log("Active foot set to RIGHT");
        }
        else if (activeFoot == "both")
        {
            if (leftFootGroup != null)
                leftFootGroup.SetActive(true);
            if (rightFootGroup != null)
                rightFootGroup.SetActive(true);
            Debug.Log("Active foot set to BOTH");
        }
        else // "none" or any other value
        {
            if (leftFootGroup != null)
                leftFootGroup.SetActive(false);
            if (rightFootGroup != null)
                rightFootGroup.SetActive(false);
            Debug.Log("Active foot set to NONE");
        }
    }
}
