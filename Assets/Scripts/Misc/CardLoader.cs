using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CardLoader : MonoBehaviour
{
    public static CardLoader Instance;
    public string jsonPath = "Assets/Game/Data/cards.json";

    public List<Card> Cards = new List<Card>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void Load()
    {
        if (!File.Exists(jsonPath))
        {
            Debug.Log($"cards.json not found at: {jsonPath}");
            return;
        }

        var text = File.ReadAllText(jsonPath);
        var wrapper = JsonUtility.FromJson<CardListWrapper>(text);

        Cards.Clear();

        if (wrapper?.cards == null)
        {
            Debug.Log("No cards found in JSON.");
            return;
        }

        foreach (var dto in wrapper.cards)
        {
            Cards.Add(new Card(dto));
        }

        Debug.Log($"Loaded {Cards.Count} cards from {jsonPath}");
    }

    public Card CardById(int id)
    {
        return Cards.FirstOrDefault(c => c.Id == id);
    }
}

[Serializable]
public class CardAbility
{
    public string type;
    public int value;
}

[Serializable]
public class CardDefinition
{
    public int id;
    public string name;
    public int cost;
    public int power;
    public CardAbility ability;
}

[Serializable]
public class CardListWrapper
{
    public CardDefinition[] cards;
}

[Serializable]
public class Card : ICard
{
    public int Id { get; }
    public string Name { get; }
    public int Cost { get; }
    public int Power { get; }
    public Abilities Ability { get; }
    public int AbilityValue { get; }

    public Card(CardDefinition cd)
    {
        Id = cd.id;
        Name = cd.name;
        Cost = cd.cost;
        Power = cd.power;
        Ability = CheckAbility(cd.ability?.type);
        AbilityValue = cd.ability?.value ?? 0;
    }

    private Abilities CheckAbility(string s)
    {
        if (string.IsNullOrEmpty(s)) return Abilities.Unknown;
        try
        {
            return (Abilities)Enum.Parse(typeof(Abilities), s);
        }
        catch
        {
            return Abilities.Unknown;
        }
    }
}