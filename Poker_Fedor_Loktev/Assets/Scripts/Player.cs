using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.WSA;
using TMPro;

public class Player : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Timer _timer;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    [SerializeField] private Button? CallButton;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    [SerializeField] private Button? FoldButton;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    [SerializeField] private Button? BetButton;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    [SerializeField] private InputField? BetInputField;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    [SerializeField] private Slider? BetSlider;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.


    [SerializeField] private TMP_Text _betText;
    [SerializeField] private TMP_Text _balanceText;

    private int _balance = 1000;
    [SerializeField] private int _betValue = 0;
    [SerializeField]  private int betAmount;

    public PlayerBoardAtributes atributes;
    public Combination combination;

    [SerializeField] 
    public bool IsBot = true;
    private bool _isActive = true;
    

    public GameObject WinChips;

    public bool IsActive
    { 
        get { return _isActive; }
        set
        {
            if (value == false)
            {
                foreach (var card in atributes.HandPosition)
                {
                    card.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.3f, 0.03f, 1);
                }
            }
            else
            {
                foreach (var card in atributes.HandPosition)
                {
                    card.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
                }
            }
            _isActive = value;
        }
    }

    public void SetUpSliderValue()
    {
        BetSlider.minValue = _gameManager.Bank.CurrentBetValue - BetValue;
        BetSlider.maxValue = Balance;
        BetSlider.value = BetSlider.minValue;
    }

    public void NullifyPlayer()
    {
        _betValue = 0;
        BetValue = 0;
        IsActive = true;
        atributes.Hand = new List<Card>();
        atributes.Blind = null;
    }

    public void StartPlayerTimer()
        => _timer.StartTimer(atributes.TimerPosition.transform.position);

    public void StopPlayerTimer()
        => _timer.StopTimer();

    public int BetValue
    {
        get { return _betValue; }
        set
        {
            if (value < 0)
                throw new Exception("Value of bet cannot be negative");

            if (value > _balance)
                throw new Exception("Value of bet cannot be more than balance");

            Balance -= value;
            _betValue += value;
            _betText.text = $"Bet: {_betValue}";
        }
    }
    public int Balance //
    {
        get { return _balance; }
        set 
        {
            if(value < 0)
            {
                throw new Exception("Value of balance cannot be negative");
            }

            _balanceText.text = $"Balance: {value}";
            _balance = value; 
        }
    }


    void Start()
    {
        if (!IsBot)
        {
            DisableButtons();
        }    
    }

    void Update()
    {
        if (!IsBot) //Check Bet button activity 
        {

            int.TryParse(BetInputField.text, out betAmount);
            if (BetSlider.value > _betValue- _betValue || betAmount > _betValue- _betValue)
            {
                BetButton.interactable = true;
                CallButton.interactable = false;
            }
            else
            {
                BetButton.interactable = false;
            }
        }
    }

    private void OnEnable()
    {
        if (!IsBot)
        {
            CallButton.onClick.AddListener(Call);
            FoldButton.onClick.AddListener(Fold);
            BetButton.onClick.AddListener(Bet);

            BetSlider.onValueChanged.AddListener(UpdateBetInputFeid);

            _gameManager.Bank.OnBettingStart += BetResponse;

        }

    }

    private void UpdateBetInputFeid(float value) //Field on the right side of the bet button
    {
        BetInputField.text = Math.Floor(value).ToString();
    }

    private void OnDisable()
    {
        if (!IsBot)
        {
            CallButton.onClick.RemoveAllListeners();
            FoldButton.onClick.RemoveAllListeners();
            BetButton.onClick.RemoveAllListeners();

            _gameManager.Bank.OnBettingStart -= BetResponse;
        }

    }

    public void EnableButtons() //enable buttons to make action
    {
        SetUpSliderValue();
        BetSlider.interactable = true;
        CallButton.interactable = true;
        FoldButton.interactable = true;
        BetButton.interactable = true;

        

        Debug.Log("Buttons are enabled");
    }

    public void DisableButtons() //Diable button when action was made 
    {
        CallButton.interactable = false;
        FoldButton.interactable = false;
        BetButton.interactable = false;

        BetSlider.interactable = false;

        Debug.Log("Buttons are disabled");
    }

    private void BetResponse() //Anable buttons for action 
    {
        EnableButtons();
        Debug.Log("Waiting for bet...");     
    }

    public void Bet() //Bet button functionality
    {
        if (int.TryParse(BetInputField.text, out var newBet))
        {
           Debug.Log($"Bet is {newBet}");
           BetValue = newBet;
            
        }
        else if(String.IsNullOrWhiteSpace(BetInputField.text))
        {
            newBet = 0;
            Debug.Log($"Bet is {newBet}");
            BetValue = newBet;
            
        }
        else
        {
            throw new Exception("Cannot convert text to int");
        }

        BetButton.GetComponent<AudioSource>().Play();

        DisableButtons();
        _gameManager.Bank.BetCancellationTokenSource.Cancel();
        _gameManager.Betted = true;
        NullBet();
        
    }

    public void Call()
    {
        CallButton.GetComponent<AudioSource>().Play();
        Debug.Log("It's call");
        var newBet = _gameManager.Bank.CurrentBetValue - BetValue;
        BetValue = newBet;
        DisableButtons();
        _gameManager.Bank.BetCancellationTokenSource.Cancel();
    }

    public void Fold()
    {
        FoldButton.GetComponent<AudioSource>().Play();
        Debug.Log("It's Fold");
        BetValue = 0;
        NullBet();
        DisableButtons();
        _gameManager.Bank.BetCancellationTokenSource.Cancel();
        _gameManager.RemovePlayerFromArray();
        
    }

   
    private void NullBet()
    {
        BetSlider.minValue = 0;
        BetSlider.value = 0;
        BetInputField.text = "0";
    }

}
