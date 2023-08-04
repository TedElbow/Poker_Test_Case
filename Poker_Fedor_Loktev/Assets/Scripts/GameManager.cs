using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System;
using TMPro;


public class GameManager : MonoBehaviour
{
    //[SerializeField] private GameObject CardPrefab;
    //[SerializeField] private GameObject BlindPrefab;
    [SerializeField] private GameObject table;
    [SerializeField] private Deck deck;
    [SerializeField] private BlindMaster blindMaster;
    //private Bank _bank;
    [SerializeField] private Player[] _players;
    [SerializeField] private Board board;

    private List<Card> cards;
    private Combination Nuts;

    [SerializeField] private TMP_Text _PotText;

    public bool IsBettingFinished { get; set; } = false;
    public Bank Bank { get; private set; } = new Bank();

    private Player _mainPlayer;
    bool _playerFold;
    public bool _gameManager;
    public bool Betted;
    void Start()
    {
        StartCoroutine(StartGameAnimation());
    }


 
    public void RemovePlayerFromArray() //remove player if he is fold 
    {
        int index = -1;

        for (int i = 0; i < _players.Length; i++)
        {
            if (_players[i].name == "Player")
            {
                _mainPlayer = _players[i];
                index = i;
                break;
            }
        }

        

        int remainingElements = _players.Length - index - 1;

        Array.Copy(_players, index + 1, _players, index, remainingElements);

        Array.Resize(ref _players, _players.Length - 1);

        _playerFold = true; //make boolian true if player fold cards
    }

    public void AddPlayerFromArray() //add player when new round started 
    {
  
        // Create a new array with a size one element larger than the old array
        Player[] newPlayers = new Player[_players.Length + 1];

        // Copy existing elements from the old array to the new array, starting from the second element (index 1)
        Array.Copy(_players, 0, newPlayers, 1, _players.Length);

        // Set the first element of the new array to the new element (_mainPlayer)
        newPlayers[0] = _mainPlayer;
        // Assign the new array to _players to update the old array
        _players = newPlayers;

        _playerFold = false; //disable boolian because round is over 
    }
    void SetBlinds()
    {
        blindMaster.GenerateBlinds();
        _players.First().atributes.Blind = blindMaster.SmallBlind;
        _players[1].atributes.Blind = blindMaster.BigBlind;
        _players.Last().atributes.Blind = blindMaster.DealerBlind;
    }

    public void ShowCards(Player player)
    {
        //ICardShowable => ShowCards
        for (int i = 0; i < 2; i++)
        {
            player.atributes.HandPosition[i].GetComponent<SpriteRenderer>().sprite = player.atributes._hand[i].CardFigure.Face;
            player.atributes.HandPosition[i].GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
        }
    }

    public void ShowBlinds()
    {
        foreach (var player in _players)
        {
            player.atributes.BlindPosition.GetComponent<SpriteRenderer>().sprite = null;
            if (player.atributes.Blind != null)
            {
                player.atributes.BlindPosition.GetComponent<SpriteRenderer>().sprite = player.atributes.Blind.BlindFigure.Face;
            }
        }
    }

    public void HideCards(Player player)
    {
        for (int i = 0; i < 2; i++)
        {
            player.atributes.HandPosition[i].GetComponent<SpriteRenderer>().sprite = deck.GetComponent<CardNames>().cardBack;
            player.atributes.HandPosition[i].GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
        }
    }


    public void PlayPreFlop()
    {
        SetBlinds();
        ShowBlinds();

        Bank.AcceptBlinds(_players);

        for (int i = 0; i < 5; i++)
        {
            board.AddCardToBoard(cards[0]);

            board.cardPlaces[i].GetComponent<SpriteRenderer>().sprite = deck.GetComponent<CardNames>().cardBack;
            cards.RemoveAt(0);
        }

        foreach (var player in _players)
        {
            player.atributes.AddCardToHand(cards[0]);
            player.atributes.AddCardToHand(cards[1]);
            cards.RemoveRange(0, 2);
            HideCards(player);
            if (!player.IsBot)
            ShowCards(player);  
        }
        _ = Bank.RequestBet(_players);
    }

    public void PlayFlop()
    {
        for (int i = 0; i < 3; i++)
        {
            board.cardPlaces[i].GetComponent<SpriteRenderer>().sprite = board.boadCards[i].CardFigure.Face;
            board.cardPlaces[i].GetComponent<AudioSource>().Play();
        }

        _PotText.text = $"Pot: {Bank.Pot}";

        _ = Bank.RequestBet(_players);

    }

    public void PlayTurn()
    {
        board.cardPlaces[3].GetComponent<SpriteRenderer>().sprite = board.boadCards[3].CardFigure.Face;
        board.cardPlaces[3].GetComponent<AudioSource>().Play();
        _ = Bank.RequestBet(_players);
    }

    public void PlayRiver()
    {
        board.cardPlaces[4].GetComponent<SpriteRenderer>().sprite = board.boadCards[4].CardFigure.Face;
        board.cardPlaces[4].GetComponent<AudioSource>().Play();
        _ = Bank.RequestBet(_players);
    }

    public void ShowDown()
    {
        Nuts = CombinationMaster.FindBestCombination((new List<Card>(_players.First().atributes.Hand)).Concat(board.boadCards).ToList());
        foreach (var player in _players)
        {
            if(player.IsActive)
            {
                ShowCards(player);
                player.combination = CombinationMaster.FindBestCombination((new List<Card>(player.atributes.Hand)).Concat(board.boadCards).ToList());
                if (player.combination.Rank < Nuts.Rank)
                    Nuts = player.combination;


                Debug.Log($"player with: {player.combination.Name + player.combination.Rank}");
            }
        }
        
    }

    public List<Player> DefineWinners()
    {
        var winners = CombinationMaster.FindWinners(_players.ToList().FindAll(p => p.combination.Rank == Nuts.Rank && p.IsActive), board.boadCards);
        Debug.Log(winners);
        foreach(var player in winners)
        {
            for (int i = 0; i < 2; i++)
            {
                player.atributes.HandPosition[i].GetComponent<SpriteRenderer>().color = new Color(1, 0.8f, 0.05f, 1); //change color for winner cards
            }

            
        }

        return winners;
    }

    public void MovePlayers()
    {
        var temp = _players[0];
        for (int i = 0; i < _players.Length; i++)
        {
            if(i != _players.Length - 1)
            {
                _players[i] = _players[ i + 1];
            }
            else
            {
                _players[i] = temp;
            }
            Debug.Log(_players[i].IsBot);
        }
    }

    public void ContinueGame()
    {
        foreach (var player in _players)
        {
            if(player.Balance != 0)
            player.NullifyPlayer();
        }
        board.boadCards.Clear();
        Bank.NullifyCurrentBank();
        MovePlayers();
        cards = deck.GenerateNewDeck();
        deck.Shuffle(cards);
        PlayPreFlop();
    }

    public void RestartGame()
        => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    void OnEnable()
    {
        Bank.OnBettingFinished += BettingEndLisentner;
    }

    void OnDisable()
    {
        Bank.OnBettingFinished -= BettingEndLisentner;
    }

    private void BettingEndLisentner()
    {
        StartCoroutine(ChipsPulling());
        IsBettingFinished = true;
    }

    void Update()
    {
        

        if (Input.GetKeyDown(KeyCode.F))
        {
            PlayFlop();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            PlayTurn();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayRiver();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            ShowDown();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            Bank.RecieveBankToWiners(DefineWinners());
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            RestartGame();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ContinueGame();
        }
    }

    IEnumerator PlayGameSequence()
    {

        ContinueGame();
        while (!IsBettingFinished)
        {
            yield return null;
        }
        yield return new WaitForSeconds(1);
        IsBettingFinished = false;

        PlayFlop();
        while (!IsBettingFinished)
        {
            yield return null;
        }
        yield return new WaitForSeconds(1);
        IsBettingFinished = false;

        PlayTurn();
        while (!IsBettingFinished)
        {
            yield return null;
        }
        yield return new WaitForSeconds(1);
        IsBettingFinished = false;

        PlayRiver();
        while (!IsBettingFinished)
        {
            yield return null;
        }
        yield return new WaitForSeconds(1);
        IsBettingFinished = false;

        ShowDown();
        yield return new WaitForSeconds(2);
        DefineWinners();


        yield return new WaitForSeconds(3);
        Bank.RecieveBankToWiners(DefineWinners());

        yield return new WaitForSeconds(4);
        _PotText.text = $"Pot: 0";
        StartCoroutine(EndGameAnimation());
    }

    #region Animation
    IEnumerator StartGameAnimation() // start animation with cards receiviing 
    {
        foreach (var player in _players)
        {
            Animator animator = player.GetComponent<Animator>(); //identify animator

            if (animator != null)
            {
                var PlayerCards = player.GetComponent<Player>().atributes.HandPosition; //identify cards for player to anable it 
                foreach (var Card in PlayerCards)
                {
                    Card.SetActive(true);
                }
                // Запускаем анимацию
                animator.SetTrigger("StartRound");
                player.GetComponent<AudioSource>().Play();

                // Ждем, пока анимация завершится
                yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
                player.GetComponent<AudioSource>().Stop();
            }
        }
        StartCoroutine(FiveCardsAnimation());
    }

    IEnumerator FiveCardsAnimation() // start animation 5 cards on the center of table 
    {

        Animator animator = table.GetComponent<Animator>();

        var FiveCards = board.cardPlaces;

        foreach (var Card in FiveCards)
        {
            Card.SetActive(true);
        }

        animator.SetTrigger("5Cards");

        table.GetComponent<AudioSource>().Play();

        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        table.GetComponent<AudioSource>().Stop();
        StartCoroutine(PlayGameSequence());
        
    }
    IEnumerator ChipsPulling() // start animation for pulling chips
    {
        
        if (Betted == true)
        {
            foreach (var player in _players)
            {
                Animator animator = player.GetComponent<Animator>(); //identify animator

                if (animator != null)
                {

                    // Запускаем анимацию
                    animator.SetTrigger("ChipsPull");

                    // Ждем, пока анимация завершится
                    yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
                }
            }
            _PotText.text = $"Pot: {Bank.Pot}";
            Betted = false;
        }
        
    }
    IEnumerator EndGameAnimation() // start animation with cards receiviing 
    {
        if (_playerFold)
        {
            AddPlayerFromArray();
        }
        foreach (var player in _players)
        {
            Animator animator = player.GetComponent<Animator>(); //identify animator

            if (animator != null)
            {
                animator.SetTrigger("EndRound");
                player.GetComponent<AudioSource>().Play();
                // Ждем, пока анимация завершится
                yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length-0.5f);

                player.GetComponent<AudioSource>().Stop();

                HideCards(player);

                var PlayerCards = player.GetComponent<Player>().atributes.HandPosition; //identify cards for player to anable it 
                foreach (var Card in PlayerCards)
                {
                    Card.GetComponent<AudioSource>().Play();
                    Card.SetActive(false);
                }
            }
        }
        StartCoroutine(FiveCardsEndAnimation());
    }

    IEnumerator FiveCardsEndAnimation() // start animation 5 cards on the center of table 
    {

        Animator animator = table.GetComponent<Animator>();

        var FiveCards = board.cardPlaces;

        animator.SetTrigger("End5Cards");
        table.GetComponent<AudioSource>().Play();

        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length-0.4f);

        table.GetComponent<AudioSource>().Stop();
        foreach (var Card in FiveCards)
        {
            Card.SetActive(false);
        }

        for (int i = 0; i < 5; i++)
        {
            board.AddCardToBoard(cards[0]);

            board.cardPlaces[i].GetComponent<SpriteRenderer>().sprite = deck.GetComponent<CardNames>().cardBack;
            //cards.RemoveAt(0);
        }

        StartCoroutine(StartGameAnimation());

    }
    #endregion
}