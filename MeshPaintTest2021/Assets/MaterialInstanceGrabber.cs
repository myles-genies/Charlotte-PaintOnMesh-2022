using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialInstanceGrabber : MonoBehaviour
{
    public GameObject go;

    private Material _grabMat;
    // Start is called before the first frame update
    void Start()
    {
        if (go) {
            // take material from specified GameObject and assign it
            // to this object
            _grabMat = go.GetComponent<Renderer>().material;
            GetComponent<Renderer>().material = _grabMat;
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        // scale according to that material's texture aspect
        Texture tex = _grabMat.GetTexture("_BaseMap");
        if (tex) {
            float aspect = tex.width / tex.height;
            transform.localScale = new Vector3(aspect, 1f, 1f);
        }
    }
}
