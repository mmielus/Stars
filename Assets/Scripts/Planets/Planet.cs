﻿using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System.Text;
using System.IO;
using Newtonsoft.Json;

public class Planet : MonoBehaviour
{
    [System.Serializable]
    public struct PlanetCharacteristics
    {
        public int temperature;
        public int radiation;
        public int oxygen;
    }

    [System.Serializable]
    public struct PlanetResources
    {
        public int minerals;
        public int energy;
        public int population;
    }

    public PlanetCharacteristics characteristics;
    public PlanetResources resources;
    public bool Colonized { get; set; }
    public Player Owner { get; set; }

    private MyUIHoverListener uiListener;
    HexGrid grid;
    public HexCoordinates Coordinates { get; set; }

    // Use this for initialization
    void Start()
    {
        Colonized = false;
        Owner = null;

        grid = (GameObject.Find("HexGrid").GetComponent("HexGrid") as HexGrid);

        uiListener = GameObject.Find("WiPCanvas").GetComponent<MyUIHoverListener>();

        UpdateCoordinates();

        characteristics.temperature = Random.Range(1, 100);

        Debug.Log("Start planet" + name + ": " + this.ToJson());
    }

    string ToJson()
    {
        StringBuilder sb = new StringBuilder();
        StringWriter sw = new StringWriter(sb);
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            writer.Formatting = Formatting.Indented;
            writer.WriteStartObject();

            writer.WritePropertyName("planetMain");
            writer.WriteRawValue(JsonUtility.ToJson(this));
            
            writer.WritePropertyName("radius");
            writer.WriteValue(this.GetComponent<SphereCollider>().radius);

            writer.WritePropertyName("texture");
            writer.WriteValue(this.GetComponent<SphereCollider>().material);

            writer.WritePropertyName("position");
            writer.WriteStartArray();
            writer.WriteRawValue(this.transform.position.ToString().Substring(1, this.transform.position.ToString().Length-2));
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
        return sb.ToString();
    }

    void fromJson(string json)
    {
        //JsonTextReader reader = new JsonTextReader(new StringReader(json));
        //while (reader.Read())
        //{
        //    if (reader.Value != null)
        //    {
        //        switch reader.
        //    }
        //    else
        //    {
        //        Console.WriteLine("Token: {0}", reader.TokenType);
        //    }
        //}
    }

    void UpdateCoordinates()
    {
        Coordinates = HexCoordinates.FromPosition(gameObject.transform.position);
        if (grid.FromCoordinates(Coordinates) != null) transform.position = grid.FromCoordinates(Coordinates).transform.localPosition; //Snap object to hex
        if (grid.FromCoordinates(Coordinates) != null) grid.FromCoordinates(Coordinates).AssignObject(this.gameObject);
        //Debug.Log(grid.FromCoordinates(Coordinates).transform.localPosition.ToString() + '\n' + Coordinates.ToString());
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnMouseUpAsButton()
    {
        if (!uiListener.isUIOverride) EventManager.selectionManager.SelectedObject = this.gameObject;
    }

    /**
     * Simple method to colonize planet.Sets the planet's owner specified in the method argument. 
     */
    public void ColonizePlanet(Player newOwner)
    {
        Colonized = true;
        Owner = newOwner;
        //   Destroy(gameObject);
    }
}