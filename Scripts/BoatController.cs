using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Uduino;

public class BoatController : MonoBehaviour
{
    public Rigidbody       rigidbody;
    public Transform       transform;
    public Transform       gate;
    public TextMeshProUGUI currentTimeText;
    public TextMeshProUGUI highScoreTimeText;
    public TextMeshProUGUI velocityText;
    public Animator        panelAnimator;
    public GameObject[]    rocks;
    public GameObject      oceanTile;
    public Transform       dangerZone;

    bool covered   = false;
    bool goingBack = true;

    int rockWidth    = 150;
    int rocksPerTile = 30;
    int lastTileX;

    float dangerZoneSpeed = 1.5f;
    float sensorVal       = 0;
    float seatLength      = 1;
    float velocity        = 0;
    float enterTime;
    float exitTime;
    float deltaTime;
    float startTime;

    List<GameObject> rocksInGame = new List<GameObject>();
    List<GameObject> oceanTilesInGame = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        UduinoManager.Instance.pinMode(AnalogPin.A0, PinMode.Input);

        startTime = Time.time;

        //set text
        currentTimeText.text = "00:00";
        highScoreTimeText.text = FormatTime(PlayerPrefs.GetFloat("highscore"));

        //spawn rocks
        SpawnRocks(rocks, rocksPerTile, 0, -680);

        lastTileX = (int)oceanTile.transform.position.x;
    }

    // Update is called once per frame
    void Update()
    {
        currentTimeText.text = FormatTime(Time.time - startTime);
        highScoreTimeText.text = FormatTime(PlayerPrefs.GetFloat("highscore"));

        sensorVal = UduinoManager.Instance.analogRead(AnalogPin.A0);

        //check sensor info
        if (sensorVal > 200) //if over sensor
        {
            //if just entered sensor
            if (!covered)
            {
                //record time at which began covering the sensor
                enterTime = Time.time;

                covered = true;
            }
        }
        else //if not over sensor
        {
            //if just exited sensor
            if (covered)
            {
                //record time at which began covering the sensor
                exitTime = Time.time;

                covered = false;

                //if going back, push boat
                if (goingBack)
                {
                    //find velocity
                    deltaTime = exitTime - enterTime;
                    velocity = seatLength / deltaTime;

                    //apply force to boat
                    PushBoat(velocity);

                    goingBack = false;
                }
                else
                {
                    goingBack = true;
                }
            }
        }

        //limit boat velocity
        if (rigidbody.velocity.x < -67)
        {
            PushBoat(-velocity / 2);
        }

        //move danger zone closer once boat has gone through gate
        if (transform.position.x < gate.position.x)
        {
            Vector3 gain = new Vector3(dangerZoneSpeed * Time.deltaTime, 0, 0);
            dangerZone.position -= gain;

            //keep dangerzone close
            if (-(transform.position.x - dangerZone.position.x) > 470)
            {
                dangerZone.position = transform.position + new Vector3(470, 1, 0);
            }

            //increase speed
            if (dangerZoneSpeed < 51)
            {
                dangerZoneSpeed *= 1.0055f;
            } else
            {
                dangerZoneSpeed = 51;
            }

        } else
        {
            //don't let time start until passes gate
            startTime = Time.time;
        }

        //check if needsd to extend scene
        if (transform.position.x < lastTileX - 200)
        {
            ExtendScene();
        }

        //display velocity
        velocityText.text = (int)(-rigidbody.velocity.x / 2) + " kn";

        //update high score
        float timeAlive = Time.time - startTime; ;

        if (timeAlive > PlayerPrefs.GetFloat("highscore"))
        {
            PlayerPrefs.SetFloat("highscore", timeAlive);
        }

        //reset high score if Z (zero) is pressed
        if (Input.GetKeyDown(KeyCode.Z))
        {
            PlayerPrefs.SetFloat("highscore", 0);
        }

        //reset game if R is pressed
        if (Input.GetKeyDown(KeyCode.R))
        {
            GameOver();
        }
    }

    public void PushBoat(float v)
    {
        rigidbody.AddRelativeForce(-v * 50, 0, 0);
    }

    //level over
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.name.Equals("Danger Zone"))
        {
            //remove text
            currentTimeText.enabled = false;
            highScoreTimeText.enabled = false;
            velocityText.enabled = false;

            //start game over
            StartCoroutine(GameOver());
        }
    }

    public string FormatTime(float seconds)
    {
        return string.Format("{0:#00}:{1:00}",
                      Mathf.Floor(seconds / 60),//minutes
                      Mathf.Floor(seconds) % 60);//seconds
    }

    public void SpawnRocks(GameObject[] objects, int count, int xStart, int xEnd)
    {
        for (int i = 0; i < count; i++)
        {
            int objectI = Random.Range(0, objects.Length - 1);

            //set location
            int x = Random.Range(xStart, xEnd);
            int z = 0;

            //prevents collisions with boat
            while (z > -25 && z < 25)
            {
                z = Random.Range(-rockWidth, rockWidth);
            }

            //set rotation
            int xRot = 0;
            int yRot = Random.Range(0, 180);
            int zRot = Random.Range(0, 180);

            //create rock
            GameObject newRock = Instantiate(objects[objectI], new Vector3(x, -0.5f, z),
                    Quaternion.Euler(xRot, yRot, zRot)) as GameObject;
            rocksInGame.Add(newRock);
        }
    }

    public void ExtendScene()
    {
        //load more of scene
        GameObject newTile = Instantiate(oceanTile, new Vector3(lastTileX - 750, 0, 0),
                    Quaternion.Euler(0, 0, 0));
        oceanTilesInGame.Add(newTile);

        //spawn rocks
        SpawnRocks(rocks, rocksPerTile, lastTileX - 750 + 340, lastTileX - 750 - 340);

        //set new variables
        lastTileX -= 750;

        //delete old rocks
        for (int i = 0; i < rocksInGame.Count; i++)
        {
            //delete rocks if behind boat
            if (rocksInGame[i].transform.position.x > transform.position.x + 30)
            {
                Destroy(rocksInGame[i]);
                rocksInGame.Remove(rocksInGame[i]);
            }
        }

        //delete old tiles
        if (oceanTilesInGame.Count > 2)
        {
            Destroy(oceanTilesInGame[0]);
            oceanTilesInGame.Remove(oceanTilesInGame[0]);
        }
    }

    IEnumerator GameOver()
    {
        //sink boat
        rigidbody.velocity = Vector3.zero;
        rigidbody.AddForce(0, -200, 0);

        //wait for boat to sink
        yield return new WaitForSeconds(4);

        //start animation
        panelAnimator.SetBool("Game Over", true);

        //wait for animation to play
        yield return new WaitForSeconds(1);

        //reset variables
        startTime = Time.time;
        panelAnimator.SetBool("Game Over", false);

        //load scene again
        SceneManager.LoadScene(0);
    }
}