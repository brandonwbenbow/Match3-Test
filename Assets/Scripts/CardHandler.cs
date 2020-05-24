using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardHandler : MonoBehaviour
{
    GameplayManager gm;
    GameStateManager.Card thisCard;

    public float TurnSpeed = 1;

    bool RecievingRaycast = false;

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameplayManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (thisCard != null)
        {
            if (!RecievingRaycast && !thisCard._facingUp)
            {
                BackToRest();
            }
            else
            {
                Debug.Log("Raycast: " + !RecievingRaycast + "\n_facingUp: " + !thisCard._facingUp);
            }
        }
        else
        {
            gm.CARD_LIST.TryGetValue(this.gameObject, out thisCard);
        }

        RecievingRaycast = false;
    }

    public void CheckRaycast()
    {
        //gm.CARD_LIST.TryGetValue(this.gameObject, out thisCard);
        RecievingRaycast = true;
    }

    private void OnMouseOver()
    {
        if (!thisCard._facingUp && transform.rotation.eulerAngles.y < 25f)
        {
            Vector3 rotation = transform.rotation.eulerAngles;
            if (rotation.y > 25.5 || rotation.y < 24.5)
                rotation = new Vector3(0, rotation.y + 25 * Time.deltaTime * TurnSpeed, 0);
            transform.eulerAngles = rotation;
        }
    }

    // Reset card after hover
    void BackToRest()
    {
        if (transform.rotation.eulerAngles.y > 0f && transform.rotation.eulerAngles.y < 30f)
        {
            Vector3 rotation = transform.rotation.eulerAngles;
            if (rotation.y > 0)
                rotation = new Vector3(0, rotation.y - 25 * Time.deltaTime*TurnSpeed, 0);
            transform.eulerAngles = rotation;
        }
        else
        {
            this.transform.localRotation = Quaternion.identity;
        }

    }

    public void Flip()
    {
        
        thisCard._facingUp = !thisCard._facingUp;

        if (thisCard._facingUp)
            FlipGameObject();
        else
            ResetGameObject();
    }

    void FlipGameObject()
    {
        this.transform.eulerAngles = new Vector3(0f, 180f, 0f);
    }

    public void ResetGameObject()
    {
        thisCard._facingUp = false;
        this.transform.eulerAngles = new Vector3(0f, 0f, 0f);
    }

    public void CompletedTrio()
    {
        thisCard._completedTrio = true;
    }
}
