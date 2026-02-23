using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : MonoBehaviour
{
    public CharacterData character;
    public TextMeshProUGUI buttonText;
    public GameObject buttonMoneyImg;

    private void Start()
    {
       
        CheckIfCharacterisSelected();
        CheckIfCharacterIsUnlocked();
        
    }

    private void CheckIfCharacterisSelected()
    {
       
        if (CharacterManager.Instance.GetSelectedCharacter() == character)
        {
           
            buttonText.text = "SELECTED";
            buttonText.alignment = TextAlignmentOptions.Center;
            if (buttonMoneyImg != null)
                buttonMoneyImg.SetActive(false);
        }
    }

    private void CheckIfCharacterIsUnlocked()
    {
        if (CharacterManager.Instance.IsUnlocked(character) && CharacterManager.Instance.GetSelectedCharacter() != character)
        {
            buttonText.text = "SELECT";
            buttonText.alignment = TextAlignmentOptions.Center;
            if (buttonMoneyImg != null)
                buttonMoneyImg.SetActive(false);

        }
    }

    public void OnClick()
    {
        if (CharacterManager.Instance.IsUnlocked(character))
        {
            CharacterManager.Instance.SelectCharacter(character);
            CharacterManager.Instance.MakeAllSelectedButtonTextsUnSelected();
            buttonText.text = "SELECTED";
            buttonText.alignment = TextAlignmentOptions.Center;
            if (buttonMoneyImg != null)
                buttonMoneyImg.SetActive(false);

        }
        else
        {
            float coins = CurrencyManager.Instance.GetCoins();

           bool unlocked =
               CharacterManager.Instance.TryUnlock(character, coins);

            if (unlocked)
            {
                CurrencyManager.Instance.SpendCoins(character.unlockCost);
                buttonText.text = "SELECT";
                buttonText.alignment = TextAlignmentOptions.Center;
                if (buttonMoneyImg != null)
                    buttonMoneyImg.SetActive(false);
            }
        }

        // refresh visuals here
    }
}
