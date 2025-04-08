using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreMenu : NetworkBehaviour
{
    private TMP_Text player1ScoreText;
    private TMP_Text player2ScoreText;

    private TMP_Text winnerText;
    private TMP_Text winAmountText;

    private Button changeWinAmountButton;

    private TMP_Text changeWinAmountText;

    private TMP_InputField changeWinAmountInput;

    private Button changeWinAmountSave;

    private Button changeWinAmountCancel;

    private TMP_Text hostChangingWinAmountText;

    private GameObject changeWinAmountBackgroundPanel;

    private GameObject scoreboardPanel;

    [SyncVar(hook = nameof(OnRedScoreChanged))]
    private int player1Score = 0;

    [SyncVar(hook = nameof(OnBlueScoreChanged))]
    private int player2Score = 0;

    [SyncVar(hook = nameof(OnWinnerChanged))]
    private bool isWinner = false;

    [SyncVar(hook = nameof(OnWinnerStringChanged))]
    private string winnerString = "";

    [SyncVar(hook = nameof(OnWinAmountChanged))]
    private int winAmount = 10;

    [SyncVar(hook = nameof(OnWinAmountUIChanged))]
    private bool showChangeWinAmountUI = false;

    private Button resetButton;

    private GameManager gameManager;

    private void Start()
    {
        // Find the TMP_Text components for red and blue scores as children of this GameObject
        player1ScoreText = transform.Find("CyanScore").GetComponent<TMP_Text>();
        player2ScoreText = transform.Find("PurpleScore").GetComponent<TMP_Text>();
        winnerText = transform.Find("WinnerText").GetComponent<TMP_Text>();
        winAmountText = transform.Find("WinAmountText").GetComponent<TMP_Text>();
        changeWinAmountButton = transform.Find("ChangeWinAmountButton").GetComponent<Button>();
        gameManager = FindObjectsByType<GameManager>(FindObjectsSortMode.None)[0];
        changeWinAmountText = transform.Find("ChangeWinAmountInstructions").GetComponent<TMP_Text>();
        changeWinAmountInput = transform.Find("ChangeWinAmountInput").GetComponent<TMP_InputField>();
        changeWinAmountSave = transform.Find("ChangeWinAmountSave").GetComponent<Button>();
        changeWinAmountCancel = transform.Find("ChangeWinAmountCancel").GetComponent<Button>();
        hostChangingWinAmountText = transform.Find("HostChangingWinAmountText").GetComponent<TMP_Text>();
        resetButton = transform.Find("ResetButton").GetComponent<Button>();
        changeWinAmountBackgroundPanel = transform.Find("ChangeWinAmountBackgroundPanel").gameObject;
        scoreboardPanel = transform.Find("Scoreboard").gameObject;


        changeWinAmountButton.onClick.AddListener(OnChangeWinAmountClick);
        changeWinAmountSave.onClick.AddListener(OnChangeWinAmountSaveClick);
        changeWinAmountCancel.onClick.AddListener(OnChangeWinAmountCancelClick);
        resetButton.onClick.AddListener(OnResetButtonClick);

        UpdateScoreUI();
    }

    private void setChangeWinAmountUIVisibility(bool isVisible)
    {
        if (changeWinAmountText != null)
        {
            changeWinAmountText.gameObject.SetActive(isVisible);
        }

        if (changeWinAmountInput != null)
        {
            changeWinAmountInput.gameObject.SetActive(isVisible);
        }

        if (changeWinAmountSave != null)
        {
            changeWinAmountSave.gameObject.SetActive(isVisible);
        }

        if (changeWinAmountCancel != null)
        {
            changeWinAmountCancel.gameObject.SetActive(isVisible);
        }

        if (changeWinAmountBackgroundPanel != null)
        {
            changeWinAmountBackgroundPanel.SetActive(isVisible);
        }
    }

    public void Update()
    {
        if (player1ScoreText == null){
            player1ScoreText = transform.Find("CyanScore").GetComponent<TMP_Text>();
        }
        if (player2ScoreText == null){
            player2ScoreText = transform.Find("PurpleScore").GetComponent<TMP_Text>();
        }
        if (winnerText == null){
            winnerText = transform.Find("WinnerText").GetComponent<TMP_Text>();
        }
        if (winAmountText == null){
            winAmountText = transform.Find("WinAmountText").GetComponent<TMP_Text>();
        }
        if (changeWinAmountButton == null){
            changeWinAmountButton = transform.Find("ChangeWinAmountButton").GetComponent<Button>();
        }
        if (changeWinAmountText == null){
            changeWinAmountText = transform.Find("ChangeWinAmountInstructions").GetComponent<TMP_Text>();
        }
        if (changeWinAmountInput == null){
            changeWinAmountInput = transform.Find("ChangeWinAmountInput").GetComponent<TMP_InputField>();
        }
        if (changeWinAmountSave == null){
            changeWinAmountSave = transform.Find("ChangeWinAmountSave").GetComponent<Button>();
        }
        if (changeWinAmountCancel == null){
            changeWinAmountCancel = transform.Find("ChangeWinAmountCancel").GetComponent<Button>();
        }
        if (hostChangingWinAmountText == null){
            hostChangingWinAmountText = transform.Find("HostChangingWinAmountText").GetComponent<TMP_Text>();
        }
        if (changeWinAmountBackgroundPanel == null){
            changeWinAmountBackgroundPanel = transform.Find("ChangeWinAmountBackgroundPanel").gameObject;
        }

        if (!isServer){
            UpdateScoreUI();
        }else{
            CheckWin();
        }
    }

    [Server]
    private void CheckWin(){
        if (player2Score >= winAmount || player1Score >= winAmount)
        {
            isWinner = true;
            winnerString = $"{(player2Score >= winAmount ? "Pink" : "Blue")} Wins!";
            StartCoroutine(WaitAndResetGame());
        }
    }

    [Server]
    private IEnumerator WaitAndResetGame()
    {
        gameManager.PauseBall();
        yield return new WaitForSeconds(5f);
        gameManager.ResetGame();
    }

    [Server] 
    public void AddPointToRed()
    {
        player2Score++;
        CheckWin();
    }

    [Server] 
    public void AddPointToBlue()
    {
        player1Score++;
        CheckWin();
    }

    [Server] 
    public void ResetPoints()
    {
        player1Score = 0;
        player2Score = 0;
        isWinner = false;
        winnerString = "";
    }

    [Server]
    public void setShowChangeWinAmount(bool val){
        showChangeWinAmountUI = val;
        if (val){
            changeWinAmountInput.text = winAmount.ToString(); 
            gameManager.PauseBall();
        }
        else{
            gameManager.PlayBall();
        }
        setChangeWinAmountUIVisibility(val);
    }

   private void OnRedScoreChanged(int oldScore, int newScore)
    {
        UpdateScoreUI();
    }

    private void OnBlueScoreChanged(int oldScore, int newScore)
    {
        UpdateScoreUI();
    }

    private void OnWinnerChanged(bool oldWinner, bool newWinner){
        UpdateScoreUI();
    }

    private void OnWinnerStringChanged(string oldWinner, string newWinner){
        UpdateScoreUI();
    }

    private void OnWinAmountChanged(int oldWinner, int newWinner){
        UpdateScoreUI();
    }   
    private void OnWinAmountUIChanged(bool oldWinner, bool newWinner){
        UpdateScoreUI();
    }
    private void UpdateScoreUI()
    {
        if (player1ScoreText != null)
        {
            player1ScoreText.text = player1Score.ToString();
        }

        if (player2ScoreText != null)
        {
            player2ScoreText.text = player2Score.ToString();
        }

        if (winnerText != null)
        {
            winnerText.text = isWinner ? winnerString : "";
            winnerText.gameObject.SetActive(isWinner);
        }

        if (winAmountText != null)
        {
            winAmountText.text = $"Win Amount: {winAmount}";
        }

        if (changeWinAmountButton != null)
        {
            changeWinAmountButton.gameObject.SetActive(isServer && isClient);
        }

        if (changeWinAmountText != null && changeWinAmountInput != null && changeWinAmountSave != null)
        {
            setChangeWinAmountUIVisibility(isServer && isClient && showChangeWinAmountUI);
        }
        else{
            if (changeWinAmountText == null){
                Debug.LogError("changeWinAmountText is null");
            }
            if (changeWinAmountInput == null){
                Debug.LogError("changeWinAmountInput is null");
            }
            if (changeWinAmountSave == null){
                Debug.LogError("changeWinAmountSave is null");
            }
        }

        if (hostChangingWinAmountText != null)
        {
            hostChangingWinAmountText.gameObject.SetActive(!isServer && showChangeWinAmountUI);
        }
        else{
            Debug.LogError("hostChangingWinAmountText is null");
        }

        if (resetButton != null)
        {
            resetButton.gameObject.SetActive(isServer);
        }
        else{
            Debug.LogError("resetButton is null");
        }
    }

    [Server]
    private void OnChangeWinAmountClick(){
        setShowChangeWinAmount(true);
    }

    [Server]
    private void OnChangeWinAmountSaveClick(){
        if (string.IsNullOrEmpty(changeWinAmountInput.text))
        {
            Debug.LogError("Win amount input is empty.");
            return;
        }
        if (!int.TryParse(changeWinAmountInput.text, out int newWinAmount) || newWinAmount <= 0)
        {
            Debug.LogError("Invalid win amount input. Must be a positive integer.");
            return;
        }
        winAmount = int.Parse(changeWinAmountInput.text);
        setShowChangeWinAmount(false);
    }

    [Server]
    private void OnChangeWinAmountCancelClick(){
        setShowChangeWinAmount(false);
    }

    [Server]
    private void OnResetButtonClick(){
        // ResetPoints();
        gameManager.ResetGame();
    }
}
