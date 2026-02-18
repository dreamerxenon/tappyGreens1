using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance;

    [SerializeField] private List<CharacterData> allCharacters;

    private CharacterData selectedCharacter;

    private const string SelectedKey = "SelectedCharacter";
    private const string UnlockKeyPrefix = "Unlocked_";
    public TextMeshProUGUI[] allButtonTexts;
    public GameObject CharacterChoosePanel,continueBtn;
    

    private void Awake()
    {
        // Make singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
       if(CharacterChoosePanel != null)
        LoadSelectedCharacter();
    }


    private void Update()
    {
        
    }

   

    public bool IsUnlocked(CharacterData character)
    {
        if (character.isFree)
            return true;

        return PlayerPrefs.GetInt(UnlockKeyPrefix + character.characterID, 0) == 1;
    }

    public bool TryUnlock(CharacterData character, int playerCoins)
    {
        if (IsUnlocked(character))
            return true;

        if (playerCoins >= character.unlockCost)
        {
            PlayerPrefs.SetInt(UnlockKeyPrefix + character.characterID, 1);
            PlayerPrefs.Save();
            return true;
        }

        return false;
    }


    public void SelectCharacter(CharacterData character)
    {
        if (!IsUnlocked(character))
            return;

        selectedCharacter = character;
        PlayerPrefs.SetString(SelectedKey, character.characterID);
        PlayerPrefs.Save();
    }

    private void LoadSelectedCharacter()
    {
        string savedID = PlayerPrefs.GetString(SelectedKey, "");

        if (string.IsNullOrEmpty(savedID))
        {
            CharacterChoosePanel.gameObject.SetActive(true);
            continueBtn.SetActive(false);
            return;
        }

        selectedCharacter = allCharacters.Find(c => c.characterID == savedID);

        if (selectedCharacter == null)
            selectedCharacter = allCharacters[0];

       
    }

    public CharacterData GetSelectedCharacter()
    {
        return selectedCharacter;
    }

  

    public float GetPowerMultiplier()
    {
        return 1f + selectedCharacter.shotPowerBonus;
    }

    public float GetAccuracyAssist()
    {
        if (selectedCharacter == null) return 0f;
        return selectedCharacter.accuracyAssist; // 0 to 0.3 maybe
    }


    public int GetBufferStrokes()
    {
        return selectedCharacter.bufferStrokes;
    }

    public float GetTimerMultiplier()
    {
        return selectedCharacter.timerMultiplier <= 0
            ? 1f
            : selectedCharacter.timerMultiplier;
    }

    public void MakeAllSelectedButtonTextsUnSelected()
    {
        foreach(TextMeshProUGUI tmp in allButtonTexts)
        {
            if (tmp.text == "SELECTED")
                tmp.text = "SELECT"; 
        }
    }
   
}
