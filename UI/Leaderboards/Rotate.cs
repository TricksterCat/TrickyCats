using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    [SerializeField]
    private float _speed;
    
    // Update is called once per frame
    private void Update()
    {
        transform.Rotate(Vector3.forward, _speed * Time.deltaTime);
    }
}
