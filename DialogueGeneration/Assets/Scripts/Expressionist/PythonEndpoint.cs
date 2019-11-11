using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Expressionist
{
    /// <summary>
    /// Struct for the NLTK sentiment
    /// </summary>
    public struct Sentiment
    {
        public string utterance;
        public float compound;
        public float pos;
        public float neg;
        public float neu;

        /// <summary>
        /// Return a string representation of the sentimen
        /// </summary>
        /// <returns>string representing the sentiment</returns>
        public override string ToString()
        {
            return
                $"Positive: {pos * 100.0f}%\nNeutral: {neu * 100.0f}%\nNegative: {neg * 100.0f}%\nOverall: {(compound >= .05 ? "Positive" : compound <= -.05 ? "Negative" : "Neutral")}";
        }

        public bool Equals(Sentiment other)
        {
            return this.utterance.Equals(other.utterance);
        }
    }

    // Delegate to handle events for text generation and sentiment analysis
    public delegate void DialogueAction();

    /// <summary>
    /// Class to handle the Unity<>Python communication
    /// </summary>
    public class PythonEndpoint : MonoBehaviour
    {
        /// <summary>
        /// The Python process
        /// </summary>
        private Process _pythonProcess;
        /// <summary>
        /// StringBuilder to assemble the Python commands
        /// </summary>
        private readonly StringBuilder _inputDataString = new StringBuilder();

        /// <summary>
        /// Helper bool to ensure that only one PythonEndpoint is created during runtime
        /// </summary>
        private static bool _isCreated;

        /// <summary>
        /// Event to handle text generation
        /// </summary>
        public event DialogueAction OnTextGenerated;
        /// <summary>
        /// Event to handle sentiment analysis
        /// </summary>
        public event DialogueAction OnSentimentProcessed;
    
        /// <summary>
        /// The current generated string
        /// </summary>
        public string currentGeneratedString = "";
        /// <summary>
        /// The current calculated sentiment
        /// </summary>
        public Sentiment currentSentiment;

        // Start is called before the first frame update
        void Awake()
        {
            // Ensure that only one PythonEndpoint exists at a given time
            if (!_isCreated)
            {
                _isCreated = true;
                DontDestroyOnLoad(this.gameObject);
                
                //Start the Python process
                StartPythonProcess();
                PythonSetupCommands();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Creates a new process and starts the Python console
        /// </summary>
        private void StartPythonProcess()
        {
            // Init the process
            _pythonProcess = new Process
            {
                StartInfo =
                {
                    FileName = System.IO.Directory.GetCurrentDirectory() + @"\Python\Python36\python.exe",
                    Arguments = "-i",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            // Redirect input and output
            _pythonProcess.OutputDataReceived += PythonOutputReceived;
            _pythonProcess.ErrorDataReceived += PythonErrorReceived;
            // Start the process
            _pythonProcess.Start();
            _pythonProcess.BeginOutputReadLine();
            _pythonProcess.BeginErrorReadLine();
        }

        /// <summary>
        /// Receives error data from the Python console
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PythonErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
        
            //Log error messages
            if (!e.Data.StartsWith(">>>"))
                Debug.LogError(e.Data);
        }

        /// <summary>
        /// Receives data from the Python console
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PythonOutputReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
        
            // intercept Expressionist text generation
            if (e.Data.StartsWith("RESULT: "))
            {
                currentGeneratedString = e.Data.Substring(8);
                OnTextGenerated?.Invoke();
            }

            // intercept NLTK sentiment analysis
            if (e.Data.StartsWith("[SENT]"))
            {
                ParseSentiment(e.Data.Substring(6));
                OnSentimentProcessed?.Invoke();
            }
            //Debug.Log(e.Data);
        }

        /// <summary>
        /// Send a command to the Python console
        /// </summary>
        /// <param name="input">the command to input</param>
        private void EnterPythonCode(string input)
        {
            _pythonProcess.StandardInput.Write(input);
            _pythonProcess.StandardInput.Flush();
        }

        /// <summary>
        /// Set up the Python console for the start
        /// </summary>
        private void PythonSetupCommands()
        {
            // Portable python setup
            _inputDataString.AppendLine("import sys");
            _inputDataString.AppendLine("import os");
            _inputDataString.AppendLine("sys.path.append(os.getcwd()+\"\\Python\\Python36\\Lib\")");
            _inputDataString.AppendLine("sys.path.append(os.getcwd()+\"\\Python\\Python36\\Lib\\site-packages\")");
        
            // Expressionist Setup
            _inputDataString.AppendLine("from productionist import Productionist, ContentRequest");
        
            // NLTK Setup
            _inputDataString.AppendLine("import nltk");
            _inputDataString.AppendLine("from nltk.probability import FreqDist");
            _inputDataString.AppendLine("from nltk.corpus import stopwords");
            _inputDataString.AppendLine("from nltk.stem import PorterStemmer");
            _inputDataString.AppendLine("from nltk.tokenize import sent_tokenize, word_tokenize");
            _inputDataString.AppendLine("from nltk.stem.wordnet import WordNetLemmatizer");
            _inputDataString.AppendLine("from nltk.sentiment.vader import SentimentIntensityAnalyzer");
        
            // Enter all commands to the console
            EnterPythonCode(_inputDataString.ToString());
            _inputDataString.Clear();
        }

        /// <summary>
        /// Executes the NLTK sentiment analysis for a given text
        /// </summary>
        /// <param name="text">the text to analyse</param>
        public void ExecuteSentimentAnalysis(string text)
        {
            _inputDataString.AppendLine("text = \"\"\""+text+"\"\"\"");
            _inputDataString.AppendLine("sid = SentimentIntensityAnalyzer()");
            _inputDataString.AppendLine("ss = sid.polarity_scores(text)");
            _inputDataString.AppendLine("print('[SENT]{0};{1};{2};{3};{4}'.format(text, ss['compound'], ss['neg'], ss['neu'], ss['pos']))");
            _inputDataString.AppendLine("print()");
            _inputDataString.AppendLine("");
        
            // Enter the command
            EnterPythonCode(_inputDataString.ToString());
            _inputDataString.Clear();
        }

        /// <summary>
        /// Execute an Expressionist request to generate new text
        /// </summary>
        /// <param name="grammarName">the file name of the grammar file</param>
        /// <param name="mustHaveTags">a list of required tags</param>
        /// <param name="mustNotHaveTags">a list of prohibited tags</param>
        /// <param name="scoringMetric">a list of scoring tuples</param>
        /// <param name="state">the current state</param>
        public void ExpressionistRequestCode(string grammarName, List<string> mustHaveTags = null,
            List<string> mustNotHaveTags = null,
            List<Tuple<string, int>> scoringMetric = null, List<Tuple<string, string>> state = null)
        {
            // form a JSON string from the must have tags
            string mustTags = "{";
            if (mustHaveTags != null)
            {
                for (int i = 0; i < mustHaveTags.Count; i++)
                {
                    mustTags += "\"" + mustHaveTags[i] + "\"";
                    if (i < mustHaveTags.Count - 1)
                        mustTags += ",";
                }
            }
            mustTags += "}";
        
            // form a JSON string from the must not have tags
            string mustNotTags = "{";
            if (mustNotHaveTags != null)
            {
                for (int i = 0; i < mustNotHaveTags.Count; i++)
                {
                    mustNotTags += "\"" + mustNotHaveTags[i] + "\"";
                    if (i < mustNotHaveTags.Count - 1)
                        mustNotTags += ",";
                }
            }
            mustNotTags += "}";

            // form a JSON array from the scoring metrics
            string scoreMetric = "[";
            if (scoringMetric != null)
            {
                for (int i = 0; i < scoringMetric.Count; i++)
                {
                    Tuple<string, int> t = scoringMetric[i];
                    scoreMetric += "(\"" + t.Item1 + "\"," + t.Item2 + ")";                
                
                    if (i < scoringMetric.Count - 1)
                        scoreMetric += ",";
                }
            }
            scoreMetric += "]";

            // form a JSON string from the state
            string stateStr = "{";
            if (state != null)
            {
                for (int i = 0; i < state.Count; i++)
                {
                    Tuple<string, string> t = state[i];
                    stateStr += "\"" + t.Item1 + "\": \"" + t.Item2 + "\"";
                
                    if (i < state.Count - 1)
                        stateStr += ",";
                }
            }
            stateStr += "}";
        
            // build the request as a Python command
            string s = "must_have_tags={0}";
            _inputDataString.AppendLine(string.Format(s, mustTags));
            s = "must_not_have_tags={0}";
            _inputDataString.AppendLine(string.Format(s, mustNotTags));
            s = "scoring_metric={0}";
            _inputDataString.AppendLine(string.Format(s, scoreMetric));
            s="state={0}";
            _inputDataString.AppendLine(string.Format(s, stateStr));
            _inputDataString.AppendLine(
                "request = ContentRequest(required_tags=must_have_tags, prohibited_tags=must_not_have_tags, scoring_metric=scoring_metric, state=state, merge_state=True)");
            s = "content_bundle = \"{0}\"";
            _inputDataString.AppendLine(string.Format(s,grammarName));
            _inputDataString.AppendLine("dir = \"_ExpressionistExports\"");
            _inputDataString.AppendLine("prod = Productionist(content_bundle_name=content_bundle, content_bundle_directory=dir, probabilistic_mode=True, repetition_penalty_mode=False, shuffle_candidate_sets=True, terse_mode=False, verbosity=1, seed=None)");
            _inputDataString.AppendLine("result = prod.fulfill_content_request(request)");
            _inputDataString.AppendLine("print(\"RESULT: \"+str(result))");
        
            // enter the command to the Python console
            EnterPythonCode(_inputDataString.ToString());
            _inputDataString.Clear();
        }

        /// <summary>
        /// Parses a received sentiment analysis to a Sentiment struct
        /// </summary>
        /// <param name="s"></param>
        private void ParseSentiment(string s)
        {
            if (s.StartsWith(currentGeneratedString))
            {
                // split the string
                string[] parts = s.Split(';');

                // create a Sentiment struct from the parts
                Sentiment sentiment = new Sentiment
                {
                    utterance = parts[0],
                    compound = float.Parse(parts[1]),
                    neg = float.Parse(parts[2]),
                    neu = float.Parse(parts[3]),
                    pos = float.Parse(parts[4])
                };
                currentSentiment = sentiment;
            }
        }

        /// <summary>
        /// Close the Python process
        /// </summary>
        private void ExitPython()
        {
            if(!_pythonProcess.HasExited)
                _pythonProcess.Kill();
        }

        // Close the Python process before quitting the application
        private void OnApplicationQuit()
        {
            ExitPython();
        }
    }
}