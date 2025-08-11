using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class delayanim : MonoBehaviour
{
    public float durasidelay;
    private Animator animator;
    // Start is called before the first frame update
    private void Awake()
    {
        gameObject.GetComponent<Animator>().enabled = false;
    }
    void Start()
    {
        animator = GetComponent<Animator>();
        
        StartCoroutine(WaitDelay());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator WaitDelay()
    {
        yield return new WaitForSeconds(durasidelay);
        gameObject.GetComponent<Animator>().enabled = true;
    }
}
