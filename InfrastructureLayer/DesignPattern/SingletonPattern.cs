using UnityEngine;

public class DesignPattern : MonoBehaviour
{
    //µ¥Àý
    public static DesignPattern Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
            Destroy(this.gameObject);
            
    }
}
