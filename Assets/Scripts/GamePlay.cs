using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePlay : MonoBehaviour
{

    public GameObject model, wall, gameOverText, scoreText, gameWonText, modelObject;

    private Vector3 wallStartPos, modelStartPos, modelStartRot;

    Text txt;
    private int currentscore = 0;

    // Use this for initialization
    void Start()
    {
        txt = scoreText.GetComponent<Text>();
        txt.text = "Score : " + currentscore;
        wallStartPos = wall.transform.position;
        modelStartPos = model.transform.position;
    }

    // Update is called once per frame
    private void Update()
    {
        Vector3 pos = wall.transform.position;
        if (pos.z < -94.5166)
        {
            wall.transform.position = wallStartPos;
            model.transform.position = modelStartPos;
            currentscore++;
            txt.text = "Score : " + currentscore;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        Debug.Log(gameObject.name + " was collided by " + other.gameObject.name);
        wall.SetActive(false);
        model.SetActive(false);
        gameOverText.SetActive(true);
    }
    // void OnTriggerEnter(Collider other) {
    //     Debug.Log(gameObject.name + " was triggered by " + other.gameObject.name);    
    // }
}
