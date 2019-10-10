using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Expressionist;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class DemoPanel : MonoBehaviour
{
	public Text TextOutput;
	public PythonEndpoint Expressionist;
	public Dropdown grammarSelection;


	private string generatedText;
	private string sentiments;
	private string selectedGrammar;
	private bool isUpdated;
	private bool isGenerated;

	private void Start()
	{
		Expressionist.OnTextGenerated += UpdateGeneratedText;
		Expressionist.OnSentimentProcessed += UpdateSentiments;

		generatedText = "";
		sentiments = "";
		selectedGrammar = "introduction";
		isUpdated = false;
		isGenerated = false;
		
		grammarSelection.options.Clear();
		
		DirectoryInfo dir = new DirectoryInfo(System.IO.Directory.GetCurrentDirectory()+@"\_ExpressionistExports");
		FileInfo[] infos = dir.GetFiles("*.grammar");
		foreach (FileInfo info in infos)
		{
			grammarSelection.options.Add(new Dropdown.OptionData(Path.GetFileNameWithoutExtension(info.ToString())));
		}
	}

	private void Update()
	{
		if (isUpdated)
		{
			TextOutput.text = generatedText + sentiments;
			isUpdated = false;
		}
	}

	public void SetSelectedGrammar(int idx)
	{
		selectedGrammar = grammarSelection.options.ElementAt(idx).text;
	}

	public void GenerateOutput()
	{
		Expressionist.ExpressionistRequestCode(selectedGrammar, 
			/*new List<string>(){"male"},
			new List<string>(){"female"},
			new List<Tuple<string,int>>(){new Tuple<string, int>("male",1)},*/
			null,null,null,
			new List<Tuple<string, string>>(){new Tuple<string, string>("dayTime", "day")}
			);
		while (!isGenerated)
			generatedText = Expressionist.currentGeneratedString;
		
		isGenerated = false;
		
		Expressionist.ExecuteSentimentAnalysis(generatedText);
	}

	private void UpdateGeneratedText()
	{
		generatedText = Expressionist.currentGeneratedString;
		isGenerated = true;
		isUpdated = true;
	}

	private void UpdateSentiments()
	{
		sentiments = "";
		sentiments += "\n" + Expressionist.currentSentiment.ToString();

		isUpdated = true;
	}
}
