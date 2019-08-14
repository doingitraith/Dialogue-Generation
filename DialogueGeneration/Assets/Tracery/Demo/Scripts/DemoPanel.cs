using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityTracery;
using Microsoft.Scripting.Hosting;

[ExecuteInEditMode]
public class DemoPanel : MonoBehaviour
{
	public TextAsset GrammarFile;
	public InputField GrammarInput;
	public InputField TokenInput;
	public Text TextOutput;

	public TraceryGrammar Grammar;

	private void Start()
	{
		Debug.Log("GrammarFile text: " + GrammarFile.text);
		GrammarInput.text = GrammarFile.text;
		UpdateGrammar();
		
		/* Iron Python
		ScriptEngine engine = Python.CreateEngine();

		ICollection<string> paths = engine.GetSearchPaths();
		// Python StdLib for random, re, os, json, pickle, argparse
		paths.Add(System.IO.Directory.GetCurrentDirectory()+@"\Packages\IronPython.StdLib.2.7.9\content\Lib");
		// marisa_trie
		paths.Add(Application.dataPath+@"\Scripts\Python\imports\");
		engine.SetSearchPaths(paths);
		
		ScriptSource source = engine.CreateScriptSourceFromFile(Application.dataPath+@"\Scripts\Python\productionist.py");
		ScriptScope scope = engine.CreateScope();
		ObjectOperations op = engine.Operations;
		
		source.Execute(scope);
		dynamic Productionist = scope.GetVariable("Productionist");
		dynamic prod = Productionist("introduction", Application.dataPath+@"\Scripts\Python\");
		//TODO: create ContentRequest (list of must_have tags, list of must_not tags, scoring metric)

		var result = prod.fulfill_content_request();
		Debug.Log(Convert.ToString(result));
		*/
		
		
		
	}

	public void UpdateGrammar()
	{
		GrammarInput.text = GrammarFile.text;
		Grammar = new TraceryGrammar(GrammarInput.text);
	}

	public void GenerateOutput()
	{
		var output = Grammar.Parse(TokenInput.text);
		TextOutput.text = output;
	}
}
