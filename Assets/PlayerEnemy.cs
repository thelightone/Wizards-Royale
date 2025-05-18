using UnityEngine;
using UnityEngine.UI;

public class PlayerEnemy : Player
{
    public MeshRenderer botPanel;
    public Image slider;

    public Material redPanel, bluePanel;
    public Color redSlider, blueSlider;
    
    private void Start()
    {
        if(team == Team.Red)
        {
            botPanel.material = redPanel;
            slider.color = redSlider;
        }
        else
        {
            botPanel.material = bluePanel;
            slider.color = blueSlider;
        }
    }
}
