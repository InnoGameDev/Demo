using System;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PlayerShipMovement : NetworkBehaviour
{
    enum MoveType
    { 
        constant,
        momentum
    }

    enum VerticalMovementType
    {
        none,
        upward,
        downward
    }

    [Serializable]
    public struct PlayerLimits
    {
        public float minLimit;
        public float maxLimit;
    }

    [SerializeField]
    MoveType m_moveType;

    [SerializeField]
    PlayerLimits m_verticalLimits;

    [SerializeField]
    PlayerLimits m_hortizontalLimits;

    [Header("ShipSprites")]
    [SerializeField]
    SpriteRenderer m_shipRenderer;

    [SerializeField]
    Sprite m_normalSprite;

    [SerializeField]
    Sprite m_upSprite;

    [SerializeField]
    Sprite m_downSprite;

    [SerializeField]
    private float m_speed;

    private float m_inputX;
    private float m_inputY;

    private VerticalMovementType m_previousVerticalMovementType = VerticalMovementType.none;
    private VerticalMovementType m_currentVerticalMovementType = VerticalMovementType.none;

    const string k_horizontalAxis = "Horizontal";
    const string k_verticalAxis = "Vertical";

    // Update is called once per frame
    void Update()
    {
        // We're only updating the ship's movements when we're surely updating on the owning
        // instance
        if (!IsOwner)
            return;

        HandleKeyboardInput();

        UpdateVerticalMovementSprite();

        // Check the limits of the ship
        CheckLimits();

        MovePlayerShip();
    }

    private void HandleKeyboardInput()
    {
        /*
            There two types of movement:
            constant -> linear move, there are no time aceleration
            momentum -> move with aceleration at the start
        */
        if (m_moveType == MoveType.constant)
        {
            m_inputX = 0f;
            m_inputY = 0f;

            // Horizontal input
            if (Input.GetKey(KeyCode.D))
            {
                m_inputX = 1f;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                m_inputX = -1f;
            }

            // Vertical input and set the ship sprite
            if (Input.GetKey(KeyCode.W))
            {
                m_inputY = 1f;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                m_inputY = -1f;
            }
        }
        else if (m_moveType == MoveType.momentum)
        {
            // Get the inputs 
            m_inputX = Input.GetAxis(k_horizontalAxis);
            m_inputY = Input.GetAxis(k_verticalAxis);
        }
    }

    private void UpdateVerticalMovementSprite()
    {
        // Change the ship sprites base on the input value
        if (m_inputY > 0f)
        {
            m_shipRenderer.sprite = m_upSprite;
            m_currentVerticalMovementType = VerticalMovementType.upward;
        }
        else if (m_inputY < 0f)
        {
            m_shipRenderer.sprite = m_downSprite;
            m_currentVerticalMovementType = VerticalMovementType.downward;
        }
        else
        {
            m_shipRenderer.sprite = m_normalSprite;
            m_currentVerticalMovementType = VerticalMovementType.none;
        }

        bool changeInVerticalMovement =
            m_currentVerticalMovementType != m_previousVerticalMovementType;

        m_previousVerticalMovementType = m_currentVerticalMovementType;

        //inform the server of any updates to vertical movement sprites
        if (changeInVerticalMovement)
        {
            NewVerticalMovementServerRPC(m_currentVerticalMovementType);
        }
    }

    [ServerRpc]
    private void NewVerticalMovementServerRPC(VerticalMovementType newVerticalMovementType)
    {
        NewVerticalMovementClientRPC(newVerticalMovementType);
    }

    [ClientRpc]
    private void NewVerticalMovementClientRPC(VerticalMovementType newVerticalMovementType)
    {
        switch (newVerticalMovementType)
        {
            case VerticalMovementType.none:
                m_shipRenderer.sprite = m_normalSprite;
                break;
            case VerticalMovementType.upward:
                m_shipRenderer.sprite = m_upSprite;
                break;
            case VerticalMovementType.downward:
                m_shipRenderer.sprite = m_downSprite;
                break;
        }
    }

    // Check the limits of the player and adjust the input
    private void CheckLimits()
    {
        // Vertical limits
        if (transform.position.y <= m_verticalLimits.minLimit)
        {
            // Check if the inputs goes on that direction -> vertical min is negative
            if (Mathf.Approximately(Mathf.Sign(m_inputY), -1f))
            {
                m_inputY = 0f;
            }
        }
        else if (transform.position.y >= m_verticalLimits.maxLimit)
        {
            // Check if the inputs goes on that direction -> vertical max is positive
            if (Mathf.Approximately(Mathf.Sign(m_inputY), 1f))
            {
                m_inputY = 0f;
            }
        }

        // Horizontal limits
        if (transform.position.x <= m_hortizontalLimits.minLimit)
        {
            // Check if the inputs goes on that direction -> horizontal min is negative
            if (Mathf.Approximately(Mathf.Sign(m_inputX), -1f))
            {
                m_inputX = 0f;
            }
        }
        else if (transform.position.x >= m_hortizontalLimits.maxLimit)
        {
            // Check if the inputs goes on that direction -> horizontal max is positive
            if (Mathf.Approximately(Mathf.Sign(m_inputX), 1f))
            {
                m_inputX = 0f;
            }
        }
    }
    private void MovePlayerShip()
    {
        // Take the value from the input and multiply by speed and time
        float speedTimesDeltaTime = m_speed * Time.deltaTime;

        float yPos = m_inputY * speedTimesDeltaTime;
        float xPos = m_inputX * speedTimesDeltaTime;

        // move the ship
        transform.Translate(xPos, yPos, 0f);
    }
}